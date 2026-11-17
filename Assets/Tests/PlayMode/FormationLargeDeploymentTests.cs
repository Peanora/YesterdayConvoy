using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationLargeDeploymentTests
    {
        FormationBattleController controller;
        Vector2 dragPoint;

        [TearDown]
        public void FreezeFixture()
        {
            if (controller != null && controller.Model != null) controller.Model.Paused = true;
        }

        FormationFighter Fighter(string artId) => controller.Model.Units.Single(unit => unit.ArtId == artId);
        SpriteRenderer Body(FormationFighter actor) => controller.GetComponentsInChildren<SpriteRenderer>()
            .Single(renderer => renderer.enabled && renderer.gameObject.layer != 31 && renderer.name == actor.Name &&
                FormationArtAssertions.IsActualIdleFrame(actor.ArtId,renderer.sprite));

        IEnumerator WaitForPresentation()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (!controller.RosterArtReady && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.RosterArtReady, Is.True, "The real large-unit action packages must finish preparing.");
            yield return null;
            controller.SetFormationView(false);
            deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.CameraTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.CanEditFormationView, Is.True);
            yield return null;
        }

        [UnityTest] public IEnumerator UndeployedNativePurchaseWarmsRealIdleAndLaterLongClipSegment()
        {
            // Explicit legal-stock presentation fixture, not natural recruitment odds.
            // B10 has a 23-second idle split across three independent cache segments.
            ArtMemoryCache.Clear();
            var session=FormationShopSession.StartNew(20261117);
            session.Shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M07",Star=1},
                new ShopTrialModel.Piece{Id="CARD_M10",Star=1},new ShopTrialModel.Piece{Id="CARD_M08",Star=1}},
                new[]{"CARD_B10"},level:10,xp:0,gold:80);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+30;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {controller=Object.FindAnyObjectByType<FormationBattleController>();if(controller!=null&&controller.Ready)break;yield return null;}
            Assert.That(controller,Is.Not.Null);Assert.That(controller.Ready,Is.True);
            var source=FormationUnitArt.For("B10");
            Assert.That(source.Root,Is.EqualTo("BossRoster/B10/main"));
            Assert.That(source.Frame("idle",0,true),Is.Null,"A shop offer must not decode the entire native body library.");
            var purchase=controller.BuyOffer(0);Assert.That(purchase.Success,Is.True);
            Assert.That(session.Seats.InLineup(purchase.Instance),Is.False);
            Assert.That(controller.Model.Units.Any(u=>u.ArtId=="B10"),Is.False,"The tested new card remains undeployed.");
            deadline=Time.realtimeSinceStartupAsDouble+20;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                var body=controller.ShopBenchModel(purchase.Instance);
                if(body!=null&&FormationArtAssertions.IsActualIdleFrame("B10",body.sprite))break;
                yield return null;
            }
            var reserve=controller.ShopBenchModel(purchase.Instance);
            Assert.That(reserve,Is.Not.Null);
            Assert.That(FormationArtAssertions.IsActualIdleFrame("B10",reserve.sprite),Is.True,"The reserve must receive the actual source idle sprite and texture.");
            var first=controller.FormationShopIdleFrame("CARD_B10",0);
            Assert.That(FormationArtAssertions.IsActualIdleFrame("B10",first),Is.True);
            Assert.That(source.Frame("idle",12,true),Is.Null,"The later long-idle segment must still be cold before its own window is requested.");
            int before=ArtMemoryCache.PreparedLoads;
            var pending=controller.FormationShopIdleFrame("CARD_B10",12);
            Assert.That(FormationArtAssertions.IsActualIdleFrame("B10",pending),Is.True,"Retain a valid last source frame during the asynchronous next-window upload.");
            deadline=Time.realtimeSinceStartupAsDouble+20;
            while(source.Frame("idle",12,true)==null&&Time.realtimeSinceStartupAsDouble<deadline)
            {controller.FormationShopIdleFrame("CARD_B10",12);yield return null;}
            var later=controller.FormationShopIdleFrame("CARD_B10",12);
            Assert.That(FormationArtAssertions.IsActualIdleFrame("B10",later),Is.True);
            Assert.That(later,Is.SameAs(source.Frame("idle",12,true)));
            Assert.That(later.texture.name,Is.Not.EqualTo(first.texture.name),"The second native segment must use its own real atlas page.");
            Assert.That(ArtMemoryCache.PreparedLoads,Is.GreaterThan(before));
            Assert.That(session.Seats.InLineup(purchase.Instance),Is.False);controller.Shop.Validate();
        }

        Vector2 Project(Vector3 point) => TacticalViewport.Current.FromScreen(controller.battleCamera.WorldToScreenPoint(point));
        Rect BodyScreenRect(SpriteRenderer body)
        {
            var bounds = body.sprite.bounds;
            var points = new[] { new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.min.x, bounds.max.y),
                new Vector2(bounds.max.x, bounds.min.y), new Vector2(bounds.max.x, bounds.max.y) }
                .Select(point => Project(body.transform.TransformPoint(new Vector3(body.flipX ? -point.x : point.x, point.y, 0)))).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
        }

        void AssertRenderedAtFootprintCenter(FormationFighter actor)
        {
            var body = Body(actor);
            Assert.That(body.sprite.texture, Is.Not.Null);
            Assert.That(Vector3.Distance(body.transform.position, controller.ActorWorldPoint(actor.SpawnCenter)), Is.LessThan(.001f), "The displayed actor must stand at the complete footprint centre.");
            Assert.That(actor.Position, Is.EqualTo(actor.SpawnCenter));
            var visible = BodyScreenRect(body); var safe = FormationBattleController.FormationCameraSafeRect;
            Assert.That(visible.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 2));
            Assert.That(visible.xMax, Is.LessThanOrEqualTo(safe.xMax + 2));
            Assert.That(visible.yMin, Is.GreaterThanOrEqualTo(safe.yMin - 2));
            Assert.That(visible.yMax, Is.LessThanOrEqualTo(safe.yMax + 2));
        }

        void GrabBody(FormationFighter actor)
        {
            var body = Body(actor); var bounds = body.sprite.bounds;
            // Grab the displayed torso. Its ground ray can land away from the anchor; the
            // production controller owns that offset, and this test never reconstructs it.
            Vector2 point = Project(body.transform.TransformPoint(new Vector3(bounds.center.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, .35f), 0)));
            controller.ProcessPointer(EventType.MouseDown, point);
            Assert.That(controller.SelectedSlot, Is.EqualTo(actor.Slot), "The actual large body must be selectable.");
        }

        IEnumerator DragUntil(Func<FormationDeploymentPreview, bool> accept)
        {
            // Probe visible ground points through production pointer input. The accepted preview,
            // rather than a guessed cursor-to-anchor formula, defines the intended drop.
            for (int row = 0; row < FormationBattleModel.Height; row++)
                for (int column = 0; column < FormationBattleModel.Width; column++)
                {
                    Vector2 point = FormationBattleController.CellScreen(new Vector2(column + .08f, row + .08f));
                    if (!FormationBattleController.FormationCameraSafeRect.Contains(point)) continue;
                    controller.ProcessPointer(EventType.MouseDrag, point);
                    var preview = controller.DeploymentPreview;
                    if (preview == null || !accept(preview)) continue;
                    dragPoint = point;
                    double deadline = Time.realtimeSinceStartupAsDouble + 3;
                    while ((!controller.DragPreviewVisible || controller.DeploymentGhost == null || controller.DeploymentGhost.sprite == null) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                    Assert.That(controller.DragPreviewVisible, Is.True, "The preview must render the already-prepared native actor.");
                    yield return null;
                    yield break;
                }
            Assert.Fail("No visible production pointer drop produced the required preview.");
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.That(frame, Is.Not.Null, "A real GPU frame is required for this presentation fixture.");
            try
            {
                Assert.That(frame.width, Is.EqualTo(Screen.width)); Assert.That(frame.height, Is.EqualTo(Screen.height));
                var pixels = frame.GetPixels32(); byte low = 255, high = 0;
                for (int i = 0; i < pixels.Length; i += Math.Max(1, pixels.Length / 4096))
                { byte value = Math.Max(pixels[i].r, Math.Max(pixels[i].g, pixels[i].b)); low = Math.Min(low, value); high = Math.Max(high, value); }
                Assert.That(high - low, Is.GreaterThan(16), "The captured game frame must contain visible scene/UI content.");
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/formation-refinement"));
                Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), frame.EncodeToPNG());
            }
            finally { Object.Destroy(frame); }
        }

        [UnityTest]
        public IEnumerator NativeTwoByTwoActorsAnimatePreviewDropAndCancelInPreparation()
        {
            // Explicit presentation fixture: legal finite stock, not natural recruitment,
            // campaign economy, balance or original boss-skill evidence.
            var session = FormationShopSession.StartNew(20261117);
            session.Shop.Preset(new[] { new ShopTrialModel.Piece { Id = "CARD_M07", Star = 1 },
                new ShopTrialModel.Piece { Id = "CARD_M20", Star = 1 }, new ShopTrialModel.Piece { Id = "CARD_M28", Star = 1 } },
                Array.Empty<string>(), level: 6, xp: 0, gold: 80);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline = Time.realtimeSinceStartupAsDouble + 600;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                controller = Object.FindAnyObjectByType<FormationBattleController>();
                if (controller != null && controller.Ready && controller.Model != null) break;
                yield return null;
            }
            Assert.That(controller, Is.Not.Null); Assert.That(controller.Ready, Is.True);
            Assert.That(controller.Session, Is.SameAs(session));
            foreach (string id in new[] { "CARD_M20", "CARD_M28" })
            {
                int instance = session.Shop.Bench.Single(piece => piece.Id == id).Instance;
                Assert.That(session.Seats.InLineup(instance), Is.False, "Large actors begin in the fixture's actual reserve.");
                Assert.That(controller.MovePiece(instance, true, session.Seats.FirstEmpty(true, session.Shop.Level)).Success, Is.True);
            }
            yield return WaitForPresentation();
            Assert.That(session.TryDeploy(Fighter("M20").Instance, new Vector2Int(0, 1)).Success, Is.True);
            Assert.That(session.TryDeploy(Fighter("M28").Instance, new Vector2Int(3, 4)).Success, Is.True);
            Assert.That(session.TryDeploy(Fighter("M07").Instance, new Vector2Int(5, 0)).Success, Is.True);
            controller.SetMarketVisible(false);
            deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.ShopPanelTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.ShopPanelTransitioning, Is.False);
            yield return WaitForPresentation();

            var model = controller.Model;
            foreach (string id in new[] { "M20", "M28" })
            {
                var actor = Fighter(id); Assert.That(actor.Footprint, Is.EqualTo(new Vector2Int(2, 2)));
                AssertRenderedAtFootprintCenter(actor);
                Assert.That(BodyScreenRect(Body(actor)).height, Is.GreaterThan(BodyScreenRect(Body(Fighter("M07"))).height * 1.1f), "Native large actors must have visibly different size from the ordinary soldier.");
            }
            int gold = controller.Shop.Gold; double clock = model.Clock;
            Assert.That(clock, Is.Zero); Assert.That(model.Phase, Is.EqualTo(FormationPhase.Preparation));
            var initial20 = Body(Fighter("M20")).sprite; var initial28 = Body(Fighter("M28")).sprite;
            bool animated20 = false, animated28 = false;
            yield return Capture("playmode-large-idle-a");
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            while ((!animated20 || !animated28) && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                animated20 |= Body(Fighter("M20")).sprite != initial20;
                animated28 |= Body(Fighter("M28")).sprite != initial28;
            }
            Assert.That(animated20 && animated28, Is.True, "Both existing native idle sequences must change Sprite frames while the preparation simulation clock stays at zero.");
            Assert.That(model.Clock, Is.EqualTo(clock));
            yield return Capture("playmode-large-idle-b");

            var moving = Fighter("M20"); int movingInstance = moving.Instance; var originalCells = moving.DeploymentCells;
            GrabBody(moving);
            yield return DragUntil(preview => preview.Valid && preview.SwapInstance == 0 && preview.Cells != originalCells);
            var expectedCells = controller.DeploymentPreview.Cells;
            Assert.That(expectedCells.size, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(Fighter("M20").DeploymentCells, Is.EqualTo(originalCells), "A visible drag preview must leave the real deployment untouched.");
            var ghost = controller.DeploymentGhost;
            Assert.That(FormationArtAssertions.IsActualIdleFrame("M20",ghost.sprite),Is.True,"The ghost must use the actual Patriot source idle and matching atlas texture.");
            Assert.That(ghost.color.a, Is.InRange(.01f, .99f));
            yield return Capture("playmode-large-drag-valid");
            controller.ProcessPointer(EventType.MouseUp, dragPoint);
            Assert.That(Fighter("M20").DeploymentCells, Is.EqualTo(expectedCells), "The final complete footprint must exactly match the preview, including the production grab offset.");
            Assert.That(controller.DragPreviewVisible, Is.False);
            yield return WaitForPresentation(); AssertRenderedAtFootprintCenter(Fighter("M20"));
            yield return Capture("playmode-large-preparation");

            var beforeInvalid = model.Units.ToDictionary(unit => unit.Instance, unit => unit.DeploymentCells);
            var seat = session.Seats.For(movingInstance).Value;
            GrabBody(Fighter("M20"));
            yield return DragUntil(preview => !preview.Valid && preview.Cells.xMin == FormationBattleModel.DeploymentColumns-1 && preview.Cells.size == new Vector2Int(2, 2));
            Assert.That(controller.DeploymentPreview.Failure, Is.Not.Empty);
            yield return Capture("playmode-large-drag-invalid");
            controller.ProcessPointer(EventType.MouseUp, dragPoint);
            Assert.That(model.Units.All(unit => beforeInvalid[unit.Instance] == unit.DeploymentCells), Is.True, "A right-edge two-cell footprint must fail without moving any actor.");
            Assert.That(session.Seats.For(movingInstance).Value.Lineup, Is.EqualTo(seat.Lineup));
            Assert.That(session.Seats.For(movingInstance).Value.Index, Is.EqualTo(seat.Index));

            GrabBody(Fighter("M20"));
            yield return DragUntil(preview => preview.Valid && preview.Cells != Fighter("M20").DeploymentCells);
            controller.SetFormationView(true);
            Assert.That(controller.DragPreviewVisible, Is.False); Assert.That(controller.DeploymentPreview, Is.Null);
            controller.ProcessPointer(EventType.MouseUp, dragPoint);
            Assert.That(model.Units.All(unit => beforeInvalid[unit.Instance] == unit.DeploymentCells), Is.True, "Switching to scouting must cancel the held deployment before any late release.");
            deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (controller.CameraTransitioning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(controller.ViewingDefenders, Is.True); Assert.That(controller.CameraTransitioning, Is.False);
            yield return Capture("playmode-large-scouting");
            Assert.That(model.Clock, Is.Zero); Assert.That(controller.Shop.Gold, Is.EqualTo(gold));
            controller.Shop.Validate();
            Debug.Log("FORMATION_LARGE_DEPLOYMENT_FIXTURE: native M20/M28 GPU frames, real 2x2 pointer preview/drop/rejection/cancel, animated idle at clock zero. Preset stock is not natural acquisition or balance evidence.");
        }
    }
}
