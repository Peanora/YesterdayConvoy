using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class HarvesterEffectTests
    {
        static ManualBattle Create(bool near=false,int blocks=0,float health=1000,float regeneration=0,float speed=.1f)
        {
            var s=new ManualBattleSettings("CARD_M34","M34_TEST",20,1,3,1,.1f,health,speed,1,1,1,1);
            var c=new EngagementSettings(unitAttack:40,unitArmor:0,unitRange:1.2f,unitInterval:3,
                defenderHealth:10000,defenderAttack:1,defenderArmor:20,defenderRange:.001f,blockCount:blocks,blockRadius:1.1f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                attackDuration:.5f,attackWindup:.2f,projectileTime:.05f,hasSkill:false,
                effects:new MonsterEffectSettings(harvester:true,regenerationFraction:regeneration));
            return new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward*(near?1:50),c);
        }
        static DeployedUnitState Start(ManualBattle b)
        {b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void UndamagedUnitWaitsThirtySecondsThenWalksSlowlyWithoutAttacking()
        {
            var b=Create();var u=Start(b);b.Advance(29.9f);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
            b.Advance(.4f);Assert.That(u.Motion.DistanceTravelled,Is.GreaterThan(0).And.LessThan(.05));
            Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Dormant));Assert.That(u.Health,Is.EqualTo(1000));Assert.That(u.OrdinaryAttacksFired,Is.Zero);
        }
        [Test]public void DamageStartsOneSecondOriginalWakeBeforeSixfoldMovementAndFourPercentDrain()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);
            Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Waking));b.Advance(.9f);Assert.That(u.Motion.DistanceTravelled,Is.Zero);
            b.Advance(.2f);Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Combat));Assert.That(b.UnitMoveMultiplier(u),Is.EqualTo(6));
            double moved=u.Motion.DistanceTravelled;float hp=u.Health;b.Advance(1);
            Assert.That(u.Motion.DistanceTravelled-moved,Is.EqualTo(.6).Within(.001));Assert.That(hp-u.Health,Is.EqualTo(40).Within(.01));
        }
        [Test]public void WakeRequiresHealthStrictlyBelowNinetyNinePointNineNinePercent()
        {
            var b=Create(health:100000);var u=Start(b);b.TryDamageUnit(u.EntityId,10,CombatDamageType.Arts);
            Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Dormant));b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);
            Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Waking));
        }
        [Test]public void ClinicalDrainNaturallyKillsWithoutDefenderOrRevival()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(27);
            Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(u.HarvesterHealthLost,Is.EqualTo(999).Within(.1));Assert.That(u.ReviveUsed,Is.False);
        }
        [Test]public void AuraAddsTwentyPercentNeuralOnlyAndIgnoresCamouflage()
        {
            var b=Create(near:true,speed:.001f);b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,Vector3.forward)});var u=Start(b);
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(2.2f);
            Assert.That(b.DefenderCamouflaged(b.Defender),Is.True);Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(8));
            Assert.That(b.Defender.Health,Is.EqualTo(10000));Assert.That(u.HarvesterAuraTicks,Is.EqualTo(1));Assert.That(u.OrdinaryAttacksFired,Is.Zero);
        }
        [Test]public void ThirteenAuraPulsesCauseOnlyOneHundredTrueDamageAndTenSecondStun()
        {
            var b=Create(near:true,speed:.001f);var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(14.2f);
            Assert.That(b.NeuralBursts,Is.EqualTo(1));Assert.That(b.Defender.Health,Is.EqualTo(9900));Assert.That(b.Defender.NeuralUntil,Is.GreaterThan(b.Clock));
        }
        [Test]public void BlockedOrdinaryPhysicalHitAddsTenPercentNeuralAtImpact()
        {
            var b=Create(near:true,blocks:1);var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(1.4f);
            Assert.That(u.OrdinaryAttacksFired,Is.EqualTo(1));Assert.That(b.Defender.Health,Is.EqualTo(9980));Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(4));
            Assert.That(b.CombatShots.Single(s=>!s.FromDefender).NeuralDamage,Is.EqualTo(4));
        }
        [Test]public void DeadCasterCannotApplyPendingOrdinaryNeuralDamage()
        {
            var b=Create(near:true,blocks:1);var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(1.1f);
            b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);b.Advance(1);
            Assert.That(b.Defender.Health,Is.EqualTo(10000));Assert.That(b.Defender.NeuralAccumulated,Is.Zero);
        }
        [Test]public void HealingNeverReturnsAwakenedUnitToDormant()
        {
            var b=Create(regeneration:1);var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(2);
            Assert.That(u.Health,Is.EqualTo(1000));Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Combat));Assert.That(u.HarvesterHealthLost,Is.GreaterThan(0));
        }
        [Test]public void PauseAndRestartRestoreDormancyAndClearLossAndAuraState()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.SetPaused(true);b.Advance(20);
            Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Waking));Assert.That(b.Clock,Is.Zero);
            b.Restart();u=Start(b);Assert.That(u.HarvesterPhase,Is.EqualTo(HarvesterPhase.Dormant));Assert.That(u.HarvesterAuraTicks,Is.Zero);Assert.That(u.HarvesterHealthLost,Is.Zero);
        }
        [Test]public void SixfoldMotionStillStopsBeforeDeclaredRubbleCell()
        {
            var b=Create();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.forward)},new[]{Vector3.forward*2});var u=Start(b);
            b.TryTopplePillar(0,Vector3.zero);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);b.Advance(6);
            Assert.That(u.Motion.Position.z,Is.GreaterThan(1).And.LessThan(1.4f));Assert.That(u.IsMoving,Is.False);
        }
        [Test]public void HealthDrainDoesNotContinuouslyResetExternalHitFlashTime()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);double incoming=u.LastHitTime;b.Advance(2);
            Assert.That(u.HarvesterHealthLost,Is.GreaterThan(0));Assert.That(u.LastHitTime,Is.EqualTo(incoming));
        }
    }
}
