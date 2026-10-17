using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class InitialDemoTests
    {
        private float originalTimeScale;

        [SetUp]
        public void SetUp() => originalTimeScale = Time.timeScale;

        [TearDown]
        public void TearDown() => Time.timeScale = originalTimeScale;

        [UnityTest]
        public IEnumerator PlayableLoopSupportsBreachDeathAndCleanRestart()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo", LoadSceneMode.Single);
            yield return null;
            var demos = Object.FindObjectsByType<InitialDemo>();
            Assert.That(demos.Length, Is.EqualTo(1));
            InitialDemo demo = demos[0];
            Assert.That(demo.Outcome, Is.EqualTo(DemoOutcome.Ready));
            Assert.That(demo.Card, Is.Not.Null);
            Assert.That(demo.Card.RecruitCost, Is.EqualTo(5));
            Assert.That(demo.Card.Portrait, Is.Not.Null);
            Assert.That(demo.Card.BattleSprite, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<DemoSpriteVisual>().Length, Is.EqualTo(4));
            Assert.That(Object.FindAnyObjectByType<DemoCardView>().Card, Is.SameAs(demo.Card));
            Assert.That(demo.Monster.GetComponentInChildren<SpriteRenderer>().sprite, Is.SameAs(demo.Card.BattleSprite));
            Assert.That(Camera.main.orthographic, Is.True);
            Assert.That(Vector3.Dot(Camera.main.transform.forward, Vector3.down), Is.InRange(0.3f, 0.95f));
            Time.timeScale = 12;
            demo.BeginAttack(100);
            float deadline = Time.realtimeSinceStartup + 5;
            while (demo.Outcome == DemoOutcome.Running && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(demo.Outcome, Is.EqualTo(DemoOutcome.Breached));
            Assert.That(demo.GateDefense, Is.EqualTo(7));
            Assert.That(demo.ShotsFired, Is.GreaterThan(0));
            Assert.That(demo.Monster.gameObject.activeSelf, Is.False);
            demo.Simulate(10);
            Assert.That(demo.GateDefense, Is.EqualTo(7), "A finished monster must not damage the gate again.");

            demo.BeginAttack(30);
            deadline = Time.realtimeSinceStartup + 5;
            while (demo.Outcome == DemoOutcome.Running && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(demo.Outcome, Is.EqualTo(DemoOutcome.Defeated));
            Assert.That(demo.Health, Is.Zero);
            Assert.That(demo.GateDefense, Is.EqualTo(8));
            Assert.That(demo.Monster.gameObject.activeSelf, Is.False);

            demo.BeginAttack(100);
            Assert.That(demo.Outcome, Is.EqualTo(DemoOutcome.Running));
            Assert.That(demo.Health, Is.EqualTo(100));
            Assert.That(demo.GateDefense, Is.EqualTo(8));
            Assert.That(demo.ShotsFired, Is.Zero);
            Assert.That(demo.Motion.DistanceTravelled, Is.Zero);
            Assert.That(demo.Monster.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator WalkingPlantsFootMovesContinuouslyAndTurnsWithRoute()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo", LoadSceneMode.Single);
            yield return null;
            Camera camera = Camera.main;
            Vector3 origin = camera.WorldToScreenPoint(Vector3.zero);
            Assert.That(camera.WorldToScreenPoint(Vector3.right).y, Is.EqualTo(origin.y).Within(0.01f), "Board rows should be horizontal.");
            Assert.That(camera.WorldToScreenPoint(Vector3.forward).x, Is.EqualTo(origin.x).Within(0.01f), "Board columns should not tilt sideways.");
            var demo = Object.FindAnyObjectByType<InitialDemo>();
            var visual = demo.Monster.GetComponentInChildren<DemoSpriteVisual>();
            Time.timeScale = 0;
            demo.BeginAttack(100);
            demo.Simulate(2.56f / 1.5f);
            yield return null;
            Assert.That(visual.Puppet, Is.Not.Null);
            Assert.That(visual.Renderer.enabled, Is.False, "The old whole-image frame flip should be disabled.");
            Vector3 planted = visual.Puppet.LeftFoot;
            Vector3 body = demo.Monster.position;
            demo.Simulate(0.05f);
            yield return null;
            Assert.That(demo.Monster.position.x, Is.GreaterThan(body.x));
            Assert.That(Vector3.Distance(visual.Puppet.LeftFoot, planted), Is.LessThan(0.001f), "A support foot must stay planted as the body advances.");
            Assert.That(Quaternion.Angle(visual.transform.Find("Front cutout rig/Near shoe").localRotation, Quaternion.identity), Is.LessThan(0.01f), "Support shoes should remain level while the knee bends.");
            var nearBoot = visual.transform.Find("Front cutout rig/Near boot");
            Quaternion before = nearBoot.localRotation;
            demo.Simulate(0.01f);
            yield return null;
            float angle = Quaternion.Angle(before, nearBoot.localRotation);
            Assert.That(angle, Is.GreaterThan(0.01f).And.LessThan(20), "Joint motion should change continuously in small simulation steps.");
            before = nearBoot.localRotation;
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(nearBoot.localRotation, Is.EqualTo(before));
            demo.Simulate((3.072f - (float)demo.Motion.DistanceTravelled) / 1.5f);
            yield return null;
            Assert.That(visual.Puppet.LeftFoot.y, Is.GreaterThan(demo.Monster.position.y + 0.04f), "The recovery foot must lift clear of the ground.");
            demo.Simulate((5.3f - (float)demo.Motion.DistanceTravelled) / 1.5f);
            yield return null;
            demo.Simulate(0.05f);
            yield return null;
            Vector3 turnScale = visual.transform.localScale;
            Assert.That(turnScale.x, Is.LessThan(1));
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(visual.transform.localScale, Is.EqualTo(turnScale), "A paused turn must stay frozen.");
            demo.Simulate(0.05f);
            yield return null;
            Assert.That(visual.Puppet.ShowingBack, Is.True, "The away-facing route segment must show the back of the character.");
            Assert.That(demo.Motion.Direction.z, Is.GreaterThan(0));
            demo.BeginAttack(100);
            yield return null;
            Assert.That(visual.Puppet.ShowingBack, Is.False);
            Assert.That(visual.transform.localScale.x, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PauseFreezesSpriteMotionAndDamageFlash()
        {
            yield return SceneManager.LoadSceneAsync("InitialDemo", LoadSceneMode.Single);
            yield return null;
            var demo = Object.FindAnyObjectByType<InitialDemo>();
            demo.BeginAttack(100);
            Time.timeScale = 8;
            float deadline = Time.realtimeSinceStartup + 5;
            while (demo.ShotsFired == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(demo.ShotsFired, Is.GreaterThan(0));
            Time.timeScale = 0;
            yield return null;
            var presentation = demo.Monster.GetComponentInChildren<DemoSpriteVisual>();
            Transform visual = presentation.transform;
            Vector3 before = visual.localPosition;
            Color tint = presentation.Renderer.color;
            Sprite pose = presentation.Renderer.sprite;
            var visiblePart = visual.GetComponentInChildren<MeshRenderer>();
            Color visibleTint = visiblePart.sharedMaterial.color;
            Assert.That(visibleTint, Is.EqualTo(tint), "The rendered rig, not only the hidden source sprite, must receive the hit flash.");
            Assert.That(tint, Is.Not.EqualTo(Color.white), "Pause must be checked while a hit flash is actually visible.");
            Vector3 position = demo.Monster.position;
            float clock = demo.SimulationTime;
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(visual.localPosition, Is.EqualTo(before));
            Assert.That(presentation.Renderer.color, Is.EqualTo(tint));
            Assert.That(presentation.Renderer.sprite, Is.SameAs(pose));
            Assert.That(visiblePart.sharedMaterial.color, Is.EqualTo(visibleTint));
            Assert.That(demo.Monster.position, Is.EqualTo(position));
            Assert.That(demo.SimulationTime, Is.EqualTo(clock));
        }
    }
}
