using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class MovementSceneTests
    {
        private float originalTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = originalTimeScale;

        [UnityTest]
        public IEnumerator EntrySceneReadsConfigurationAndActuallyMoves()
        {
            var units = Object.FindObjectsByType<PrototypeUnit>();
            Assert.That(units.Length, Is.EqualTo(1));
            PrototypeUnit unit = units[0];
            Assert.That(unit.UnitId, Is.EqualTo("U01"));
            Assert.That(unit.Motion.Speed, Is.EqualTo(unit.Definition.MoveSpeed));
            Vector3 before = unit.transform.position;
            yield return new WaitForSeconds(0.1f);
            Assert.That(Vector3.Distance(before, unit.transform.position), Is.GreaterThan(0.01f));
            Assert.That(Vector3.Distance(unit.Motion.Position, unit.transform.position), Is.LessThan(0.0001f));
        }

        [UnityTest]
        public IEnumerator ZeroTimeScaleStopsTheSharedDriver()
        {
            PrototypeUnit unit = Object.FindAnyObjectByType<PrototypeUnit>();
            Time.timeScale = 0;
            Vector3 before = unit.transform.position;
            double progress = unit.Motion.DistanceTravelled;
            for (int index = 0; index < 5; index++)
                yield return null;
            Assert.That(unit.transform.position, Is.EqualTo(before));
            Assert.That(unit.Motion.DistanceTravelled, Is.EqualTo(progress));
        }

        [UnityTest]
        public IEnumerator UnitReachesEndpointAndDoesNotLoopOrRewriteDefinition()
        {
            MovementPrototype prototype = Object.FindAnyObjectByType<MovementPrototype>();
            PrototypeUnit unit = prototype.Unit;
            float authoredSpeed = unit.Definition.MoveSpeed;
            Time.timeScale = 20;
            float deadline = Time.realtimeSinceStartup + 5;
            while (!unit.Motion.HasArrived && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(unit.Motion.HasArrived, Is.True);
            Vector3[] route = prototype.CopyWorldWaypoints();
            Assert.That(unit.transform.position, Is.EqualTo(route[route.Length - 1]));
            for (int index = 0; index < 5; index++)
                yield return null;
            Assert.That(unit.transform.position, Is.EqualTo(route[route.Length - 1]));
            Assert.That(unit.Definition.MoveSpeed, Is.EqualTo(authoredSpeed));
            Assert.That(Object.FindObjectsByType<PrototypeUnit>().Length, Is.EqualTo(1));
        }
    }
}
