using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class EngagementTests
    {
        private static ManualBattle New(EngagementSettings combat, Vector3? defender = null, float health = 100)
        {
            var basic = new ManualBattleSettings("CARD", "UNIT", 8, 3, 6, 2, 0.1f, health, 1, 1, 3, 10, 1);
            var b = new ManualBattle(basic, new[] { Vector3.zero, Vector3.forward * 20 }, defender ?? Vector3.forward * 2, combat);
            b.Begin(); return b;
        }
        private static DeployedUnitState Deploy(ManualBattle b, long request = 1)
        { Assert.That(b.TryDeploy(b.RunId, request, b.Entry).Success, Is.True); return b.Units[b.Units.Count - 1]; }

        [Test]
        public void RangeStopsMovementAndDefenderDeathResumesTheOriginalRoute()
        {
            var b = New(new EngagementSettings(unitRange: 0.8f, defenderHealth: 1, defenderRange: 0.1f));
            var u = Deploy(b); b.Advance(1.3f);
            Assert.That(u.HasCombatTarget, Is.True); Assert.That(u.IsMoving, Is.False);
            double stopped = u.Motion.DistanceTravelled; b.Advance(0.1f);
            Assert.That(u.Motion.DistanceTravelled, Is.EqualTo(stopped));
            b.Advance(0.3f); Assert.That(b.Defender.Alive, Is.False); Assert.That(u.IsMoving, Is.True);
            Assert.That(u.Motion.DistanceTravelled, Is.GreaterThan(stopped));
            Assert.That(u.Motion.Speed, Is.EqualTo(1), "Attack presentation cannot change logical speed.");
        }

        [Test]
        public void BlockingUsesCapacityWhileOverflowCanKeepAdvancing()
        {
            var b = New(new EngagementSettings(unitRange: 0.01f, defenderRange: 0.01f, blockRadius: 0.7f));
            var first = Deploy(b); b.Advance(1.4f); var second = Deploy(b, 2); b.Advance(1.7f);
            Assert.That(first.IsBlocked, Is.True); Assert.That(second.IsBlocked, Is.False);
            Assert.That(b.Defender.BlockedCount, Is.EqualTo(1));
            Assert.That(second.Motion.DistanceTravelled, Is.GreaterThan(first.Motion.DistanceTravelled));
        }

        [Test]
        public void DeathReleasesBlockAndPendingDefenderAttacksAreCancelled()
        {
            var b = New(new EngagementSettings(unitRange: 0.01f, defenderHealth: 1, defenderAttack: 1,
                blockRadius: 0.7f, initialSP: 12));
            var u = Deploy(b); b.Advance(1.4f); Assert.That(u.IsBlocked, Is.True);
            Assert.That(b.TryActivateSkill(b.RunId, 1, u.EntityId), Is.EqualTo(SkillFailure.None));
            b.Advance(0.3f);
            Assert.That(b.Defender.Alive, Is.False); Assert.That(b.Defender.BlockedCount, Is.Zero);
            Assert.That(u.IsBlocked, Is.False); Assert.That(u.IsMoving, Is.True);
            int shots = b.ShotsFired; b.Advance(2); Assert.That(b.ShotsFired, Is.EqualTo(shots));
        }

        [Test]
        public void ChargeIsPerEntityManualAndIdempotentWithoutSpendingDP()
        {
            var b = New(new EngagementSettings(initialSP: 2, requiredSP: 4, defenderRange: 0.01f), Vector3.forward * 15);
            var first = Deploy(b); b.Advance(1); var second = Deploy(b, 2); b.Advance(1);
            Assert.That(first.SkillSP, Is.EqualTo(4).Within(0.0001)); Assert.That(second.SkillSP, Is.EqualTo(3).Within(0.0001));
            Assert.That(first.SkillAmmo, Is.Zero, "A full bar never activates itself.");
            int dp = b.DP, uses = b.RemainingUses;
            Assert.That(b.TryActivateSkill(b.RunId, 10, first.EntityId), Is.EqualTo(SkillFailure.None));
            Assert.That(b.TryActivateSkill(b.RunId, 10, first.EntityId), Is.EqualTo(SkillFailure.None));
            Assert.That(b.SkillCasts, Is.EqualTo(1)); Assert.That(first.SkillAmmo, Is.EqualTo(6)); Assert.That(first.SkillSP, Is.Zero);
            Assert.That(second.SkillSP, Is.EqualTo(3).Within(0.0001)); Assert.That(b.DP, Is.EqualTo(dp)); Assert.That(b.RemainingUses, Is.EqualTo(uses));
            Assert.That(b.TryActivateSkill(b.RunId, 11, second.EntityId), Is.EqualTo(SkillFailure.NotReady));
        }

        [Test]
        public void PauseFreezesSPProjectileDamageMovementAndDeploymentCD()
        {
            var b = New(new EngagementSettings(unitRange: 5, initialSP: 0)); var u = Deploy(b); b.Advance(0.1f);
            Assert.That(b.CombatShots.Count, Is.GreaterThan(0));
            double clock = b.Clock, cd = b.CooldownRemaining; float sp = u.SkillSP, hp = u.Health, defenderHP = b.Defender.Health;
            b.SetPaused(true); b.Advance(30);
            Assert.That(b.TryActivateSkill(b.RunId, 1, u.EntityId), Is.EqualTo(SkillFailure.Paused));
            Assert.That(b.Clock, Is.EqualTo(clock)); Assert.That(b.CooldownRemaining, Is.EqualTo(cd));
            Assert.That(u.SkillSP, Is.EqualTo(sp)); Assert.That(u.Health, Is.EqualTo(hp)); Assert.That(b.Defender.Health, Is.EqualTo(defenderHP));
            b.SetPaused(false); b.Advance(0.3f); Assert.That(b.Defender.Health, Is.LessThan(defenderHP));
        }

        [Test]
        public void AmmoIsFiniteAndSPOnlyRechargesAfterItIsSpent()
        {
            var b = New(new EngagementSettings(unitRange: 5, defenderHealth: 10000, defenderRange: 0.01f,
                initialSP: 1, requiredSP: 1, skillAmmo: 6, skillInterval: 0.2f, skillAttack: 1));
            var u = Deploy(b); b.TryActivateSkill(b.RunId, 1, u.EntityId);
            Assert.That(u.SkillAmmo,Is.EqualTo(6));
            for(int remaining=5;remaining>=0;remaining--)
            {
                for(int step=0;u.SkillAmmo>remaining && step<30;step++) b.Advance(.01f);
                Assert.That(u.SkillAmmo,Is.EqualTo(remaining));
                Assert.That(u.LastAttackEmpowered,Is.True,"The last round retains its skill presentation after ammo reaches zero.");
                Assert.That(u.SkillSP,Is.Zero,"SP stays empty until the last round is fired.");
                b.SetPaused(true); b.Advance(2);
                Assert.That(u.SkillAmmo,Is.EqualTo(remaining)); Assert.That(u.SkillSP,Is.Zero);
                b.SetPaused(false);
            }
            b.Advance(.1f); Assert.That(u.SkillSP,Is.GreaterThan(0).And.LessThan(1));
            b.Advance(1); Assert.That(u.SkillSP, Is.EqualTo(1)); Assert.That(b.SkillCasts, Is.EqualTo(1));
        }

        [Test]
        public void ArmorStillAllowsAtLeastOneDamageOnBothSides()
        {
            var b = New(new EngagementSettings(unitAttack: 1, unitArmor: 100, unitRange: 5, defenderAttack: 1, defenderArmor: 100), health: 1000);
            var u = Deploy(b); b.Advance(0.3f);
            Assert.That(u.Health, Is.EqualTo(999)); Assert.That(b.Defender.Health, Is.EqualTo(179));
        }

        [Test]
        public void ADeadUnitCannotDeliverAPendingSkillHit()
        {
            var b = New(new EngagementSettings(unitRange: 1, defenderAttack: 1000, defenderRange: 10, projectileTime: 0.2f), Vector3.forward * 1.1f);
            var u = Deploy(b); b.Advance(0.4f);
            Assert.That(u.Active, Is.False); Assert.That(b.Defender.Health, Is.EqualTo(b.CombatSettings.DefenderHealth));
        }

        [Test]
        public void RestartRestoresDefenderAndRejectsOldSkillRequests()
        {
            var b = New(new EngagementSettings(initialSP: 12)); var u = Deploy(b); int oldRun = b.RunId;
            b.TryActivateSkill(b.RunId, 5, u.EntityId); b.Advance(1); b.Restart();
            Assert.That(b.Defender.Alive, Is.True); Assert.That(b.Defender.Health, Is.EqualTo(180));
            Assert.That(b.CombatShots.Count, Is.Zero); Assert.That(b.SkillCasts, Is.Zero); Assert.That(b.Units.Count, Is.Zero);
            Assert.That(b.TryActivateSkill(oldRun, 5, u.EntityId), Is.EqualTo(SkillFailure.StaleRun));
            b.Begin(); Assert.That(Deploy(b).SkillSP, Is.EqualTo(12));
        }
    }
}
