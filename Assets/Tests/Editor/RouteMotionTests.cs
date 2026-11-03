using System;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class RouteMotionTests
    {
        private static Vector3[] Corner() => new[] { Vector3.zero, new Vector3(3, 0, 0), new Vector3(3, 4, 0) };

        [Test]
        public void CrossesCornerUsingRemainingDistance()
        {
            var motion = new RouteMotion(Corner(), 2);
            motion.Advance(2.5f);
            Assert.That(Vector3.Distance(motion.Position, new Vector3(3, 2, 0)), Is.LessThan(0.0001f));
            Assert.That(motion.DistanceTravelled, Is.EqualTo(5).Within(0.0001));
            Assert.That(motion.HasArrived, Is.False);
        }

        [Test]
        public void LargeStepStopsAtEndpointAndStaysThere()
        {
            var motion = new RouteMotion(Corner(), 2);
            motion.Advance(float.MaxValue);
            Assert.That(motion.Position, Is.EqualTo(new Vector3(3, 4, 0)));
            Assert.That(motion.HasArrived, Is.True);
            Assert.That(motion.DistanceTravelled, Is.EqualTo(7));
            motion.Advance(10);
            Assert.That(motion.Position, Is.EqualTo(new Vector3(3, 4, 0)));
            Assert.That(motion.DistanceTravelled, Is.EqualTo(7));
        }

        [Test]
        public void DifferentStepSizesFollowTheSameRoute()
        {
            var oneStep = new RouteMotion(Corner(), 2);
            var manySteps = new RouteMotion(Corner(), 2);
            oneStep.Advance(2.5f);
            for (int index = 0; index < 25; index++)
                manySteps.Advance(0.1f);
            Assert.That(Vector3.Distance(oneStep.Position, manySteps.Position), Is.LessThan(0.0001f));
        }

        [Test]
        public void RepeatedWaypointsDoNotStallMovement()
        {
            var motion = new RouteMotion(new[] { Vector3.zero, Vector3.zero, Vector3.right, Vector3.right, Vector3.right * 2 }, 1);
            motion.Advance(3);
            Assert.That(motion.HasArrived, Is.True);
            Assert.That(motion.Position, Is.EqualTo(Vector3.right * 2));
        }

        [Test]
        public void ZeroSimulationDeltaFreezesProgress()
        {
            var motion = new RouteMotion(Corner(), 2);
            motion.Advance(1);
            Vector3 position = motion.Position;
            motion.Advance(0);
            Assert.That(motion.Position, Is.EqualTo(position));
            Assert.That(motion.DistanceTravelled, Is.EqualTo(2));
        }

        [TestCase(0.0002f)]
        [TestCase(0.1f)]
        [TestCase(1f / 60f)]
        public void SmallFrameStepsKeepProgressMonotonicAndWithinRouteLength(float delta)
        {
            var points = new[]
            {
                new Vector3(-5, -1.5f, 0), new Vector3(-2, -1.5f, 0),
                new Vector3(-2, 1, 0), new Vector3(2.5f, 1, 0),
                new Vector3(2.5f, -1.5f, 0), new Vector3(5, -1.5f, 0)
            };
            var motion = new RouteMotion(points, 1.5f);
            double previous = 0;
            for (int index = 0; index < 100000 && !motion.HasArrived; index++)
            {
                motion.Advance(delta);
                Assert.That(motion.DistanceTravelled, Is.GreaterThanOrEqualTo(previous), "Progress must not go backwards at arrival.");
                Assert.That(motion.DistanceTravelled, Is.LessThanOrEqualTo(motion.TotalLength), "Progress must not exceed the authored route length.");
                previous = motion.DistanceTravelled;
            }
            Assert.That(motion.HasArrived, Is.True);
            Assert.That(motion.DistanceTravelled, Is.EqualTo(15));
        }

        [Test]
        public void RouteIsCopiedAndInvalidRoutesAreRejected()
        {
            Vector3[] points = Corner();
            var motion = new RouteMotion(points, 2);
            points[1] = Vector3.one * 100;
            motion.Advance(2.5f);
            Assert.That(motion.Position, Is.EqualTo(new Vector3(3, 2, 0)));
            Assert.Throws<ArgumentException>(() => new RouteMotion(null, 1));
            Assert.Throws<ArgumentException>(() => new RouteMotion(new[] { Vector3.zero }, 1));
            Assert.Throws<ArgumentException>(() => new RouteMotion(new[] { Vector3.zero, Vector3.zero }, 1));
            Assert.Throws<ArgumentException>(() => new RouteMotion(new[] { Vector3.zero, new Vector3(float.NaN, 0, 0) }, 1));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidTimeIsRejectedWithoutMoving(float delta)
        {
            var motion = new RouteMotion(Corner(), 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Advance(delta));
            Assert.That(motion.Position, Is.EqualTo(Vector3.zero));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidSpeedIsRejected(float speed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteMotion(Corner(), speed));
        }
    }
}
