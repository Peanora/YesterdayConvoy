using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignEvidence
    {
        static bool MapVariantsRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-formationMapVariantsSmokeTest") >= 0;

        IEnumerator VerifyMapVariants()
        {
            result.scope = "Windows Player. Fixture settlements reach 1-4 through the production campaign transition. The difficulty selector command then switches maps for scouting and real auto-battle with repeatable legal armies. Initial clips run at 1x; finish is accelerated.";
            Assert(controller.maps.ActiveBoard == FormationBattleMaps.Board.Canyon, "Default campaign opens on Canyon");
            yield return WaitForOpeningDelivery("00-opening");
            yield return ResolveChoices();
            DeployOpeningRecruits();
            var productionModel = controller.Model;
            var productionArmy = productionModel.Units.ToDictionary(unit => unit.Instance, unit => unit.Spawn);
            while (controller.Campaign.CurrentNode.Id != "1-4")
            {
                var run = controller.Campaign;
                Assert(run.SettleBattle(true, 1, run.CurrentNode.DefenderCount, 0, false) != null,
                    run.CurrentNode.Id + " settles the campaign progression fixture");
                Assert(controller.ContinueCampaign(), "Formal campaign advances to " + run.CurrentNode.Id);
                yield return ResolveChoices();
            }
            Assert(controller.maps.ActiveBoard == FormationBattleMaps.Board.Street &&
                controller.Model == productionModel && productionModel.Units.Count == productionArmy.Count &&
                productionModel.Units.All(unit => productionArmy.TryGetValue(unit.Instance, out var start) && unit.Spawn == start),
                "Formal 1-4 swaps scenery and combat terrain while retaining the challenge lineup");
            var samples = new[]
            {
                (node: "1-4", board: FormationBattleMaps.Board.Street, feature: new Vector2(15, 3)),
                (node: "2-1", board: FormationBattleMaps.Board.Hall, feature: new Vector2(13, 1)),
                (node: "3-1", board: FormationBattleMaps.Board.Ice, feature: new Vector2(15, 5))
            };
            foreach (var sample in samples)
            {
                result.node = sample.node;
                var oldSession = controller.Session;
                var oldArmy = controller.Model.Units.ToDictionary(unit => unit.Instance, unit => unit.Spawn);
                int node = Array.FindIndex(FormationCampaignRules.Nodes, entry => entry.Id == sample.node);
                controller.SelectPracticeBattle(node);
                Assert(controller.Campaign.CurrentNode.Id == sample.node && controller.maps.ActiveBoard == sample.board,
                    sample.node + " loads the matching authored map");
                if (oldSession.Run.IsDifficultyTrial)
                    Assert(controller.Session == oldSession && controller.Model.Units.Count == oldArmy.Count && controller.Model.Units.All(unit =>
                        oldArmy.TryGetValue(unit.Instance, out var previous) && previous == unit.Spawn),
                        sample.node + " retains the difficulty-trial roster and deployment while switching maps");
                var authoring = sample.board switch
                {
                    FormationBattleMaps.Board.Street => controller.maps.street,
                    FormationBattleMaps.Board.Hall => controller.maps.hall,
                    _ => controller.maps.ice
                };
                Assert(authoring.gameObject.activeInHierarchy &&
                    new[] { controller.maps.street, controller.maps.hall, controller.maps.ice }.Count(a => a.gameObject.activeInHierarchy) == 1 &&
                    !controller.terrain.gameObject.activeInHierarchy &&
                    controller.maps.canyonScenery.All(root => !root.activeInHierarchy), sample.node + " renders one terrain only, including raised surfaces");
                var model = controller.Model;
                Assert(!model.Terrain.IsWalkable(sample.feature) &&
                    model.Defenders.All(d => model.Terrain.CanPlaceDefender(d.Position) &&
                        (!d.SkillCell.HasValue || model.Terrain.IsWalkable(d.SkillCell.Value))),
                    sample.node + " blocking scenery and guard positions use the active geometry");
                var approach = new Vector2(11, sample.board == FormationBattleMaps.Board.Street ? 3 : 1);
                var destination = sample.board == FormationBattleMaps.Board.Street ? new Vector2(19, 3) : new Vector2(20, 3);
                Assert(UsesAuthoredCorridor(model.Terrain, authoring.preferredGround, approach, destination),
                    sample.node + " follows the authored corridor through the middle of the board");

                yield return ResolveChoices();
                StageDefenseEvidenceRoster(controller.Campaign.CurrentNode.Plane, false);
                yield return ReadyArt();
                controller.SyncActors();
                controller.SetFormationView(true);
                yield return WaitForScoutingView(true);
                yield return Frame(sample.node + "-scout");
                controller.SetFormationView(false);
                yield return WaitForScoutingView(false);
                yield return Frame(sample.node + "-deploy");
                controller.StartBattle();
                Assert(model.Phase == FormationPhase.Fighting && model.Speed == 1, sample.node + " starts at normal speed");
                yield return CaptureInterfaceClip(sample.node + "-advance", 72, "-formationMapClips");
                double deadline = Time.realtimeSinceStartupAsDouble + 30;
                while (!model.Hits.Any(hit => hit.Amount > 0) && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    model.Paused = false;
                    yield return null;
                }
                Assert(model.Clock > 0 && model.Hits.Any(hit => hit.Amount > 0) &&
                    model.Units.Where(unit => unit.Enabled && unit.Alive).All(unit => model.Terrain.IsWalkable(unit.Position)),
                    sample.node + " reaches real combat without traversing blocked ground");
                yield return Frame(sample.node + "-battle");
                yield return CaptureInterfaceClip(sample.node + "-1x", 48, "-formationMapClips");
                model.Paused = true;
                double pausedAt = model.Clock;
                yield return Frame(sample.node + "-paused");
                Assert(model.Clock == pausedAt && controller.maps.ActiveBoard == sample.board, sample.node + " preserves its board while paused");
                while (model.Phase == FormationPhase.Fighting || model.Phase == FormationPhase.Exiting)
                {
                    model.Paused = false;
                    for (int step = 0; step < 20 && controller.CombatViewActive; step++) model.Advance(.025f);
                    yield return null;
                }
                Assert(model.Phase == FormationPhase.Won && model.Units.Any(unit =>
                    unit.Enabled && unit.Alive && unit.Position.x >= FormationBattleModel.Width - 1 - .001f),
                    sample.node + " clears the guards and a surviving fighter reaches the real blue gate");
                result.battles++;
                controller.ResetDemo();
                Assert(controller.Model.Phase == FormationPhase.Preparation && controller.maps.ActiveBoard == sample.board,
                    sample.node + " retries on the same terrain");
            }
            controller.SelectPracticeBattle(0);
            Assert(controller.maps.ActiveBoard == FormationBattleMaps.Board.Canyon &&
                controller.terrain.gameObject.activeInHierarchy &&
                controller.maps.canyonScenery.All(root => root.activeInHierarchy), "Returning to 1-1 restores the Canyon scenery");
        }

        static bool UsesAuthoredCorridor(FormationBattleTerrainLayout layout, RectInt[] ground, Vector2 position, Vector2 goal)
        {
            int middleSteps = 0, onRoad = 0;
            for (int step = 0; step < 100 && position != goal; step++)
            {
                if (!layout.TryApproach(position, goal, .8f, out var waypoint, out _)) return false;
                Vector2 next = Vector2.MoveTowards(position, waypoint, .25f);
                if (!layout.CanTravel(position, next) || next == position) return false;
                position = next;
                if (position.x >= 13 && position.x <= 18)
                {
                    middleSteps++;
                    if (ground.Any(area => area.Contains(Vector2Int.RoundToInt(position)))) onRoad++;
                }
            }
            return position == goal && middleSteps > 0 && onRoad >= middleSteps * .70f;
        }
    }
}
