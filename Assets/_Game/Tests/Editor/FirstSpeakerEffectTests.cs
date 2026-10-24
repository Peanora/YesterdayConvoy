using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FirstSpeakerEffectTests
    {
        static ManualBattle Create(bool enabled=true,float attack=10)
        {
            var s=new ManualBattleSettings("CARD_M36","M36_TEST",20,1,3,1,.1f,1000,.001f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:attack,unitArmor:0,unitRange:2.5f,unitInterval:1000,
                defenderHealth:10000,defenderAttack:1,defenderArmor:2,defenderRange:.001f,blockCount:0,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.05f,hasSkill:false,effects:new MonsterEffectSettings(firstSpeaker:enabled));
            return new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward,c,
                new[]{Vector3.forward+Vector3.right,Vector3.forward*2,Vector3.forward*2+Vector3.right,Vector3.forward*4});
        }
        static DeployedUnitState Start(ManualBattle b)
        {b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void OrdinaryHitIsPhysicalAndAddsFortyPercentNeuralOnlyOnImpact()
        {
            var b=Create();Start(b);b.Advance(.04f);Assert.That(b.Defender.NeuralAccumulated,Is.Zero);
            b.Advance(.1f);Assert.That(b.Defender.Health,Is.EqualTo(9992));Assert.That(b.Defender.NeuralAccumulated,Is.EqualTo(4));
        }
        [Test]public void BubbleWaitsTenSecondsHoldsFiveAndRejectsDamageOnlyDuringCast()
        {
            var b=Create();var u=Start(b);b.Advance(9.9f);Assert.That(b.CorrosiveBubbles,Is.Empty);
            b.Advance(.2f);Assert.That(b.FirstSpeakerCasting(u),Is.True);Assert.That(b.TryDamageUnit(u.EntityId,100,CombatDamageType.Arts),Is.False);
            float hp=u.Health;double position=u.Motion.DistanceTravelled;b.Advance(4.8f);
            Assert.That(u.Health,Is.EqualTo(hp));Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(position));
            b.Advance(.2f);Assert.That(b.FirstSpeakerCasting(u),Is.False);Assert.That(b.TryDamageUnit(u.EntityId,100,CombatDamageType.Arts),Is.True);
        }
        [Test]public void FiveHalfSecondPulsesStartAfterThreeAndRetargetLeastInjuredCrossCells()
        {
            var b=Create();Start(b);b.Advance(12.9f);var bubble=b.CorrosiveBubbles.Single();Assert.That(bubble.Pulses,Is.Zero);
            b.Advance(.2f);Assert.That(bubble.Pulses,Is.EqualTo(1));Assert.That(bubble.TargetIds[0],Is.EqualTo("kroos-1"));
            b.Advance(.5f);Assert.That(bubble.Pulses,Is.EqualTo(2));Assert.That(bubble.TargetIds[1],Is.EqualTo("kroos-2"));
            b.Advance(2);Assert.That(bubble.Pulses,Is.EqualTo(5));Assert.That(bubble.TargetIds.Distinct().Count(),Is.EqualTo(3));
            Assert.That(b.Defenders[3].Health,Is.EqualTo(10000),"Diagonal cell is outside the cross.");Assert.That(b.Defenders[4].Health,Is.EqualTo(10000));
            b.Advance(5);Assert.That(bubble.Pulses,Is.EqualTo(5));
        }
        [Test]public void NextCastUsesTwentySecondCooldownAfterCastEnds()
        {
            var b=Create();Start(b);b.Advance(34.9f);Assert.That(b.CorrosiveBubbles.Count,Is.EqualTo(1));
            b.Advance(.2f);Assert.That(b.CorrosiveBubbles.Count,Is.EqualTo(2));
        }
        [Test]public void PauseFreezesCastAndPendingPulseAndRestartClearsEverything()
        {
            var b=Create();Start(b);b.Advance(12);b.SetPaused(true);b.Advance(30);
            Assert.That(b.Clock,Is.EqualTo(12).Within(.001));Assert.That(b.CorrosiveBubbles.Single().Pulses,Is.Zero);
            b.SetPaused(false);b.Advance(1.1f);Assert.That(b.CorrosiveBubbles.Single().Pulses,Is.EqualTo(1));
            b.Restart();Assert.That(b.CorrosiveBubbles,Is.Empty);Assert.That(b.NeuralBursts,Is.Zero);Assert.That(b.Units,Is.Empty);
        }
        [Test]public void DeadOriginalTargetDoesNotDestroyAnchoredBubbleOrHitDiagonal()
        {
            var b=Create(attack:20000);Start(b);b.Advance(10.1f);
            // Ordinary hit already killed the nearest guard; the bubble anchors on the next live cell.
            var bubble=b.CorrosiveBubbles.Single();b.Advance(5);
            Assert.That(bubble.Pulses,Is.EqualTo(5));Assert.That(bubble.TargetIds.Distinct().Count(),Is.GreaterThan(1));
            Assert.That(b.Defenders[4].Health,Is.EqualTo(10000));
        }
        [Test]public void DefaultOffDoesNotAddBubbleOrNeuralEffects()
        {
            var b=Create(false);Start(b);b.Advance(40);Assert.That(b.CorrosiveBubbles,Is.Empty);Assert.That(b.Defender.NeuralAccumulated,Is.Zero);
        }
    }
}
