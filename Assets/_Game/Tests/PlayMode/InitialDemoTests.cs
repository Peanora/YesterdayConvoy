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
            Assert.That(tint, Is.Not.EqualTo(Color.white), "Pause must be checked while a hit flash is actually visible.");
            Vector3 position = demo.Monster.position;
            float clock = demo.SimulationTime;
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(visual.localPosition, Is.EqualTo(before));
            Assert.That(presentation.Renderer.color, Is.EqualTo(tint));
            Assert.That(demo.Monster.position, Is.EqualTo(position));
            Assert.That(demo.SimulationTime, Is.EqualTo(clock));
        }
    }
}
