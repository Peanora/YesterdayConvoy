using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class MonsterEffectTests
    {
        private static ManualBattle Battle(MonsterEffectSettings effects,float health=1000,int uses=1,float defenderAttack=1,bool extra=false,float windup=0)
        {
            var combat=new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:3,unitInterval:.4f,
                defenderHealth:10000,defenderAttack:defenderAttack,defenderArmor:10,defenderRange:3,defenderInterval:.4f,
                blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.1f,hasSkill:false,effects:effects,attackDuration:windup,attackWindup:windup);
            var settings=new ManualBattleSettings("EFFECT_TEST","EFFECT_TEST",12,1,uses,1,.1f,health,1,1,2,1,1);
            var b=new ManualBattle(settings,new[]{Vector3.zero,Vector3.forward*6},Vector3.forward,combat,
                extra ? new[]{Vector3.forward*5} : null);b.Begin();return b;
        }
        private static DeployedUnitState Deploy(ManualBattle b,long request)
        { var r=b.TryDeploy(b.RunId,request,b.Entry);Assert.That(r.Success,Is.True);return b.FindUnit(r.EntityId); }

        [Test] public void PhysicalAndArtsUseSeparateDefensesAndMinimumOneDamage()
        {
            Assert.That(ManualBattle.CalculateDamage(20,18,25,CombatDamageType.Physical),Is.EqualTo(2));
            Assert.That(ManualBattle.CalculateDamage(20,18,25,CombatDamageType.Arts),Is.EqualTo(15));
            Assert.That(ManualBattle.CalculateDamage(20,1000,100,CombatDamageType.Arts),Is.EqualTo(1));
            Assert.That(ManualBattle.CalculateDamage(20,-10,0,CombatDamageType.Physical),Is.EqualTo(30));
            var b=Battle(new MonsterEffectSettings(CombatDamageType.Arts,resistance:25));Deploy(b,1);b.Advance(.2f);
            Assert.That(b.Defender.Health,Is.EqualTo(9985));
        }
        [Test] public void AcidStacksOncePerAppliedHitOnItsOwnDefenderAndRestartClearsIt()
        {
            var b=Battle(new MonsterEffectSettings(armorShred:2),extra:true);Deploy(b,1);b.Advance(.2f);
            Assert.That(b.Defender.ArmorReduction,Is.EqualTo(2));Assert.That(b.Defenders[1].ArmorReduction,Is.Zero);
            Assert.That(b.Defender.Health,Is.EqualTo(9990));b.Advance(.4f);
            Assert.That(b.Defender.ArmorReduction,Is.EqualTo(4));Assert.That(b.Defender.Health,Is.EqualTo(9978));
            b.Restart();Assert.That(b.Defender.ArmorReduction,Is.Zero);Assert.That(b.DeathBlasts,Is.Empty);
        }
        [Test] public void LastUnitDeathWaitsForItsDelayedPhysicalBlastAndDoesNotRequireLivingSource()
        {
            var b=Battle(new MonsterEffectSettings(blastAttack:40,blastRadius:2,blastDelay:1),health:1,defenderAttack:100,extra:true);
            var unit=Deploy(b,1);b.Advance(.3f);Assert.That(unit.Resolution,Is.EqualTo(UnitResolution.Dead));
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(b.HasPendingDeathBlast,Is.True);
            Assert.That(b.DeathBlasts.Count,Is.EqualTo(1));double clock=b.Clock;b.SetPaused(true);b.Advance(10);
            Assert.That(b.Clock,Is.EqualTo(clock));Assert.That(b.DeathBlasts[0].Resolved,Is.False);
            b.SetPaused(false);b.Advance(1.1f);Assert.That(b.DeathBlasts[0].Resolved,Is.True);
            Assert.That(b.DeathBlasts[0].TargetsHit,Is.EqualTo(1));Assert.That(b.Defenders[1].Health,Is.EqualTo(10000));
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));float health=b.Defender.Health;b.Advance(20);
            Assert.That(b.Defender.Health,Is.EqualTo(health));Assert.That(b.DeathBlasts.Count,Is.EqualTo(1));
        }
        [Test] public void RepeatedIceBlastsApplyColdThenFreezeAndExpireOnSimulationClock()
        {
            var b=Battle(new MonsterEffectSettings(blastAttack:1,blastRadius:2,blastDelay:1,coldDuration:10),health:1,uses:3,defenderAttack:100);
            Deploy(b,1);b.Advance(1.3f);Assert.That(b.Defender.ColdUntil,Is.GreaterThan(b.Clock));Assert.That(b.Defender.FrozenUntil,Is.LessThan(b.Clock));
            Deploy(b,2);b.Advance(1.8f);Assert.That(b.DeathBlasts.Count,Is.EqualTo(2));
            Assert.That(b.Defender.FrozenUntil,Is.GreaterThan(b.Clock));double frozenUntil=b.Defender.FrozenUntil;
            b.SetPaused(true);b.Advance(100);Assert.That(b.Defender.FrozenUntil,Is.EqualTo(frozenUntil));
            b.SetPaused(false);Deploy(b,3);int shots=b.ShotsFired;b.Advance(1);
            Assert.That(b.ShotsFired,Is.EqualTo(shots),"Frozen defender must not attack a live target.");
            Assert.That(b.Units[2].Active,Is.True);
            b.Advance(11);Assert.That(b.Defender.FrozenUntil,Is.LessThanOrEqualTo(b.Clock));
            b.Restart();Assert.That(b.Defender.ColdUntil,Is.LessThan(b.Clock));Assert.That(b.Defender.FrozenUntil,Is.LessThan(b.Clock));
        }
        [Test] public void EffectsRejectInvalidValuesAndLegacySettingsHaveNoNewBehavior()
        {
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(armorShred:float.NaN));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(resistance:101));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(blastAttack:1));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(coldDuration:10));
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(attackDuration:1,attackWindup:2));
            var old=new EngagementSettings(unitAttack:14);Assert.That(old.Effects.BlastAttack,Is.Zero);
            Assert.That(old.Effects.ArmorShred,Is.Zero);Assert.That(old.Effects.DamageType,Is.EqualTo(CombatDamageType.Physical));
        }

        [TestCase(1.5f,20f)]
        [TestCase(2.5f,40f)]
        public void FrozenBonusUsesImpactStateAndScalesAttackBeforeArmorWithoutConsumingFreeze(float multiplier,float damage)
        {
            var b=Battle(new MonsterEffectSettings(frozenAttackMultiplier:multiplier));Deploy(b,1);
            b.Advance(.025f);var shot=b.CombatShots[0];
            b.TryApplyCold(b.Defender.EntityId,1);b.TryApplyCold(b.Defender.EntityId,1);b.Advance(.15f);
            Assert.That(shot.FrozenBonusApplied,Is.True);
            Assert.That(shot.DamageDealt,Is.EqualTo(damage),"Scale attack before subtracting armor.");
            Assert.That(b.Defender.FrozenUntil,Is.GreaterThan(b.Clock));
            float hp=b.Defender.Health;b.SetPaused(true);b.Advance(10);
            Assert.That(b.Defender.Health,Is.EqualTo(hp));
            b.Restart();b.Begin();Deploy(b,2);b.TryApplyCold(b.Defender.EntityId,.05f);b.TryApplyCold(b.Defender.EntityId,.05f);b.Advance(.2f);
            Assert.That(b.CombatShots[0].FrozenBonusApplied,Is.False,"Expired freeze at impact gives no bonus.");
            Assert.That(b.CombatShots[0].DamageDealt,Is.EqualTo(10));
        }

        [Test] public void ColdAloneDoesNotGrantFrozenBonusAndFrozenMultiplierMustBeValid()
        {
            var b=Battle(new MonsterEffectSettings(frozenAttackMultiplier:1.5f));Deploy(b,1);
            b.TryApplyCold(b.Defender.EntityId,10);b.Advance(.2f);
            Assert.That(b.CombatShots[0].FrozenBonusApplied,Is.False);
            Assert.That(b.CombatShots[0].DamageDealt,Is.EqualTo(10));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(frozenAttackMultiplier:.5f));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(frozenAttackMultiplier:float.NaN));
        }

        [Test] public void EveryThirdOrdinaryAttackAppliesColdOnImpactAndRestartResetsCounter()
        {
            var b=Battle(new MonsterEffectSettings(CombatDamageType.Arts,coldAttackEvery:3,attackColdDuration:10));
            var unit=Deploy(b,1);b.Advance(.7f);
            Assert.That(unit.OrdinaryAttacksFired,Is.EqualTo(2));
            Assert.That(b.Defender.ColdUntil,Is.LessThan(b.Clock));
            b.Advance(.4f);
            var shots=b.CombatShots.Where(x=>!x.FromDefender).ToArray();
            Assert.That(shots.Take(2).All(x=>x.ColdDuration==0),Is.True);
            Assert.That(shots[2].ColdDuration,Is.EqualTo(10));Assert.That(shots[2].Applied,Is.True);
            Assert.That(b.Defender.ColdUntil-b.Clock,Is.InRange(9.7,10));
            Assert.That(b.Defender.FrozenUntil,Is.LessThan(b.Clock));
            b.SetPaused(true);int count=unit.OrdinaryAttacksFired;double clock=b.Clock;b.Advance(20);
            Assert.That(unit.OrdinaryAttacksFired,Is.EqualTo(count));Assert.That(b.Clock,Is.EqualTo(clock));
            b.Restart();b.Begin();unit=Deploy(b,2);b.Advance(.2f);
            Assert.That(unit.OrdinaryAttacksFired,Is.EqualTo(1));Assert.That(b.Defender.ColdUntil,Is.LessThan(b.Clock));
        }

        [Test] public void ColdShotFromDeadSourceDoesNotApplyAndInvalidCadencesAreRejected()
        {
            var b=Battle(new MonsterEffectSettings(coldAttackEvery:1,attackColdDuration:10),health:1,defenderAttack:100,windup:.2f);
            Deploy(b,1);b.Advance(.5f);
            Assert.That(b.Units[0].Active,Is.False);
            Assert.That(b.CombatShots.Any(s=>!s.FromDefender && s.ColdDuration>0),Is.True);
            Assert.That(b.CombatShots.Any(s=>!s.FromDefender && s.Applied),Is.False);
            Assert.That(b.Defender.ColdUntil,Is.LessThan(b.Clock));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(coldAttackEvery:3));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(attackColdDuration:10));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(coldAttackEvery:-1,attackColdDuration:10));
        }

        [Test] public void FrostNovaRevivesOnceWithoutEndingBattleAndPauseStopsTheFiveSecondTimer()
        {
            var b=Battle(new MonsterEffectSettings(frostNova:true),health:1,defenderAttack:100);
            var unit=Deploy(b,1);b.Advance(.3f);
            Assert.That(unit.IsReviving,Is.True);Assert.That(unit.Active,Is.True);
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Running));
            double at=unit.ReviveAt,distance=unit.Motion.DistanceTravelled;
            b.SetPaused(true);b.Advance(100);Assert.That(unit.ReviveAt,Is.EqualTo(at));
            b.SetPaused(false);b.Advance(4.7f);Assert.That(unit.IsReviving,Is.True);
            Assert.That(unit.Motion.DistanceTravelled,Is.EqualTo(distance));
            b.Advance(.15f);Assert.That(unit.IsReviving,Is.False);Assert.That(unit.Health,Is.EqualTo(1));
            Assert.That(b.UnitAttackMultiplier(unit),Is.EqualTo(1.5f));
            b.Advance(1);Assert.That(unit.Resolution,Is.EqualTo(UnitResolution.Dead));
            Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));
            b.Restart();b.Begin();unit=Deploy(b,2);Assert.That(unit.ReviveUsed,Is.False);
        }

        [Test] public void FrostNovaRingHitsOnlyInRangeAndSlowsWithoutApplyingColdOrFreeze()
        {
            var b=Battle(new MonsterEffectSettings(CombatDamageType.Arts,frostNova:true),extra:true);
            var unit=Deploy(b,1);
            // Defender at z=1 stops the caster at the entry; z=5 is outside its ring.
            b.Advance(8.45f);Assert.That(b.IceRings,Is.Empty);
            b.Advance(.1f);Assert.That(b.IceRings.Count,Is.EqualTo(1));
            Assert.That(b.IceRings[0].TargetsHit,Is.EqualTo(1));Assert.That(b.Defenders[1].Health,Is.EqualTo(10000));
            Assert.That(b.Defender.FrostSlowUntil-b.Clock,Is.InRange(7.9,8));
            Assert.That(b.Defender.ColdUntil,Is.LessThan(b.Clock));Assert.That(b.Defender.FrozenUntil,Is.LessThan(b.Clock));
            int count=unit.OrdinaryAttacksFired;float hp=b.Defender.Health;b.Advance(2);
            Assert.That(unit.OrdinaryAttacksFired,Is.EqualTo(count));Assert.That(b.Defender.Health,Is.EqualTo(hp));
            b.SetPaused(true);double clock=b.Clock;b.Advance(20);Assert.That(b.Clock,Is.EqualTo(clock));
            b.Restart();Assert.That(b.IceRings,Is.Empty);Assert.That(b.Defender.FrostSlowUntil,Is.LessThan(b.Clock));
        }

        [Test] public void RegenerationUsesSimulationTimeCapsAtMaximumAndCannotReviveDeadUnits()
        {
            var b=Battle(new MonsterEffectSettings(regenerationFraction:.1f),health:100,defenderAttack:10);
            var unit=Deploy(b,1);b.Advance(.2f);
            Assert.That(unit.HealthRecovered,Is.GreaterThan(0));Assert.That(unit.Health,Is.LessThanOrEqualTo(100));
            float hp=unit.Health,recovered=unit.HealthRecovered;
            b.SetPaused(true);b.Advance(30);Assert.That(unit.Health,Is.EqualTo(hp));Assert.That(unit.HealthRecovered,Is.EqualTo(recovered));
            var lethal=Battle(new MonsterEffectSettings(regenerationFraction:1),health:1,defenderAttack:100);
            var victim=Deploy(lethal,1);lethal.Advance(1);Assert.That(victim.Resolution,Is.EqualTo(UnitResolution.Dead));
            Assert.That(victim.Health,Is.Zero);Assert.That(victim.HealthRecovered,Is.Zero);
            b.Restart();b.Begin();unit=Deploy(b,2);Assert.That(unit.HealthRecovered,Is.Zero);
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(regenerationFraction:1.01f));
        }

        [Test] public void CenturionLaunchesThreeDistinctArtsHitsAndHealsOnlyFromAppliedDamage()
        {
            var prototype=Battle(new MonsterEffectSettings(CombatDamageType.Arts,attackTargets:3,lifeSteal:1.5f),health:100,defenderAttack:10);
            var b=new ManualBattle(prototype.Settings,new[]{Vector3.zero,Vector3.forward*6},Vector3.forward,prototype.CombatSettings,
                new[]{Vector3.forward*1.5f,Vector3.forward*2,Vector3.forward*5});b.Begin();
            var unit=Deploy(b,1);b.Advance(.7f);
            var volley=b.CombatShots.Where(s=>!s.FromDefender && s.StartedAt<.1).ToArray();
            Assert.That(volley.Length,Is.EqualTo(3));Assert.That(volley.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(3));
            Assert.That(volley.All(s=>s.Applied && s.DamageDealt==20),Is.True);
            Assert.That(b.Defenders[3].Health,Is.EqualTo(10000));
            Assert.That(unit.HealthRecovered,Is.GreaterThan(0));Assert.That(unit.Health,Is.LessThanOrEqualTo(100));
            b.Restart();Assert.That(b.CombatShots,Is.Empty);
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(attackTargets:4));
            Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(lifeSteal:float.PositiveInfinity));
        }

        private static ManualBattle HealerBattle(float regeneration=0)
        {
            var c=new EngagementSettings(unitAttack:20,unitRange:20,unitInterval:2,unitArmor:0,defenderRange:1,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,
                canAttack:false,attackDuration:1,attackWindup:.2f,effects:new MonsterEffectSettings(healer:true,regenerationFraction:regeneration));
            var s=new ManualBattleSettings("HEAL","HEAL",20,1,4,1,.1f,100,1,1,1,1,1);
            var b=new ManualBattle(s,new[]{Vector3.zero,Vector3.forward*100},Vector3.right*100,c);b.Begin();return b;
        }
        [Test] public void HealerTreatsAtMostThreeWoundedTargetsAndInFlightHealingCannotReviveItsDeadCaster()
        {
            var b=HealerBattle();for(int i=0;i<4;i++) {Deploy(b,i+1);b.Advance(.4f);}
            Assert.That(b.HealingCasts,Is.Empty,"Healthy units do not trigger casts.");
            for(int i=0;i<4;i++) b.TryDamageUnit(b.Units[i].EntityId,(i+1)*10,CombatDamageType.Physical);
            b.Advance(.05f);Assert.That(b.HealingCasts.Count,Is.EqualTo(4));var cast=b.HealingCasts[0];
            Assert.That(cast.TargetIds.Count,Is.EqualTo(3));Assert.That(cast.TargetIds.Distinct().Count(),Is.EqualTo(3));
            Assert.That(cast.TargetIds,Does.Not.Contain(b.Units[0].EntityId),"The three most wounded are chosen first.");
            b.SetPaused(true);b.Advance(30);Assert.That(cast.Resolved,Is.False);
            Assert.That(b.TryDamageUnit(b.Units[0].EntityId,1000,CombatDamageType.Physical),Is.False);
            b.SetPaused(false);b.TryDamageUnit(b.Units[0].EntityId,1000,CombatDamageType.Physical);b.Advance(.3f);
            Assert.That(cast.Resolved,Is.True);Assert.That(cast.HealthRestored,Is.Zero);
            Assert.That(b.Units[0].Health,Is.Zero);Assert.That(b.Units[0].Active,Is.False);
            Assert.That(b.Units.Skip(1).Any(u=>u.HealthRecovered>0),Is.True);
            Assert.That(b.Units.All(u=>u.Health<=100),Is.True);Assert.That(b.CombatShots,Is.Empty);
            b.Restart();Assert.That(b.HealingCasts,Is.Empty);Assert.That(b.RegenerationMultiplier,Is.EqualTo(1));
        }
        [Test] public void HealerPresenceDoublesNaturalRecoveryWithoutMakingExtraAttackShots()
        {
            var b=HealerBattle(.1f);var unit=Deploy(b,1);
            b.TryDamageUnit(unit.EntityId,10,CombatDamageType.Physical);b.Advance(.05f);
            Assert.That(unit.Health,Is.EqualTo(91).Within(.001));Assert.That(b.RegenerationMultiplier,Is.EqualTo(2));
            Assert.That(b.CombatShots,Is.Empty);Assert.That(unit.OrdinaryAttacksFired,Is.Zero);
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(effects:new MonsterEffectSettings(healer:true)));
        }

        [Test] public void PractitionerBarrierAbsorbsOnlyArtsAndLosesItsHealthAndSpeedBonusWhenBroken()
        {
            var b=Battle(new MonsterEffectSettings(artsBarrier:true,unitResistance:25),health:100);
            var unit=Deploy(b,1);Assert.That(unit.MaxHealth,Is.EqualTo(150));Assert.That(unit.Health,Is.EqualTo(150));
            Assert.That(unit.ArtsBarrierLayers,Is.EqualTo(3));Assert.That(b.UnitAttackRate(unit),Is.EqualTo(1.5f));
            b.TryDamageUnit(unit.EntityId,10,CombatDamageType.Physical);
            Assert.That(unit.Health,Is.EqualTo(140));Assert.That(unit.ArtsBarrierRemaining,Is.EqualTo(60).Within(.001));
            b.TryDamageUnit(unit.EntityId,40,CombatDamageType.Arts);
            Assert.That(unit.ArtsBarrierRemaining,Is.EqualTo(30).Within(.001));Assert.That(unit.Health,Is.EqualTo(140));
            b.SetPaused(true);Assert.That(b.TryDamageUnit(unit.EntityId,40,CombatDamageType.Arts),Is.False);
            b.SetPaused(false);b.TryDamageUnit(unit.EntityId,40,CombatDamageType.Arts);
            Assert.That(unit.ArtsBarrierRemaining,Is.Zero);Assert.That(unit.MaxHealth,Is.EqualTo(100));Assert.That(unit.Health,Is.EqualTo(100));
            Assert.That(b.UnitAttackRate(unit),Is.EqualTo(1));
            b.TryDamageUnit(unit.EntityId,40,CombatDamageType.Arts);Assert.That(unit.Health,Is.EqualTo(70).Within(.001));
            b.Restart();b.Begin();unit=Deploy(b,2);Assert.That(unit.ArtsBarrierLayers,Is.EqualTo(3));
            Assert.That(unit.MaxHealth,Is.EqualTo(150));Assert.Throws<ArgumentException>(()=>new MonsterEffectSettings(unitResistance:101));
        }

        [Test] public void EnemyMudrockRampsBeforeEachAttackAndRefreshesHerArtsBarrierWithoutDirectHealing()
        {
            var b=Battle(new MonsterEffectSettings(mudrock:true),health:1000);var unit=Deploy(b,1);
            Assert.That(unit.ArtsBarrierRemaining,Is.EqualTo(1000*16500f/45000).Within(.001));
            b.TryDamageUnit(unit.EntityId,400,CombatDamageType.Arts);Assert.That(unit.ArtsBarrierRemaining,Is.Zero);
            Assert.That(unit.MaxHealth,Is.EqualTo(1000));b.Advance(16.9f);
            var shots=b.CombatShots.Where(s=>!s.FromDefender).ToArray();Assert.That(shots.Length,Is.GreaterThan(6));
            Assert.That(shots[0].Attack,Is.EqualTo(32).Within(.001));Assert.That(shots[5].Attack,Is.EqualTo(92).Within(.001));
            Assert.That(shots.Skip(5).All(s=>Mathf.Abs(s.Attack-92)<.001),Is.True);
            Assert.That(unit.BarrierRefreshes,Is.Zero);float hp=unit.Health;
            b.SetPaused(true);b.Advance(100);Assert.That(unit.BarrierRefreshes,Is.Zero);
            b.SetPaused(false);b.Advance(.2f);Assert.That(unit.BarrierRefreshes,Is.EqualTo(1));
            Assert.That(unit.ArtsBarrierLayers,Is.EqualTo(3));Assert.That(unit.MaxHealth,Is.EqualTo(1500));
            Assert.That(unit.Health,Is.LessThanOrEqualTo(hp),"Refreshing a barrier must not grant direct healing.");
            Assert.That(b.UnitAttackRate(unit),Is.EqualTo(1.5f));
            b.Restart();b.Begin();unit=Deploy(b,2);Assert.That(unit.BarrierRefreshes,Is.Zero);Assert.That(unit.OrdinaryAttacksFired,Is.Zero);
        }
    }
}
