using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationBattleCameraTests
    {
        FormationBattleController controller;

        [TearDown]
        public void FreezeCompletedScene()
        {
            if (controller != null && controller.Model != null) controller.Model.Paused = true;
        }

        IEnumerator WaitForCamera()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.CameraTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.CameraTransitioning, Is.False, "The camera must finish moving.");
            yield return null;
        }

        void AssertJoinedSeam()
        {
            var camera = controller.battleCamera;
            var board = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>());
            int row = FormationBattleModel.Height / 2;
            foreach (int column in new[] { FormationBattleModel.DeploymentColumns - 1, FormationBattleModel.DeploymentColumns + 1 })
            {
                var tile = board.Single(t => t.name == $"Tile {column:00},{row:00}");
                Assert.That(camera.cullingMask & (1 << tile.gameObject.layer), Is.Not.EqualTo(0),
                    "The camera must render the saved tiles on both sides of the red gate.");
                var cell = new Vector2(column, row);
                var point = camera.WorldToViewportPoint(FormationBattleController.WorldPoint(cell));
                Assert.That(point.x, Is.InRange(0f, 1f), "The center of the same board remains visible across the seam.");
                Assert.That(point.y, Is.InRange(0f, 1f));
                Assert.That(controller.FormationPointVisibleInView(cell), Is.True,
                    "A visible point across the seam must be available to actor presentation.");
            }
        }

        void AssertInFocusedFrame(Vector3 world, string message)
        {
            var projected = controller.battleCamera.WorldToScreenPoint(world);
            var point = TacticalViewport.Current.FromScreen(projected);
            var safe = controller.CurrentFormationCameraSafeRect;
            Assert.That(projected.z, Is.GreaterThan(0), message);
            Assert.That(point.x, Is.InRange(safe.xMin - 1, safe.xMax + 1), message);
            Assert.That(point.y, Is.InRange(safe.yMin - 1, safe.yMax + 1), message);
        }

        void AssertHalfAndActorHeads(bool defenders)
        {
            float first = defenders ? FormationBattleModel.DeploymentColumns + .5f : -.5f, last = defenders ? FormationBattleModel.Width - .5f : FormationBattleModel.DeploymentColumns + .5f;
            foreach (float column in new[] { first, last })
                foreach (float row in new[] { -.5f, FormationBattleModel.Height - .5f })
                    AssertInFocusedFrame(FormationBattleController.WorldPoint(new Vector2(column, row)), "Every row of the normal focused board must fit when the recruitment drawer is collapsed.");
            var actors = defenders ? controller.Model.Defenders.Cast<FormationActor>() :
                controller.Model.Units.Where(u => u.Enabled).Cast<FormationActor>();
            foreach (var actor in actors)
            {
                AssertInFocusedFrame(controller.ActorWorldPoint(actor), actor.Name + " feet must remain visible.");
                AssertInFocusedFrame(controller.ActorWorldPoint(actor) + controller.battleCamera.transform.up * controller.ActorPresentationHeight(actor), actor.Name + " visible body height must remain in the current view.");
            }
        }

        static float TileScreenWidth(int column, int row)
        {
            var cell = new Vector2(column, row);
            return Vector2.Distance(FormationBattleController.CellScreen(cell),
                FormationBattleController.CellScreen(cell + Vector2.right));
        }

        [UnityTest]
        public IEnumerator ScoutingPansAcrossTheJoinedBoardAndKeepsPreparationAndBattleFraming()
        {
            FormationShopSession.StartNew(20261117);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                controller = Object.FindAnyObjectByType<FormationBattleController>();
                if (controller != null && controller.Ready && controller.Model != null) break;
                yield return null;
            }
            Assert.That(controller, Is.Not.Null); Assert.That(controller.Ready, Is.True);
            controller.SetMarketVisible(false);
            deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.ShopPanelTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.ShopPanelTransitioning, Is.False);
            Assert.That(controller.MarketVisible, Is.False, "The collapsed drawer leaves the normal sized preparation board unobstructed.");
            Assert.That(controller.ShopPanelVisibility, Is.EqualTo(0).Within(.001f));
            yield return WaitForCamera();
            var model = controller.Model;
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation));
            Assert.That(controller.ViewingDefenders, Is.False, "Ordinary preparation starts on the editable left army.");
            Assert.That(controller.CanEditFormationView, Is.True);
            AssertHalfAndActorHeads(false);
            AssertJoinedSeam();
            var leftPose = controller.battleCamera.transform.position;
            var sampleRows = new[] { 0, FormationBattleModel.Height / 2, FormationBattleModel.Height - 1 };
            var leftTileWidths = sampleRows.Select(row => TileScreenWidth(3, row)).ToArray();
            var shopCamera = controller.ShopModelCamera;
            Assert.That(shopCamera, Is.Not.Null);
            var shopPose = shopCamera.transform.position; var shopRotation = shopCamera.transform.rotation;
            var shopTarget = controller.ShopModelTarget;
            var positions = model.Units.Select(u => u.Position).ToArray();
            var spawns = model.Units.Select(u => u.Spawn).ToArray();
            double clock = model.Clock, remaining = model.TimeRemaining;
            int gold = controller.Shop.Gold;

            controller.SetFormationView(true);
            Assert.That(controller.ViewingDefenders, Is.True);
            Assert.That(controller.CanEditFormationView, Is.False);
            Assert.That(controller.CameraTransitioning, Is.True);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(leftPose), "Scouting starts from the current preparation view.");
            deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.CameraTransitioning && controller.battleCamera.transform.position.x <= leftPose.x + .05f &&
                Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            var movingPose = controller.battleCamera.transform.position;
            Assert.That(controller.CameraTransitioning, Is.True, "Scouting must render a position between the two views.");
            AssertJoinedSeam();
            yield return WaitForCamera();
            AssertHalfAndActorHeads(true);
            AssertJoinedSeam();
            var rightPose = controller.battleCamera.transform.position;
            Assert.That(rightPose.x - leftPose.x, Is.GreaterThan(4), "The scout view pans far enough to focus on the guard deployment.");
            Assert.That(movingPose.x, Is.InRange(leftPose.x + .05f, rightPose.x - .05f));
            Assert.That(model.Clock, Is.EqualTo(clock)); Assert.That(model.TimeRemaining, Is.EqualTo(remaining));
            CollectionAssert.AreEqual(positions, model.Units.Select(u => u.Position));
            CollectionAssert.AreEqual(spawns, model.Units.Select(u => u.Spawn));
            Assert.That(controller.Shop.Gold, Is.EqualTo(gold));
            Assert.That(controller.ShopModelCamera, Is.SameAs(shopCamera));
            Assert.That(shopCamera.transform.position, Is.EqualTo(shopPose));
            Assert.That(shopCamera.transform.rotation, Is.EqualTo(shopRotation));
            Assert.That(controller.ShopModelTarget, Is.SameAs(shopTarget));
            Assert.That(controller.battleCamera.rect, Is.EqualTo(TacticalViewport.Current.CameraRect(FormationBattleController.BattleRect)));
            Assert.That(controller.battleCamera.fieldOfView, Is.EqualTo(40));
            Assert.That(Quaternion.Angle(controller.battleCamera.transform.rotation, Quaternion.Euler(60, 0, 0)), Is.LessThan(.01f));

            int guardSampleColumn = FormationBattleModel.DeploymentColumns + 3;
            float scoutingTileWidth = TileScreenWidth(guardSampleColumn, FormationBattleModel.Height / 2);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(rightPose), "The defender camera must never drift.");
            Assert.That(model.Units.Any(u => u.Enabled && controller.FormationActorVisibleInView(u)), Is.True,
                "Allied actors near the seam remain present in the scout view.");
            controller.ToggleFormationView();
            Assert.That(controller.CameraTransitioning, Is.True, "Returning also pans back across the board.");
            Assert.That(controller.CanEditFormationView, Is.False, "Dragging stays disabled while the board moves under the pointer.");
            deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.CameraTransitioning && controller.battleCamera.transform.position.x >= rightPose.x - .05f &&
                Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert.That(controller.CameraTransitioning, Is.True);
            Assert.That(controller.battleCamera.transform.position.x, Is.InRange(leftPose.x + .05f, rightPose.x - .05f));
            yield return WaitForCamera();
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(leftPose), "Returning restores the same fixed pose.");
            AssertJoinedSeam();
            Assert.That(controller.ViewingDefenders, Is.False); Assert.That(controller.CanEditFormationView, Is.True);
            var first = model.Units.Single(u => u.Enabled && u.ArtId == "M21");
            var second = model.Units.Single(u => u.Enabled && u.ArtId == "M11");
            var a = first.Spawn; var b = second.Spawn;
            controller.ProcessPointer(EventType.MouseDown, FormationBattleController.CellScreen(a));
            controller.ProcessPointer(EventType.MouseDrag, FormationBattleController.CellScreen(b));
            controller.ProcessPointer(EventType.MouseUp, FormationBattleController.CellScreen(b));
            first = model.Units.Single(u => u.ArtId == "M21"); second = model.Units.Single(u => u.ArtId == "M11");
            Assert.That(first.Spawn, Is.EqualTo(b)); Assert.That(second.Spawn, Is.EqualTo(a), "The focused camera must retain accurate drag-to-cell projection.");

            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(leftPose), "Dragging and swapping must not reframe the camera.");
            deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (!controller.RosterArtReady && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.RosterArtReady, Is.True);
            controller.StartBattle(); model.Paused = true;
            yield return null;
            Assert.That(controller.ViewingDefenders, Is.True, "The real battle transition must select the guard half automatically.");
            clock = model.Clock;
            yield return WaitForCamera();
            Assert.That(model.Clock, Is.EqualTo(clock), "View changes must not consume paused simulation time.");
            Assert.That(controller.CurrentFormationCameraSafeRect, Is.EqualTo(FormationBattleController.BattleCameraSafeRect));
            Assert.That(controller.CombatViewActive, Is.True);
            Assert.That(controller.FormationPreparationUiVisible, Is.False);
            Assert.That(controller.battleCamera.transform.position, Is.Not.EqualTo(rightPose), "Combat uses its own fixed enlarged view after hiding the preparation panels.");
            float battleTileWidth = TileScreenWidth(guardSampleColumn, FormationBattleModel.Height / 2);
            Assert.That(battleTileWidth, Is.GreaterThan(scoutingTileWidth * 1.08f), "Battles retain their larger scale compared with guard scouting.");
            for (int index = 0; index < sampleRows.Length; index++)
            {
                int row = sampleRows[index];
                Assert.That(TileScreenWidth(guardSampleColumn, row), Is.EqualTo(leftTileWidths[index]).Within(.1f),
                    $"Corresponding floor tiles in row {row} must be the same size in deployment and battle.");
            }
            var combatPose = controller.battleCamera.transform.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(combatPose), "Paused combat keeps its dedicated fixed camera pose.");
            controller.ToggleFormationView();
            Assert.That(controller.ViewingDefenders, Is.True, "Preparation scouting must not pull an active battle back to the left.");
            controller.Retry();
            yield return WaitForCamera();
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation));
            Assert.That(model.Clock, Is.Zero); Assert.That(controller.ViewingDefenders, Is.False);
            Assert.That(controller.CanEditFormationView, Is.True);
            Assert.That(controller.CurrentFormationCameraSafeRect, Is.EqualTo(FormationBattleController.LeftPreparationCameraSafeRect));
            Assert.That(controller.FormationPreparationUiVisible, Is.True);
            Assert.That(controller.CombatViewActive, Is.False);
            AssertHalfAndActorHeads(false);
        }
    }
}
