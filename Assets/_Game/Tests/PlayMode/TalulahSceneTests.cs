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
            Assert.That(visual.Rig,Is.Not.Null); Assert.That(visual.Renderer.enabled,Is.False);
            var fire=Object.FindAnyObjectByType<FireRainView>(); Assert.That(fire.VisibleStreams,Is.EqualTo(48));
            var renderers=visual.Rig.GetComponentsInChildren<SpriteRenderer>();
            Vector3[] positions=new Vector3[renderers.Length]; Quaternion[] rotations=new Quaternion[renderers.Length];
            for(int i=0;i<renderers.Length;i++) { positions[i]=renderers[i].transform.position; rotations[i]=renderers[i].transform.rotation; }
            var stream=fire.GetComponentsInChildren<SpriteRenderer>()[0]; Vector3 streamPosition=stream.transform.position; Sprite flame=stream.sprite;
            c.Battle.SetPaused(true); double clock=c.Battle.Clock; var sprite=visual.Renderer.sprite;
            c.Tick(10); yield return null; Assert.That(c.Battle.Clock,Is.EqualTo(clock)); Assert.That(visual.Renderer.sprite,Is.EqualTo(sprite));
            for(int i=0;i<renderers.Length;i++)
            { Assert.That(renderers[i].transform.position,Is.EqualTo(positions[i])); Assert.That(renderers[i].transform.rotation,Is.EqualTo(rotations[i])); }
            Assert.That(stream.transform.position,Is.EqualTo(streamPosition)); Assert.That(stream.sprite,Is.EqualTo(flame));
            c.RestartBattle(); yield return null;
            Assert.That(c.Battle.CombatShots,Is.Empty); Assert.That(c.Battle.Units,Is.Empty);
            foreach(var d in c.Battle.Defenders) Assert.That(d.Alive,Is.True);
            c.SwitchTrial(false); yield return null;
            Assert.That(c.Definition.Card.DisplayName,Is.EqualTo("维维美")); Assert.That(c.Battle.Defenders.Count,Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<DemoCardView>().Card,Is.EqualTo(c.Definition.Card));
        }

        [UnityTest]
        public IEnumerator ArticulatedWalkHasIntermediatePosesWithoutChangingModelTravel()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo"); yield return null; Time.timeScale=0;
            var c=Object.FindAnyObjectByType<ManualBattleController>(); c.SwitchTrial(true); c.SelectCard(true); c.TryDeployAt(c.Battle.Entry);
            yield return null; var visual=Object.FindAnyObjectByType<DemoSpriteVisual>(); var u=c.Battle.Units[0];
            var poses=new System.Collections.Generic.HashSet<int>(); Vector2 previous=visual.Rig.LeftFoot;
            for(int i=0;i<36;i++)
            {
                c.Tick(1f/30); yield return null;
                Vector2 foot=visual.Rig.LeftFoot; poses.Add(Mathf.RoundToInt(foot.x*1000));
                Assert.That(Vector2.Distance(previous,foot),Is.LessThan(.09f),"A frame must not swap between two far-apart foot poses."); previous=foot;
            }
            Assert.That(poses.Count,Is.GreaterThan(20)); Assert.That(visual.Rig.WalkBlend,Is.EqualTo(1));
            Assert.That(u.Motion.DistanceTravelled,Is.EqualTo(1.2f*c.Battle.Settings.Speed).Within(.0001));
            Assert.That(visual.FrameState,Is.EqualTo("Walk"));
            for(int i=0;i<300 && double.IsInfinity(u.LastAttackTime);i++) { c.Tick(.025f); yield return null; }
            Assert.That(double.IsInfinity(u.LastAttackTime),Is.False);
            Quaternion weapon=visual.Rig.WeaponRotation;
            for(int i=0;i<36;i++)
            {
                c.Tick(1f/30); yield return null;
                Assert.That(Quaternion.Angle(weapon,visual.Rig.WeaponRotation),Is.LessThan(52),"Wind-up, strike and recovery must join without snapping back to neutral.");
                weapon=visual.Rig.WeaponRotation;
            }
        }
    }
}
