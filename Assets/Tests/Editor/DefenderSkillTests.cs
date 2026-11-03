using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderSkillTests
    {
        static DefenderSettings Guard(string id,Vector3 p,DefenderSkillSettings skill=default,DefenderRole role=DefenderRole.Fighter,
            float health=100,float range=4,float healRange=4,float attack=20,float interval=.6f,float extra=0,bool canAttack=true)
            =>new DefenderSettings(id,id,"D01",p,health,attack,0,range,interval,role==DefenderRole.Medic?0:1,role,
                windup:.1f,actionDuration:.2f,healingRange:healRange,skill:skill,extraHealChance:extra,canAttack:canAttack);
        static ManualBattle Setup(DefenderSettings[] roster,float attack=1,float interval=100,int targets=1)
        {
            var b=new ManualBattle(new ManualBattleSettings("card","unit",20,1,5,1,.1f,100000,.1f,1,1,1,1),
                new[]{Vector3.zero,Vector3.forward*30},Vector3.one,new EngagementSettings(unitAttack:attack,unitInterval:interval,unitRange:10,effects:new MonsterEffectSettings(attackTargets:targets)),roster:roster);
            return b;
        }
        static void Begin(ManualBattle b){b.Begin();Assert.That(b.TryDeploy(b.RunId,1,b.Entry).Success,Is.True);}

        [Test]public void PreparationAndPauseDoNotChargeSkillsAndRestartRestoresInitialSP()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.MidnightEnchant,.25f,1,1,2,1.35f);
            var b=Setup(new[]{Guard("mid",Vector3.forward,skill)});b.Advance(50);Assert.That(b.Defender.SkillSP,Is.EqualTo(.25f));
            Begin(b);b.Advance(.25f);float sp=b.Defender.SkillSP;b.SetPaused(true);b.Advance(20);Assert.That(b.Defender.SkillSP,Is.EqualTo(sp));
            b.SetPaused(false);b.Advance(1);Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));
            b.SetPaused(true);double end=b.Defender.SkillActiveUntil;b.Advance(20);Assert.That(b.Defender.SkillActiveUntil,Is.EqualTo(end));Assert.That(b.DefenderSkillActive(b.Defender),Is.True);
            b.Restart();Assert.That(b.Defender.SkillCasts,Is.Zero);Assert.That(b.Defender.SkillSP,Is.EqualTo(.25f));Assert.That(b.DefenderSkillActive(b.Defender),Is.False);
        }
        [Test]public void CardiganRestoresFortyPercentWithoutOverhealAndHoldsFullSPWhenHealthy()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.CardiganRecovery,0,.5f,1,0,healFraction:.4f);
            var b=Setup(new[]{Guard("cardigan",Vector3.forward,skill)},attack:40);Begin(b);b.Advance(2);
            Assert.That(b.Defender.Health,Is.EqualTo(100));Assert.That(b.Defender.SkillHealthRestored,Is.EqualTo(40));Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));
            b.Advance(10);Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));Assert.That(b.Defender.SkillSP,Is.EqualTo(.5f));
        }
        [Test]public void DeadCardiganCannotReviveOrContinueCharging()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.CardiganRecovery,0,1,1,0,healFraction:.4f);
            var b=Setup(new[]{Guard("cardigan",Vector3.forward,skill,health:20)},attack:40);Begin(b);b.Advance(1);
            Assert.That(b.Defender.Alive,Is.False);float sp=b.Defender.SkillSP;b.Advance(10);
            Assert.That(b.Defender.SkillCasts,Is.Zero);Assert.That(b.Defender.SkillSP,Is.EqualTo(sp));
        }
        [Test]public void MidnightActuallySwitchesDamageTypeAndAttackMultiplierThenReturnsToPhysical()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.MidnightEnchant,0,1,1,1,1.35f);
            var b=Setup(new[]{Guard("mid",Vector3.forward,skill)});Begin(b);b.Advance(2.7f);
            var shots=b.CombatShots.Where(s=>s.FromDefender&&!s.Healing).ToArray();
            Assert.That(shots.Any(s=>s.DamageType==CombatDamageType.Physical&&Math.Abs(s.Attack-20)<.01),Is.True);
            Assert.That(shots.Any(s=>s.DamageType==CombatDamageType.Arts&&Math.Abs(s.Attack-27)<.01&&s.Applied),Is.True);
            Assert.That(shots.Last().DamageType,Is.EqualTo(CombatDamageType.Physical));Assert.That(b.Defender.SkillSP,Is.GreaterThan(0));
        }
        [Test]public void MidnightRangedPenaltyAndCriticalAreSnapshotIntoTheShot()
        {
            var spec=new DefenderSettings("mid","mid","D05",Vector3.forward*2,100,20,0,4,1,
                rangedAttackMultiplier:.8f,criticalChance:1,criticalMultiplier:1.5f);
            var b=Setup(new[]{spec});Begin(b);b.Advance(.1f);
            Assert.That(b.CombatShots.Single(s=>s.FromDefender).Attack,Is.EqualTo(24).Within(.001));Assert.That(b.Defender.CriticalHits,Is.EqualTo(1));
        }
        [Test]public void PlumeSpeedsUpAttackWindupAndActionButDoesNotRecoverSPDuringActiveWindow()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.PlumeSwiftStrike,1,1,1,2,1.25f,1.25f);
            var b=Setup(new[]{Guard("plume",Vector3.forward,skill,interval:1)});Begin(b);b.Advance(1.1f);
            var shots=b.CombatShots.Where(s=>s.FromDefender).ToArray();Assert.That(shots.Length,Is.EqualTo(2));
            Assert.That(shots[1].StartedAt-shots[0].StartedAt,Is.EqualTo(.8).Within(.051));Assert.That(shots[0].Attack,Is.EqualTo(25));
            Assert.That(b.Defender.LastActionRate,Is.EqualTo(1.25f));Assert.That(b.Defender.SkillSP,Is.Zero);
        }
        [Test]public void AnselActivatesForWoundedAllyOutsideBaseRangeAndHealingUsesBoost()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.AnselRange,0,1,1,1,1.4f,healRangeBonus:2);
            var b=Setup(new[]{Guard("tank",Vector3.forward,health:1000),Guard("ansel",Vector3.right*3,skill,DefenderRole.Medic,healRange:1.5f)},attack:40);
            Begin(b);b.Advance(1.7f);
            var healer=b.FindDefender("ansel");Assert.That(healer.SkillCasts,Is.EqualTo(1));
            Assert.That(b.CombatShots.Any(s=>s.SourceId=="ansel"&&s.Healing&&s.TargetId=="tank"&&s.Applied&&Math.Abs(s.Attack-28)<.01),Is.True);
            Assert.That(b.CombatShots.Any(s=>s.SourceId=="ansel"&&!s.Healing),Is.False);
            b.Advance(.6f);Assert.That(b.DefenderSkillActive(healer),Is.False);Assert.That(b.DefenderHealRange(healer),Is.EqualTo(1.5f));
        }
        [Test]public void ExtraHealingTargetsAnotherWoundedAllyWithoutDuplicateOrResurrection()
        {
            var b=Setup(new[]{Guard("a",Vector3.forward,health:1000),Guard("b",Vector3.forward*2,health:1000),
                Guard("ansel",Vector3.right,role:DefenderRole.Medic,extra:1)},attack:5,targets:3);
            Begin(b);b.Advance(.9f);
            var heals=b.CombatShots.Where(s=>s.SourceId=="ansel"&&s.Healing).ToArray();
            Assert.That(b.FindDefender("ansel").ExtraHeals,Is.GreaterThan(0));
            foreach(var group in heals.GroupBy(s=>s.StartedAt))Assert.That(group.Select(s=>s.TargetId).Distinct().Count(),Is.EqualTo(group.Count()));
            Assert.That(b.Defenders.All(d=>d.Health<=d.MaxHealth),Is.True);
        }
        [Test]public void FrozenGuardCanStoreFullSPButCannotCastUntilControlEnds()
        {
            var skill=new DefenderSkillSettings(DefenderSkillKind.MidnightEnchant,0,1,1,1,1.35f);
            var b=Setup(new[]{Guard("mid",Vector3.forward,skill)});Begin(b);b.TryApplyCold("mid",3);b.TryApplyCold("mid",3);b.Advance(2);
            Assert.That(b.Defender.SkillSP,Is.EqualTo(1));Assert.That(b.Defender.SkillCasts,Is.Zero);
            b.Advance(1.5f);Assert.That(b.Defender.SkillCasts,Is.EqualTo(1));
        }
        [Test]public void PassiveGuardDoesNotFabricateOrdinaryAttack()
        {
            var b=Setup(new[]{Guard("passive",Vector3.forward,canAttack:false)});Begin(b);b.Advance(3);
            Assert.That(b.CombatShots.Any(s=>s.FromDefender),Is.False);
        }
        [Test]public void InvalidSkillNumbersAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new DefenderSkillSettings(DefenderSkillKind.MidnightEnchant,0,0,1,1));
            Assert.Throws<ArgumentException>(()=>new DefenderSkillSettings(DefenderSkillKind.PlumeSwiftStrike,0,10,1,0));
            Assert.Throws<ArgumentException>(()=>new DefenderSkillSettings(DefenderSkillKind.CardiganRecovery,0,10,1,0,healFraction:float.NaN));
        }
        [Test]public void LegacyColdStillSlowsIntervalWithoutChangingItsAuthoredWindup()
        {
            var guard=new DefenderSettings("legacy","legacy","D01",Vector3.forward,1000,10,0,4,1,windup:.4f,actionDuration:.5f);
            var b=Setup(new[]{guard});Begin(b);b.TryApplyCold("legacy",10);b.Advance(.05f);
            Assert.That(b.CombatShots.Single(s=>s.FromDefender).StartedAt-b.Defender.LastActionTime,Is.EqualTo(.4).Within(.001));
            b.Advance(1.6f);var shots=b.CombatShots.Where(s=>s.FromDefender).ToArray();
            Assert.That(shots.Length,Is.EqualTo(2));Assert.That(shots[1].StartedAt-shots[0].StartedAt,Is.EqualTo(1/.7).Within(.051));
        }
    }
}
