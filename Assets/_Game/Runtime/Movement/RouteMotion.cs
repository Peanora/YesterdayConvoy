using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>One entity's position on an immutable route; owns no Unity clock or authored assets.</summary>
    public sealed class RouteMotion
    {
        private readonly Vector3[] points;
        private readonly double[] cumulativeLengths;
        private int nextPoint = 1;

        public Vector3 Position { get; private set; }
        public float Speed { get; }
        public double DistanceTravelled { get; private set; }
        public double TotalLength { get; }
        public bool HasArrived => nextPoint >= points.Length;
        public Vector3 Direction => (points[Math.Min(nextPoint, points.Length - 1)] - points[Math.Max(0, Math.Min(nextPoint, points.Length - 1) - 1)]).normalized;

        public RouteMotion(IReadOnlyList<Vector3> waypoints, float speed)
        {
            if (waypoints == null || waypoints.Count < 2)
                throw new ArgumentException("Route requires at least two waypoints.", nameof(waypoints));
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be finite and positive.");
            points = new Vector3[waypoints.Count];
            cumulativeLengths = new double[waypoints.Count];
            double length = 0;
            for (int index = 0; index < points.Length; index++)
            {
                Vector3 point = waypoints[index];
                if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z))
                    throw new ArgumentException($"Waypoint {index} is not finite.", nameof(waypoints));
                points[index] = point;
                if (index == 0)
                    continue;
                float segment = Vector3.Distance(points[index - 1], point);
                if (!Finite(segment))
                    throw new ArgumentException($"Route segment {index - 1} is too large.", nameof(waypoints));
                length += segment;
                cumulativeLengths[index] = length;
            }
            if (length <= 0)
                throw new ArgumentException("Route must contain a nonzero segment.", nameof(waypoints));
            Speed = speed;
            TotalLength = length;
            Position = points[0];
        }

        public void Advance(float simulationDelta)
        {
            if (!Finite(simulationDelta) || simulationDelta < 0f)
                throw new ArgumentOutOfRangeException(nameof(simulationDelta), "Simulation delta must be finite and nonnegative.");
            if (simulationDelta == 0f || HasArrived)
                return;

            // Integrate one authoritative path distance, then derive position from the immutable route.
            // Integrating rounded Vector3 positions frame by frame can drift beyond the route length.
            DistanceTravelled = Math.Min(TotalLength, DistanceTravelled + (double)Speed * simulationDelta);
            if (DistanceTravelled >= TotalLength)
            {
                nextPoint = points.Length;
                Position = points[points.Length - 1];
                return;
            }
            while (DistanceTravelled >= cumulativeLengths[nextPoint])
                nextPoint++;
            double segmentStart = cumulativeLengths[nextPoint - 1];
            double segmentLength = cumulativeLengths[nextPoint] - segmentStart;
            float fraction = (float)((DistanceTravelled - segmentStart) / segmentLength);
            Position = Vector3.LerpUnclamped(points[nextPoint - 1], points[nextPoint], fraction);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
