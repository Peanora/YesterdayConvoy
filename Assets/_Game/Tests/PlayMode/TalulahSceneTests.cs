using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class TalulahSceneTests
    {
        private float scale;
        [SetUp] public void Setup() { scale=Time.timeScale; }
        [TearDown] public void TearDown() { Time.timeScale=scale; }
        [UnityTest]
        public IEnumerator SwitchDragCastPauseAndReturnToOriginalTrial()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); c.SwitchTrial(true);
            Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("塔露拉")); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(3));
            c.StartBattle(); c.Battle.SetPaused(true);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(ManualBattleController.RosterSlot.center));
            Assert.That(c.ProcessPointerUp(Camera.main.WorldToScreenPoint(c.Battle.Entry)).Success,Is.True);
            c.Tick(10); Assert.That(c.Battle.Clock,Is.Zero);
            c.Battle.SetPaused(false); c.Tick(4.05f); yield return null;
            var u=c.Battle.Units[0]; Assert.That(c.Battle.IsSkillReady(u.EntityId),Is.True);
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.UnitScreenRect(u.EntityId).center));
            Assert.That(c.SkillMenuOpen,Is.True); c.Tick(1); Assert.That(c.EffectiveSimulationSpeed,Is.EqualTo(.03f));
            c.ProcessPointerDown(ManualBattleController.ScreenPosition(c.SkillCastRect.center));
            Assert.That(c.SkillMenuOpen,Is.False); Assert.That(c.Battle.SkillCasts,Is.EqualTo(1));
            c.Tick(1.25f); yield return null;
            foreach(var d in c.Battle.Defenders) Assert.That(d.Health,Is.LessThan(c.Battle.CombatSettings.DefenderHealth));
            var visual=Object.FindAnyObjectByType<DemoSpriteVisual>();
            Assert.That(visual.FrameState,Is.EqualTo("Skill"));
            if(!visual.Renderer.sprite.name.StartsWith("TalulahOriginal/")) Assert.Ignore("Install the approved local art package to run original-art presentation checks.");
            Assert.That(visual.Rig,Is.Null); Assert.That(visual.Renderer.enabled,Is.True);
            var fire=Object.FindAnyObjectByType<FireRainView>(); Assert.That(fire.VisibleStreams,Is.GreaterThanOrEqualTo(6));
            var renderers=visual.GetComponentsInChildren<SpriteRenderer>();
            Vector3[] positions=new Vector3[renderers.Length]; Quaternion[] rotations=new Quaternion[renderers.Length];
            for(int i=0;i<renderers.Length;i++) { positions[i]=renderers[i].transform.position; rotations[i]=renderers[i].transform.rotation; }
            var stream=fire.GetComponentsInChildren<SpriteRenderer>()[0]; Vector3 streamPosition=stream.transform.position; Sprite flame=stream.sprite;
            c.Battle.SetPaused(true); double clock=c.Battle.Clock; var sprite=visual.Renderer.sprite;
            c.Tick(10); yield return null; Assert.That(c.Battle.Clock,Is.EqualTo(clock)); Assert.That(visual.Renderer.sprite,Is.EqualTo(sprite));
            for(int i=0;i<renderers.Length;i++)
            { Assert.That(renderers[i].transform.position,Is.EqualTo(positions[i])); Assert.That(renderers[i].transform.rotation,Is.EqualTo(rotations[i])); }
            Assert.That(stream.transform.position,Is.EqualTo(streamPosition)); Assert.That(stream.sprite,Is.EqualTo(flame));
            double distance=u.Motion.DistanceTravelled;
            c.Battle.SetPaused(false); c.Tick(4.5f); yield return null;
            foreach(var d in c.Battle.Defenders) Assert.That(d.Alive,Is.False);
            Assert.That(visual.FrameState,Is.EqualTo("Skill"),"Target death must not cancel the original casting gesture.");
            Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(distance));
            c.Tick(.35f); yield return null;
            Assert.That(u.Motion.DistanceTravelled,Is.GreaterThan(distance));
            c.RestartBattle(); yield return null;
            Assert.That(c.Battle.CombatShots,Is.Empty); Assert.That(c.Battle.Units,Is.Empty);
            foreach(var d in c.Battle.Defenders) Assert.That(d.Alive,Is.True);
            c.SwitchTrial(false); yield return null;
            Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("维维美")); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<DemoCardView>().Card,Is.EqualTo(c.Definition.Card));
        }

        [UnityTest]
        public IEnumerator OriginalFramesAdvanceWithTravelAndFreezeWithoutChangingModelTravel()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); c.SwitchTrial(true); c.SelectCard(true); c.TryDeployAt(c.Battle.Entry);
            yield return null; var visual=Object.FindAnyObjectByType<DemoSpriteVisual>(); var u=c.Battle.Units[0];
            if(!visual.Renderer.sprite.name.StartsWith("TalulahOriginal/")) Assert.Ignore("Install the approved local art package to run original-art presentation checks.");
            var poses=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<36;i++)
            {
                c.Tick(1f/30); yield return null;
                poses.Add(visual.FrameIndex);
                Assert.That(visual.Renderer.sprite.name,Does.StartWith("TalulahOriginal/walk/"));
                Assert.That(visual.transform.localPosition,Is.EqualTo(Vector3.zero),"Frame pivots must carry the source root, without moving the visual transform.");
            }
            Assert.That(poses.Count,Is.GreaterThan(20)); Assert.That(visual.Rig,Is.Null);
            Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(1.2f*c.Battle.Settings.Speed).Within(.0001));
            Assert.That(visual.FrameState,Is.EqualTo("Walk"));
            c.Battle.SetPaused(true); Sprite frozen=visual.Renderer.sprite; int frame=visual.FrameIndex;
            c.Tick(10); yield return null; Assert.That(visual.Renderer.sprite,Is.EqualTo(frozen)); Assert.That(visual.FrameIndex,Is.EqualTo(frame));
            c.Battle.SetPaused(false);
            for(int i=0;i<300 && double.IsInfinity(u.LastAttackTime);i++) { c.Tick(.025f); yield return null; }
            Assert.That(double.IsInfinity(u.LastAttackTime),Is.False);
            var attackFrames=new System.Collections.Generic.HashSet<Sprite>();
            for(int i=0;i<36;i++)
            {
                c.Tick(1f/30); yield return null;
                Assert.That(visual.FrameState,Is.EqualTo("Attack"));
                Assert.That(visual.Renderer.sprite.name,Does.StartWith("TalulahOriginal/attack/"));
                attackFrames.Add(visual.Renderer.sprite);
            }
            Assert.That(attackFrames.Count,Is.GreaterThan(20));
        }
    }
}
