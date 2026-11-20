using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class FormationAirborneDragTests
    {
        FormationBattleController controller;

        [TearDown]
        public void FreezeFixture()
        {if(controller!=null&&controller.Model!=null)controller.Model.Paused=true;}

        Vector2 Project(Vector3 point)=>TacticalViewport.Current.FromScreen(controller.battleCamera.WorldToScreenPoint(point));

        [UnityTest]
        public IEnumerator HeldNativeActorFollowsPointerBetweenGridSnapsAboveGroundShadow()
        {
            FormationShopSession.StartNew(20261117);
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=Object.FindAnyObjectByType<FormationBattleController>();
                if(controller!=null&&controller.Ready&&controller.Model!=null&&controller.RosterArtReady)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);Assert.That(controller.Ready&&controller.RosterArtReady,Is.True);
            controller.SetMarketVisible(false);controller.SetFormationView(false,true);
            deadline=Time.realtimeSinceStartupAsDouble+3;
            while(controller.ShopPanelTransitioning&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            yield return null;

            var actor=controller.Model.Units.Single(unit=>unit.Enabled&&unit.ArtId=="M21");
            var body=controller.GetComponentsInChildren<SpriteRenderer>().Single(renderer=>renderer.name==actor.Name&&
                renderer.gameObject.layer!=31&&renderer.sprite!=null);
            var source=FormationUnitArt.For(actor.ArtId);
            deadline=Time.realtimeSinceStartupAsDouble+3;
            double idleTime;
            do
            {
                yield return new WaitForEndOfFrame();
                idleTime=Time.unscaledTimeAsDouble;
            } while(source.Frame("idle",idleTime)==source.Frame("idle",idleTime*2)&&Time.realtimeSinceStartupAsDouble<deadline);
            var expectedIdle=source.Frame("idle",idleTime);
            Assert.That(expectedIdle,Is.Not.Null);
            Assert.That(expectedIdle,Is.Not.SameAs(source.Frame("idle",idleTime*2)));
            Assert.That(body.sprite,Is.SameAs(expectedIdle),"The deployed actor must sample idle at 1× before combat.");
            Assert.That(source.Frame("idle",.25),Is.Not.SameAs(source.Frame("idle",.5)));
            Assert.That(controller.FormationShopIdleFrame(actor.ArtId,.25),Is.SameAs(source.Frame("idle",.25)),
                "The reserve stage must sample idle at 1× as well.");
            var bounds=body.sprite.bounds;
            var grab=Project(body.transform.TransformPoint(new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,.35f),0)));
            var original=actor.DeploymentCells;int instance=actor.Instance;
            controller.ProcessPointer(EventType.MouseDown,grab);
            Assert.That(controller.SelectedSlot,Is.EqualTo(actor.Slot));

            Vector2 pointer=default;bool found=false;
            for(int row=0;row<FormationBattleModel.Height&&!found;row++)
                for(int column=0;column<FormationBattleModel.DeploymentColumns;column++)
                {
                    var candidate=FormationBattleController.CellScreen(new Vector2(column+.12f,row+.12f));
                    if(!controller.CurrentFormationCameraSafeRect.Contains(candidate))continue;
                    controller.ProcessPointer(EventType.MouseDrag,candidate);
                    if(controller.DeploymentPreview==null||!controller.DeploymentPreview.Valid||
                        controller.DeploymentPreview.SwapInstance!=0||controller.DeploymentPreview.Cells==original||!controller.DragPreviewVisible)continue;
                    pointer=candidate;found=true;break;
                }
            Assert.That(found,Is.True,"A visible legal destination must be reachable through production pointer input.");
            var landing=controller.DeploymentPreview.Cells;var ghost=controller.DeploymentGhost;
            var shadow=ghost.GetComponent<ActorGrounding>().Shadow;
            Assert.That(FormationArtAssertions.IsActualIdleFrame(actor.ArtId,ghost.sprite),Is.True);
            Assert.That(ghost.sprite,Is.SameAs(source.Frame("idle",Time.unscaledTimeAsDouble)),
                "The held preview must match the deployed actor's idle clock.");
            Assert.That(shadow.enabled,Is.True);
            Assert.That(shadow.transform.position.y,Is.EqualTo(FormationBattleController.WorldPoint(Vector2.zero).y+.016f).Within(.001f));
            Assert.That(ghost.transform.position.y-shadow.transform.position.y,Is.GreaterThan(.5f),"The held body must visibly leave its ground shadow.");
            var held=ghost.transform.position;var heldScreen=Project(held);

            controller.ProcessPointer(EventType.MouseDrag,pointer+new Vector2(2,0));
            Assert.That(controller.DeploymentPreview.Cells,Is.EqualTo(landing),"The landing preview can stay in one cell while the mouse moves within it.");
            Assert.That(Vector3.Distance(held,ghost.transform.position),Is.GreaterThan(.001f),"The body must move continuously instead of waiting for the next grid snap.");
            var movedScreen=Project(ghost.transform.position)-heldScreen;
            Assert.That(movedScreen.x,Is.InRange(1,3));Assert.That(Mathf.Abs(movedScreen.y),Is.LessThan(.1f));
            Assert.That(controller.Model.Units.Single(unit=>unit.Instance==instance).DeploymentCells,Is.EqualTo(original),"Lifting and moving the preview cannot commit the roster.");
            Assert.That(controller.Model.Clock,Is.Zero);

            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Assert.That(frame,Is.Not.Null);
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts/formation-refinement"));
                Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,"playmode-airborne-drag.png"),frame.EncodeToPNG());
            }
            finally{if(frame!=null)Object.Destroy(frame);}

            controller.ProcessPointer(EventType.MouseUp,pointer+new Vector2(2,0));
            Assert.That(controller.Model.Units.Single(unit=>unit.Instance==instance).DeploymentCells,Is.EqualTo(landing));
            Assert.That(controller.DragPreviewVisible,Is.False);Assert.That(shadow.enabled,Is.False);

            int reserveSeat=controller.Session.Seats.FirstEmpty(false,10);
            var bench=FormationBattleController.FormationReserveRect(reserveSeat).center;
            var gap=new Vector2(bench.x,(FormationBattleController.BoardInputRect.yMax+FormationBattleController.FormationBenchRect.yMin)/2);
            bounds=body.sprite.bounds;
            var regrab=Project(body.transform.TransformPoint(new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,.35f),0)));
            controller.ProcessPointer(EventType.MouseDown,regrab);
            controller.ProcessPointer(EventType.MouseDrag,gap);
            Assert.That(controller.DragPreviewVisible,Is.True,"The held actor must remain visible between the board and the bench.");
            Assert.That(controller.DeploymentPreview,Is.Null,"The gap is not a deployment cell.");
            Assert.That(shadow.enabled,Is.False);
            var gapPosition=ghost.transform.position;
            controller.ProcessPointer(EventType.MouseDrag,bench);
            Assert.That(controller.DragPreviewVisible,Is.True,"The held actor must remain visible in front of the bench.");
            Assert.That(controller.DeploymentPreview,Is.Null);
            Assert.That(ghost.transform.position,Is.Not.EqualTo(gapPosition));
            Assert.That(FormationArtAssertions.IsActualIdleFrame(actor.ArtId,ghost.sprite),Is.True);
            Assert.That(controller.Model.Units.Single(unit=>unit.Instance==instance).DeploymentCells,Is.EqualTo(landing));
            yield return new WaitForEndOfFrame();
            Assert.That(controller.DragPreviewVisible,Is.True);
            controller.ProcessPointer(EventType.MouseUp,bench);
            Assert.That(controller.Session.Seats.At(false,reserveSeat),Is.EqualTo(instance));
            Assert.That(controller.Model.Units.Any(unit=>unit.Instance==instance),Is.False);
            Assert.That(controller.DragPreviewVisible,Is.False);

            controller.ProcessPointer(EventType.MouseDown,bench);
            controller.ProcessPointer(EventType.MouseUp,bench);
            controller.ProcessPointer(EventType.MouseDown,pointer);
            controller.ProcessPointer(EventType.MouseUp,pointer);
            Assert.That(controller.Session.Seats.At(false,reserveSeat),Is.EqualTo(instance),"Clicking a tile must not deploy a selected bench character.");
            Assert.That(controller.Model.Clock,Is.Zero);controller.Shop.Validate();
        }
    }
}
