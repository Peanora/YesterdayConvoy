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
    public sealed class FormationSynergyTrialTests
    {
        const int Seed = 20261117;
        const BindingFlags SessionCommand = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
        FormationBattleTerrainLayout terrain;

        static IEnumerable<string> TrialIds => FormationCampaignRules.AllFactions.Concat(FormationCampaignRules.AllStyles)
            .Select(rule => rule.Id);

        [OneTimeSetUp]
        public void ReadBoard()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FormationBattleTools.ScenePath);
            try
            {
                var controller = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FormationBattleController>(true)).Single();
                terrain = controller.terrain.BuildLayout();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void CatalogCoversEveryCurrentTraitOnce()
        {
            Assert.That(FormationSynergyTrial.All.Select(trial => trial.Trait.Id), Is.EquivalentTo(TrialIds));
        }

        [TestCaseSource(nameof(TrialIds))]
        public void PresetStartsAtHighestTierOnTheSavedBoard(string id)
        {
            var (session, model) = Start(id);
            var trial = session.Run.SynergyTrial;
            var tier = trial.Trait.Levels.Max();
            var run = session.Run;
            var bench = session.Shop.Bench.ToArray();
            var view = FormationTraitView.For(run, session.Shop, id);

            Assert.That(run.IsPractice && session.Shop.UnlimitedGold, Is.True);
            Assert.That(run.HasBlockingChoice, Is.False);
            Assert.That(run.CurrentNode, Is.SameAs(FormationCampaignRules.Nodes[trial.NodeIndex]));
            Assert.That(model.Defenders.Count, Is.EqualTo(run.CurrentNode.DefenderCount).And.GreaterThan(0));
            Assert.That(model.Defenders.All(guard => terrain.CanPlaceDefender(guard.Position)), Is.True);
            Assert.That(bench.Select(piece => piece.Id), Is.EquivalentTo(trial.Roster));
            Assert.That(model.Units.Select(unit => unit.Instance), Is.EquivalentTo(bench.Select(piece => piece.Instance)));
            Assert.That(model.EnabledCount, Is.LessThanOrEqualTo(session.Shop.PopulationCap));
            Assert.That(model.Units.All(unit => FormationDeployment.Fits(unit.DeploymentCells)), Is.True);
            var occupied = new HashSet<Vector2Int>();
            foreach (var unit in model.Units)
                foreach (var cell in unit.DeploymentCells.allPositionsWithin)
                    Assert.That(occupied.Add(cell), Is.True, "Overlapping deployment at " + cell);
            Assert.That(view.BadgeMembers.Select(member => member.CardId), Is.EquivalentTo(trial.BadgeWearers));
            Assert.That(view.ActiveLevel, Is.EqualTo(tier));
            Assert.DoesNotThrow(session.Shop.Validate);

            Assert.That(model.StartBattle(), Is.True);
            Assert.That(model.ActiveSynergies[id], Is.EqualTo(tier));
            model.Advance(28f);
            Assert.That(model.EncounterProgress, Is.GreaterThan(0), "The default formation reaches and damages the guards");
        }

        [TestCase("整合")]
        [TestCase("泥岩")]
        public void TrialProgressPaysOnceThenCarriesIntoTheNextAttempt(string id)
        {
            var (session, model) = Start(id);
            var run = session.Run;
            var positions = model.Units.Select(unit => (unit.Instance, unit.Spawn)).ToArray();
            var defenders = model.Defenders.Select(guard => (guard.ArtId, guard.Position, guard.MaxHealth)).ToArray();
            int gold = session.Shop.Gold;

            Assert.That(model.StartBattle(), Is.True);
            if (id == "泥岩") Assert.That(model.Summons.Single(unit => unit.ArtId == "GOLEM").Name, Is.EqualTo("泥岩巨像 4"));
            model.Advance((float)model.LimitSeconds + 1);
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Won).Or.EqualTo(FormationPhase.Lost));
            bool won = model.Phase == FormationPhase.Won;
            Assert.That(session.SettleBattle(won, model.EncounterProgress, model.InitialDefenderCount,
                model.RemainingDefenders, model.BossAlive), Is.Null);
            var firstProgress = (run.Hope, run.MudrockXP, run.LastTrialProgress);
            Assert.That(firstProgress.LastTrialProgress, Is.Not.Empty);
            int hope = 30 + (won ? 2 : 0);
            Assert.That(firstProgress.Hope, Is.EqualTo(id == "整合" ? hope : 0));
            Assert.That(firstProgress.MudrockXP, Is.EqualTo(id == "泥岩" ? 81 : 0));

            session.SettleBattle(won, model.EncounterProgress, model.InitialDefenderCount, model.RemainingDefenders, model.BossAlive);
            Assert.That((run.Hope, run.MudrockXP, run.LastTrialProgress), Is.EqualTo(firstProgress));
            Assert.That(run.PendingBalls, Is.Empty);
            Assert.That(run.SquadHP, Is.EqualTo(100));
            if (id == "泥岩") Assert.That(session.Shop.Gold, Is.EqualTo(gold));

            var retry = (FormationBattleModel)typeof(FormationShopSession).GetMethod("PreparePracticeBattle", SessionCommand)
                .Invoke(session, new object[] { run.NodeIndex });
            Assert.That(retry, Is.Not.Null);
            Assert.That(retry.Units.Select(unit => (unit.Instance, unit.Spawn)), Is.EqualTo(positions));
            Assert.That(retry.Defenders.Select(guard => (guard.ArtId, guard.Position, guard.MaxHealth)), Is.EqualTo(defenders));
            Assert.That((run.Hope, run.MudrockXP), Is.EqualTo((firstProgress.Hope, firstProgress.MudrockXP)));
            Assert.That(retry.StartBattle(), Is.True);
            if (id == "泥岩") Assert.That(retry.Summons.Single(unit => unit.ArtId == "GOLEM").Name, Is.EqualTo("泥岩巨像 5"));
        }

        (FormationShopSession Session, FormationBattleModel Model) Start(string id)
        {
            var trial = FormationSynergyTrial.Find(id);
            Assert.That(trial, Is.Not.Null);
            var session = (FormationShopSession)typeof(FormationShopSession).GetMethod("StartSynergyTrial", SessionCommand)
                .Invoke(null, new object[] { Seed, trial });
            var model = new FormationBattleModel(Seed, terrain);
            session.ApplyTo(model);
            return (session, model);
        }
    }
}
