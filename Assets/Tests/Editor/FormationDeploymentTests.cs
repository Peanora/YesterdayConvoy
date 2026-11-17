using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationDeploymentTests
    {
        static FormationRosterEntry Entry(int instance, string art, int col, int row) =>
            new FormationRosterEntry { Instance = instance, ArtId = art, Star = 1, Spawn = new Vector2(col, row) };

        static FormationBattleModel Roster(params FormationRosterEntry[] entries)
        {
            var model = new FormationBattleModel(20261117);
            Assert.That(model.ApplyRoster(entries, 10), Is.True);
            return model;
        }

        static FormationShopSession Session(int level, params string[] ids)
        {
            var session = new FormationShopSession(20261117);
            session.Shop.Preset(ids.Select(id => new ShopTrialModel.Piece { Id = id, Star = 1 }),
                Array.Empty<string>(), level: level, xp: 0, gold: 12);
            return session;
        }

        static FormationBattleModel Bind(FormationShopSession session)
        {
            var model = new FormationBattleModel(session.Seed); session.ApplyTo(model); return model;
        }

        static int Instance(FormationShopSession session, string id) => session.Shop.Bench.Single(piece => piece.Id == id).Instance;

        static string Board(FormationBattleModel model) => string.Join("|", model.Units.Select(unit =>
            unit.Instance + ":" + unit.ArtId + ":" + unit.Spawn.x + "," + unit.Spawn.y + ":" + unit.Position.x + "," + unit.Position.y));

        static string Snapshot(FormationShopSession session, FormationBattleModel model) => Board(model) + "#" +
            session.Shop.Gold + ":" + session.Shop.Level + ":" + session.Shop.XP + ":" +
            string.Join("|", session.Shop.Bench.Select(piece => piece.Instance + ":" + piece.Id + ":" + piece.Star + ":" +
                session.Seats.For(piece.Instance)?.Lineup + ":" + session.Seats.For(piece.Instance)?.Index)) + "#" +
            string.Join("|", session.Shop.Offers) + "#" + string.Join("|", session.Shop.Roles.Select(role => role.Id + ":" + session.Shop.Available(role.Id)));

        [TestCase(5, 6)]
        [TestCase(4, 7)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void CompleteFootprintMustFitBeforeEitherPreviewOrPlacementAccepts(int col, int row)
        {
            var model = Roster(Entry(1, "M28", 4, 6)); var unit = model.Units.Single();
            Assert.That(unit.Footprint, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(unit.Position, Is.EqualTo(new Vector2(4.5f, 6.5f)));
            string before = Board(model);
            var preview = model.PreviewPlace(unit.Slot, col, row);
            Assert.That(preview.Cells, Is.EqualTo(new RectInt(col, row, 2, 2)));
            Assert.That(preview.Valid, Is.False); Assert.That(model.TryPlace(unit.Slot, col, row), Is.False);
            Assert.That(Board(model), Is.EqualTo(before));
        }

        [Test]
        public void ImportRejectsOverlappingAreasWithDifferentAnchorsWithoutReplacingExistingBodies()
        {
            var model = Roster(Entry(1, "M28", 0, 0)); var original = model.Units.Single();
            Assert.That(model.ApplyRoster(new[] { Entry(2, "M28", 2, 2), Entry(3, "M07", 3, 3) }, 10), Is.False);
            var invalid = Entry(4, "M07", 1, 1); invalid.Spawn = new Vector2(float.NaN, 1);
            Assert.That(model.ApplyRoster(new[] { invalid }, 10), Is.False);
            Assert.That(model.Units.Single(), Is.SameAs(original));
            Assert.That(original.Spawn, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void EveryExistingCatalogRoleProjectsIntoAValidBodyWithItsOwnFootprint()
        {
            var model = new FormationBattleModel();
            foreach (var definition in FormationUnitCatalog.All)
            {
                Assert.That(model.ApplyRoster(new[] { Entry(1, definition.ArtId, 0, 0) }, 1), Is.True, definition.ArtId);
                var unit = model.Units.Single();
                Assert.That(unit.ArtId, Is.EqualTo(definition.ArtId));
                Assert.That(unit.Footprint, Is.EqualTo(definition.Footprint));
                Assert.That(unit.Position, Is.EqualTo(unit.SpawnCenter));
                Assert.That(unit.MaxHealth, Is.GreaterThan(0));
                Assert.That(unit.Interval, Is.GreaterThan(0));
            }
        }

        [Test]
        public void TwoDifferentCharactersCanShareTheSameSimplifiedCombatTemplate()
        {
            var model = Roster(Entry(1, "M20", 0, 0), Entry(2, "M28", 3, 0));
            Assert.That(model.Units.Select(unit => unit.Kind).Distinct().Count(), Is.EqualTo(1));
            Assert.That(model.Units.Select(unit => unit.ArtId).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void UnequalFootprintsSwapBothWaysAsOneValidatedMove()
        {
            var model = Roster(Entry(1, "M28", 0, 0), Entry(2, "M07", 4, 4));
            var large = model.Units[0]; var small = model.Units[1];
            var preview = model.PreviewPlace(large.Slot, 4, 4);
            Assert.That(preview.Valid, Is.True); Assert.That(preview.SwapInstance, Is.EqualTo(small.Instance));
            Assert.That(preview.SwapCells, Is.EqualTo(new RectInt(0, 0, 1, 1)));
            Assert.That(model.TryPlace(large.Slot, 4, 4), Is.True);
            Assert.That(large.Position, Is.EqualTo(new Vector2(4.5f, 4.5f)));
            Assert.That(small.Spawn, Is.EqualTo(Vector2.zero));
            Assert.That(model.PreviewPlace(small.Slot, 4, 4).Valid, Is.True);
            Assert.That(model.TryPlace(small.Slot, 4, 4), Is.True);
            Assert.That(large.Spawn, Is.EqualTo(Vector2.zero));
            Assert.That(small.Spawn, Is.EqualTo(new Vector2(4, 4)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SwapRejectsEitherSideCrossingTheBoundaryAndKeepsBothOriginalPositions(bool moveLarge)
        {
            var model = Roster(Entry(1, "M28", 2, 2), Entry(2, "M07", 5, 7));
            string before = Board(model);
            Assert.That(model.TryPlace(moveLarge ? 0 : 1, moveLarge ? 5 : 2, moveLarge ? 7 : 2), Is.False);
            Assert.That(Board(model), Is.EqualTo(before));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SwapRejectsAThirdCharacterUnderEitherResultingArea(bool moveLarge)
        {
            var model = Roster(Entry(1, "M28", 2, 2), Entry(2, "M07", 0, 0), Entry(3, "M08", 1, 0));
            string before = Board(model);
            Assert.That(model.TryPlace(moveLarge ? 0 : 1, moveLarge ? 0 : 2, moveLarge ? 0 : 2), Is.False);
            Assert.That(Board(model), Is.EqualTo(before));
        }

        [Test]
        public void LargeBodyRevivesAtTheSavedFootprintCentre()
        {
            var model = Roster(Entry(1, "M28", 1, 2)); var unit = model.Units.Single();
            model.Defenders.RemoveRange(1, model.Defenders.Count - 1); var guard = model.Defenders[0];
            guard.Position = unit.SpawnCenter; guard.MaxHealth = guard.Health = 1000000;
            guard.Attack = 1000000; guard.Range = 20; guard.Interval = 1; guard.Healer = false;
            Assert.That(model.StartBattle(), Is.True); model.Advance(.025f);
            Assert.That(unit.Alive, Is.False); Assert.That(unit.Deaths, Is.EqualTo(1));
            guard.Alive = false; guard.Health = 0;
            model.Advance((float)model.ReviveSeconds);
            Assert.That(unit.Alive, Is.True); Assert.That(unit.Position, Is.EqualTo(new Vector2(1.5f, 2.5f)));
            Assert.That(unit.Spawn, Is.EqualTo(new Vector2(1, 2))); Assert.That(unit.Health, Is.EqualTo(unit.MaxHealth));
        }

        [Test]
        public void InvalidReserveDropDoesNotJoinLineupOrChangeAnyShopTransaction()
        {
            var session = Session(3, "CARD_M07", "CARD_M28"); var model = Bind(session);
            int instance = Instance(session, "CARD_M28"); string before = Snapshot(session, model);
            var preview = session.PreviewDeploy(instance, new Vector2Int(5, 7));
            Assert.That(preview.Valid, Is.False); Assert.That(preview.Cells, Is.EqualTo(new RectInt(5, 7, 2, 2)));
            Assert.That(Snapshot(session, model), Is.EqualTo(before), "Preview cannot commit a seat or a new spawn.");
            Assert.That(session.TryDeploy(instance, new Vector2Int(5, 7)).Success, Is.False);
            Assert.That(Snapshot(session, model), Is.EqualTo(before)); Assert.That(session.Seats.InLineup(instance), Is.False);
            Assert.That(session.PreviewDeploy(instance, new Vector2Int(0, 0)).Valid, Is.True);
            Assert.That(session.TryDeploy(instance, new Vector2Int(0, 0)).Success, Is.True);
            session.ApplyTo(model);
            Assert.That(model.Units.Single(unit => unit.Instance == instance).Spawn, Is.EqualTo(Vector2.zero));
            Assert.That(session.Shop.Gold, Is.EqualTo(12)); session.Shop.Validate();
        }

        [Test]
        public void CommitRechecksTheCurrentBoardWhenAFormerlyValidPreviewBecomesBlocked()
        {
            var session = Session(3, "CARD_M07", "CARD_M28"); var model = Bind(session);
            int small = Instance(session, "CARD_M07"), large = Instance(session, "CARD_M28");
            Assert.That(session.TryDeploy(small, new Vector2Int(5, 7)).Success, Is.True);
            Assert.That(session.TryDeploy(large, Vector2Int.zero).Success, Is.True);
            Assert.That(session.PreviewDeploy(small, new Vector2Int(2, 4)).Valid, Is.True);
            Assert.That(session.TryDeploy(large, new Vector2Int(2, 4)).Success, Is.True);
            string before = Snapshot(session, model);
            Assert.That(session.TryDeploy(small, new Vector2Int(2, 4)).Success, Is.False,
                "The now-displaced 2x2 body would not fit at the 1x1 body's old edge cell.");
            Assert.That(Snapshot(session, model), Is.EqualTo(before));
        }

        [Test]
        public void SlotMoveFailsWithoutChangingSeatsWhenNoCompleteLargeFootprintFits()
        {
            var session = Session(7, "CARD_M07", "CARD_M08", "CARD_M10", "CARD_M20", "CARD_M28", "CARD_M34", "CARD_M36");
            var model = Bind(session);
            Assert.That(session.Move(Instance(session, "CARD_M20"), true, 3).Success, Is.True);
            Assert.That(session.Move(Instance(session, "CARD_M28"), true, 4).Success, Is.True);
            Assert.That(session.Move(Instance(session, "CARD_M34"), true, 5).Success, Is.True);
            var anchors = new Dictionary<string, Vector2>
            {
                { "M20", new Vector2(1, 1) }, { "M28", new Vector2(3, 5) }, { "M34", new Vector2(3, 2) },
                { "M07", new Vector2(4, 1) }, { "M08", new Vector2(1, 4) }, { "M10", new Vector2(1, 6) }
            };
            Assert.That(model.ApplyRoster(model.Units.Select(unit => new FormationRosterEntry
            { Instance = unit.Instance, ArtId = unit.ArtId, Star = unit.Star, Spawn = anchors[unit.ArtId] }).ToArray(), 7), Is.True);
            session.SavePositions(model); string before = Snapshot(session, model);
            Assert.That(session.Move(Instance(session, "CARD_M36"), true, 6).Success, Is.False,
                "These six bodies leave empty cells but intersect every possible 2x2 placement.");
            Assert.That(Snapshot(session, model), Is.EqualTo(before));
        }

        [Test]
        public void LargeFootprintAnchorAndCentrePersistAcrossTheNextRoundAndRebinding()
        {
            var session = Session(3, "CARD_M07", "CARD_M28"); var model = Bind(session);
            int instance = Instance(session, "CARD_M28");
            Assert.That(session.TryDeploy(instance, new Vector2Int(0, 4)).Success, Is.True);
            Assert.That(model.StartBattle(), Is.True);
            foreach (var guard in model.Defenders) { guard.Alive = false; guard.Health = 0; }
            model.Units.Single(unit => unit.Instance == instance).Position = new Vector2(13, 0);
            model.Advance(.025f); Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
            Assert.That(session.AdvanceRound(), Is.True); model = Bind(session);
            var large = model.Units.Single(unit => unit.Instance == instance);
            Assert.That(large.Spawn, Is.EqualTo(new Vector2(0, 4)));
            Assert.That(large.Position, Is.EqualTo(new Vector2(.5f, 4.5f)));
            Assert.That(large.DeploymentCells, Is.EqualTo(new RectInt(0, 4, 2, 2)));
        }

        [Test]
        public void GenericThreeByThreePlanningSupportsFutureLargeRolesWithoutInventingACatalogCard()
        {
            var occupied = new[] { new RectInt(0, 0, 3, 3), new RectInt(4, 5, 1, 1) };
            var preview = FormationDeployment.Preview(occupied, 0, new Vector2Int(3, 5), new[] { 1, 2 });
            Assert.That(preview.Valid, Is.True); Assert.That(preview.Cells, Is.EqualTo(new RectInt(3, 5, 3, 3)));
            Assert.That(preview.SwapCells, Is.EqualTo(new RectInt(0, 0, 1, 1)));
            Assert.That(FormationDeployment.Center(new Vector2(3, 5), new Vector2Int(3, 3)), Is.EqualTo(new Vector2(4, 6)));
            Assert.That(FormationDeployment.Preview(occupied, 0, new Vector2Int(4, 5)).Valid, Is.False);
            Assert.That(occupied[0], Is.EqualTo(new RectInt(0, 0, 3, 3)), "Planning never modifies the original occupied areas.");
        }
    }
}
