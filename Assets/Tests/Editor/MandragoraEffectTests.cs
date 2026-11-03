using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class MandragoraEffectTests
    {
        static ManualBattle Create(int blocks=0,bool air=true,float guardRange=.001f,float guardHealth=10000,bool extra=false)
        {
            var s=new ManualBattleSettings("CARD_M32","M32_TEST",20,1,3,1,.1f,1000,.001f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:100,unitArmor:0,unitRange:1.5f,unitInterval:1.8f,
                defenderHealth:guardHealth,defenderAttack:1,defenderArmor:0,defenderRange:guardRange,blockCount:blocks,blockRadius:1.1f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.05f,hasSkill:false,effects:new MonsterEffectSettings(CombatDamageType.Arts,mandragora:true),defenderCanHitAir:air);
            return new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward,c,extra?new[]{Vector3.right*.5f}:null);
        }
        static DeployedUnitState Start(ManualBattle b)
        {b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        static void Pillar(ManualBattle b,float damage=100)
        {b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.left,power:damage)});}
        [TestCase(CombatDamageType.Physical)] [TestCase(CombatDamageType.Arts)]public void ShieldReducesBothOrdinaryDamageTypesBySixtyPercent(CombatDamageType type)
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,100,type);
            Assert.That(u.StoneShield,Is.True);Assert.That(u.Health,Is.EqualTo(960).Within(.001));
        }
        [Test]public void FirstFatalHitRebirthsOnceWithFiveSecondTransitionAndThreeSecondProtection()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);
            Assert.That(u.MandraPhase,Is.EqualTo(MandragoraPhase.Reborn));Assert.That(u.Active,Is.True);
            Assert.That(b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Physical),Is.False);
            b.Advance(4.9f);Assert.That(u.MandraPhase,Is.EqualTo(MandragoraPhase.Reborn));
            b.Advance(.2f);Assert.That(u.MandraPhase,Is.EqualTo(MandragoraPhase.Second));Assert.That(u.Health,Is.EqualTo(1000));
            Assert.That(b.UnitAirborne(u),Is.True);Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.False);
            b.Advance(3.1f);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
        }
        [Test]public void PillarDamageBypassesShieldAndRestoresItAfterTwentyFiveSeconds()
        {
            var b=Create();Pillar(b);var u=Start(b);Assert.That(b.TryTopplePillar(0,Vector3.left*2),Is.True);
            Assert.That(u.Health,Is.EqualTo(900));Assert.That(u.StoneShield,Is.False);Assert.That(u.StoneShieldBreaks,Is.EqualTo(1));
            b.Advance(24.9f);Assert.That(u.StoneShield,Is.False);b.Advance(.2f);Assert.That(u.StoneShield,Is.True);
            Assert.That(u.StoneShieldRestores,Is.EqualTo(1));
        }
        [Test]public void ShieldRestoresOnlyAfterLossExceedsFortyPercentOfMaximumHealth()
        {
            var b=Create();Pillar(b);var u=Start(b);b.TryTopplePillar(0,Vector3.left*2);
            b.TryDamageUnit(u.EntityId,300,CombatDamageType.Arts);b.Advance(.1f);Assert.That(u.StoneShield,Is.False);
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(.1f);Assert.That(u.StoneShield,Is.True);
        }
        [Test]public void FullGazeHasSevenTicksAndNoUnblockedGroundOrdinaryAttacks()
        {
            var b=Create();var u=Start(b);b.Advance(12.3f);
            Assert.That(u.Gaze,Is.Not.Null);Assert.That(u.Gaze.Ended,Is.True);Assert.That(u.Gaze.Ticks,Is.EqualTo(7));
            Assert.That(u.Gaze.DamageDealt,Is.EqualTo(700).Within(.1));Assert.That(u.OrdinaryAttacksFired,Is.Zero);
        }
        [Test]public void GazeRetargetsWithinSameDurationAndSealsDefeatedTargetsTiles()
        {
            var b=Create(guardHealth:100,extra:true);Start(b);b.Advance(7.2f);
            Assert.That(b.Defenders.All(d=>!d.Alive),Is.True);Assert.That(b.SealedTileCount,Is.EqualTo(2));
            Assert.That(b.Gazes.Count,Is.EqualTo(1));Assert.That(b.Gazes[0].Ticks,Is.EqualTo(2));
        }
        [Test]public void ShieldBreakInterruptsGazeAndFreezesSkillCooldownWhileBroken()
        {
            var b=Create();Pillar(b);var u=Start(b);b.Advance(6.2f);var g=u.Gaze;Assert.That(g.Ticks,Is.EqualTo(1));
            b.TryTopplePillar(0,Vector3.left*2);Assert.That(g.Interrupted,Is.True);b.Advance(26);
            Assert.That(b.Gazes.Count,Is.EqualTo(1));Assert.That(g.Ticks,Is.EqualTo(1));
            b.Advance(14);Assert.That(b.Gazes.Count,Is.EqualTo(2));
        }
        [TestCase(false)] [TestCase(true)]public void AirbornePhaseRejectsOnlyGroundOnlyDefenderTargeting(bool canHitAir)
        {
            var b=Create(air:canHitAir,guardRange:3);var u=Start(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(8.3f);
            Assert.That(b.UnitAirborne(u),Is.True);Assert.That(b.ShotsFired>0,Is.EqualTo(canHitAir));
        }
        [Test]public void SecondPhaseCommandsExistingPillarWithFiveSecondWarningAndCanBreakOwnShield()
        {
            var b=Create();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.forward*2,power:100)});
            var u=Start(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(29.8f);
            Assert.That(b.PillarCommands.Count,Is.EqualTo(1));Assert.That(b.PillarCommands[0].Resolved,Is.False);
            b.Advance(2);Assert.That(b.PillarCommands[0].Applied,Is.True);Assert.That(b.Facilities[0].Collapsed,Is.True);
            Assert.That(u.StoneShield,Is.False);Assert.That(b.UnitAirborne(u),Is.False);
        }
        [Test]public void PauseFreezesRebirthAndTopplingAndRestartClearsAllAbilityState()
        {
            var b=Create();Pillar(b);var u=Start(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);
            b.SetPaused(true);b.Advance(50);Assert.That(b.TryTopplePillar(0,Vector3.left*2),Is.False);Assert.That(b.Clock,Is.Zero);
            b.Restart();Assert.That(b.Gazes,Is.Empty);Assert.That(b.PillarCommands,Is.Empty);Assert.That(b.Facilities[0].Collapsed,Is.False);
            u=Start(b);Assert.That(u.MandraPhase,Is.EqualTo(MandragoraPhase.Ground));Assert.That(u.StoneShield,Is.True);
        }
        [Test]public void SecondPhaseProtectionAlsoRejectsPillarShieldBreakAndStun()
        {
            var b=Create();Pillar(b);var u=Start(b);b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);b.Advance(5.1f);
            Assert.That(b.TryTopplePillar(0,Vector3.left*2),Is.True);
            Assert.That(u.Health,Is.EqualTo(1000));Assert.That(u.StoneShield,Is.True);
            Assert.That(u.PillarStunnedUntil,Is.EqualTo(double.NegativeInfinity));Assert.That(u.StoneShieldBreaks,Is.Zero);
        }
    }
}
