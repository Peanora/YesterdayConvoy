using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationCadejoBlinkTests
    {
        static FormationBattleModel StartSingleCadejo(FormationBattleTerrainLayout terrain = null)
        {
            var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(unit => new ShopTrialModel.Role
            {
                Id = unit.CardId, Name = unit.Name, Cost = unit.Cost
            }), 71);
            shop.EnableFormationEconomy();
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(), Array.Empty<string>(), 10, 0, 80);
            var run = new FormationCampaignRun(shop, 71);
            for (int choices = 0; run.HasBlockingChoice; choices++)
            {
                Assert.That(choices, Is.LessThan(32));
                Assert.That(run.Choose(0).Success, Is.True);
            }
            shop.Preset(new[] { new ShopTrialModel.Piece { Id = "CARD_B14", Star = 1 } }, Array.Empty<string>(), 10, 0, 80);

            var model = new FormationBattleModel(71, terrain);
            model.BindCampaign(run);
            Assert.That(model.ApplyRoster(new[] { new FormationRosterEntry
            {
                Instance = shop.Bench.Single().Instance, ArtId = "CARD_B14", Star = 1, Spawn = new Vector2(8, 3)
            } }, 10), Is.True);
            Assert.That(model.StartBattle(), Is.True);
            return model;
        }

        [Test]
        public void CadejoBlinksFromDeploymentIntoEnemyReachOnceAndPauseFreezesWindup()
        {
            var model = StartSingleCadejo();
            var unit = model.Units.Single();
            var spawn = unit.Position;
            var target = model.Defenders.First();
            target.Position = new Vector2(20, 3);
            foreach (var guard in model.Defenders.Skip(1)) guard.Alive = false;
            model.Advance(.025f);

            var blink = model.CadejoBlinks.Single();
            Assert.That(blink.ActorId, Is.EqualTo(unit.Id));
            Assert.That(blink.From, Is.EqualTo(spawn));
            Assert.That(unit.Position, Is.EqualTo(spawn));
            Assert.That(blink.To.x, Is.GreaterThanOrEqualTo(FormationBattleModel.BattleAreaStart));
            Assert.That(Vector2.Distance(blink.To, target.Position), Is.InRange(.35f, unit.Range));
            Assert.That(double.IsPositiveInfinity(blink.ArrivedAt), Is.True);
            model.Paused = true;
            model.Advance(1);
            Assert.That(model.Clock, Is.EqualTo(blink.StartedAt));
            Assert.That(unit.Position, Is.EqualTo(spawn));
            Assert.That(double.IsPositiveInfinity(blink.ArrivedAt), Is.True);

            model.Paused = false;
            for (int i = 0; double.IsPositiveInfinity(blink.ArrivedAt) && i < 16; i++) model.Advance(.025f);
            Assert.That(blink.Cancelled, Is.False);
            Assert.That(blink.ArrivedAt - blink.StartedAt, Is.GreaterThanOrEqualTo(.0999));
            Assert.That(unit.Position, Is.EqualTo(blink.To));
            Assert.That(model.CadejoBlinks.Count, Is.EqualTo(1));
            Assert.That(unit.TargetId, Is.EqualTo(target.Id));
            Assert.That(unit.IsMoving, Is.False);
            Assert.That(model.SkillCasts, Is.Empty);
            Assert.That(model.Hits.Any(hit=>hit.FromId==unit.Id),Is.False,"Landing must not create an extra attack. The guard may react immediately.");
        }

        [Test]
        public void CadejoAimsForWalkableGroundBelowHighGuardAndRetargetsIfThatGuardDies()
        {
            var terrain = new FormationBattleTerrainLayout(new[]
            {
                new FormationBattleTerrainRegion { cells = new RectInt(18, 3, 2, 2), height = 1, allowDefender = true }
            });
            var model = StartSingleCadejo(terrain);
            var unit = model.Units.Single();
            var elevated = model.Defenders[0];
            elevated.Position = new Vector2(18, 3);
            var next = model.Defenders[1];
            next.Position = new Vector2(22, 6);
            foreach (var guard in model.Defenders.Skip(2)) guard.Alive = false;

            model.Advance(.025f);
            var first = model.CadejoBlinks.Single();
            Assert.That(unit.TargetId, Is.EqualTo(elevated.Id));
            Assert.That(terrain.HeightAt(elevated.Position), Is.GreaterThan(0));
            Assert.That(terrain.IsWalkable(first.To), Is.True);
            Assert.That(terrain.HeightAt(first.To), Is.Zero);
            Assert.That(Vector2.Distance(first.To, elevated.Position), Is.InRange(.35f, unit.Range));

            elevated.Alive = false;
            model.Advance(.025f);
            var second = model.CadejoBlinks.Last();
            Assert.That(first.Cancelled, Is.True);
            Assert.That(double.IsPositiveInfinity(first.ArrivedAt), Is.True);
            Assert.That(second.Sequence, Is.GreaterThan(first.Sequence));
            Assert.That(second.From, Is.EqualTo(first.From));
            Assert.That(unit.TargetId, Is.EqualTo(next.Id));
            Assert.That(Vector2.Distance(second.To, next.Position), Is.InRange(.35f, unit.Range));

            for (int i = 0; double.IsPositiveInfinity(second.ArrivedAt) && i < 16; i++) model.Advance(.025f);
            Assert.That(unit.Position, Is.EqualTo(second.To));
            Assert.That(model.CadejoBlinks.Count(cue => !double.IsPositiveInfinity(cue.ArrivedAt)), Is.EqualTo(1));
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Fighting));
        }

        [Test]
        public void CadejoGoesAroundTerrainAndOnlyWinsByActuallyLandingAtBlueGate()
        {
            var terrain = new FormationBattleTerrainLayout(new[]
            {
                new FormationBattleTerrainRegion { cells = new RectInt(13, 3, 2, 2), height = 1, allowDefender = false }
            });
            var model = StartSingleCadejo(terrain);
            var unit = model.Units.Single();
            foreach (var guard in model.Defenders) guard.Alive = false;

            bool sawDetour = false;
            for (int i = 0; i < 1600 && model.Phase != FormationPhase.Won; i++)
            {
                var before = unit.Position;
                model.Advance(.025f);
                Assert.That(unit.IsMoving, Is.False);
                if (unit.Position == before) continue;
                Assert.That(terrain.CanTravel(before, unit.Position), Is.True);
                Assert.That(model.CadejoBlinks.Any(blink => !blink.Cancelled && blink.From == before &&
                    blink.To == unit.Position && blink.ArrivedAt == model.Clock), Is.True);
                if (Mathf.Abs(unit.Position.y - unit.SpawnCenter.y) > .25f) sawDetour = true;
                if (unit.Position.x < FormationBattleModel.Width - 1 - .0001f) Assert.That(model.Phase, Is.EqualTo(FormationPhase.Exiting));
            }

            Assert.That(sawDetour, Is.True);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
            Assert.That(unit.Position.x, Is.EqualTo(FormationBattleModel.Width - 1).Within(.0001f));
            Assert.That(model.Retry(), Is.True);
            Assert.That(model.CadejoBlinks, Is.Empty);
            Assert.That(unit.Position, Is.EqualTo(unit.SpawnCenter));
        }
    }
}
