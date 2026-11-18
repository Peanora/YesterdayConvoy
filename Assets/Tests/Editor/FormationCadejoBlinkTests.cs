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
        public void CadejoWaitsThenMovesOnlyWhenTheBlinkLandsAndPauseFreezesWindup()
        {
            var model = StartSingleCadejo();
            var unit = model.Units.Single();
            var spawn = unit.Position;
            for (int i = 0; model.CadejoBlinks.Count == 0 && i < 120; i++)
            {
                model.Advance(.025f);
                Assert.That(unit.Position, Is.EqualTo(spawn));
                Assert.That(unit.IsMoving, Is.False);
            }

            var blink = model.CadejoBlinks.Single();
            Assert.That(blink.ActorId, Is.EqualTo(unit.Id));
            Assert.That(blink.From, Is.EqualTo(spawn));
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
            Assert.That(unit.IsMoving, Is.False);
            Assert.That(model.SkillCasts, Is.Empty);
            Assert.That(model.Hits, Is.Empty);
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
