using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FlamechaserEffectTests
    {
        static ManualBattle Create(bool reveal=false,bool nearbyGuard=false,int blocks=0,float attack=1,float interval=1,float guardInterval=1)
        {
            var settings=new ManualBattleSettings("CARD_M30","M30_DUBLIN_FIRECHASE",20,1,3,1,.1f,1000,.001f,1,1,1,1);
            var c=new EngagementSettings(unitAttack:20,unitArmor:5,unitRange:3,unitInterval:interval,defenderHealth:10000,
                defenderAttack:attack,defenderArmor:0,defenderRange:nearbyGuard?2:.1f,defenderInterval:guardInterval,blockCount:blocks,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.05f,hasSkill:false,effects:new MonsterEffectSettings(CombatDamageType.Arts,unitResistance:50,flamechaser:true),revealsInvisible:reveal);
            return new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*100},nearbyGuard?Vector3.zero:Vector3.right*50,c);
        }
        static DeployedUnitState Start(ManualBattle b)
        {b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId);}
        [Test]public void TransitionHasOneSecondProtectionThenEmberDiesFromExactlyFiveDamageEvents()
        {
            var b=Create();var u=Start(b);Assert.That(b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical),Is.True);
            Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Transforming));Assert.That(u.Health,Is.EqualTo(5));Assert.That(u.Active,Is.True);
            Assert.That(b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),Is.False);
            b.Advance(.8f);Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Transforming));b.Advance(.3f);
            Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Ember));Assert.That(b.UnitInvisible(u),Is.True);
            Assert.That(u.IsMoving,Is.True);Assert.That(u.OrdinaryAttacksFired,Is.Zero);
            for(int i=0;i<4;i++){b.TryDamageUnit(u.EntityId,i%2==0?10000:1,i%2==0?CombatDamageType.Physical:CombatDamageType.Arts);Assert.That(u.Health,Is.EqualTo(4-i));}
            b.TryDamageUnit(u.EntityId,1,CombatDamageType.Physical);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
            b.Advance(20);Assert.That(u.EmberRevivals,Is.Zero);Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));
        }
        [Test]public void SurvivingEmberReformsAtTenSecondsAndCanRepeatTheWholeCycle()
        {
            var b=Create();var u=Start(b);
            for(int cycle=1;cycle<=2;cycle++)
            {
                b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);b.Advance(1.1f);
                b.Advance(9.5f);Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Ember));
                b.Advance(.5f);Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Reforming));Assert.That(b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts),Is.False);
                b.Advance(1.1f);Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Soldier));Assert.That(u.Health,Is.EqualTo(1000));
                Assert.That(u.MaxHealth,Is.EqualTo(1000));Assert.That(u.EmberRevivals,Is.EqualTo(cycle));Assert.That(u.EmberTransformations,Is.EqualTo(cycle));
            }
            b.Restart();u=Start(b);Assert.That(u.EmberRevivals,Is.Zero);Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Soldier));
        }
        [Test]public void PauseFreezesAllPhasesAndRejectsDamage()
        {
            var b=Create();var u=Start(b);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);
            b.SetPaused(true);double clock=b.Clock;b.Advance(50);Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Transforming));
            b.SetPaused(false);b.Advance(1.1f);b.SetPaused(true);clock=b.Clock;b.Advance(50);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Ember));Assert.That(b.TryDamageUnit(u.EntityId,20,CombatDamageType.Physical),Is.False);
        }
        [Test]public void HiddenEmberCannotBeSelectedButRevealingDefenderNaturallyClearsFiveHits()
        {
            foreach(bool reveal in new[]{false,true})
            {
                var b=Create(reveal,true,attack:10000);var u=Start(b);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Physical);b.Advance(6.3f);
                if(reveal){Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(b.ShotsFired,Is.EqualTo(5));}
                else{Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Ember));Assert.That(b.ShotsFired,Is.Zero);Assert.That(u.Health,Is.EqualTo(5));}
            }
        }
        [Test]public void TransformReleasesBlockingAndBlockingLaterRevealsTheEmberWithoutEnablingItsAttacks()
        {
            var b=Create(nearbyGuard:true,blocks:1);var u=Start(b);b.Advance(.1f);Assert.That(u.IsBlocked,Is.True);
            b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);Assert.That(u.IsBlocked,Is.False);Assert.That(b.UnitInvulnerable(u),Is.True);
            int attacks=u.OrdinaryAttacksFired;b.Advance(1.3f);Assert.That(u.IsBlocked,Is.True);Assert.That(b.UnitInvisible(u),Is.False);
            b.Advance(2);Assert.That(u.OrdinaryAttacksFired,Is.EqualTo(attacks));
        }
        [Test]public void OnlyNearbyBurningReedsAddBurnToTheActualAttackSnapshot()
        {
            foreach(bool near in new[]{false,true})
            {
                var b=Create(nearbyGuard:true,interval:.4f);
                b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Reeds,near?Vector3.right:Vector3.right*10,1,1,4,initiallyBurning:true)});
                Start(b);b.Advance(.3f);var shot=b.CombatShots.First(s=>!s.FromDefender&&s.Applied);
                Assert.That(shot.BurnDamage,Is.EqualTo(near?20:0));Assert.That(b.Defender.BurnAccumulated,Is.EqualTo(near?20:0));
                if(near){b.Advance(2);Assert.That(b.BurnBursts,Is.GreaterThan(0));}
            }
        }
        [Test]public void AltarTrueDamageAlsoHonorsTheEmberHitCounter()
        {
            var b=Create();b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Altar,Vector3.zero,2,1,10000)});var u=Start(b);
            b.Advance(3.1f);Assert.That(u.FlamePhase,Is.EqualTo(FlamechaserPhase.Ember));Assert.That(u.Health,Is.GreaterThanOrEqualTo(3));Assert.That(u.EmberTransformations,Is.EqualTo(1));
        }
        [Test]public void EmberStaysRevealedForThreeSecondsAfterItsBlockerDies()
        {
            var b=Create(nearbyGuard:true,blocks:1,guardInterval:100);
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Altar,Vector3.zero,2,3,5000)});
            var u=Start(b);b.Advance(.1f);b.TryDamageUnit(u.EntityId,10000,CombatDamageType.Arts);b.Advance(6);
            Assert.That(b.Defender.Alive,Is.False);Assert.That(u.IsBlocked,Is.False);Assert.That(b.UnitInvisible(u),Is.False);
            b.Advance(2.7f);Assert.That(b.UnitInvisible(u),Is.False);b.Advance(.4f);Assert.That(b.UnitInvisible(u),Is.True);
        }
    }
}
