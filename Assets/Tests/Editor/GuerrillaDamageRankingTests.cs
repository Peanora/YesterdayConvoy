using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class GuerrillaDamageRankingTests
    {
        static readonly string[] Members={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M20","CARD_M07","CARD_M08"};
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;

        static ManualBattleSettings Unit(string id)=>new ManualBattleSettings(id,id,1000,100,20,1,.01f,10000,.001f,1,1,1,1);
        static EngagementSettings Combat(float attack,bool arts=false,float resistance=0)
            =>new EngagementSettings(unitAttack:attack,unitArmor:0,unitRange:.01f,unitInterval:1000,defenderRange:.001f,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.01f,hasSkill:false,effects:new MonsterEffectSettings(damageType:arts?CombatDamageType.Arts:CombatDamageType.Physical,resistance:resistance));
        static DefenderSettings Guard(string id="guard",float hp=10000,float armor=0,float x=10)
            =>new DefenderSettings(id,id,null,Vector3.right*x,hp,1,armor,.001f,1000,canAttack:false);
        static ManualBattle Fixture(DefenderSettings[] guards=null,bool secondaryArts=false,float resistance=0)
        {
            var primary=Combat(10,resistance:resistance);var other=Combat(30,secondaryArts);
            var b=new ManualBattle(Unit(Members[0]),new[]{Vector3.zero,Vector3.forward*1000},Vector3.right*10,primary,
                roster:guards??new[]{Guard()});
            b.ConfigureCardLoadout(Members.Skip(1).Select(id=>new BasicRosterProfile(Unit(id),other,true)));
            b.ConfigureSynergies(Members.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),extraGuerrillaMembers:new[]{"CARD_M07","CARD_M08"});
            // Nine Reunion kinds have only two formations, so tier six adds 30% attack.
            // The tier-nine order then contributes 10*1.3*3=39 and 30*1.3*3=117 per guard.
            Assert.That(b.ReunionTier,Is.EqualTo(6));Assert.That(b.GuerrillaTier,Is.EqualTo(9));return b;
        }
        static GuerrillaOrderVolley StartOrder(ManualBattle b,bool second=true,bool third=false)
        {
            b.Begin();
            Assert.That(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M03").Success,Is.True);
            if(second)Assert.That(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M07").Success,Is.True);
            if(third)Assert.That(b.TryDeployRole(b.RunId,3,b.Entry,"CARD_M08").Success,Is.True);
            // Tier nine's real initial balance commits an automatic order without ordinary hits.
            b.Advance(.05f,0);
            Assert.That(b.LatestGuerrillaOrder,Is.Not.Null);Assert.That(b.OrdersUsed,Is.EqualTo(1));
            Assert.That(b.CombatShots,Is.Empty);Assert.That(b.TotalPlayerDamage,Is.Zero);
            Assert.That(b.TotalGuerrillaDamage,Is.Zero);return b.LatestGuerrillaOrder;
        }
        static float Damage(ManualBattle b,string id)=>b.DamageRanking.Where(r=>r.CardId==id).Sum(r=>r.Damage);
        static float Pursuit(ManualBattle b,string id)=>b.DamageRanking.Where(r=>r.CardId==id).Sum(r=>r.GuerrillaDamage);
        static void AssertSubset(ManualBattle b)
        {
            Assert.That(b.DamageRanking.Sum(r=>r.Damage),Is.EqualTo(b.TotalPlayerDamage).Within(.02));
            Assert.That(b.DamageRanking.Sum(r=>r.GuerrillaDamage),Is.EqualTo(b.TotalGuerrillaDamage).Within(.02));
            foreach(var entry in b.DamageRanking)
                Assert.That(entry.GuerrillaDamage,Is.InRange(0,entry.Damage+.001f),entry.CardId);
        }

        [Test]public void AllFiveGlobalStrikesCreditTheirContributorsAndRemainASubsetOfTotalDamage()
        {
            var b=Fixture(new[]{Guard(),Guard("far",hp:20000,x:50)});var order=StartOrder(b);
            b.Advance(2.8f,2.8f);
            Assert.That(order.CompletedStrikes,Is.EqualTo(5));Assert.That(order.Resolved,Is.True);
            Assert.That(order.Hits.Count,Is.EqualTo(2));Assert.That(order.Hits.Sum(h=>h.Damage),Is.EqualTo(312).Within(.02));
            Assert.That(Pursuit(b,"CARD_M03"),Is.EqualTo(78).Within(.02));
            Assert.That(Pursuit(b,"CARD_M07"),Is.EqualTo(234).Within(.02));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(312).Within(.02));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(312).Within(.02));
            Assert.That(b.DamageRanking.Select(r=>r.CardId),Is.EqualTo(new[]{"CARD_M07","CARD_M03"}));
            Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.Zero);AssertSubset(b);
            float total=b.TotalPlayerDamage;b.Advance(4,4);
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(total));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(total));
        }

        [Test]public void ArmorAndResistanceApplyBeforeTheFiveWeightsAndOnlyActualHpLossIsCounted()
        {
            var b=Fixture(new[]{Guard(armor:1000)},secondaryArts:true,resistance:50);var order=StartOrder(b);
            b.Advance(2.8f,2.8f);
            // Physical: max(1,39-1000), once before splitting. Arts: 117*(1-.5).
            Assert.That(Pursuit(b,"CARD_M03"),Is.EqualTo(1).Within(.02));
            Assert.That(Pursuit(b,"CARD_M07"),Is.EqualTo(58.5f).Within(.02));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(59.5f).Within(.02));
            Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(b.Defender.MaxHealth-b.Defender.Health).Within(.02));
            Assert.That(order.Hits.Single().Damage,Is.EqualTo(b.TotalGuerrillaDamage).Within(.02));AssertSubset(b);
        }

        [Test]public void OverkillIsAssignedInTheExistingContributionOrderWithoutInventingExtraDamage()
        {
            // Six HP keeps both sources involved after the added Reunion attack bonus.
            var b=Fixture(new[]{Guard(hp:6)});var order=StartOrder(b);b.Advance(3,3);
            // First source pays 39*.15=5.85; the next can remove only the remaining .15 HP.
            Assert.That(Pursuit(b,"CARD_M03"),Is.EqualTo(5.85f).Within(.0001));
            Assert.That(Pursuit(b,"CARD_M07"),Is.EqualTo(.15f).Within(.0001));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(6));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(6));
            Assert.That(order.CompletedStrikes,Is.EqualTo(1));Assert.That(order.Cancelled,Is.True);AssertSubset(b);
        }

        [Test]public void ProtectionTransfersActualHpLossToTheOriginalContributorAndExcludesDebt()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.HoshigumaInferno,0,100,1,32,effectRadius:5);
            var protector=new DefenderSettings("protector","protector","D25",Vector3.right*11,100,1,0,.001f,1000,skill:skill,canAttack:false);
            var b=Fixture(new[]{Guard(hp:10),protector});
            typeof(DefenderState).GetProperty("ProtectionUntil").SetValue(b.FindDefender("protector"),100d);
            var order=StartOrder(b);b.Advance(2.8f,2.8f);
            Assert.That(b.Defender.Health,Is.EqualTo(1));Assert.That(b.FindDefender("protector").Debt,Is.GreaterThan(0));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(109).Within(.001));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(109).Within(.001));
            Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(b.Defenders.Sum(d=>d.MaxHealth-d.Health)).Within(.001));
            Assert.That(order.Hits.Sum(h=>h.Damage),Is.EqualTo(b.TotalGuerrillaDamage).Within(.001));
            Assert.That(Pursuit(b,ManualBattle.SharedDamageCardId),Is.Zero);AssertSubset(b);
        }

        [Test]public void ASourceThatDiesAfterTheFirstStrikeKeepsItsPastCreditAndAddsNoLaterCredit()
        {
            var b=Fixture();var order=StartOrder(b);b.Advance(.9f,.9f);
            Assert.That(order.CompletedStrikes,Is.EqualTo(1));
            var source=b.Units.Single(u=>!u.SynergySupport&&u.CardId=="CARD_M07");
            Assert.That(b.TryDamageUnit(source.EntityId,100000,CombatDamageType.Arts),Is.True);b.Advance(1.9f,1.9f);
            Assert.That(order.CompletedStrikes,Is.EqualTo(5));
            Assert.That(Pursuit(b,"CARD_M03"),Is.EqualTo(39).Within(.02));
            Assert.That(Pursuit(b,"CARD_M07"),Is.EqualTo(17.55f).Within(.02));
            Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(56.55f).Within(.02));AssertSubset(b);
        }

        [Test]public void GeneratedSupportDoesNotBecomeAHiddenGlobalContributor()
        {
            var b=Fixture();var order=StartOrder(b,third:true);
            Assert.That(b.Units.Any(u=>u.SynergySupport),Is.True);
            Assert.That(order.SourceIds.Count,Is.EqualTo(3));
            Assert.That(order.SourceIds.All(id=>!b.FindUnit(id).SynergySupport),Is.True);b.Advance(2.8f,2.8f);
            Assert.That(Pursuit(b,"CARD_M03"),Is.EqualTo(39).Within(.02));
            Assert.That(Pursuit(b,"CARD_M07"),Is.EqualTo(117).Within(.02));
            Assert.That(Pursuit(b,"CARD_M08"),Is.EqualTo(117).Within(.02));
            Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(273).Within(.02));AssertSubset(b);
        }

        [Test]public void OrdinaryDamageAfterAResolvedOrderDoesNotInheritTheGuerrillaClassification()
        {
            var b=Fixture();StartOrder(b,second:false);b.Advance(2.8f,2.8f);
            var source=b.Units.Single();
            typeof(ManualBattle).GetMethod("DamageDefender",Hidden).Invoke(b,new object[]{b.Defender,20f,CombatDamageType.Physical,false,source,1f});
            typeof(ManualBattle).GetMethod("DamageDefender",Hidden).Invoke(b,new object[]{b.Defender,11f,CombatDamageType.Physical,false,null,1f});
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(70).Within(.02));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(39).Within(.02));
            Assert.That(Damage(b,"CARD_M03"),Is.EqualTo(59).Within(.02));Assert.That(Pursuit(b,"CARD_M03"),Is.EqualTo(39).Within(.02));
            Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.EqualTo(11).Within(.02));
            Assert.That(Pursuit(b,ManualBattle.SharedDamageCardId),Is.Zero);AssertSubset(b);
            b.Restart();Assert.That(b.TotalPlayerDamage,Is.Zero);Assert.That(b.TotalGuerrillaDamage,Is.Zero);Assert.That(b.DamageRanking,Is.Empty);
        }

        [Test]public void AnUnidentifiedDamageSourceUsesTheSharedRowWithoutDuplicatingItsTotal()
        {
            var b=Fixture();b.Begin();
            typeof(ManualBattle).GetMethod("DamageGuerrillaDefenderFrom",Hidden).Invoke(b,
                new object[]{b.Defender,20f,CombatDamageType.Physical,null,.15f});
            Assert.That(Damage(b,ManualBattle.SharedDamageCardId),Is.EqualTo(3));
            Assert.That(Pursuit(b,ManualBattle.SharedDamageCardId),Is.EqualTo(3));
            Assert.That(b.TotalPlayerDamage,Is.EqualTo(3));Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(3));AssertSubset(b);
        }

        [Test]public void WaveBudgetChangesPreserveTheGuerrillaSubtotalUntilTheWholeBattleRestarts()
        {
            var b=Fixture();b.ConfigureWaves(new BattleWavePlan(new[]{new BattleWaveDefinition("R1",16,1),new BattleWaveDefinition("R1",18,1)},10));
            StartOrder(b);b.Advance(.9f,.9f);float first=b.TotalGuerrillaDamage;
            Assert.That(first,Is.GreaterThan(0));b.Advance(1.9f,1.9f);
            Assert.That(b.FinalAdvanceActive,Is.True);Assert.That(b.CurrentWaveIndex,Is.EqualTo(1));
            Assert.That(b.TotalGuerrillaDamage,Is.EqualTo(156).Within(.02));Assert.That(b.TotalGuerrillaDamage,Is.GreaterThan(first));AssertSubset(b);
            b.Restart();Assert.That(b.TotalGuerrillaDamage,Is.Zero);Assert.That(b.TotalPlayerDamage,Is.Zero);
        }
    }
}
