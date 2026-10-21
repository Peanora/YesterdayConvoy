using System;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class NoSkillCombatTests
    {
        private static EngagementSettings Combat(float attack=30,float defenderHealth=20,float defenderAttack=1) =>
            new EngagementSettings(unitAttack:attack,unitRange:1,defenderHealth:defenderHealth,defenderAttack:defenderAttack,
                defenderRange:1,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                hasSkill:false,melee:true);
        private static ManualBattle Battle(EngagementSettings c,float health=100) => new ManualBattle(
            new ManualBattleSettings("SLUG","SLUG",12,1,1,1,1,health,.75f,1,2,1,1),
            new[]{Vector3.zero,Vector3.forward*4},Vector3.forward,c);

        [Test] public void NoSkillNeverChargesOrCastsAndCanStillAttackAndBreach()
        {
            var b=Battle(Combat());b.Begin();var receipt=b.TryDeploy(b.RunId,1,b.Entry);var u=b.FindUnit(receipt.EntityId);
            Assert.That(b.CheckSkill(u.EntityId),Is.EqualTo(SkillFailure.NoSkill));
            Assert.That(b.TryActivateSkill(b.RunId,2,u.EntityId),Is.EqualTo(SkillFailure.NoSkill));
            b.Advance(20);
            Assert.That(u.SkillSP,Is.Zero);Assert.That(u.SkillAmmo,Is.Zero);Assert.That(b.SkillCasts,Is.Zero);
            Assert.That(b.Defender.Alive,Is.False);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Won));
            Assert.That(b.IsSkillReady(u.EntityId),Is.False);
        }
        [Test] public void OrdinaryDeathResolvesLossWithoutWaitingForAnImaginarySkill()
        {
            var b=Battle(Combat(1,100,100),1);b.Begin();var r=b.TryDeploy(b.RunId,1,b.Entry);b.Advance(2);
            var u=b.FindUnit(r.EntityId);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Lost));
            Assert.That(u.Resolution,Is.EqualTo(UnitResolution.Dead));Assert.That(double.IsInfinity(u.DeathTime),Is.False);
            Assert.That(u.DeathTime,Is.EqualTo(b.Clock));
        }
        [Test] public void NoSkillRejectsNonzeroOrNonfiniteResourcesAndLegacySkillDefaultsRemainEnabled()
        {
            Assert.That(new EngagementSettings(unitAttack:14).HasSkill,Is.True);
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(hasSkill:false));
            Assert.Throws<ArgumentException>(()=>new EngagementSettings(initialSP:0,requiredSP:float.NaN,spPerSecond:0,
                skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false));
            Assert.That(Combat().HasSkill,Is.False);
        }
    }
}
