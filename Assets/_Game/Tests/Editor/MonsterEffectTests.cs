using System;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class MonsterEffectTests
    {
        private static ManualBattle Battle(MonsterEffectSettings effects,float health=1000,int uses=1,float defenderAttack=1,bool extra=false)
        {
            var combat=new EngagementSettings(unitAttack:20,unitArmor:0,unitRange:3,unitInterval:.4f,
                defenderHealth:10000,defenderAttack:defenderAttack,defenderArmor:10,defenderRange:3,defenderInterval:.4f,
                blockCount:0,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                projectileTime:.1f,hasSkill:false,effects:effects);
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

        [Test] public void FrozenBonusUsesImpactStateAndScalesAttackBeforeArmorWithoutConsumingFreeze()
        {
            var b=Battle(new MonsterEffectSettings(frozenAttackMultiplier:1.5f));Deploy(b,1);
            b.Advance(.025f);var shot=b.CombatShots[0];
            b.TryApplyCold(b.Defender.EntityId,1);b.TryApplyCold(b.Defender.EntityId,1);b.Advance(.15f);
            Assert.That(shot.FrozenBonusApplied,Is.True);
            Assert.That(shot.DamageDealt,Is.EqualTo(20),"20*1.5-10, not (20-10)*1.5");
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
    }
}
