using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    /// <summary>Controlled combat fixtures isolate mechanics; these are not natural shop-acquisition runs.</summary>
    public sealed class FormationCampaignCombatTests
    {
        static FormationBattleModel Fixture(out FormationCampaignRun run,params string[] ids)
        {
            var shop=new ShopTrialModel(FormationCampaignRules.AllUnits.Select(p=>new ShopTrialModel.Role{Id=p.CardId,Name=p.Name,Cost=p.Cost}),71);
            shop.EnableFormationEconomy();shop.Preset(Array.Empty<ShopTrialModel.Piece>(),Array.Empty<string>(),10,0,80);
            run=new FormationCampaignRun(shop,71);int safety=0;while(run.HasBlockingChoice){Assert.That(++safety,Is.LessThan(100));Assert.That(run.Choose(0).Success,Is.True);}
            shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=1}),Array.Empty<string>(),10,0,80);
            var model=new FormationBattleModel(71);model.BindCampaign(run);
            Assert.That(model.ApplyRoster(shop.Bench.Select((p,i)=>new FormationRosterEntry{Instance=p.Instance,Star=p.Star,ArtId=p.Id,Spawn=new Vector2((i%5)*2,(i/5)*3)}),10),Is.True);
            return model;
        }
        static FormationDefender Target(FormationBattleModel model)
        {
            model.Defenders.RemoveRange(1,model.Defenders.Count-1);var d=model.Defenders[0];
            d.Position=new Vector2(15,3);d.MaxHealth=100000000;d.Attack=0;d.Range=.01f;d.Armor=0;d.MagicResistance=0;
            return d;
        }
        static void Enter(FormationBattleModel model)
        {foreach(var u in model.Units){u.Position=new Vector2(14.5f,3);u.Health=u.MaxHealth;}}
        static void Advance(FormationBattleModel model,float seconds)
        {for(int i=0;i<Mathf.CeilToInt(seconds/.025f);i++)model.Advance(.025f);}

        static FormationBattleTerrainLayout SavedTerrain()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FormationBattleTools.ScenePath);
            try
            {
                var controller = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FormationBattleController>(true)).Single();
                Assert.That(controller.terrain, Is.Not.Null);
                return controller.terrain.BuildLayout();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [TestCase(71)]
        [TestCase(20261119)]
        public void AuthoredCampaignUsesAllNamedDefendersOnCompactLegalFormations(int seed)
        {
            var terrain = SavedTerrain();
            var run = NewDifficultyRun(seed);
            var expectedNames = new Dictionary<string, string>
            {
                ["D01"] = "米格鲁", ["D02"] = "卡缇", ["D03"] = "斑点", ["D04"] = "玫兰莎",
                ["D05"] = "月见夜", ["D06"] = "翎羽", ["D07"] = "克洛丝", ["D08"] = "史都华德",
                ["D09"] = "炎熔", ["D10"] = "芙蓉", ["D11"] = "安赛尔", ["D12"] = "梓兰",
                ["D21"] = "荒芜拉普兰德", ["D22"] = "余", ["D23"] = "Mon3tr", ["D24"] = "新约能天使",
                ["D25"] = "斩业星熊", ["D26"] = "凛御银灰", ["D27"] = "圣聆初雪", ["D28"] = "望",
                ["D29"] = "塞雷娅"
            };
            var north = new RectInt(15, 0, 5, 4);
            var south = new RectInt(15, 4, 5, 4);
            var seenNames = new HashSet<string>();
            var seenSides = new HashSet<bool>();

            foreach (var node in FormationCampaignRules.Nodes)
            {
                ReachNode(run, node.Id);
                var model = new FormationBattleModel(seed, terrain);
                model.BindCampaign(run);
                Assert.That(model.Defenders.Count, Is.EqualTo(node.DefenderCount), node.Id);
                Assert.That(model.LimitSeconds, Is.EqualTo(node.TimeLimit), node.Id);
                if (node.DefenderCount == 0) continue;

                var actorCells = model.Defenders.Select(defender => Vector2Int.RoundToInt(defender.Position)).ToArray();
                Assert.That(actorCells.Distinct().Count(), Is.EqualTo(actorCells.Length), node.Id);
                Assert.That(model.Defenders.Select(defender => defender.ArtId).Distinct().Count(), Is.EqualTo(actorCells.Length), node.Id);
                Assert.That(model.Defenders.All(defender => terrain.CanPlaceDefender(defender.Position)), Is.True, node.Id);
                bool inNorth = actorCells.All(north.Contains);
                Assert.That(inNorth || actorCells.All(south.Contains), Is.True, node.Id);
                seenSides.Add(inNorth);

                var reservedCells = model.Defenders.Where(defender => defender.SkillCell.HasValue)
                    .Select(defender => defender.SkillCell.Value).ToArray();
                int expectedReserved = model.Defenders.Count(defender => defender.ArtId == "D23" || defender.ArtId == "D28");
                Assert.That(reservedCells.Length, Is.EqualTo(expectedReserved), node.Id);
                Assert.That(reservedCells.Concat(actorCells).Distinct().Count(), Is.EqualTo(reservedCells.Length + actorCells.Length), node.Id);
                Assert.That(reservedCells.All(cell => (inNorth ? north : south).Contains(cell) && terrain.IsWalkable(cell)), Is.True, node.Id);
                foreach (var defender in model.Defenders)
                {
                    Assert.That(expectedNames.ContainsKey(defender.ArtId), Is.True, $"{node.Id}: {defender.ArtId}");
                    Assert.That(defender.Name, Is.EqualTo(expectedNames[defender.ArtId]), node.Id);
                    seenNames.Add(defender.ArtId);
                }
                if (node.Id == "3-7")
                {
                    var boss = model.Defenders.Single();
                    Assert.That(boss.ArtId, Is.EqualTo("D27"));
                    Assert.That(boss.IsSnowBoss, Is.True);
                    Assert.That(boss.SnowBossPhase, Is.EqualTo(1));
                    Assert.That(terrain.IsWalkable(Vector2Int.RoundToInt(boss.Position)), Is.True);
                }
                else
                    Assert.That(model.Defenders.Any(defender => defender.IsSnowBoss), Is.False, node.Id);

                var original = model.Defenders.Select(defender => (defender.ArtId, defender.Position, defender.SkillCell,
                    defender.MaxHealth, defender.Attack)).ToArray();
                Assert.That(model.Retry(), Is.True, node.Id);
                Assert.That(model.Defenders.Select(defender => (defender.ArtId, defender.Position, defender.SkillCell,
                    defender.MaxHealth, defender.Attack)).ToArray(), Is.EqualTo(original), node.Id);
                Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation), node.Id);
                Assert.That(model.Clock, Is.Zero, node.Id);
            }

            CollectionAssert.AreEquivalent(expectedNames.Keys, seenNames);
            CollectionAssert.AreEquivalent(new[] { false, true }, seenSides);
            Assert.That(FormationCampaignRules.DefenderByArtId("D30").Name, Is.EqualTo("玛恩纳"));
        }

        [TestCase("1-7")]
        [TestCase("2-5")]
        [TestCase("3-5")]
        public void EncounterTiersKeepTheirAuthoredFormationAndScaleHealthAndAttack(string nodeId)
        {
            const int seed = 71;
            var run = NewDifficultyRun(seed);
            ReachNode(run, nodeId);
            var model = new FormationBattleModel(seed, SavedTerrain());
            model.BindCampaign(run);
            var original = model.Defenders.Select(defender => (defender.ArtId, defender.Position, defender.SkillCell,
                defender.MaxHealth, defender.Attack)).ToArray();
            (float Health, float Attack)[] scales = { (1f, 1f), (1.7f, 1.08f), (3f, 1.18f), (5.5f, 1.3f) };
            for (int tier = 2; tier <= scales.Length; tier++)
            {
                run.CurrentEncounterTier = tier;
                model.BindCampaign(run);
                Assert.That(model.LimitSeconds, Is.EqualTo(90));
                Assert.That(model.Defenders.Count, Is.EqualTo(original.Length));
                for (int index = 0; index < original.Length; index++)
                {
                    var expected = original[index];
                    var actual = model.Defenders[index];
                    Assert.That((actual.ArtId, actual.Position, actual.SkillCell), Is.EqualTo((expected.ArtId, expected.Position, expected.SkillCell)));
                    Assert.That(actual.MaxHealth, Is.EqualTo(expected.MaxHealth * scales[tier - 1].Health).Within(1));
                    Assert.That(actual.Attack, Is.EqualTo(expected.Attack * scales[tier - 1].Attack).Within(1));
                }

                var defense = model.Defenders.Select(defender => (defender.ArtId, defender.Position, defender.SkillCell,
                    defender.MaxHealth, defender.Attack)).ToArray();
                Assert.That(model.Retry(), Is.True);
                Assert.That(model.Defenders.Select(defender => (defender.ArtId, defender.Position, defender.SkillCell,
                    defender.MaxHealth, defender.Attack)).ToArray(), Is.EqualTo(defense));
            }
        }

        [Test]
        public void SceneTerrainEncounterTierFourPressuresTheSameTeamAndSettlesActualProgress()
        {
            const string nodeId = "3-5";
            const int seed = 71;
            var terrain = SavedTerrain();
            var first = ReportHighCostLineup(nodeId, seed, terrain, 1);
            var fourth = ReportHighCostLineup(nodeId, seed, terrain, 4);
            Assert.That(first.Phase, Is.EqualTo(FormationPhase.Won));
            Assert.That(fourth.Phase == FormationPhase.Lost || fourth.Clock > first.Clock, Is.True);

            foreach (var model in new[] { first, fourth })
            {
                var run = model.Campaign;
                int tier = run.CurrentEncounterTier;
                Assert.That(run.SettleBattle(model.Phase == FormationPhase.Won, model.EncounterProgress,
                    model.InitialDefenderCount, model.RemainingDefenders, model.BossAlive), Is.Not.Null);
                var colors = run.PendingBalls.Where(drop => drop.Node == nodeId && drop.Component == null)
                    .Select(drop => drop.Color).ToArray();
                string[] expectedColors = model.EncounterProgress <= .25f ? Array.Empty<string>()
                    : tier == 4 ? new[] { "金", "金", "彩" } : new[] { "蓝" };
                if (expectedColors.Length > 0 && run.PreplanId == "P14") expectedColors = expectedColors.Append("蓝").ToArray();
                CollectionAssert.AreEquivalent(expectedColors, colors);
                TestContext.WriteLine($"ENCOUNTER tier={tier} progress={model.EncounterProgress:P2} reward={string.Join(",", colors)}");
                Assert.That(run.SettleBattle(model.Phase == FormationPhase.Won, model.EncounterProgress,
                    model.InitialDefenderCount, model.RemainingDefenders, model.BossAlive), Is.Null);
            }
        }

        [TestCase("1-3", 71)]
        [TestCase("1-3", 20261119)]
        [TestCase("1-4", 71)]
        [TestCase("1-4", 20261119)]
        [TestCase("1-6", 71)]
        [TestCase("1-6", 20261119)]
        [TestCase("1-9", 71)]
        [TestCase("1-9", 20261119)]
        [TestCase("2-4", 71)]
        [TestCase("2-4", 20261119)]
        [TestCase("3-7", 71)]
        [TestCase("3-7", 20261119)]
        public void SceneTerrainDifficultyLineupsReportBattleOutcomes(string nodeId, int seed)
        {
            // Earlier nodes advance only to select the real defense. These are combat budgets, not shop rollouts.
            var terrain = SavedTerrain();
            bool alternate = seed != 71;
            if (nodeId.StartsWith("1-", StringComparison.Ordinal))
            {
                var frozen = alternate ? new[] { "M21", "M11", "M17" } : new[] { "B22", "B01", "M03" };
                ReportDifficultyOutcome(nodeId, seed, terrain, "frozen", frozen, new[] { 1, 1, 1 });
            }

            switch (nodeId)
            {
                case "1-3":
                    ReportDifficultyOutcome(nodeId, seed, terrain, "normal",
                        alternate ? new[] { "M21", "M11", "M17", "B01" } : new[] { "B22", "B01", "M03" },
                        alternate ? new[] { 1, 2, 1, 1 } : new[] { 1, 1, 1 }, (0, "DD"), (1, "AF"));
                    break;
                case "1-4":
                    ReportDifficultyOutcome(nodeId, seed, terrain, "normal",
                        alternate ? new[] { "M27", "M11", "M17", "B01" } : new[] { "B22", "B01", "M03", "M21" },
                        new[] { 1, 2, 1, 1 }, (0, "DD"), (1, "AF"));
                    break;
                case "1-6":
                    ReportDifficultyOutcome(nodeId, seed, terrain, "normal",
                        alternate ? new[] { "M29", "M11", "M17", "B01", "M12" } : new[] { "M27", "B01", "M21", "M38", "M42" },
                        new[] { 2, 2, 1, 1, 1 }, (0, "DD"), (1, "AF"), (4, "CF"));
                    if(!alternate)
                        ReportDifficultyOutcome(nodeId, seed, terrain, "quality-four",
                            new[] { "M27", "B01", "M38", "M42" }, new[] { 2, 2, 1, 1 }, (0, "DD"), (1, "AF"), (3, "CF"));
                    break;
                case "1-9":
                    ReportDifficultyOutcome(nodeId, seed, terrain, "normal",
                        alternate ? new[] { "M27", "M12", "M11", "M13", "M25" } : new[] { "M27", "M42", "B01", "M38", "M25" },
                        new[] { 2, 2, 2, 1, 1 }, (0, "DD"), (1, "AF"), (1, "CF"));
                    break;
                case "2-4":
                    ReportDifficultyOutcome(nodeId, seed, terrain, "normal",
                        alternate ? new[] { "M18", "B23", "M12", "M13", "M25", "M14" } : new[] { "M27", "M42", "B01", "M38", "M25", "M24" },
                        new[] { 2, 2, 2, 2, 2, 1 }, (0, "DD"), (1, "AF"), (1, "CF"), (2, "AC"));
                    break;
                case "3-7":
                    if (alternate)
                    {
                        ReportDifficultyOutcome(nodeId, seed, terrain, "low-cost-carry",
                            new[] { "M20", "B22", "B23", "B25", "B13", "M25", "M32", "B11" },
                            new[] { 2, 3, 2, 2, 2, 2, 2, 2 }, (0, "DD"), (0, "DE"), (1, "徽章:同频"),
                            (1, "AC"), (1, "CF"), (2, "AA"), (2, "AF"), (4, "CC"), (5, "CE"), (7, "CC"));
                        ReportDifficultyOutcome(nodeId, seed, terrain, "low-cost-mature",
                            new[] { "M20", "B22", "B23", "B25", "B13", "M25", "M32", "B11" },
                            new[] { 2, 3, 2, 2, 2, 2, 2, 2 }, (0, "DD"), (0, "DE"), (1, "徽章:同频"),
                            (1, "AC"), (1, "CF"), (2, "AA"), (2, "AF"), (4, "CC"), (5, "徽章:连击"), (5, "CE"), (7, "CC"));
                    }
                    else
                    {
                        ReportHighCostLineup(nodeId, seed, terrain, 1);
                    }
                    break;
            }
        }

        static FormationBattleModel ReportHighCostLineup(string nodeId, int seed, FormationBattleTerrainLayout terrain, int tier) =>
            ReportDifficultyOutcome(nodeId, seed, terrain, "high-cost-carry",
                new[] { "M20", "WIS", "M28", "B08", "B13", "B11", "M25", "M32" },
                new[] { 2, 2, 2, 2, 2, 2, 2, 2 }, tier, (0, "DD"), (0, "DE"), (1, "AA"),
                (1, "AF"), (1, "CF"), (2, "CD"), (3, "AF"), (3, "CF"), (4, "CC"), (5, "CC"), (6, "CE"));

        static FormationBattleModel ReportDifficultyOutcome(string nodeId, int seed, FormationBattleTerrainLayout terrain, string lineup,
            string[] ids, int[] stars, params (int Owner, string Item)[] equipment) =>
            ReportDifficultyOutcome(nodeId, seed, terrain, lineup, ids, stars, 1, equipment);

        static FormationBattleModel ReportDifficultyOutcome(string nodeId, int seed, FormationBattleTerrainLayout terrain, string lineup,
            string[] ids, int[] stars, int encounterTier, params (int Owner, string Item)[] equipment)
        {
            var run = NewDifficultyRun(seed);
            ReachNode(run, nodeId);
            run.CurrentEncounterTier = encounterTier;
            var shop = run.Shop;
            shop.Preset(ids.Select((id, index) => new ShopTrialModel.Piece
            {
                Id = FormationCampaignRules.Find(id).CardId, Star = stars[index]
            }), Array.Empty<string>(), ids.Length, 0, 0);
            var pieces = shop.Bench.ToArray();
            Assert.That(shop.SetLineup(pieces.Select(piece => piece.Id), pieces.Select(piece => piece.Instance)), Is.True);
            run.MainCarryInstance = pieces[1].Instance;
            if (lineup == "low-cost-carry" || lineup == "low-cost-mature")
            {
                run.ResonanceLeaderInstance = pieces[1].Instance;
                run.ResonancePartnerInstance = pieces[4].Instance;
            }
            foreach (var (owner, item) in equipment)
                Assert.That(run.Equip(run.AddItem(item).Instance, pieces[owner].Instance).Success, Is.True, item);

            var model = new FormationBattleModel(seed, terrain);
            int firstColumn = FormationBattleModel.DeploymentColumns;
            var anchors = new[]
            {
                new Vector2Int(firstColumn - 2, 2), new Vector2Int(firstColumn - 4, 2),
                new Vector2Int(firstColumn - 2, 4), new Vector2Int(firstColumn - 4, 4),
                new Vector2Int(firstColumn - 6, 2), new Vector2Int(firstColumn - 6, 4),
                new Vector2Int(firstColumn - 5, 6), new Vector2Int(firstColumn - 3, 6)
            };
            Assert.That(model.ApplyRoster(pieces.Select((piece, index) => new FormationRosterEntry
            {
                Instance = piece.Instance, ArtId = piece.Id, Star = piece.Star, Spawn = anchors[index]
            }), ids.Length), Is.True);
            model.BindCampaign(run);
            if (lineup == "low-cost-carry" || lineup == "low-cost-mature")
            {
                Assert.That(model.SynergyCounts["连击"], Is.EqualTo(lineup == "low-cost-mature" ? 6 : 5));
                Assert.That(model.ActiveSynergies["连击"], Is.EqualTo(lineup == "low-cost-mature" ? 6 : 4));
                Assert.That(model.ActiveSynergies["同频"], Is.EqualTo(5));
            }
            float initialHealth = model.Defenders.Sum(defender => defender.MaxHealth);
            Assert.That(model.StartBattle(), Is.True);
            double? clearAt = null;
            const float step = .025f;
            int maxSteps = Mathf.CeilToInt((float)(model.LimitSeconds + 1) / step);
            for (int tick = 0; tick < maxSteps && (model.Phase == FormationPhase.Fighting || model.Phase == FormationPhase.Exiting); tick++)
            {
                model.Advance(step);
                if (clearAt == null && model.RemainingDefenders == 0) clearAt = model.Clock;
            }
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won).Or.EqualTo(FormationPhase.Lost));
            TestContext.WriteLine($"DIFFICULTY node={nodeId} seed={seed} lineup={lineup} phase={model.Phase} tier={encounterTier} " +
                $"end={model.Clock:0.000} clear={clearAt?.ToString("0.000") ?? "-"} initialHP={initialHealth:0} " +
                $"remaining={model.RemainingDefenders} progress={model.EncounterProgress:P2} deaths={model.Units.Sum(unit => unit.Deaths)}");
            TestContext.WriteLine(string.Join("; ", model.Units.Select(unit =>
                $"{unit.Name} {unit.Star}★ damage={unit.DamageDone:0} deaths={unit.Deaths}")));
            TestContext.WriteLine("REMAINING " + string.Join("; ", model.Defenders.Where(defender => defender.Alive)
                .Select(defender => $"{defender.Name}:{defender.Health:0}/{defender.MaxHealth:0}@{defender.Position}")));
            if (lineup == "frozen")
            {
                var expected = nodeId == "1-3" && seed == 20261119 ? FormationPhase.Won : FormationPhase.Lost;
                Assert.That(model.Phase, Is.EqualTo(expected), nodeId);
            }
            if (lineup == "normal") Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won), nodeId);
            return model;
        }

        static FormationCampaignRun NewDifficultyRun(int seed)
        {
            var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(unit =>
                new ShopTrialModel.Role { Id = unit.CardId, Name = unit.Name, Cost = unit.Cost }), seed);
            shop.EnableFormationEconomy();
            var run = new FormationCampaignRun(shop, seed);
            while (run.HasBlockingChoice) Assert.That(run.Choose(0).Success, Is.True);
            return run;
        }

        [Test]
        public void CampaignUsesApprovedPanelAndNaturalRecoveryInsteadOfSixTemplates()
        {
            var model=Fixture(out var run,"M25","M14");
            Assert.That(model.Units[0].MaxHealth,Is.EqualTo(700));Assert.That(model.Units[0].Attack,Is.EqualTo(90));Assert.That(model.Units[0].SkillRequired,Is.EqualTo(18));
            Target(model);Assert.That(model.StartBattle(),Is.True);
            float charge=model.Units[0].SkillCharge;model.Paused=true;Advance(model,2);Assert.That(model.Units[0].SkillCharge,Is.EqualTo(charge));
            model.Paused=false;Advance(model,2);Assert.That(model.Units[0].SkillCharge,Is.EqualTo(charge+2).Within(.001));
        }
        [TestCase("SLUG")][TestCase("M03")][TestCase("M11")][TestCase("M17")][TestCase("M21")][TestCase("M35")]
        [TestCase("B01")][TestCase("B22")][TestCase("M12")][TestCase("M13")][TestCase("M16")][TestCase("M25")]
        [TestCase("M27")][TestCase("M29")][TestCase("M30")][TestCase("M33")][TestCase("M38")][TestCase("B03")]
        [TestCase("B05")][TestCase("M18")][TestCase("M24")][TestCase("M42")][TestCase("M31")][TestCase("M34")]
        [TestCase("TAL")][TestCase("B09")][TestCase("B14")][TestCase("B23")][TestCase("M14")][TestCase("M28")]
        [TestCase("M32")][TestCase("M36")][TestCase("B04")][TestCase("B07")][TestCase("B15")][TestCase("B24")]
        [TestCase("B25")][TestCase("B26")][TestCase("M20")][TestCase("WIS")][TestCase("B08")][TestCase("B10")]
        [TestCase("B11")][TestCase("B12")][TestCase("B13")][TestCase("B21")]
        public void EveryApprovedRoleDispatchesItsNativeSkillOrExplicitPassive(string id)
        {
            var model=Fixture(out var run,id);Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);
            var u=model.Units.Single();u.SkillCharge=u.SkillRequired;Advance(model,6);
            Assert.That(float.IsNaN(u.Health)||float.IsNaN(u.SkillCharge),Is.False);
            if(u.SkillRequired>0)Assert.That(u.LastSkillAt,Is.GreaterThanOrEqualTo(0),id);
            else Assert.That(FormationCampaignRules.Find(id).Recovery,Is.EqualTo("被动"));
        }

        [Test]
        public void HostSoldierRegeneratesSilentlyAndReportsTheActiveHealOnce()
        {
            var model=Fixture(out _,"M21");
            Target(model);
            Assert.That(model.StartBattle(),Is.True);
            var soldier=model.Units.Single();
            soldier.Health=soldier.MaxHealth*.5f;
            float initialHealth=soldier.Health;
            Advance(model,1);
            float expectedRegeneration=soldier.MaxHealth*.02f;
            Assert.That(soldier.Health-initialHealth,Is.EqualTo(expectedRegeneration).Within(.01f));
            Assert.That(soldier.HealingDone,Is.EqualTo(expectedRegeneration).Within(.01f));
            Assert.That(model.Hits.Any(hit=>hit.Heal&&hit.FromId==soldier.Id),Is.False);

            Enter(model);
            soldier.Health=initialHealth;
            soldier.SkillCharge=soldier.SkillRequired;
            Advance(model,1.5f);
            var activeHeal=model.Hits.Single(hit=>hit.Heal&&hit.FromId==soldier.Id);
            Assert.That(activeHeal.Skill,Is.True);
            var skillHit=model.Hits.Single(hit=>hit.Skill&&!hit.Heal&&hit.FromId==soldier.Id);
            Assert.That(activeHeal.Amount,Is.EqualTo(skillHit.Amount*.25f).Within(.01f));
        }
        [Test]
        public void RipperSkillDealsFourIndependentNativeSegmentsAndNeverRecursesOnDerivedDamage()
        {
            var model=Fixture(out var run,"B22");Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);
            var u=model.Units.Single();u.SkillCharge=u.SkillRequired;Advance(model,1.05f);
            Assert.That(model.Hits.Count(h=>h.FromId==u.Id&&h.Skill&&!h.Heal),Is.EqualTo(4));
            Assert.That(u.SkillCharge,Is.LessThan(u.SkillRequired));
        }
        [Test]
        public void AttackRecoveryWaitsForCompletedAutosAndIgnoresNaturalTime()
        {
            var model=Fixture(out var run,"M11");Target(model);Assert.That(model.StartBattle(),Is.True);
            var u=model.Units.Single();Advance(model,1);Assert.That(u.SkillCharge,Is.Zero);
            Enter(model);Advance(model,.025f);Assert.That(u.SkillCharge,Is.EqualTo(1));
        }
        [Test]
        public void EncounterProgressKeepsHistoricalDamageAfterDefenderHealing()
        {
            var model=Fixture(out var run,"M11");var d=Target(model);d.MaxHealth=100;Assert.That(model.StartBattle(),Is.True);Enter(model);Advance(model,.025f);
            float progress=model.EncounterProgress;Assert.That(progress,Is.GreaterThan(0));d.Health=d.MaxHealth;
            model.Paused=true;Advance(model,1);Assert.That(model.EncounterProgress,Is.EqualTo(progress));
        }
        [Test]
        public void NativePhaseReturnsBeforeModeRevivalAndRetainsCharge()
        {
            var model=Fixture(out var run,"M30");var d=Target(model);d.Attack=100000;d.Range=2;Assert.That(model.StartBattle(),Is.True);Enter(model);
            var u=model.Units.Single();Advance(model,.025f);Assert.That(u.Alive,Is.False);Assert.That(u.Deaths,Is.Zero);Assert.That(u.NativePhaseReturningUntil,Is.GreaterThan(0));
            d.Attack=0;float sp=u.SkillCharge;Advance(model,3.9f);Assert.That(u.Alive,Is.False);Assert.That(u.SkillCharge,Is.EqualTo(sp));
            Advance(model,.15f);Assert.That(u.Alive,Is.True);Assert.That(u.NativePhase,Is.EqualTo(1));Assert.That(u.Deaths,Is.Zero);
        }
        [Test]
        public void ClearingDefendersStillRequiresARealBodyAtTheGate()
        {
            var model=Fixture(out var run,"M11");Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);
            foreach(var d in model.Defenders){d.Health=0;d.Alive=false;}
            model.Advance(.025f);Assert.That(model.Phase,Is.EqualTo(FormationPhase.Exiting));
            model.Units.Single().Position=new Vector2(FormationBattleModel.Width-1,3);model.Advance(.025f);Assert.That(model.Phase,Is.EqualTo(FormationPhase.Won));
        }
        [Test]
        public void ComboAndResonanceRequireDistinctSpeciesAndEquippedBadges()
        {
            var model=Fixture(out var run,"B22","B23","B25","WIS","M20","B13","M25","M32","B11");
            int beast=model.Units.First(u=>u.Name=="狂躁异质裂兽").Instance;
            Assert.That(run.Equip(run.AddItem("徽章:同频").Instance,beast).Success,Is.True);
            Assert.That(run.Equip(run.AddItem("AC").Instance,beast).Success,Is.True);
            Assert.That(run.Equip(run.AddItem("CF").Instance,beast).Success,Is.True);
            foreach(string name in new[]{"梅菲斯特","蔓德拉"})Assert.That(run.Equip(run.AddItem("徽章:连击").Instance,model.Units.First(u=>u.Name==name).Instance).Success,Is.True);
            model.BindCampaign(run);Assert.That(model.ActiveSynergies["连击"],Is.EqualTo(8));Assert.That(model.ActiveSynergies["同频"],Is.EqualTo(5));
            run.ResonanceLeaderInstance=beast;run.ResonancePartnerInstance=model.Units.First(u=>u.Name=="圣徒").Instance;run.MainCarryInstance=beast;
            Target(model);Assert.That(model.StartBattle(),Is.True);Enter(model);Advance(model,75);
            Assert.That(model.BattleEvents.Count(e=>e.Contains("同频")),Is.GreaterThan(30));
            Assert.That(model.Units.First(u=>u.Instance==beast).LastSkillAt,Is.GreaterThan(65));
            var casts=model.SkillCasts.Where(c=>c.Instance==beast&&c.At>=45).Select(c=>c.At).ToArray();
            var gaps=casts.Skip(1).Zip(casts,(later,earlier)=>later-earlier).ToArray();
            TestContext.WriteLine($"RIPPER_RUNTIME casts={casts.Length}; mature_gap={gaps.DefaultIfEmpty(0).Min():0.000}..{gaps.DefaultIfEmpty(0).Max():0.000}");
            Assert.That(gaps.Length,Is.GreaterThan(10));Assert.That(gaps.Min(),Is.GreaterThanOrEqualTo(.89));
        }

        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void WisadelThreePreparationChoicesUseDifferentRecoveryAndActionStates(int skill)
        {
            var model=Fixture(out var run,"WIS");run.WisadelSkill=skill;model.BindCampaign(run);Target(model);
            Assert.That(model.StartBattle(),Is.True);Enter(model);var u=model.Units.Single();
            Assert.That(u.SkillRequired,Is.EqualTo(skill==1?3:skill==2?24:28));
            u.SkillCharge=u.SkillRequired;Advance(model,.025f);
            Assert.That(u.SkillAnimation,Is.EqualTo(skill==1?"Skill_1":skill==2?"Skill_2_Begin":"Skill_3_Begin"));
            Advance(model,5);Assert.That(model.Hits.Any(h=>h.FromId==u.Id),Is.True);
        }

        [Test]
        public void DegenbrecherDistributesEightStrikesAcrossThreeTargetsInsteadOfTriplingThem()
        {
            var model=Fixture(out var run,"B04");
            for(int i=0;i<model.Defenders.Count;i++)
            {var d=model.Defenders[i];d.Position=new Vector2(15+i*.2f,3);d.MaxHealth=100000000;d.Attack=0;d.Range=.01f;d.Armor=0;}
            Assert.That(model.StartBattle(),Is.True);Enter(model);var u=model.Units.Single();u.SkillCharge=u.SkillRequired;Advance(model,1.25f);
            var strikes=model.Hits.Where(h=>h.FromId==u.Id&&h.Skill&&!h.Heal).ToArray();
            Assert.That(strikes.Length,Is.EqualTo(8));Assert.That(strikes.Select(h=>h.ToId).Distinct().Count(),Is.EqualTo(3));
        }

        [TestCase("1-3")][TestCase("2-4")][TestCase("3-7")]
        public void PresetBudgetedLineupsFightUnmodifiedNodeDefenders(string nodeId)
        {
            // Previous nodes are skipped deliberately: this measures the real combat at a
            // documented assembled board, not acquisition probability or a natural full run.
            string[] ids=nodeId=="1-3"?new[]{"M03","M11","M12"}:nodeId=="2-4"?new[]{"M14","M12","M13","M25","M18","B23"}:new[]{"B22","B23","B25","WIS","M20","B13","M25","M32","B11"};
            var model=Fixture(out var run,ids);
            int safety=0;
            while(run.CurrentNode.Id!=nodeId)
            {
                Assert.That(++safety,Is.LessThan(30));
                if(!run.NodeSettled)run.SettleBattle(true,1,3,0,false);
                FormationCampaignRunTests.CollectDrops(run);
                Assert.That(run.AdvanceNode(),Is.True);
                while(run.HasBlockingChoice)Assert.That(run.Choose(0).Success,Is.True);
            }
            run.Shop.Preset(ids.Select(id=>new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=id=="B22"?3:2}),Array.Empty<string>(),10,0,80);
            Assert.That(model.ApplyRoster(run.Shop.Bench.Select((p,i)=>new FormationRosterEntry{Instance=p.Instance,Star=p.Star,ArtId=p.Id,Spawn=new Vector2(9-(i/4)*3,(i%4)*2)}),10),Is.True);
            if(nodeId=="3-7")
            {
                int beast=model.Units.First(u=>u.Name=="狂躁异质裂兽").Instance;
                run.MainCarryInstance=run.ResonanceLeaderInstance=beast;run.ResonancePartnerInstance=model.Units.First(u=>u.Name=="圣徒").Instance;
                foreach(string key in new[]{"徽章:同频","AC","CF"})Assert.That(run.Equip(run.AddItem(key).Instance,beast).Success,Is.True);
                foreach(string name in new[]{"梅菲斯特","蔓德拉"})Assert.That(run.Equip(run.AddItem("徽章:连击").Instance,model.Units.First(u=>u.Name==name).Instance).Success,Is.True);
                // Fourteen occupied equipment slots total, including the three badges. This
                // keeps the prebuilt endgame inside the document's component-budget horizon.
                foreach(var equipment in new[]{("M25","CE"),("M25","BC"),("M32","CC"),("B11","CC"),("B11","CF"),("B13","CC"),("B13","CF"),("WIS","AA"),("WIS","AF")})
                {
                    string name=FormationCampaignRules.Find(equipment.Item1).Name;
                    Assert.That(run.Equip(run.AddItem(equipment.Item2).Instance,model.Units.First(u=>u.Name==name).Instance).Success,Is.True);
                }
            }
            model.BindCampaign(run);
            float health=model.Defenders.Sum(d=>d.MaxHealth);Assert.That(model.StartBattle(),Is.True);
            Advance(model,float.IsPositiveInfinity((float)model.LimitSeconds)?180:(float)model.LimitSeconds+1);
            TestContext.WriteLine($"PRESET_RULES preplan={run.PreplanId}; augments={string.Join(",",run.AugmentIds)}; equipped={run.AllEquipment.Count(e=>e.Owner!=0)}");
            TestContext.WriteLine($"PRESET_NODE {nodeId}: phase={model.Phase}; clock={model.Clock:0.000}; initialHP={health:0}; progress={model.EncounterProgress:P2}; remaining={model.RemainingDefenders}; deaths={model.Units.Sum(u=>u.Deaths)}; damage={model.Units.Sum(u=>u.DamageDone):0}");
            TestContext.WriteLine("PRESET_REMAINING "+string.Join(";",model.Defenders.Where(d=>d.Alive).Select(d=>$"{d.Name}:{d.Health:0}/{d.MaxHealth:0}@{d.Position}")));
            Assert.That(model.EncounterProgress,Is.GreaterThan(.25f),"An assembled board must at least earn encounter progress against the real node curve.");
            Assert.That(model.Units.Sum(u=>u.DamageDone),Is.GreaterThan(100));
            Assert.That(model.Clock,Is.GreaterThan(0));
            Assert.That(model.Phase,Is.EqualTo(FormationPhase.Won));
        }

        [Test]
        public void ManualSeaCellsMustBeFourConnectedAndWithinCurrentTraitBudget()
        {
            var model=Fixture(out var run,"M33","M35");
            run.SeaCellIds.Add(14+3*FormationBattleModel.Width);run.SeaCellIds.Add(16+3*FormationBattleModel.Width);
            Assert.That(model.ValidateSeaPlacement(out _),Is.False);
            run.SeaCellIds.Add(15+3*FormationBattleModel.Width);Assert.That(model.ValidateSeaPlacement(out _),Is.True);
            model.RefreshSeaPreview();Assert.That(model.SeaCells.Count,Is.EqualTo(3));
            run.SeaCellIds.Add(17+3*FormationBattleModel.Width);run.SeaCellIds.Add(18+3*FormationBattleModel.Width);
            Assert.That(model.StartBattle(),Is.False);
        }

        [TestCase("M17")][TestCase("M03")][TestCase("M11")]
        public void ApprovedOneCostRolesCanDeployFourStarsRegardlessOfLegacyCatalogCost(string id)
        {
            var model=Fixture(out var run,id);
            run.Shop.Preset(new[]{new ShopTrialModel.Piece{Id=FormationCampaignRules.Find(id).CardId,Star=4}},Array.Empty<string>(),10,0,80);
            var card=run.Shop.Bench.Single();
            Assert.That(model.ApplyRoster(new[]{new FormationRosterEntry{Instance=card.Instance,ArtId=card.Id,Star=4,Spawn=new Vector2(FormationBattleModel.DeploymentColumns-1,3)}},10),Is.True);
            Assert.That(model.Units.Single().MaxHealth,Is.EqualTo(FormationCampaignRules.Find(id).Health*5.832f).Within(.01f));
        }

        [Test]
        public void NativeAndModeReturnFromOneSourceShareExactlyOneHandoffCredit()
        {
            var model=Fixture(out var run,"M30","M14","M32","B24","M20","B08");
            var source=model.Units.First(u=>u.Name=="深池逐火战士");
            run.RelayReceiverInstance=model.Units.First(u=>u.Name=="万火归一").Instance;model.BindCampaign(run);
            var guard=Target(model);guard.Attack=100000;guard.Range=2;Assert.That(model.StartBattle(),Is.True);
            source.Position=new Vector2(14.5f,3);Advance(model,.025f);Assert.That(source.Alive,Is.False);
            guard.Attack=0;Advance(model,4.05f);Assert.That(source.Alive,Is.True);Assert.That(source.NativePhase,Is.EqualTo(1));
            Assert.That(model.BattleEvents.Count(e=>e.Contains("接棒:")),Is.EqualTo(1));
            guard.TargetId=source.Id;guard.Attack=100000;
            for(int i=0;i<100&&source.Alive;i++)model.Advance(.025f);
            Assert.That(source.Alive,Is.False);Assert.That(source.Deaths,Is.EqualTo(1));guard.Attack=0;
            Advance(model,8.05f);Assert.That(source.Alive,Is.True);
            Assert.That(model.BattleEvents.Count(e=>e.Contains("接棒:")),Is.EqualTo(1),"Native and mode returns must share a source credit.");
        }

        static void ReachNode(FormationCampaignRun run,string nodeId,bool collectDrops=true)
        {
            int safety=0;
            while(run.CurrentNode.Id!=nodeId)
            {
                Assert.That(++safety,Is.LessThan(30));
                if(!run.NodeSettled)run.SettleBattle(true,1,3,0,false);
                if(collectDrops)FormationCampaignRunTests.CollectDrops(run);
                Assert.That(run.AdvanceNode(),Is.True);
                while(run.HasBlockingChoice)Assert.That(run.Choose(0).Success,Is.True);
            }
        }
        [TestCase("1-3","D06",8,20,1)]
        [TestCase("1-3","D04",8,22,1.4f)]
        [TestCase("1-3","D02",10,24,1)]
        [TestCase("1-3","D07",8,24,1.5f)]
        [TestCase("1-3","D08",6,22,1.4f)]
        [TestCase("1-4","D10",10,24,1.2f)]
        [TestCase("1-6","D12",10,24,1.4f)]
        public void AuthoredDefendersPreserveInitialChargeThresholdAndRealSkillAction(string nodeId,string artId,float initial,float required,float seconds)
        {
            var model=Fixture(out var run,"M11");ReachNode(run,nodeId,collectDrops:false);model.BindCampaign(run);
            var guard=model.Defenders.Single(defender=>defender.ArtId==artId);
            Assert.That(guard.SkillCharge,Is.EqualTo(initial));Assert.That(guard.SkillRequired,Is.EqualTo(required));
            model.Defenders.RemoveAll(d=>d!=guard);guard.Position=new Vector2(15,3);
            Assert.That(model.StartBattle(),Is.True);Assert.That(guard.SkillCharge,Is.EqualTo(initial));Assert.That(guard.SkillRequired,Is.EqualTo(required));
            var fighter=model.Units.Single();fighter.Position=new Vector2(14.5f,3);
            guard.SkillCharge=guard.SkillRequired;guard.Health-=1;
            model.Advance(.025f);
            Assert.That(guard.LastSkillAt,Is.GreaterThanOrEqualTo(0));Assert.That(guard.PresentationDuration,Is.EqualTo(seconds).Within(.0001));
        }

        [TestCase("1-9",12,24,1)]
        [TestCase("2-7",10,24,1)]
        public void BossesUseTheirOwnAuthoredInitialChargeAndActionInsteadOfAUniversalTwelve(string nodeId,float initial,float required,float seconds)
        {
            var model=Fixture(out var run,"M11");ReachNode(run,nodeId,collectDrops:false);model.BindCampaign(run);
            var spec=FormationCampaignRules.Bosses.Single(b=>b.NodeId==nodeId);var boss=model.Defenders.Single(d=>d.Name==spec.Name);
            Assert.That(boss.SkillCharge,Is.EqualTo(initial));Assert.That(boss.SkillRequired,Is.EqualTo(required));
            model.Defenders.RemoveAll(d=>d!=boss);boss.Position=new Vector2(15,3);
            Assert.That(model.StartBattle(),Is.True);Assert.That(boss.SkillCharge,Is.EqualTo(initial));
            model.Units.Single().Position=new Vector2(14.5f,3);boss.SkillCharge=boss.SkillRequired;
            model.Advance(.025f);
            Assert.That(boss.LastSkillAt,Is.GreaterThanOrEqualTo(0));Assert.That(boss.PresentationDuration,Is.EqualTo(seconds).Within(.0001));
            Assert.That(boss.MaxHealth,Is.EqualTo(spec.Health));Assert.That(boss.Attack,Is.EqualTo(spec.Attack));
        }

        [Test]
        public void SnowBossFreezesAcrossTheBoardOnceAndNeedsASecondClearBeforeTheGate()
        {
            var model = Fixture(out var run, "M03", "M11");
            ReachNode(run, "3-7", collectDrops: false);
            model.BindCampaign(run);
            var guard = model.Defenders.Single();
            var fighter = model.Units[0];
            var distant = model.Units[1];
            Assert.That(model.StartBattle(), Is.True);
            fighter.Position = guard.Position + Vector2.left;
            fighter.MaxHealth = fighter.Health = 100000;
            distant.Position = Vector2.zero;
            for (int tick = 0; tick < 400 && model.SnowBossSnow(guard).Count == 0; tick++)
                Advance(model, .025f);
            Assert.That(model.SnowBossSnow(guard), Is.Not.Empty, "Phase 1 builds snow at real contact cells");
            Assert.That(model.Hits.Any(hit => hit.FromId == guard.Id && hit.Skill && hit.Amount > 0), Is.True);
            Assert.That(Vector2.Distance(distant.Position, guard.Position), Is.GreaterThan(guard.Range));

            guard.Health = 1;
            for (int tick = 0; tick < 160 && !model.SnowBossTransitionActive(guard); tick++)
                Advance(model, .025f);
            Assert.That(model.SnowBossTransitionActive(guard), Is.True);
            Assert.That(guard.Health, Is.Zero);
            Assert.That(model.SnowBossSnow(guard), Is.Empty);
            Assert.That(model.BossAlive, Is.True);
            Assert.That(model.RemainingDefenders, Is.EqualTo(1));
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Fighting));
            Assert.That(model.Hits.Any(hit => hit.ToId == guard.Id && hit.Killed), Is.False);

            var clock = model.Clock;
            var freeze = model.SnowBossFreezeRemaining(distant);
            var actors = model.Units.Where(unit => unit.Alive).ToArray();
            Assert.That(actors.All(unit => model.SnowBossFreezeRemaining(unit) > 0), Is.True);
            var poses = actors.Select(unit => (unit.Position, unit.LastActionAt, unit.SkillCharge)).ToArray();
            model.Paused = true;
            Advance(model, 2);
            Assert.That(model.Clock, Is.EqualTo(clock));
            Assert.That(model.SnowBossFreezeRemaining(distant), Is.EqualTo(freeze));
            model.Paused = false;
            Advance(model, .25f);
            Assert.That(actors.Select(unit => (unit.Position, unit.LastActionAt, unit.SkillCharge)), Is.EqualTo(poses));
            Assert.That(model.SnowBossFreezeRemaining(distant), Is.LessThan(freeze));

            for (int tick = 0; tick < 90 && model.SnowBossTransitionActive(guard); tick++)
                Advance(model, .025f);
            Assert.That(guard.SnowBossPhase, Is.EqualTo(2));
            Assert.That(guard.Health, Is.GreaterThan(guard.MaxHealth * .95f));
            for (int tick = 0; tick < 240 && !model.Hits.Any(hit => hit.FromId == guard.Id && hit.At > clock && hit.Skill && hit.Amount > 0); tick++)
                Advance(model, .025f);
            Assert.That(model.Hits.Any(hit => hit.FromId == guard.Id && hit.At > clock && hit.Skill && hit.Amount > 0), Is.True);
            guard.Health = 1;
            for (int tick = 0; tick < 240 && guard.Alive; tick++)
                Advance(model, .025f);
            Assert.That(guard.Alive, Is.False);
            Assert.That(model.BossAlive, Is.False);
            Assert.That(model.RemainingDefenders, Is.Zero);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Exiting));
            Advance(model, 25);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
            Assert.That(model.Retry(), Is.True);
            Assert.That(model.Defenders.Single().SnowBossPhase, Is.EqualTo(1));
            Assert.That(model.SnowBossFreezeRemaining(distant), Is.Zero);
        }

        [Test]
        public void DefaultSnowBossTrialRosterWinsTheUnmodifiedBattle()
        {
            const int seed = 20261117;
            var start = typeof(FormationShopSession).GetMethod("StartPramanixBossTrial", BindingFlags.Static | BindingFlags.NonPublic);
            var session = (FormationShopSession)start.Invoke(null, new object[] { seed });
            try
            {
                var model = new FormationBattleModel(seed, SavedTerrain());
                session.ApplyTo(model);
                Assert.That(session.Run.IsPramanixBossTrial, Is.True);
                Assert.That(model.EnabledCount, Is.GreaterThan(0));
                var boss = model.Defenders.Single();
                Assert.That(boss.IsSnowBoss, Is.True);
                Assert.That(model.StartBattle(), Is.True);
                double transition = 0, clear = 0;
                for (int tick = 0; tick < 6000 && (model.Phase == FormationPhase.Fighting || model.Phase == FormationPhase.Exiting); tick++)
                {
                    model.Advance(.025f);
                    if (transition == 0 && model.SnowBossTransitionActive(boss)) transition = model.Clock;
                    if (clear == 0 && model.RemainingDefenders == 0) clear = model.Clock;
                }
                TestContext.WriteLine($"Snow boss trial: phase={model.Phase}, transition={transition:0.000}s, clear={clear:0.000}s, end={model.Clock:0.000}s, HP={boss.Health:0}/{boss.MaxHealth:0}");
                Assert.That(transition, Is.GreaterThan(0));
                Assert.That(clear, Is.GreaterThan(transition + 2));
                Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won));
            }
            finally
            {
                FormationShopSession.StartNew(seed, true);
            }
        }

        [Test]
        public void YuOnlyCastsOneWallInTheFirstPlaneBattle()
        {
            var model = Fixture(out var run, "M11");
            ReachNode(run, "1-6", collectDrops: false);
            model.BindCampaign(run);
            var guard = model.Defenders.Single(defender => defender.ArtId == "D22");
            model.Defenders.RemoveAll(defender => defender != guard);
            guard.MaxHealth = 100000;
            Assert.That(model.StartBattle(), Is.True);
            var fighter = model.Units.Single();
            fighter.Position = guard.Position + Vector2.left * .5f;
            fighter.MaxHealth = fighter.Health = 100000;
            Advance(model, .025f);
            double firstCastAt = guard.LastSkillAt;
            Assert.That(firstCastAt, Is.EqualTo(model.Clock));
            Advance(model, 24);
            Assert.That(guard.LastSkillAt, Is.EqualTo(firstCastAt));
        }

        [Test]
        public void WangsReservedChessCellTriggersSkillDamageOnContact()
        {
            var model = Fixture(out var run, "M11");
            ReachNode(run, "3-4", collectDrops: false);
            model.BindCampaign(run);
            var guard = model.Defenders.Single(defender => defender.ArtId == "D28");
            Assert.That(guard.SkillCell.HasValue, Is.True);
            model.Defenders.RemoveAll(defender => defender != guard);
            Assert.That(model.StartBattle(), Is.True);
            var fighter = model.Units.Single();
            fighter.Position = guard.SkillCell.Value;
            fighter.MaxHealth = fighter.Health = 100000;
            Advance(model, .025f);
            Assert.That(model.Hits.Count(hit => hit.FromId == guard.Id && hit.ToId == fighter.Id && hit.Skill), Is.EqualTo(1));
            Advance(model, 2);
            Assert.That(model.Hits.Count(hit => hit.FromId == guard.Id && hit.ToId == fighter.Id && hit.Skill), Is.EqualTo(1));
        }
    }
}
