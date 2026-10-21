using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class FireRainPresentationTests
    {
        private float scale;
        [SetUp] public void Setup() { scale=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=scale; }

        [UnityTest]
        public IEnumerator FireWaveAndScreenFreezeTogetherThenClearOnRestartAndTrialSwitch()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); c.SwitchTrial(true);
            c.SelectCard(true); Assert.That(c.TryDeployAt(c.Battle.Entry).Success,Is.True);
            c.Tick(4.05f); var unit=c.Battle.Units[0]; c.SelectSkillUnit(unit.EntityId);
            Assert.That(c.ActivateSelectedSkill(),Is.EqualTo(SkillFailure.None)); c.Tick(1.25f); yield return null;
            var fire=Object.FindAnyObjectByType<FireRainView>(); Camera camera=Camera.main;
            var effect=camera.GetComponent<FireRainScreenEffect>(); Assert.That(effect,Is.Not.Null);
            Vector3 cameraPosition=camera.transform.position; Quaternion rotation=camera.transform.rotation;
            var first=fire.GetComponentsInChildren<SpriteRenderer>()[0]; Sprite frame=first.sprite;
            Vector3 position=first.transform.position;
            var active=Capture(camera); effect.enabled=false; var withoutEffect=Capture(camera); effect.enabled=true;
            Assert.That(Difference(active,withoutEffect),Is.GreaterThan(.5),"The actual GPU image effect must change the world image.");
            yield return null; // OnDisable clears uniforms; the owner re-applies the unchanged clock sample.
            c.Battle.SetPaused(true); var paused=Capture(camera); double clock=c.Battle.Clock;
            c.Tick(10); yield return null;
            Assert.That(c.Battle.Clock,Is.EqualTo(clock)); Assert.That(first.sprite,Is.EqualTo(frame));
            Assert.That(first.transform.position,Is.EqualTo(position));
            Assert.That(Difference(paused,Capture(camera)),Is.LessThan(.01),"Pause must also freeze screen shake and tint.");
            Assert.That(camera.transform.position,Is.EqualTo(cameraPosition)); Assert.That(camera.transform.rotation,Is.EqualTo(rotation));
            c.RestartBattle(); yield return null; Assert.That(fire.VisibleStreams,Is.Zero);
            var reset=Capture(camera); effect.enabled=false;
            Assert.That(Difference(reset,Capture(camera)),Is.LessThan(.01),"Restart must remove all color and camera offset."); effect.enabled=true;
            c.SwitchTrial(false); yield return null; Assert.That(fire.VisibleStreams,Is.Zero);
            Assert.That(camera.transform.position,Is.EqualTo(cameraPosition));
        }

        [UnityTest]
        public IEnumerator FireTravelsRightAndFinishesAfterTargetsDieWithoutLeavingScreenTint()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); c.SwitchTrial(true); c.SelectCard(true); c.TryDeployAt(c.Battle.Entry);
            c.Tick(4.05f); var unit=c.Battle.Units[0]; c.SelectSkillUnit(unit.EntityId); c.ActivateSelectedSkill();
            c.Tick(1.25f); yield return null;
            var fire=Object.FindAnyObjectByType<FireRainView>(); Camera cam=Camera.main;
            var first=fire.GetComponentsInChildren<SpriteRenderer>()[0];
            float x=cam.WorldToViewportPoint(first.transform.position).x; Sprite frame=first.sprite;
            c.Tick(.17f); yield return null;
            Assert.That(cam.WorldToViewportPoint(first.transform.position).x,Is.GreaterThan(x));
            Assert.That(first.sprite,Is.Not.EqualTo(frame),"Flame texture must animate, not just translate.");
            double distance=unit.Motion.DistanceTravelled; c.Tick(4.2f); yield return null;
            foreach(var defender in c.Battle.Defenders) Assert.That(defender.Alive,Is.False);
            Assert.That(fire.VisibleStreams,Is.GreaterThan(0)); Assert.That(unit.Motion.DistanceTravelled,Is.EqualTo(distance));
            c.Tick(.45f); yield return null; Assert.That(fire.VisibleStreams,Is.Zero);
            Assert.That(unit.Motion.DistanceTravelled,Is.GreaterThan(distance));
            var effect=cam.GetComponent<FireRainScreenEffect>(); var restored=Capture(cam); effect.enabled=false;
            Assert.That(Difference(restored,Capture(cam)),Is.LessThan(.01),"Natural completion must restore the untouched image.");
        }

        private static Color32[] Capture(Camera camera)
        {
            var target=new RenderTexture(320,180,24); var image=new Texture2D(320,180,TextureFormat.RGBA32,false);
            var previousTarget=camera.targetTexture; var previousActive=RenderTexture.active;
            try { camera.targetTexture=target; camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,320,180),0,0); image.Apply(); return image.GetPixels32(); }
            finally { camera.targetTexture=previousTarget; RenderTexture.active=previousActive; target.Release(); Object.Destroy(target); Object.Destroy(image); }
        }
        private static double Difference(Color32[] a,Color32[] b)
        {
            double delta=0; for(int i=0;i<a.Length;i++) delta+=System.Math.Abs(a[i].r-b[i].r)+System.Math.Abs(a[i].g-b[i].g)+System.Math.Abs(a[i].b-b[i].b);
            return delta/(a.Length*3);
        }
    }
}
