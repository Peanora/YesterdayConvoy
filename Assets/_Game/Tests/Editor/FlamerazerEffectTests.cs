using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FlamerazerEffectTests
    {
        static ManualBattle Create(int blocks=0,float range=1.5f,float speed=.001f,int uses=3)
        {
            var s=new ManualBattleSettings("CARD_M31","M31_TEST",20,1,uses,1,.1f,1000,speed,1,1,1,1);
            var c=new EngagementSettings(unitAttack:100,unitArmor:0,unitRange:range,unitInterval:2,
                defenderHealth:10000,defenderAttack:1,defenderArmor:90,defenderRange:.01f,blockCount:blocks,blockRadius:1.1f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.05f,hasSkill:false,effects:new MonsterEffectSettings(flamerazer:true));
            return new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.right,c);
        }
        static DeployedUnitState Start(ManualBattle b,long request=1)
        {b.Begin();var r=b.TryDeploy(b.RunId,request,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void FullCastUsesTwentyOneHalfSecondArtsTicksAndBurnInsteadOfPhysicalArmor()
        {
            var b=Create();var u=Start(b);b.Advance(.1f);Assert.That(u.Spray,Is.Not.Null);
            b.Advance(10.7f);Assert.That(u.Spray.Ticks,Is.EqualTo(21));Assert.That(u.Spray.Ended,Is.True);
            Assert.That(b.BurnBursts,Is.EqualTo(1));Assert.That(u.Spray.DamageDealt,Is.EqualTo(21*12+120).Within(.1));
            Assert.That(u.OrdinaryAttacksFired,Is.Zero);Assert.That(b.Defender.BurnUntil,Is.GreaterThan(b.Clock));
            b.Advance(4.8f);Assert.That(b.FlameSprays.Count,Is.EqualTo(1));
            b.Advance(.6f);Assert.That(b.FlameSprays.Count,Is.EqualTo(2));
        }
        [TestCase(false)] [TestCase(true)]public void ControlInterruptsTicksAndObeysPauseAndRecovery(bool silence)
        {
            var b=Create();var u=Start(b);b.Advance(1.1f);var cast=u.Spray;int ticks=cast.Ticks;
            Assert.That(b.TryDisableFlamerazer(u.EntityId,2,silence),Is.True);Assert.That(cast.Interrupted,Is.True);
            b.Advance(1);Assert.That(cast.Ticks,Is.EqualTo(ticks));Assert.That(b.FlameSprays.Count,Is.EqualTo(1));
            b.SetPaused(true);double clock=b.Clock;Assert.That(b.TryDisableFlamerazer(u.EntityId,4),Is.False);b.Advance(20);Assert.That(b.Clock,Is.EqualTo(clock));
            b.SetPaused(false);b.Advance(1.2f);Assert.That(b.FlameSprays.Count,Is.EqualTo(2));
        }
        [Test]public void OnlyBlockedUnitMayOrdinarilyAttackWhileSilenced()
        {
            foreach(int blocks in new[]{0,1})
            {
                var b=Create(blocks);var u=Start(b);b.TryDisableFlamerazer(u.EntityId,20,true);b.Advance(1);
                Assert.That(u.OrdinaryAttacksFired,blocks==0?Is.Zero:Is.EqualTo(1));
                Assert.That(b.FlameSprays,Is.Empty);
                if(blocks==1)Assert.That(10000-b.Defender.Health,Is.EqualTo(10).Within(.1));
            }
        }
        [Test]public void FatalHitProtectsOneHpThenExplodesExactlyOnceCancellingSpray()
        {
            var b=Create(uses:1);var u=Start(b);b.Advance(1);var cast=u.Spray;
            Assert.That(b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts),Is.True);Assert.That(u.Health,Is.EqualTo(1));
            Assert.That(b.UnitInvulnerable(u),Is.True);Assert.That(b.TryDamageUnit(u.EntityId,100,CombatDamageType.Physical),Is.False);
            Assert.That(b.TryDisableFlamerazer(u.EntityId,2),Is.False);Assert.That(cast.Interrupted,Is.True);
            b.Advance(.2f);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));
            Assert.That(b.FuelExplosions.Count,Is.EqualTo(1));Assert.That(b.FuelExplosions[0].GuardsHit,Is.EqualTo(1));
            int ticks=cast.Ticks;b.Advance(10);Assert.That(cast.Ticks,Is.EqualTo(ticks));
        }
        [Test]public void NaturalNearbyDeploymentChainsOnlyOnceAndResetClearsAllQueuesAndPillar()
        {
            var b=Create(speed:.8f);b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,new Vector3(1,0,0))},new[]{new Vector3(2,0,0),new Vector3(3,0,0)});
            var a=Start(b);b.TryDisableFlamerazer(a.EntityId,20,true);b.Advance(.5f);var d=Start(b,2);
            b.TryDamageUnit(a.EntityId,10000,CombatDamageType.Arts);b.Advance(.4f);
            Assert.That(b.FuelExplosions.Count,Is.EqualTo(2));Assert.That(b.FuelExplosions.All(e=>e.Resolved),Is.True);
            Assert.That(d.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(b.Facilities[0].Collapsed,Is.True);
            Assert.That(b.RubbleTiles.Count(),Is.EqualTo(2));
            b.Restart();Assert.That(b.FlameSprays,Is.Empty);Assert.That(b.FuelExplosions,Is.Empty);Assert.That(b.RubbleTiles,Is.Empty);Assert.That(b.Facilities[0].Collapsed,Is.False);
        }
        [Test]public void DistantGuardDoesNotTriggerSprayAndInvalidControlIsRejected()
        {
            var b=Create(range:.2f);var u=Start(b);b.Advance(2);Assert.That(b.FlameSprays,Is.Empty);Assert.That(u.IsMoving,Is.True);
            Assert.Throws<ArgumentException>(()=>b.TryDisableFlamerazer(u.EntityId,float.NaN));
            Assert.That(b.TryDisableFlamerazer("missing",2),Is.False);
        }
        [Test]public void ExplosionLeavesDistantPeerAndGuardAliveAndPauseDefersDetonation()
        {
            var b=Create(speed:4);var a=Start(b);b.TryDisableFlamerazer(a.EntityId,20,true);b.Advance(1);
            var peer=Start(b,2);b.TryDamageUnit(a.EntityId,10000,CombatDamageType.Arts);
            b.SetPaused(true);b.Advance(5);Assert.That(b.FuelExplosions[0].Resolved,Is.False);
            b.SetPaused(false);b.Advance(.2f);Assert.That(b.FuelExplosions.Count,Is.EqualTo(1));
            Assert.That(peer.Active,Is.True);Assert.That(b.Defender.Health,Is.EqualTo(10000));
        }
        [Test]public void FallingPillarsChainOnlyOnceAndRubbleRequiresDeclaredGround()
        {
            var b=Create(speed:.8f);b.ConfigureFacilities(new[]{
                new FacilitySettings(FacilityKind.Pillar,Vector3.right),new FacilitySettings(FacilityKind.Pillar,Vector3.right*2)},new[]{Vector3.right*3});
            var u=Start(b);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);b.Advance(.2f);
            Assert.That(b.Facilities.All(f=>f.Collapsed),Is.True);Assert.That(b.Facilities.All(f=>f.Activations==1),Is.True);
            Assert.That(b.RubbleTiles.Single(),Is.EqualTo(new Vector2Int(3,0)));
        }
        [Test]public void RubbleStopsLaterUnitBeforeEnteringBlockedCell()
        {
            var b=Create(speed:.8f);b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,Vector3.forward)},new[]{Vector3.forward*2});
            var u=Start(b);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);b.Advance(.2f);
            var peer=Start(b,2);b.TryDisableFlamerazer(peer.EntityId,100,true);b.Advance(10);
            Assert.That(peer.Active,Is.True);Assert.That(peer.Motion.Position.z,Is.GreaterThan(1).And.LessThan(1.4f));
            Assert.That(peer.IsMoving,Is.False);
        }
    }
}
