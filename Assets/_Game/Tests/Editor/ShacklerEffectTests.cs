using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ShacklerEffectTests
    {
        static ManualBattle Create(bool enabled=true,float health=1000,float guardHealth=10000,float resistance=0)
        {
            var s=new ManualBattleSettings("CARD_M38","M38_TEST",20,1,3,1,.1f,health,.001f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:50,unitArmor:0,unitRange:2.5f,unitInterval:4,
                defenderHealth:guardHealth,defenderAttack:1,defenderArmor:1000,defenderRange:.001f,blockCount:1,blockRadius:1.1f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.05f,hasSkill:false,effects:new MonsterEffectSettings(damageType:CombatDamageType.Arts,resistance:resistance,shackler:enabled));
            return new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.forward,c);
        }
        static DeployedUnitState Start(ManualBattle b)
        {b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void BindingStopsBlockingAndOrdinaryAttackForTwentyTicksThenReleasesOnce()
        {
            var b=Create();var u=Start(b);b.Advance(.2f);Assert.That(b.DefenderShackled(b.Defender),Is.True);
            Assert.That(u.IsBlocked,Is.False);Assert.That(u.OrdinaryAttacksFired,Is.Zero);b.Advance(20);
            var cast=b.Shackles.Single();Assert.That(cast.Ended,Is.True);Assert.That(cast.Interrupted,Is.False);
            Assert.That(cast.Ticks,Is.EqualTo(20));Assert.That(cast.DamageDealt,Is.EqualTo(350).Within(.001));Assert.That(b.DefenderShackled(b.Defender),Is.False);
            b.Advance(50);Assert.That(b.Shackles.Count,Is.EqualTo(1));Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));Assert.That(u.IsBlocked,Is.True);
        }
        [Test]public void PeriodicArtsDamageUsesResistanceNotPhysicalArmor()
        {
            var b=Create(resistance:40);Start(b);b.Advance(1.2f);Assert.That(b.Shackles.Single().Ticks,Is.EqualTo(1));
            Assert.That(b.Shackles.Single().DamageDealt,Is.EqualTo(10.5f).Within(.001));
        }
        [TestCase(false)][TestCase(true)]public void StunOrSilenceImmediatelyReleasesAndDoesNotRearm(bool silence)
        {
            var b=Create();var u=Start(b);b.Advance(3.2f);Assert.That(b.TryDisableShackler(u.EntityId,3,silence),Is.True);
            Assert.That(u.Shackle.Ended&&u.Shackle.Interrupted,Is.True);Assert.That(b.DefenderShackled(b.Defender),Is.False);
            int ticks=u.Shackle.Ticks;b.Advance(30);Assert.That(u.Shackle.Ticks,Is.EqualTo(ticks));Assert.That(b.Shackles.Count,Is.EqualTo(1));
        }
        [Test]public void RealPillarControlImmediatelyInterruptsWithoutReusingSkill()
        {
            var b=Create();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.left,power:1)});var u=Start(b);b.Advance(2);
            Assert.That(b.TryTopplePillar(0,Vector3.left*2),Is.True);Assert.That(u.Shackle.Interrupted,Is.True);
            Assert.That(b.DefenderShackled(b.Defender),Is.False);b.Advance(30);Assert.That(b.Shackles.Count,Is.EqualTo(1));
        }
        [Test]public void SourceDeathReleasesImmediatelyAndCancelsFutureTicks()
        {
            var b=Create();var u=Start(b);b.Advance(2.2f);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);
            Assert.That(u.Shackle.Interrupted,Is.True);Assert.That(b.DefenderShackled(b.Defender),Is.False);
            int ticks=u.Shackle.Ticks;b.Advance(1);Assert.That(u.Shackle.Ticks,Is.EqualTo(ticks));
        }
        [Test]public void TargetDeathEndsWithoutContinuingDamageOrRetargeting()
        {
            var b=Create(guardHealth:30);var u=Start(b);b.Advance(3);
            Assert.That(b.Defender.Alive,Is.False);Assert.That(u.Shackle.Ended,Is.True);Assert.That(u.Shackle.Ticks,Is.EqualTo(2));
            Assert.That(u.Shackle.DamageDealt,Is.EqualTo(30));
        }
        [Test]public void SilenceBeforeFirstCastDelaysButDoesNotConsumeTheOneUse()
        {
            var b=Create();var u=Start(b);b.TryDisableShackler(u.EntityId,3,true);b.Advance(2.9f);Assert.That(b.Shackles,Is.Empty);
            b.Advance(.3f);Assert.That(b.Shackles.Count,Is.EqualTo(1));
        }
        [Test]public void PauseRejectsControlAndFreezesBindingRestartClearsIt()
        {
            var b=Create();var u=Start(b);b.Advance(2);b.SetPaused(true);b.Advance(50);
            Assert.That(b.Clock,Is.EqualTo(2).Within(.001));Assert.That(b.TryDisableShackler(u.EntityId,1),Is.False);Assert.That(b.DefenderShackled(b.Defender),Is.True);
            b.Restart();Assert.That(b.Shackles,Is.Empty);Assert.That(b.DefenderShackled(b.Defender),Is.False);
        }
        [Test]public void DefaultOffKeepsOrdinaryAttacksAndNoBinding()
        {
            var b=Create(false);var u=Start(b);b.Advance(5);Assert.That(b.Shackles,Is.Empty);Assert.That(u.OrdinaryAttacksFired,Is.GreaterThan(0));
        }
    }
}
