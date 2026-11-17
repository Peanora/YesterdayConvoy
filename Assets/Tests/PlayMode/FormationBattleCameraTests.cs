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
            Assert.That(controller.CameraTransitioning, Is.False, "A focused camera transition must finish even while combat is paused.");
            yield return null;
        }

        void AssertInFocusedFrame(Vector3 world, string message)
        {
            var projected = controller.battleCamera.WorldToScreenPoint(world);
            var point = TacticalViewport.Current.FromScreen(projected);
            var safe = FormationBattleController.FormationCameraSafeRect;
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
                AssertInFocusedFrame(controller.ActorWorldPoint(actor) + controller.battleCamera.transform.up * controller.ActorPresentationHeight(actor), actor.Name + " complete presented height must fit above the original A lineup.");
            }
        }

        [UnityTest]
        public IEnumerator FocusedViewsKeepPreparationAndOriginalShopAndFollowBattleAndRetry()
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
            var leftPose = controller.battleCamera.transform.position;
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
            Assert.That(controller.CameraTransitioning, Is.False, "Scouting cuts directly between two fixed views without panning.");
            yield return WaitForCamera();
            AssertHalfAndActorHeads(true);
            Assert.That(controller.battleCamera.transform.position.x - leftPose.x, Is.GreaterThan(4), "The fixed defender view must be distinct from the fixed deployment view.");
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

            var rightPose = controller.battleCamera.transform.position;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(rightPose), "The defender camera must never drift.");
            Assert.That(model.Units.All(u => !controller.FormationActorVisibleInView(u)), Is.True);
            controller.ToggleFormationView();
            yield return WaitForCamera();
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(leftPose), "Returning restores the same fixed pose.");
            Assert.That(model.Defenders.All(d => !controller.FormationActorVisibleInView(d)), Is.True);
            Assert.That(controller.ViewingDefenders, Is.False); Assert.That(controller.CanEditFormationView, Is.True);
            var first = model.Units.Single(u => u.Enabled && u.ArtId == "M07");
            var second = model.Units.Single(u => u.Enabled && u.ArtId == "M08");
            var a = first.Spawn; var b = second.Spawn;
            controller.ProcessPointer(EventType.MouseDown, FormationBattleController.CellScreen(a));
            controller.ProcessPointer(EventType.MouseDrag, FormationBattleController.CellScreen(b));
            controller.ProcessPointer(EventType.MouseUp, FormationBattleController.CellScreen(b));
            first = model.Units.Single(u => u.ArtId == "M07"); second = model.Units.Single(u => u.ArtId == "M08");
            Assert.That(first.Spawn, Is.EqualTo(b)); Assert.That(second.Spawn, Is.EqualTo(a), "The focused camera must retain accurate drag-to-cell projection.");

            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(leftPose), "Dragging and swapping must not reframe the camera.");
            var redTile = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Single(t => t.name == "Tile " + FormationBattleModel.DeploymentColumns.ToString("00") + ",00");
            var enemyTile = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Single(t => t.name == "Tile " + (FormationBattleModel.DeploymentColumns + 1).ToString("00") + ",00");
            Assert.That((controller.battleCamera.cullingMask & (1 << redTile.gameObject.layer)) != 0, Is.True);
            Assert.That((controller.battleCamera.cullingMask & (1 << enemyTile.gameObject.layer)) == 0, Is.True, "The left view excludes the defender tiles.");
            deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (!controller.RosterArtReady && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.RosterArtReady, Is.True);
            controller.StartBattle(); model.Paused = true;
            yield return null;
            Assert.That(controller.ViewingDefenders, Is.True, "The real battle transition must select the guard half automatically.");
            clock = model.Clock;
            yield return WaitForCamera();
            Assert.That(model.Clock, Is.EqualTo(clock), "View changes must not consume paused simulation time.");
            Assert.That(controller.battleCamera.transform.position, Is.EqualTo(rightPose), "Starting combat uses the same fixed defender pose.");
            controller.ToggleFormationView();
            Assert.That(controller.ViewingDefenders, Is.True, "Preparation scouting must not pull an active battle back to the left.");
            controller.Retry();
            yield return WaitForCamera();
            Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation));
            Assert.That(model.Clock, Is.Zero); Assert.That(controller.ViewingDefenders, Is.False);
            Assert.That(controller.CanEditFormationView, Is.True);
            AssertHalfAndActorHeads(false);
        }
    }
}
