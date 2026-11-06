using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class HerdSpiritTests
    {
        static readonly string[] Herd = { "CARD_M21", "CARD_M22", "CARD_M23", "CARD_M24" };

        static ManualBattle Make(int tier = 4, bool adjacentDefender = false, string source = "CARD_M21", int uses = 2)
        {
            var unit = new ManualBattleSettings(source, source, 100, 10, uses, 2, .1f, 200, 1, 1, 1, 1, 1);
            var combat = new EngagementSettings(unitAttack: 20, unitArmor: 0, unitRange: 1, unitInterval: 2,
                defenderHealth: 10000, defenderAttack: 1, defenderArmor: 0, defenderRange: 3,
                blockCount: adjacentDefender ? 1 : 0, initialSP: 0, requiredSP: 0, spPerSecond: 0,
                skillAmmo: 0, skillAttack: 0, skillInterval: 0, skillRange: 0, hasSkill: false,
                attackDuration: .3f, attackWindup: .1f);
            var battle = new ManualBattle(unit, new[] { Vector3.zero, new Vector3(100, 0, 0) },
                new Vector3(adjacentDefender ? .25f : 50, 0, 0), combat);
            battle.ConfigureSynergies(Herd.Take(tier).Select(id => new ShopTrialModel.Piece { Id = id, Star = 1 }));
            battle.Begin();
            return battle;
        }

        static DeployedUnitState Deploy(ManualBattle battle)
        {
            var receipt = battle.TryDeploy(battle.RunId, 1, battle.Entry);
            Assert.That(receipt.Success, Is.True);
            return battle.FindUnit(receipt.EntityId);
        }

        static DeployedUnitState Convert(ManualBattle battle, DeployedUnitState source)
        {
            Assert.That(battle.TryDamageUnit(source.EntityId, 10000, CombatDamageType.Physical), Is.True);
            battle.Advance(.02f);
            return battle.Units.Single(unit => battle.IsHerdHost(unit.EntityId));
        }

        static void AdvanceTo(ManualBattle battle, double time)
        { battle.Advance((float)Math.Max(0, time - battle.Clock)); }

        [Test]
        public void FinalDeathCreatesOneLinkedSpiritAtSourceProgressWithoutAnotherDeployment()
        {
            // The effect applies to ordinary non-Herd cards too, not just M21-M24.
            var battle = Make(source: "CARD_M07");
            var source = Deploy(battle);
            battle.Advance(.2f);
            double distance = source.Motion.DistanceTravelled;
            int dp = battle.DP, uses = battle.RemainingUses;
            Assert.That(battle.TryDamageUnit(source.EntityId, 10000, CombatDamageType.Physical), Is.True);
            Assert.That(battle.TryDamageUnit(source.EntityId, 10000, CombatDamageType.Physical), Is.False);
            battle.Advance(.02f);

            var host = battle.Units.Single(unit => battle.IsHerdHost(unit.EntityId));
            var state = battle.HerdSpiritFor(host.EntityId);
            Assert.That(state, Is.Not.Null);
            Assert.That(battle.HerdSpiritFrom(source.EntityId), Is.SameAs(state));
            Assert.That(state.SourceId, Is.EqualTo(source.EntityId));
            Assert.That(state.EntityId, Is.EqualTo(host.EntityId));
            Assert.That(state.StartedAt, Is.EqualTo(source.DeathTime));
            Assert.That(state.ReadyAt, Is.EqualTo(source.DeathTime + 1.25).Within(.000001));
            Assert.That(host.CardId, Is.EqualTo(source.CardId));
            Assert.That(host.RouteId, Is.EqualTo(source.RouteId));
            Assert.That(host.Motion.DistanceTravelled, Is.EqualTo(distance).Within(.00001));
            Assert.That(host.SynergySupport, Is.True);
            Assert.That(battle.UnitCombat(host).HasSkill, Is.False);
            Assert.That(battle.HerdUsed, Is.EqualTo(1));
            Assert.That(battle.Units.Count, Is.EqualTo(2));
            Assert.That(battle.DP, Is.EqualTo(dp));
            Assert.That(battle.RemainingUses, Is.EqualTo(uses));
        }

        [Test]
        public void ReassemblyCannotMoveBlockAttackOrTakeDamageAndPauseFreezesItsTimeline()
        {
            var battle = Make(adjacentDefender: true);
            var host = Convert(battle, Deploy(battle));
            var state = battle.HerdSpiritFor(host.EntityId);
            double distance = host.Motion.DistanceTravelled;
            float defenderHealth = battle.Defender.Health;
            battle.Advance(.9f);

            Assert.That(host.IsReviving, Is.True);
            Assert.That(host.IsMoving, Is.False);
            Assert.That(host.IsBlocked, Is.False);
            Assert.That(host.HasCombatTarget, Is.False);
            Assert.That(battle.Defender.BlockedCount, Is.Zero);
            Assert.That(battle.Defender.TargetId, Is.Null);
            Assert.That(host.Motion.DistanceTravelled, Is.EqualTo(distance));
            Assert.That(host.OrdinaryAttacksFired, Is.Zero);
            Assert.That(battle.CombatShots.Any(shot => shot.SourceId == host.EntityId || shot.TargetId == host.EntityId), Is.False);
            Assert.That(battle.TryDamageUnit(host.EntityId, 10000, CombatDamageType.Physical), Is.False);
            Assert.That(host.Health, Is.EqualTo(host.MaxHealth));
            Assert.That(battle.Defender.Health, Is.EqualTo(defenderHealth));

            battle.SetPaused(true);
            double clock = battle.Clock, remaining = battle.HerdRemaining(host.EntityId);
            battle.Advance(20);
            Assert.That(battle.Clock, Is.EqualTo(clock));
            Assert.That(battle.HerdSpiritFor(host.EntityId), Is.SameAs(state));
            Assert.That(host.IsReviving, Is.True);
            Assert.That(host.Motion.DistanceTravelled, Is.EqualTo(distance));
            Assert.That(battle.HerdRemaining(host.EntityId), Is.EqualTo(remaining));

            battle.SetPaused(false);
            AdvanceTo(battle, state.ReadyAt + .02);
            Assert.That(host.IsReviving, Is.False);
            Assert.That(host.IsBlocked, Is.True);
            Assert.That(battle.Defender.BlockedCount, Is.EqualTo(1));
            Assert.That(host.OrdinaryAttacksFired, Is.GreaterThan(0));
            Assert.That(battle.TryDamageUnit(host.EntityId, 1, CombatDamageType.Physical), Is.True);
            battle.Advance(.4f);
            Assert.That(battle.Defender.Health, Is.LessThan(defenderHealth));
        }

        [TestCase(2, 10)]
        [TestCase(4, 12)]
        public void FullActiveLifetimeStartsAfterRisingAndExpiryCannotConvertAgain(int tier, double lifetime)
        {
            var battle = Make(tier);
            var host = Convert(battle, Deploy(battle));
            var state = battle.HerdSpiritFor(host.EntityId);
            Assert.That(battle.HerdRemaining(host.EntityId), Is.EqualTo(lifetime).Within(.00001));
            AdvanceTo(battle, state.ReadyAt + .2);
            Assert.That(host.IsReviving, Is.False);
            Assert.That(battle.HerdRemaining(host.EntityId), Is.EqualTo(lifetime - .2).Within(.001));
            AdvanceTo(battle, state.ReadyAt + lifetime - .1);
            Assert.That(host.Active, Is.True);
            AdvanceTo(battle, state.ReadyAt + lifetime + .06);
            Assert.That(host.Active, Is.False);
            Assert.That(battle.HerdRemaining(host.EntityId), Is.Zero);
            Assert.That(battle.HerdUsed, Is.EqualTo(1));
            Assert.That(battle.Units.Count, Is.EqualTo(2));
            Assert.That(battle.HasPendingDeathBlast, Is.False);
            Assert.That(battle.DeathBlasts, Is.Empty);
        }

        [Test]
        public void KillingAReadySpiritCannotCreateAnotherConversion()
        {
            var battle = Make();
            var host = Convert(battle, Deploy(battle));
            AdvanceTo(battle, battle.HerdSpiritFor(host.EntityId).ReadyAt + .02);
            Assert.That(battle.TryDamageUnit(host.EntityId, 10000, CombatDamageType.Physical), Is.True);
            battle.Advance(.1f);
            Assert.That(host.Active, Is.False);
            Assert.That(battle.HerdUsed, Is.EqualTo(1));
            Assert.That(battle.Units.Count, Is.EqualTo(2));
            Assert.That(battle.HerdSpiritFrom(host.EntityId), Is.Null);
        }

        [Test]
        public void LastAvailableDeploymentWaitsForReassemblyAndRestartClearsBothLinks()
        {
            var battle = Make(uses: 1);
            var source = Deploy(battle);
            var host = Convert(battle, source);
            Assert.That(battle.RemainingUses, Is.Zero);
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Running));
            Assert.That(host.Active, Is.True);
            Assert.That(host.IsReviving, Is.True);
            battle.Advance(.8f);
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Running));

            battle.Restart();
            Assert.That(battle.HerdSpiritFrom(source.EntityId), Is.Null);
            Assert.That(battle.HerdSpiritFor(host.EntityId), Is.Null);
            Assert.That(battle.IsHerdHost(host.EntityId), Is.False);
            Assert.That(battle.HerdRemaining(host.EntityId), Is.Zero);
            Assert.That(battle.HerdUsed, Is.Zero);
            Assert.That(battle.Units, Is.Empty);
            Assert.That(battle.HerdSpiritFor(null), Is.Null);
            Assert.That(battle.HerdSpiritFrom(null), Is.Null);
        }
    }
}
