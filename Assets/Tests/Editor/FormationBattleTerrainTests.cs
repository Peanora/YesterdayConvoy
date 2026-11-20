using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationBattleTerrainTests
    {
        static FormationBattleTerrainRegion[] Regions() => new[]
        {
            new FormationBattleTerrainRegion(new RectInt(FormationBattleModel.Width - 5, 1, 2, 2), .48f, true),
            new FormationBattleTerrainRegion(new RectInt(FormationBattleModel.Width - 4, FormationBattleModel.Height - 3, 2, 2), .48f, true),
            new FormationBattleTerrainRegion(new RectInt(FormationBattleModel.Width - 10, 1, 2, 1), .36f, false),
            new FormationBattleTerrainRegion(new RectInt(FormationBattleModel.Width - 7, 4, 2, 1), .36f, false)
        };

        static FormationBattleModel SoloDog()
        {
            var model = new FormationBattleModel(20261117, new FormationBattleTerrainLayout(Regions()));
            for (int i = 0; i < 4; i++) if (i != 1) Assert.That(model.ToggleSlot(i), Is.True);
            Assert.That(model.TryPlace(1, FormationBattleModel.DeploymentColumns - 1, 2), Is.True);
            model.LimitSeconds = 45;
            return model;
        }

        static FormationDefender SingleHarmlessGuard(FormationBattleModel model, Vector2 position, float health)
        {
            var guard = model.Defenders[0];
            model.Defenders.RemoveRange(1, model.Defenders.Count - 1);
            guard.Position = position; guard.MaxHealth = guard.Health = health;
            guard.Attack = guard.Armor = 0; guard.Range = .1f;
            guard.Healer = false; guard.SkillRequired = 1000000;
            return guard;
        }

        static string Snapshot(FormationBattleModel model) => string.Join("|", model.Defenders.Select(d =>
            d.ArtId + ":" + d.Position.x + "," + d.Position.y + ":" + d.MaxHealth + ":" + d.Attack));

        [TestCase(FormationBattleModel.Width - 5, 1)]
        [TestCase(FormationBattleModel.Width - 4, 1)]
        [TestCase(FormationBattleModel.Width - 5, 2)]
        [TestCase(FormationBattleModel.Width - 4, 2)]
        [TestCase(FormationBattleModel.Width - 4, FormationBattleModel.Height - 3)]
        [TestCase(FormationBattleModel.Width - 3, FormationBattleModel.Height - 3)]
        [TestCase(FormationBattleModel.Width - 4, FormationBattleModel.Height - 2)]
        [TestCase(FormationBattleModel.Width - 3, FormationBattleModel.Height - 2)]
        public void ShortRangeMeleeReachesEveryPlatformSeatAndKillsWithoutClimbing(int col, int row)
        {
            var model = SoloDog();
            var guard = SingleHarmlessGuard(model, new Vector2(col, row), 100);
            var dog = model.Units[1];
            Assert.That(dog.Range, Is.EqualTo(.8f));
            Assert.That(model.StartBattle(), Is.True);
            for (int step = 0; step < 1200 && guard.Alive; step++)
            {
                Vector2 before = dog.Position;
                model.Advance(.025f);
                Assert.That(model.Terrain.CanTravel(before, dog.Position), Is.True,
                    "A body must route around the platform and cover, including the final attack approach.");
                Assert.That(model.Terrain.HeightAt(dog.Position), Is.Zero, "The melee body stays on the floor.");
            }
            Assert.That(guard.Alive, Is.False, "Stopping at an adjacent cell centre leaves 0.8-range melee unable to attack.");
            Assert.That(model.Hits.Any(h => h.FromId == dog.Id && h.ToId == guard.Id &&
                !h.Heal && Vector2.Distance(h.From, h.To) <= dog.Range + .0001f), Is.True);
        }

        [Test]
        public void CrowdedApproachCannotPushBodiesThroughCoverOrPlatform()
        {
            var model = new FormationBattleModel(17, new FormationBattleTerrainLayout(Regions()));
            SingleHarmlessGuard(model, new Vector2(FormationBattleModel.Width - 4, FormationBattleModel.Height - 3), 1000000);
            Assert.That(model.StartBattle(), Is.True);
            var fighters = model.Units.Where(u => u.Enabled).ToArray();
            for (int step = 0; step < 1000; step++)
            {
                var before = fighters.Select(u => u.Position).ToArray();
                model.Advance(.025f);
                for (int i = 0; i < fighters.Length; i++)
                    Assert.That(model.Terrain.CanTravel(before[i], fighters[i].Position), Is.True,
                        "Local spacing must not override the static obstacle contour.");
            }
            Assert.That(fighters.All(f => f.DamageDone > 0), Is.True,
                "Every fighter, including the short-range bodies, must still reach combat in the crowd.");
        }

        [Test]
        public void ClearedBoardRoutesAroundBothObstacleTypesBeforePhysicallyEnteringBlueGate()
        {
            var model = SoloDog(); var dog = model.Units[1];
            Assert.That(model.StartBattle(), Is.True);
            dog.Position = new Vector2(FormationBattleModel.DeploymentColumns + 1, 1);
            foreach (var guard in model.Defenders) { guard.Alive = false; guard.Health = 0; }
            bool sawDetour = false;
            for (int step = 0; step < 1000 && model.Phase != FormationPhase.Won; step++)
            {
                Vector2 before = dog.Position;
                model.Advance(.025f);
                Assert.That(model.Terrain.CanTravel(before, dog.Position), Is.True);
                sawDetour |= Mathf.Abs(dog.Position.y - 1) > .2f;
                if (dog.Position.x < FormationBattleModel.Width - 1 - .0001f)
                    Assert.That(model.Phase, Is.Not.EqualTo(FormationPhase.Won));
            }
            Assert.That(sawDetour, Is.True, "The direct line crosses the saved cover and high platform.");
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
            Assert.That(dog.Position.x, Is.EqualTo(FormationBattleModel.Width - 1).Within(.0001f));
        }

        [TestCase(3)]
        [TestCase(91)]
        [TestCase(20261117)]
        public void RandomDefendersUseLegalUniqueSeatsAndRetryTheSameBoard(int seed)
        {
            var model = new FormationBattleModel(seed, new FormationBattleTerrainLayout(Regions()));
            var replay = new FormationBattleModel(seed, new FormationBattleTerrainLayout(Regions()));
            for (int round = 1; round <= 5; round++)
            {
                Assert.That(model.Round, Is.EqualTo(round));
                Assert.That(Snapshot(model), Is.EqualTo(Snapshot(replay)));
                Assert.That(model.Defenders.Select(d => d.Position).Distinct().Count(), Is.EqualTo(model.Defenders.Count));
                Assert.That(model.Defenders.All(d => model.Terrain.CanPlaceDefender(d.Position)), Is.True);
                Assert.That(model.Defenders.Where(d => d.Kind < 6).All(d => model.Terrain.HeightAt(d.Position) == 0), Is.True);
                if (model.Defenders.Any(d => d.Kind >= 6))
                    Assert.That(model.Defenders.Any(d => model.Terrain.HeightAt(d.Position) > 0), Is.True);
                string beforeRetry = Snapshot(model);
                model.Retry();
                Assert.That(Snapshot(model), Is.EqualTo(beforeRetry));
                if (round == 5) break;
                FinishRound(model); FinishRound(replay);
                Assert.That(model.NextRound(), Is.True); Assert.That(replay.NextRound(), Is.True);
            }
        }

        static void FinishRound(FormationBattleModel model)
        {
            Assert.That(model.StartBattle(), Is.True);
            foreach (var guard in model.Defenders) { guard.Alive = false; guard.Health = 0; }
            model.Units.First(u => u.Enabled).Position = new Vector2(FormationBattleModel.Width - 1, 0);
            model.Advance(.025f);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void LayoutSnapshotsSavedRegionsWithoutAllowingAuthoringChangesToAlterCombat()
        {
            var source = Regions(); var layout = new FormationBattleTerrainLayout(source);
            source[0] = new FormationBattleTerrainRegion(new RectInt(14, 0, 1, 1), 1, false);
            Assert.That(layout.HeightAt(new Vector2(FormationBattleModel.Width - 5, 1)), Is.EqualTo(.48f));
            Assert.That(layout.CanPlaceDefender(new Vector2(FormationBattleModel.Width - 5, 1)), Is.True);
            Assert.That(layout.CanPlaceDefender(new Vector2(FormationBattleModel.Width - 10, 1)), Is.False);
            Assert.That(layout.HeightAt(new Vector2(5, 2)), Is.Zero);
        }

        [Test]
        public void RestoredTerrainKeepsEveryCampaignNodeGuardSeatLegalAndUnique()
        {
            var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(unit =>
                new ShopTrialModel.Role { Id = unit.CardId, Name = unit.Name, Cost = unit.Cost }), 71);
            shop.EnableFormationEconomy();
            shop.Preset(Array.Empty<ShopTrialModel.Piece>(), Array.Empty<string>(), 10, 0, 80);
            var run = new FormationCampaignRun(shop, 71);
            var model = new FormationBattleModel(71, new FormationBattleTerrainLayout(Regions()));
            int choices = 0;
            foreach (var node in FormationCampaignRules.Nodes)
            {
                while (run.HasBlockingChoice)
                {
                    Assert.That(++choices, Is.LessThan(100));
                    Assert.That(run.Choose(0).Success, Is.True);
                }
                Assert.That(run.CurrentNode.Id, Is.EqualTo(node.Id));
                model.BindCampaign(run);
                Assert.That(model.Defenders.Count, Is.EqualTo(node.DefenderCount), node.Id);
                Assert.That(model.Defenders.Select(d => d.Position).Distinct().Count(), Is.EqualTo(model.Defenders.Count), node.Id);
                Assert.That(model.Defenders.All(d => d.Position.x >= FormationBattleModel.DeploymentColumns + 2 &&
                    d.Position.x < FormationBattleModel.Width - 1 && d.Position.y >= 0 &&
                    d.Position.y < FormationBattleModel.Height && model.Terrain.CanPlaceDefender(d.Position)), Is.True, node.Id);
                foreach(var defender in model.Defenders)
                    Assert.That(System.IO.File.Exists(ArtSourcePaths.Resolve("DefenderArt/"+defender.ArtId+"/manifest.json")),
                        Is.True,node.Id+" · "+defender.Name+" · "+defender.ArtId);
                if (node == FormationCampaignRules.Nodes.Last()) break;
                if (!run.NodeSettled) run.SettleBattle(true, 1, 1, 0, false);
                FormationCampaignRunTests.CollectDrops(run);
                Assert.That(run.AdvanceNode(), Is.True);
            }
        }
    }
}
