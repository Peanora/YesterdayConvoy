using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class NeuralEffectTests
    {
        static ManualBattle Create(float resistance=0,bool extra=false,float regeneration=0,float range=1)
        {
            var s=new ManualBattleSettings("CARD_M33","M33_TEST",20,1,3,1,.1f,1000,.001f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:30,unitArmor:0,unitRange:.1f,unitInterval:1,
                defenderHealth:10000,defenderAttack:1,defenderArmor:1000,defenderRange:.001f,blockCount:1,blockRadius:1.1f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.05f,hasSkill:false,canAttack:false,effects:new MonsterEffectSettings(resistance:resistance,regenerationFraction:regeneration,nervousCreeper:true));
            return new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward*range,c,extra?new[]{Vector3.right,Vector3.left*3}:null);
        }
        static DeployedUnitState Start(ManualBattle b)
        {b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void ReactionRequiresStrictlyMoreThanEachFifteenPercentBoundary()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,150,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.Zero);
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(1));
            b.TryDamageUnit(u.EntityId,149,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(1));
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(2));
        }
        [Test]public void SingleLargeHitProducesOneReactionWithoutCatchUpAndFatalHitProducesNone()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,500,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(1));
            b.TryDamageUnit(u.EntityId,100,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(1));
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(2));
            b.TryDamageUnit(u.EntityId,1000,CombatDamageType.Arts);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(u.CreeperRetaliations,Is.EqualTo(2));
            var lethal=Create();var v=Start(lethal);lethal.TryDamageUnit(v.EntityId,1000,CombatDamageType.Arts);Assert.That(lethal.CreeperPulses,Is.Empty);
        }
        [Test]public void ReactionsHitAllTargetsInTwoTilesUsingArtsAndRawAttackForNeuralInjury()
        {
            var b=Create(.0f,extra:true);var u=Start(b);b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);
            Assert.That(b.CreeperPulses.Single().TargetsHit,Is.EqualTo(2));Assert.That(b.Defenders[0].Health,Is.EqualTo(9970));
            Assert.That(b.Defenders[1].NeuralAccumulated,Is.EqualTo(30));Assert.That(b.Defenders[2].Health,Is.EqualTo(10000));
        }
        [Test]public void NeuralBurstBypassesArmorAndResistanceAndPreventsBlockingForTenSeconds()
        {
            var b=Create(90);var u=Start(b);
            for(int i=0;i<4;i++)b.TryDamageUnit(u.EntityId,161,CombatDamageType.Arts);
            Assert.That(b.NeuralBursts,Is.EqualTo(1));Assert.That(b.Defender.NeuralAccumulated,Is.Zero);
            Assert.That(b.Defender.Health,Is.EqualTo(10000-12-100).Within(.01));
            b.Advance(.1f);Assert.That(u.IsBlocked,Is.False);Assert.That(b.Defender.NeuralUntil,Is.GreaterThan(b.Clock));
            b.Advance(10);Assert.That(u.IsBlocked,Is.True);
        }
        [Test]public void BurstCooldownRejectsFurtherAccumulationButNotFurtherArtsDamage()
        {
            var b=Create();var u=Start(b);for(int i=0;i<5;i++)b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);
            Assert.That(u.CreeperRetaliations,Is.EqualTo(5));Assert.That(b.NeuralBursts,Is.EqualTo(1));Assert.That(b.Defender.NeuralAccumulated,Is.Zero);
            Assert.That(b.Defender.Health,Is.EqualTo(10000-150-100));
        }
        [Test]public void HealingNeverRearmsPreviouslyCrossedLifeThreshold()
        {
            var b=Create(regeneration:1);var u=Start(b);b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);b.Advance(1);
            Assert.That(u.Health,Is.EqualTo(1000));b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(1));
        }
        [Test]public void ControlledUnitConsumesCrossedThresholdWithoutDelayedRetaliation()
        {
            var b=Create();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.left,power:1)});var u=Start(b);
            b.TryTopplePillar(0,Vector3.left*2);b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.Zero);
            b.Advance(3.1f);b.TryDamageUnit(u.EntityId,1,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.Zero);
            b.TryDamageUnit(u.EntityId,150,CombatDamageType.Arts);Assert.That(u.CreeperRetaliations,Is.EqualTo(1));
        }
        [Test]public void PausedDamageCannotTriggerAndRestartClearsPulsesAndNeuralState()
        {
            var b=Create();var u=Start(b);b.SetPaused(true);Assert.That(b.TryDamageUnit(u.EntityId,900,CombatDamageType.Arts),Is.False);Assert.That(b.CreeperPulses,Is.Empty);
            b.SetPaused(false);for(int i=0;i<4;i++)b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);Assert.That(b.NeuralBursts,Is.EqualTo(1));
            b.Restart();Assert.That(b.CreeperPulses,Is.Empty);Assert.That(b.NeuralBursts,Is.Zero);Assert.That(b.Defender.NeuralUntil,Is.EqualTo(double.NegativeInfinity));
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(effects:new MonsterEffectSettings(nervousCreeper:true)));
        }
        [Test]public void RadialRetaliationDoesNotRequireSelectingCamouflagedTarget()
        {
            var b=Create();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,Vector3.forward)});var u=Start(b);
            Assert.That(b.DefenderCamouflaged(b.Defender),Is.True);b.TryDamageUnit(u.EntityId,151,CombatDamageType.Arts);
            Assert.That(b.CreeperPulses.Single().TargetsHit,Is.EqualTo(1));Assert.That(b.Defender.Health,Is.EqualTo(9970));
        }
    }
}
