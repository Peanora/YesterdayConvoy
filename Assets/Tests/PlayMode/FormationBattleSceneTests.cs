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
    public sealed class FormationBattleSceneTests
    {
        FormationBattleController controller;
        float previousScale;
        [SetUp] public void SaveClock()=>previousScale=Time.timeScale;
        [TearDown] public void RestoreClock(){if(controller!=null&&controller.Model!=null)controller.Model.Paused=true;Time.timeScale=previousScale;}
        IEnumerator Ready()
        {
            yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+600;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=Object.FindAnyObjectByType<FormationBattleController>();
                if(controller!=null&&controller.Ready&&controller.Model!=null)break;
                yield return null;
            }
            Assert.That(controller,Is.Not.Null);Assert.That(controller.Ready,Is.True,"Existing animation packages must finish preparing.");
            Assert.That(controller.Model,Is.Not.Null);yield return null;
        }
        static MeshFilter[] Terrain()=>Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(m=>m.gameObject.scene==SceneManager.GetActiveScene()&&m.sharedMesh!=null).ToArray();
        void Drag(Vector2 from,Vector2 to)
        {
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(from));
            controller.ProcessPointer(EventType.MouseDrag,FormationBattleController.CellScreen(to));
            controller.ProcessPointer(EventType.MouseUp,FormationBattleController.CellScreen(to));
        }
        [UnityTest] public IEnumerator AuthoredRectangleAndOriginalActorsRemainVisibleAndSurviveRetry()
        {
            yield return Ready();
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo("Assets/Scenes/FormationBattle.unity"));
            var meshes=Terrain();Assert.That(meshes.Length,Is.GreaterThan(20),"The saved independent scene must contain terrain before combat.");
            var positions=meshes.ToDictionary(m=>m,m=>m.transform.position);
            var cam=controller.battleCamera;Assert.That(cam,Is.Not.Null);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.x,60)),Is.LessThan(.1f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y,0)),Is.LessThan(.1f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.z,0)),Is.LessThan(.1f));
            var sprites=controller.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null).ToArray();
            Assert.That(sprites.Length,Is.GreaterThanOrEqualTo(controller.Model.Units.Count(u=>u.Enabled)+controller.Model.Defenders.Count));
            Assert.That(sprites.All(s=>s.sprite.texture!=null),Is.True);
            foreach(var actor in controller.Model.Units.Where(u=>u.Enabled).Cast<FormationActor>().Concat(controller.Model.Defenders))
            {
                var projected=cam.WorldToScreenPoint(FormationBattleController.WorldPoint(actor.Position));
                var point=TacticalViewport.Current.FromScreen(projected);
                Assert.That(projected.z,Is.GreaterThan(0),actor.Name+" is behind the camera.");
                Assert.That(point.x,Is.InRange(12,1268),actor.Name+" horizontal projection");
                Assert.That(point.y,Is.InRange(114,531),actor.Name+" feet must avoid top/bottom controls.");
            }
            yield return new WaitForEndOfFrame();
            var screenshot=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(screenshot,Is.Not.Null);
            try
            {
                Assert.That(screenshot.width,Is.EqualTo(Screen.width));Assert.That(screenshot.height,Is.EqualTo(Screen.height));
                var pixels=screenshot.GetPixels32();int visible=0,samples=0;byte low=255,high=0;
                for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/4096))
                {byte v=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));low=Math.Min(low,v);high=Math.Max(high,v);if(v>24)visible++;samples++;}
                Assert.That(visible,Is.GreaterThan(samples/20));Assert.That(high-low,Is.GreaterThan(16));
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../artifacts/formation-demo"));Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory,"playmode-preparation.png"),screenshot.EncodeToPNG());
            }
            finally{Object.Destroy(screenshot);}
            controller.StartBattle();controller.Model.Advance(.5f);yield return null;controller.Retry();yield return null;
            Assert.That(controller.Model.Phase,Is.EqualTo(FormationPhase.Preparation));Assert.That(controller.Model.Clock,Is.Zero);
            CollectionAssert.AreEquivalent(meshes,Terrain());
            Assert.That(Terrain().All(m=>m.transform.position==positions[m]),Is.True,"Retry must preserve saved terrain objects and edits.");
            Debug.Log("FORMATION_SCENE_CHECK savedMeshes="+meshes.Length+" originalSprites="+sprites.Length+" screenshot=playmode-preparation.png");
        }
        [UnityTest] public IEnumerator PreparationPointerSwapsOccupiedCellsRejectsEnemyHalfAndLocksAtStart()
        {
            yield return Ready();var m=controller.Model;
            var first=m.Units.First(u=>u.Enabled);var second=m.Units.First(u=>u.Enabled&&u.Slot!=first.Slot);
            Vector2 a=first.Spawn,b=second.Spawn;int enabled=m.Units.Count(u=>u.Enabled);
            Vector2 head=FormationBattleController.CellScreen(first.Spawn)+new Vector2(0,-35);
            controller.ProcessPointer(EventType.MouseDown,head);controller.ProcessPointer(EventType.MouseUp,head);
            Assert.That(first.Spawn,Is.EqualTo(a),"Clicking the visible character head must select without relocating its ground cell.");
            Assert.That(controller.SelectedSlot,Is.EqualTo(first.Slot));
            Drag(a,b);Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
            Assert.That(controller.SelectedSlot,Is.EqualTo(first.Slot));
            Drag(first.Spawn,new Vector2(10,4));Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
            controller.ProcessPointer(EventType.MouseDown,FormationBattleController.CellScreen(first.Spawn));
            controller.ProcessPointer(EventType.MouseDrag,new Vector2(640,550));controller.ProcessPointer(EventType.MouseUp,new Vector2(640,550));
            Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(m.Units.Count(u=>u.Enabled),Is.EqualTo(enabled));
            Assert.That(m.Phase,Is.EqualTo(FormationPhase.Preparation));Assert.That(m.Clock,Is.Zero);
            controller.StartBattle();Assert.That(m.Phase,Is.EqualTo(FormationPhase.Fighting));
            Drag(first.Spawn,second.Spawn);Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
            controller.Retry();Assert.That(m.Phase,Is.EqualTo(FormationPhase.Preparation));
            Assert.That(first.Spawn,Is.EqualTo(b));Assert.That(second.Spawn,Is.EqualTo(a));
        }
        [UnityTest] public IEnumerator PauseFreezesSharedCombatClockAndConfiguredDeadlineUsesRealLossPath()
        {
            yield return Ready();var m=controller.Model;controller.StartBattle();m.Advance(.25f);m.Paused=true;
            double clock=m.Clock,remaining=m.TimeRemaining;
            var positions=m.Units.Select(u=>u.Position).ToArray();var hp=m.Units.Select(u=>u.Health).ToArray();
            var charge=m.Units.Select(u=>u.SkillCharge).ToArray();var revive=m.Units.Select(u=>u.RespawnRemaining).ToArray();
            m.Advance(2);yield return new WaitForSecondsRealtime(.12f);
            Assert.That(m.Clock,Is.EqualTo(clock));Assert.That(m.TimeRemaining,Is.EqualTo(remaining));
            CollectionAssert.AreEqual(positions,m.Units.Select(u=>u.Position));CollectionAssert.AreEqual(hp,m.Units.Select(u=>u.Health));
            CollectionAssert.AreEqual(charge,m.Units.Select(u=>u.SkillCharge));CollectionAssert.AreEqual(revive,m.Units.Select(u=>u.RespawnRemaining));
            controller.Retry();m.LimitSeconds=.1;controller.StartBattle();m.Advance(.2f);
            Assert.That(m.Phase,Is.EqualTo(FormationPhase.Lost),"A deliberately shortened configured limit must use the ordinary timeout transition.");
            Assert.That(m.TimeRemaining,Is.LessThanOrEqualTo(.001));
            controller.Retry();Assert.That(m.Phase,Is.EqualTo(FormationPhase.Preparation));Assert.That(m.Clock,Is.Zero);
        }
    }
}
