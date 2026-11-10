using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class BattleDamageRankingTests
    {
        static ManualBattleSettings Settings(string card="CARD_M03")=>new ManualBattleSettings(card,"M03_TEST",1000,1,200,1,.1f,10000,.001f,1,1,1,1);
        static EngagementSettings Combat(float attack=20,float range=5,float flight=.1f,MonsterEffectSettings effects=default)
            =>new EngagementSettings(unitAttack:attack,unitArmor:0,unitRange:range,unitInterval:1,defenderHealth:10000,defenderAttack:1,
                defenderArmor:0,defenderRange:.001f,blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,
                skillAttack:0,skillInterval:0,skillRange:0,projectileTime:flight,hasSkill:false,effects:effects);
        static DefenderSettings Guard(string id="guard",float hp=10000,float armor=0,Vector3? position=null)
            =>new DefenderSettings(id,id,null,position??Vector3.right,hp,1,armor,.001f,100,canAttack:false);
        static ManualBattle Battle(EngagementSettings? combat=null,DefenderSettings[] roster=null,string card="CARD_M03")
            =>new ManualBattle(Settings(card),new[]{Vector3.zero,Vector3.forward*1000},Vector3.right,combat??Combat(),roster:roster??new[]{Guard()});
        static DeployedUnitState Deploy(ManualBattle b,long request=1,string card=null)
        {
            if(b.Phase==BattlePhase.Ready)b.Begin();
            var receipt=b.TryDeployRole(b.RunId,request,b.Entry,card??b.Settings.CardId);
            Assert.That(receipt.Success,Is.True);return b.FindUnit(receipt.EntityId);
        }
        static object Call(ManualBattle b,string method,params object[] args)
            =>typeof(ManualBattle).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,args);
        static void Hit(ManualBattle b,DefenderState d,float attack,DeployedUnitState source=null)
            =>Call(b,"DamageDefender",d,attack,CombatDamageType.Physical,false,source,1f);
        static float Damage(ManualBattle b,string card)=>b.DamageRanking.Where(e=>e.CardId==card).Sum(e=>e.Damage);

        [Test]public void OrdinaryImpactsCreditOnlyActualHpLossAndResolvedShotsCannotRepeat()
        {
            var b=Battle(roster:new[]{Guard(hp:7,armor:3)});Deploy(b);
            Assert.That(b.DamageRanking,Is.Empty);b.Advance(.3f);
            Assert.That(b.Defender.Health,Is.Zero);Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(7));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(7));b.Advance(2);
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(7),"Overkill and subsequent reads cannot add damage.");
        }
        [Test]public void SameCardEntitiesAggregateAndDifferentCardsRankByDamageWithStableTies()
        {
            var b=Battle();b.ConfigureCardLoadout(new[]{new BasicRosterProfile(new ManualBattleSettings("CARD_M02","M02_TEST",1000,1,200,1,.1f,10000,.001f,1,1,1,1),Combat(attack:10))});
            var primary=Deploy(b);var other=Deploy(b,2,"CARD_M02");b.Advance(.11f);Deploy(b,3);b.Advance(.3f);
            Assert.That(b.DamageRanking.Select(e=>e.CardId),Is.EqualTo(new[]{"CARD_M03","CARD_M02"}));
            Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(40));Assert.That(Damage(b,"CARD_M02"),Is.EqualTo(10));
            Hit(b,b.Defender,30,other);
            Assert.That(b.DamageRanking.Select(e=>e.CardId),Is.EqualTo(new[]{"CARD_M02","CARD_M03"}),"Ties use a stable card ID order.");
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(b.Defender.MaxHealth-b.Defender.Health));
            Assert.That(primary,Is.Not.Null);
        }
        [Test]public void PauseKeepsPendingAttacksAtZeroThenResumeCreditsTheRealImpact()
        {
            var b=Battle(Combat(flight:.5f));Deploy(b);b.Advance(.05f);b.SetPaused(true);double clock=b.Clock;
            b.Advance(20);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.TotalPlayerDamage,Is.Zero);
            b.SetPaused(false);b.Advance(.6f);Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(20));
        }
        [Test]public void DefeatedSourcesStayRankedHealingDoesNotSubtractAndRestartClearsEverything()
        {
            var b=Battle();var u=Deploy(b);b.Advance(.3f);float damage=b.TotalPlayerDamage;
            Call(b,"HealDefender",b.Defender,b.Defender,10000f);Assert.That(b.TotalPlayerDamage,Is.EqualTo(damage));
            Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.True);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
            Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(damage));
            Assert.That(()=>((IList<BattleDamageEntry>)b.DamageRanking).Clear(),Throws.TypeOf<NotSupportedException>());
            b.Restart();Assert.That(b.DamageRanking,Is.Empty);Assert.That(b.TotalPlayerDamage,Is.Zero);
        }
        [Test]public void ViviSplashCreditsEveryActuallyHitTargetOnce()
        {
            const string card="CARD_WISADEL_APRIL";
            var combat=new EngagementSettings(unitAttack:14,unitRange:5,unitInterval:1.7f,defenderRange:.001f,
                initialSP:12,requiredSP:12,spPerSecond:1,skillAmmo:6,skillAttack:105,skillInterval:3.2f,skillRange:5,projectileTime:.22f);
            var b=Battle(combat,new[]{Guard(hp:1000,armor:3),Guard("inside",1000,20,new Vector3(3.1f,2,0)),Guard("outside",1000,0,new Vector3(3.3f,0,0))},card);
            var u=Deploy(b);Assert.That(b.TryActivateSkill(b.RunId,1,u.EntityId),Is.EqualTo(SkillFailure.None));b.Advance(.35f);
            Assert.That(Damage(b,card),Is.EqualTo(187));Assert.That(b.DamageRanking.Count,Is.EqualTo(1));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(b.Defenders.Sum(d=>d.MaxHealth-d.Health)));
        }
        [Test]public void ShacklerPeriodicSkillKeepsItsCardCreditAndClampsTheLethalTick()
        {
            var b=Battle(Combat(attack:50,effects:new MonsterEffectSettings(shackler:true)),new[]{Guard(hp:30,armor:1000)},"CARD_M38");
            Deploy(b);b.Advance(3);
            Assert.That(b.Shackles.Single().Ticks,Is.EqualTo(2));Assert.That(b.TotalPlayerDamage,Is.EqualTo(30));
            Assert.That(Damage(b,"CARD_M38"),Is.EqualTo(30));Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.Zero);
        }
        [Test]public void DelayedDeathBlastRetainsTheDeadCardAsSource()
        {
            var b=Battle(Combat(range:.1f,effects:new MonsterEffectSettings(blastAttack:70,blastRadius:2,blastDelay:.2f)));
            var u=Deploy(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);Assert.That(b.TotalPlayerDamage,Is.Zero);
            b.Advance(.3f);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(b.DeathBlasts.Single().Resolved,Is.True);
            Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(70));
        }
        [Test]public void BurnAndNeuralBurstsUseTheirTriggeringCardAndNeverCountElementAccumulation()
        {
            var b=Battle();var u=Deploy(b);
            Call(b,"ApplyBurn",b.Defender,99f,u);Call(b,"ApplyNeuralDamage",b.Defender,99f,u);
            Assert.That(b.TotalPlayerDamage,Is.Zero);
            Call(b,"ApplyBurn",b.Defender,1f,u);Call(b,"ApplyNeuralDamage",b.Defender,1f,u);
            Assert.That(b.BurnBursts,Is.EqualTo(1));Assert.That(b.NeuralBursts,Is.EqualTo(1));
            Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(220));Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.Zero);
        }
        [Test]public void ProtectorRedirectCreditsBothActualHpLossesButNeverDebtOrSelfDrain()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.HoshigumaInferno,0,100,1,32,effectRadius:5);
            var b=Battle(roster:new[]{Guard(hp:10),new DefenderSettings("protector","protector","D25",Vector3.right*2,100,1,0,.001f,100,skill:skill,canAttack:false)});
            var u=Deploy(b);var protector=b.FindDefender("protector");protector.GetType().GetProperty("ProtectionUntil").SetValue(protector,10d);
            Hit(b,b.Defender,50,u);Assert.That(b.Defender.Health,Is.EqualTo(1));Assert.That(protector.Health,Is.EqualTo(59));Assert.That(b.TotalPlayerDamage,Is.EqualTo(50));
            Hit(b,b.Defender,10000,u);Assert.That(protector.Debt,Is.GreaterThan(0));Assert.That(b.TotalPlayerDamage,Is.EqualTo(109),"Only 109 of the initial 110 real HP were lost; the tank retains 1 HP.");
            Call(b,"ApplyDefenderLoss",b.Defender,.5f);Assert.That(b.TotalPlayerDamage,Is.EqualTo(109),"Self-drain or a diagnostic loss has no player credit.");
        }
        [Test]public void SharedEffectsHaveAnExplicitSeparateRow()
        {
            var b=Battle();var u=Deploy(b);Hit(b,b.Defender,25,u);Hit(b,b.Defender,12);
            Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(25));Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.EqualTo(12));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(37));
        }
        [Test]public void NativePatriotTrueDamageRemainsAttributedDuringRebirth()
        {
            var b=Battle(Combat(attack:100,range:.1f,effects:new MonsterEffectSettings(patriot:true)),new[]{Guard(armor:9999)},"CARD_M20");
            var u=Deploy(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(.81f);
            Assert.That(u.PatriotPhase,Is.EqualTo(PatriotPhase.Reborn));Assert.That(u.ImmolationTicks,Is.EqualTo(2));
            Assert.That(Damage(b,"CARD_M20"),Is.EqualTo(u.ImmolationDamage).Within(.001));Assert.That(b.TotalPlayerDamage,Is.GreaterThan(0));
        }
        [Test]public void SilverWolfSwordFinisherKeepsTheSameCardCreditAsItsOriginalAreaHit()
        {
            var combat=new EngagementSettings(unitRange:4,unitInterval:1.9f,attackDuration:1.7f,attackWindup:.8f,
                initialSP:28,requiredSP:28,spPerSecond:1,skillAmmo:3,skillAttack:100,skillInterval:4.6f,skillRange:5);
            var b=Battle(combat,card:SilverWolfCombat.CardId);
            int seed=Enumerable.Range(0,100).First(s=>new System.Random(s).Next(3)==0);
            typeof(ManualBattle).GetField("wolfRolls",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,new System.Random(seed));
            var u=Deploy(b);Assert.That(b.TryActivateSkill(b.RunId,1,u.EntityId),Is.EqualTo(SkillFailure.None));b.Advance(4.6f);
            var shot=b.CombatShots.Single(s=>!s.FromDefender);
            Assert.That(shot.WolfBox,Is.EqualTo(WolfBoxKind.Sword));Assert.That(shot.WolfBonusTargetId,Is.Not.Null);
            Assert.That(shot.DamageDealt,Is.EqualTo(120));Assert.That(Damage(b,SilverWolfCombat.CardId),Is.EqualTo(120));
            Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.Zero);
        }
    }
}
