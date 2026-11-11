using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FrostSealTests
    {
        static EngagementSettings Combat()=>new EngagementSettings(unitAttack:1,unitRange:.1f,unitInterval:10,
            initialSP:32,requiredSP:32,spPerSecond:1,skillAmmo:0,skillAttack:72,skillInterval:3,skillRange:2.5f,
            frostSeal:true,effects:new MonsterEffectSettings(CombatDamageType.Arts,frostNova:true));
        static ManualBattleSettings Unit(string card="CARD_M14",float health=1000)=>new ManualBattleSettings(card,card+"_unit",30,1,3,3,.1f,health,1,1,1,1,1);
        static DefenderSettings Guard(string id,float x,float health=1000)=>new DefenderSettings(id,id,"D01",new Vector3(x,0,0),health,1,0,.1f,10,canAttack:false);
        static ManualBattle Battle(float health=1000)
        {
            var b=new ManualBattle(Unit(),new[]{Vector3.zero,Vector3.forward*30},Vector3.right,Combat(),
                roster:new[]{Guard("near",1,health),Guard("mid",2,health),Guard("edge",2.4f,health),Guard("far",4,health)});
            b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);return b;
        }
        [Test]public void DelayedArtsHitFreezesOnlyTwoClosestAndPauseFreezesCastResourcesAndDamage()
        {
            var b=Battle();var u=b.Units[0];Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.None));
            Assert.That(u.SkillSP,Is.Zero);Assert.That(u.SkillAmmo,Is.Zero);Assert.That(u.IsMoving,Is.False);
            b.Advance(1);Assert.That(b.Defender.Health,Is.EqualTo(1000));double clock=b.Clock;
            b.SetPaused(true);b.Advance(20);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.FrostSeals[0].Resolved,Is.False);
            Assert.That(b.CheckSkill(u.EntityId),Is.EqualTo(SkillFailure.Paused));b.SetPaused(false);b.Advance(.15f);
            Assert.That(b.FrostSeals[0].FrozenTargets,Is.EqualTo(new[]{"near","mid"}));
            Assert.That(b.Defenders.Take(3).All(d=>d.Health==928),Is.True);Assert.That(b.FindDefender("far").Health,Is.EqualTo(1000));
            Assert.That(b.FindDefender("near").FrozenUntil,Is.GreaterThan(b.Clock));Assert.That(b.FindDefender("edge").FrozenUntil,Is.LessThan(b.Clock));
            Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.None));Assert.That(b.SkillCasts,Is.EqualTo(1));
            Assert.That(b.TryActivateSkill(b.RunId,3,u.EntityId),Is.EqualTo(SkillFailure.AlreadyActive));
            b.Advance(1.8f);Assert.That(u.SkillSP,Is.Zero);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
            b.Advance(1.55f);Assert.That(u.SkillSP,Is.InRange(1.4f,1.6f));Assert.That(b.Defender.FrozenUntil,Is.LessThan(b.Clock));
            b.Restart();Assert.That(b.FrostSeals,Is.Empty);Assert.That(b.Defender.Health,Is.EqualTo(1000));Assert.That(b.Defender.FrozenUntil,Is.LessThan(0));
        }
        [Test]public void TargetsKilledByImpactDoNotInterruptTheRemainingThreeSecondAction()
        {
            var b=Battle(50);var u=b.Units[0];b.TryActivateSkill(b.RunId,2,u.EntityId);b.Advance(1.2f);
            Assert.That(b.Defenders.Take(3).All(d=>!d.Alive),Is.True);Assert.That(b.IsSkillActionPlaying(u),Is.True);
            b.Advance(1.7f);Assert.That(u.Motion.DistanceTravelled,Is.Zero);Assert.That(u.SkillSP,Is.Zero);
            b.Advance(.3f);Assert.That(u.Motion.DistanceTravelled,Is.GreaterThan(0));Assert.That(b.IsSkillActionPlaying(u),Is.False);
        }
        [Test]public void MixedRosterRevivesOnlyFrostNovaAtHerOwnMaximumAndCancelsUnfiredCast()
        {
            var basic=new EngagementSettings(hasSkill:false,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0);
            var b=new ManualBattle(Unit("CARD_M08",80),new[]{Vector3.zero,Vector3.forward*30},Vector3.right,basic,roster:new[]{Guard("guard",1)});
            b.ConfigureCardLoadout(new[]{new BasicRosterProfile(Unit(health:360),Combat())});b.Begin();
            var frost=b.FindUnit(b.TryDeployRole(b.RunId,1,b.Entry,"CARD_M14").EntityId);
            var archer=b.FindUnit(b.TryDeployRole(b.RunId,2,b.Entry,"CARD_M08").EntityId);
            b.TryActivateSkill(b.RunId,3,frost.EntityId);b.TryDamageUnit(archer.EntityId,10000,CombatDamageType.Arts);b.TryDamageUnit(frost.EntityId,10000,CombatDamageType.Arts);
            Assert.That(archer.Active,Is.False);Assert.That(archer.ReviveUsed,Is.False);Assert.That(frost.IsReviving,Is.True);
            b.Advance(1.2f);Assert.That(b.FrostSeals[0].Cancelled,Is.True);Assert.That(b.FrostSeals[0].FrozenTargets,Is.Empty);
            Assert.That(b.IsSkillReady(frost.EntityId),Is.False);b.Advance(4);Assert.That(frost.IsReviving,Is.False);Assert.That(frost.Health,Is.EqualTo(360));
            Assert.That(b.UnitInvulnerable(frost),Is.True);Assert.That(b.TryDamageUnit(frost.EntityId,10000,CombatDamageType.Arts),Is.False,"Winter Scar now owns twenty seconds of source post-revival protection.");Assert.That(frost.Health,Is.EqualTo(360));
            b.Advance((float)(b.BossCoreState(frost).ProtectedUntil-b.Clock)+.05f);Assert.That(b.UnitInvulnerable(frost),Is.False);
            Assert.That(b.TryDamageUnit(frost.EntityId,10000,CombatDamageType.Arts),Is.True);Assert.That(frost.Active,Is.False);Assert.That(frost.ReviveUsed,Is.True);
        }
        [Test]public void SkillConfigurationRejectsHybridAmmoOrNonFrostNovaCast()
        {
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(frostSeal:true));
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(frostSeal:true,skillInterval:3,effects:new MonsterEffectSettings(frostNova:true)));
            Assert.That(Combat().FrostSeal,Is.True);
        }
    }
}
