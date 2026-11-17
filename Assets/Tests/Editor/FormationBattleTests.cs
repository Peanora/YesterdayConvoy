using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationBattleTests
    {
        static FormationBattleModel Solo(int slot = 0)
        {
            var model = new FormationBattleModel(20261117);
            for (int i = 0; i < 4; i++) if (i != slot) Assert.That(model.ToggleSlot(i), Is.True);
            return model;
        }

        static FormationDefender SingleGuard(FormationBattleModel model, Vector2 position)
        {
            model.Defenders.RemoveRange(1, model.Defenders.Count - 1);
            var guard = model.Defenders[0];
            guard.Position = position; guard.MaxHealth = guard.Health = 100000;
            guard.Attack = 0; guard.Armor = 0; guard.Range = .1f; guard.Healer = false;
            return guard;
        }

        static void FinishFixture(FormationBattleModel model)
        {
            Assert.That(model.StartBattle(), Is.True);
            foreach (var defender in model.Defenders) { defender.Health = 0; defender.Alive = false; }
            model.Units.First(u => u.Enabled).Position = new Vector2(FormationBattleModel.Width - 1, 2);
            model.Advance(.025f);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void PreparationPlacesAndSwapsAtomicallyInsidePlayerHalf()
        {
            var model = new FormationBattleModel();
            Vector2 first = model.Units[0].Spawn, second = model.Units[1].Spawn;
            Assert.That(model.TryPlace(0, (int)second.x, (int)second.y), Is.True);
            Assert.That(model.Units[0].Spawn, Is.EqualTo(second));
            Assert.That(model.Units[1].Spawn, Is.EqualTo(first));
            Assert.That(model.TryPlace(0, FormationBattleModel.DeploymentColumns, 2), Is.False);
            Assert.That(model.TryPlace(0, -1, 2), Is.False);
            Assert.That(model.TryPlace(0, 3, 8), Is.False);
            Assert.That(model.TryPlace(8, 2, 2), Is.False);
            Assert.That(model.TryPlace(4, 3, 2), Is.False, "Bench cards cannot silently enter combat through dragging.");
            Assert.That(model.Units[0].Spawn, Is.EqualTo(second));
            Assert.That(model.EnabledCount, Is.EqualTo(4));
            model.StartBattle();
            Assert.That(model.TryPlace(0, 1, 1), Is.False);
            Assert.That(model.ToggleSlot(0), Is.False);
            Assert.That(model.Upgrade(0), Is.False);
            Assert.That(model.Expand(), Is.False);
        }

        [Test]
        public void CapacityAndStarChangesAreBoundedAndNeverLeaveAnEmptyTeam()
        {
            var model = new FormationBattleModel();
            Assert.That(model.ToggleSlot(4), Is.False);
            Assert.That(model.Expand(), Is.True);
            Assert.That(model.ToggleSlot(4), Is.True);
            Assert.That(model.ToggleSlot(5), Is.False);
            Assert.That(model.Expand(), Is.True);
            Assert.That(model.ToggleSlot(5), Is.True);
            Assert.That(model.Expand(), Is.False);
            Assert.That(model.EnabledCount, Is.EqualTo(6));
            float originalHealth = model.Units[0].MaxHealth, originalAttack = model.Units[0].Attack;
            Assert.That(model.Upgrade(0), Is.True);
            Assert.That(model.Upgrade(0), Is.True);
            Assert.That(model.Upgrade(0), Is.False);
            Assert.That(model.Units[0].Star, Is.EqualTo(3));
            Assert.That(model.Units[0].MaxHealth, Is.GreaterThan(originalHealth));
            Assert.That(model.Units[0].Attack, Is.GreaterThan(originalAttack));
            for (int i = 0; i < 5; i++) Assert.That(model.ToggleSlot(i), Is.True);
            Assert.That(model.ToggleSlot(5), Is.False);
            Assert.That(model.EnabledCount, Is.EqualTo(1));
        }

        [Test]
        public void PauseAndDoubleSpeedShareOneFixedSimulationClock()
        {
            var fast = new FormationBattleModel(41);
            var normal = new FormationBattleModel(41);
            fast.StartBattle(); normal.StartBattle(); fast.Paused = true;
            Vector2 initialPosition = fast.Units[0].Position;
            fast.Advance(10);
            Assert.That(fast.Clock, Is.Zero);
            Assert.That(fast.Units[0].Position, Is.EqualTo(initialPosition));
            Assert.That(fast.Units[0].SkillCharge, Is.Zero);
            fast.Paused = false; fast.Speed = 2;
            fast.Advance(.5f);
            for (int i = 0; i < 10; i++) normal.Advance(.1f);
            Assert.That(fast.Clock, Is.EqualTo(1).Within(1e-6));
            Assert.That(normal.Clock, Is.EqualTo(fast.Clock).Within(1e-6));
            for (int i = 0; i < 6; i++)
            {
                Assert.That(fast.Units[i].Position, Is.EqualTo(normal.Units[i].Position));
                Assert.That(fast.Units[i].Health, Is.EqualTo(normal.Units[i].Health));
                Assert.That(fast.Units[i].SkillCharge, Is.EqualTo(normal.Units[i].SkillCharge));
            }
        }

        [Test]
        public void DeathWaitsForRevivalAtOriginalCellWithoutRefreshingSkillOrEnemyHealth()
        {
            var model = Solo(); model.StartBattle();
            var unit = model.Units[0]; unit.Position = new Vector2(FormationBattleModel.DeploymentColumns + 1, 2);
            var guard = SingleGuard(model, unit.Position + Vector2.right * .5f);
            guard.Attack = 100000; guard.Range = 10; guard.Interval = 100;
            unit.SkillCharge = 3;
            model.Advance(.025f);
            Assert.That(unit.Alive, Is.False);
            Assert.That(unit.Deaths, Is.EqualTo(1));
            Assert.That(unit.RespawnRemaining, Is.EqualTo(8).Within(.001));
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Fighting));
            float charge = unit.SkillCharge, guardHealth = guard.Health;
            model.Paused = true; model.Advance(20);
            Assert.That(unit.RespawnRemaining, Is.EqualTo(8).Within(.001));
            model.Paused = false; model.Advance(7.95f);
            Assert.That(unit.Alive, Is.False);
            Assert.That(unit.SkillCharge, Is.EqualTo(charge));
            Assert.That(guard.Health, Is.EqualTo(guardHealth));
            model.Advance(.05f);
            Assert.That(unit.Alive, Is.True);
            Assert.That(unit.Health, Is.EqualTo(unit.MaxHealth));
            Assert.That(unit.Position, Is.EqualTo(unit.Spawn));
            Assert.That(unit.SkillCharge, Is.EqualTo(charge));
            Assert.That(unit.Deaths, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RangedBodyMarchesIntoVisibleBattleAreaBeforeEitherSideCanDealDamage(int terrainCase)
        {
            FormationBattleTerrainLayout terrain = terrainCase == 0 ? null : new FormationBattleTerrainLayout(
                terrainCase == 1 ? Array.Empty<FormationBattleTerrainRegion>() : new[]
                {
                    new FormationBattleTerrainRegion(new RectInt(17, 1, 2, 2), .48f, true),
                    new FormationBattleTerrainRegion(new RectInt(18, 4, 2, 2), .48f, true),
                    new FormationBattleTerrainRegion(new RectInt(13, 1, 2, 1), .36f, false),
                    new FormationBattleTerrainRegion(new RectInt(16, 4, 2, 1), .36f, false)
                });
            // The wider map changes the deterministic RNG consumption for seat selection.
            // Select an ordinary generated board containing the ranged profile this boundary case needs.
            var model = Enumerable.Range(20261117,32).Select(seed=>new FormationBattleModel(seed,terrain))
                .First(candidate=>candidate.Defenders.Any(defender=>defender.Kind>=6&&!defender.Healer));
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry
            {
                Instance = 1, ArtId = "M08", Star = 1,
                Spawn = new Vector2(FormationBattleModel.DeploymentColumns - 1, 2)
            } }, 1), Is.True);
            // Use the generated ranged guard and its ordinary combat profile; only isolate
            // the encounter and put the guard in the front legal guard cell.
            var guard = model.Defenders.First(defender => defender.Kind >= 6 && !defender.Healer);
            model.Defenders.RemoveAll(defender => defender != guard);
            guard.Position = new Vector2(FormationBattleModel.DeploymentColumns + 2, 2);
            var bow = model.Units.Single(); var anchor = bow.Spawn;
            Assert.That(Vector2.Distance(bow.Position, guard.Position), Is.LessThan(bow.Range));
            Assert.That(Vector2.Distance(bow.Position, guard.Position), Is.LessThan(guard.Range));
            Assert.That(model.StartBattle(), Is.True);
            int approachTicks = 0;
            while (bow.Position.x < FormationBattleModel.BattleAreaStart && approachTicks++ < 200)
            {
                model.Advance(.025f);
                if (bow.Position.x >= FormationBattleModel.BattleAreaStart) break;
                Assert.That(bow.IsMoving, Is.True);
                Assert.That(bow.TargetId, Is.EqualTo(-1));
                Assert.That(bow.DamageDone, Is.Zero); Assert.That(guard.DamageDone, Is.Zero);
                Assert.That(bow.Health, Is.EqualTo(bow.MaxHealth));
                Assert.That(guard.Health, Is.EqualTo(guard.MaxHealth));
                Assert.That(bow.LastActionAt, Is.LessThan(0));
                Assert.That(bow.LastSkillAt, Is.LessThan(0));
            }
            Assert.That(approachTicks, Is.GreaterThan(1));
            Assert.That(bow.Position.x, Is.GreaterThanOrEqualTo(FormationBattleModel.BattleAreaStart));
            for (int step = 0; step < 100 && (bow.DamageDone == 0 || guard.DamageDone == 0); step++) model.Advance(.025f);
            Assert.That(bow.DamageDone, Is.GreaterThan(0));
            Assert.That(guard.DamageDone, Is.GreaterThan(0));
            Assert.That(model.Hits.All(hit => hit.Heal ||
                (hit.FromId == bow.Id ? hit.From.x : hit.To.x) >= FormationBattleModel.BattleAreaStart), Is.True);
            Assert.That(bow.Spawn, Is.EqualTo(anchor));
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Fighting));
        }

        [Test]
        public void AutomaticSkillEmitsOnlyRealDamageAndLocksMovementThroughItsAction()
        {
            var model = Solo(3); model.StartBattle();
            var unit = model.Units[3]; unit.Position = new Vector2(FormationBattleModel.DeploymentColumns + 2, 4); unit.Attack = 100;
            var guard = SingleGuard(model, new Vector2(FormationBattleModel.DeploymentColumns + 3, 4)); guard.MaxHealth = guard.Health = 1000;
            unit.SkillCharge = unit.SkillRequired;
            model.Advance(.025f);
            Assert.That(unit.LastSkillAt, Is.EqualTo(model.Clock));
            Assert.That(model.Hits.Count, Is.EqualTo(3));
            Assert.That(model.Hits.All(hit => hit.Skill && !hit.Heal && !hit.Killed), Is.True);
            Assert.That(1000 - guard.Health, Is.EqualTo(model.Hits.Sum(hit => hit.Amount)).Within(.001));
            Assert.That(unit.DamageDone, Is.EqualTo(345).Within(.001));
            Vector2 firingPosition = unit.Position;
            guard.Alive = false; guard.Health = 0;
            model.Advance(.5f);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Exiting));
            Assert.That(unit.Position, Is.EqualTo(firingPosition));
            model.Advance(.3f);
            Assert.That(unit.Position.x, Is.GreaterThan(firingPosition.x));
        }

        [Test]
        public void HeraldHealingRestoresActualMissingHealthAndIsReported()
        {
            var model = new FormationBattleModel();
            model.ToggleSlot(1); model.ToggleSlot(5); model.StartBattle();
            var herald = model.Units[5]; var tank = model.Units[0];
            herald.Position = new Vector2(FormationBattleModel.DeploymentColumns + 2, 3); tank.Position = new Vector2(FormationBattleModel.DeploymentColumns + 2, 2);
            tank.Health = 300; herald.SkillCharge = herald.SkillRequired;
            var guard = SingleGuard(model, new Vector2(FormationBattleModel.DeploymentColumns + 3, 3)); guard.MaxHealth = guard.Health = 100000;
            float before = tank.Health;
            model.Advance(.025f);
            Assert.That(tank.Health, Is.GreaterThan(before));
            Assert.That(model.Hits.Any(hit => hit.Heal && hit.FromId == herald.Id && hit.ToId == tank.Id), Is.True);
            Assert.That(herald.HealingDone, Is.EqualTo(tank.Health - before).Within(.001));
            Assert.That(tank.Health, Is.LessThanOrEqualTo(tank.MaxHealth));
        }

        [Test]
        public void GateOnlyCountsAfterClearAndActionFinishes()
        {
            var model = Solo(); model.StartBattle();
            var unit = model.Units[0];
            unit.Position = new Vector2(FormationBattleModel.Width - 1, 2); SingleGuard(model, new Vector2(FormationBattleModel.DeploymentColumns + 2, 2));
            model.Advance(.025f);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Fighting));
            model.Retry(); model.StartBattle();
            unit.Position = new Vector2(FormationBattleModel.Width - 2, 2); unit.Attack = 100000;
            var guard = SingleGuard(model, new Vector2(FormationBattleModel.Width - 1.5f, 2)); guard.Health = 1;
            model.Advance(.025f);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Exiting));
            Assert.That(unit.Position.x, Is.LessThan(FormationBattleModel.Width - 1));
            Assert.That(model.Hits.Any(hit => hit.Killed), Is.True);
            model.Advance(2);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void TimerIncludesExitTravelButGateCompletionWinsExactBoundary()
        {
            var lost = Solo(); lost.LimitSeconds = .1; lost.StartBattle();
            lost.Defenders.Clear(); lost.Advance(1);
            Assert.That(lost.Phase, Is.EqualTo(FormationPhase.Lost));
            Assert.That(lost.Clock, Is.EqualTo(.1).Within(1e-6));
            Assert.That(lost.TimeRemaining, Is.Zero);
            var won = Solo(); won.LimitSeconds = .025; won.StartBattle(); won.Defenders.Clear();
            var unit = won.Units[0]; unit.Position = new Vector2(FormationBattleModel.Width - 1 - unit.MoveSpeed * .025f, 2);
            won.Advance(.025f);
            Assert.That(won.Phase, Is.EqualTo(FormationPhase.Won));
            Assert.That(won.Clock, Is.EqualTo(.025).Within(1e-6));
        }

        [Test]
        public void RetryReplaysSameEnemyBoardAndNextRoundKeepsRosterChanges()
        {
            var model = new FormationBattleModel(7341);
            model.Upgrade(2); model.Expand(); model.ToggleSlot(5); model.TryPlace(2, 1, FormationBattleModel.Height - 1);
            string enemies = BoardSnapshot(model);
            model.StartBattle(); model.Advance(3); model.Retry();
            Assert.That(BoardSnapshot(model), Is.EqualTo(enemies));
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation));
            Assert.That(model.Clock, Is.Zero); Assert.That(model.Hits.Count, Is.Zero);
            Assert.That(model.Units.All(u => u.Deaths == 0 && u.SkillCharge == 0), Is.True);
            Assert.That(model.Units[2].Star, Is.EqualTo(2));
            Assert.That(model.Units[2].Spawn, Is.EqualTo(new Vector2(1, FormationBattleModel.Height - 1)));
            Assert.That(model.NextRound(), Is.False);
            int[] counts = { 4, 6, 9, 12, 16 };
            for (int round = 1; round <= 5; round++)
            {
                Assert.That(model.Round, Is.EqualTo(round)); Assert.That(model.Defenders.Count, Is.EqualTo(counts[round - 1]));
                Assert.That(model.Units[2].Star, Is.EqualTo(2));
                Assert.That(model.Capacity, Is.EqualTo(5)); Assert.That(model.Units[5].Enabled, Is.True);
                FinishFixture(model);
                Assert.That(model.NextRound(), Is.EqualTo(round < 5));
            }
        }

        [Test]
        public void SeedAndFramePartitionReplayTheSameBoardAndCombat()
        {
            var first = new FormationBattleModel(937);
            var replay = new FormationBattleModel(937);
            var different = new FormationBattleModel(938);
            Assert.That(BoardSnapshot(first), Is.EqualTo(BoardSnapshot(replay)));
            Assert.That(BoardSnapshot(first), Is.Not.EqualTo(BoardSnapshot(different)));
            first.StartBattle(); replay.StartBattle(); first.Advance(4);
            for (int i = 0; i < 160; i++) replay.Advance(.025f);
            Assert.That(first.Clock, Is.EqualTo(replay.Clock).Within(1e-6));
            for (int i = 0; i < first.Units.Count; i++)
            {
                Assert.That(first.Units[i].Position, Is.EqualTo(replay.Units[i].Position));
                Assert.That(first.Units[i].Health, Is.EqualTo(replay.Units[i].Health));
            }
            for (int i = 0; i < first.Defenders.Count; i++) Assert.That(first.Defenders[i].Health, Is.EqualTo(replay.Defenders[i].Health));
        }

        [Test]
        public void DefaultFirstRoundAndFullyUpgradedFiveRoundFixtureHaveWinningWitnesses()
        {
            var model = new FormationBattleModel(20261117);
            model.StartBattle(); model.Advance(100);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won), "Default entry should demonstrate a complete battle.");
            model.Reset(20261117); model.Expand(); model.Expand(); model.ToggleSlot(4); model.ToggleSlot(5);
            for (int i = 0; i < 6; i++) { model.Upgrade(i); model.Upgrade(i); }
            for (int round = 1; round <= 5; round++)
            {
                model.StartBattle(); model.Advance(100);
                Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won), "Preset upgraded witness for round " + round);
                if (round < 5) Assert.That(model.NextRound(), Is.True);
            }
            // This is a preset combat witness, not evidence about acquisition, balance, or player win rate.
        }

        static string BoardSnapshot(FormationBattleModel model) => string.Join("|", model.Defenders.Select(d => d.ArtId + ":" + d.Position.x + "," + d.Position.y + ":" + d.MaxHealth + ":" + d.Attack));
    }
}
