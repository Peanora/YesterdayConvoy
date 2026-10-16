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
    }
}
