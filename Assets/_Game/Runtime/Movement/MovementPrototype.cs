using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>The demo's single simulation-time source. Units do not run their own Update clocks.</summary>
    public sealed class MovementPrototype : MonoBehaviour
    {
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private PrototypeUnit unit;
        [SerializeField] private LineRenderer pathLine;

        public PrototypeUnit Unit => unit;
        public LineRenderer PathLine => pathLine;

        public Vector3[] CopyWorldWaypoints()
        {
            if (waypoints == null)
                throw new InvalidOperationException(name + ": missing waypoints.");
            var world = new Vector3[waypoints.Length];
            for (int index = 0; index < world.Length; index++)
                world[index] = transform.TransformPoint(waypoints[index]);
            return world;
        }

        private void Start()
        {
            if (unit == null || pathLine == null)
            {
                enabled = false;
                throw new InvalidOperationException(name + ": missing unit or path renderer.");
            }
            Vector3[] points = CopyWorldWaypoints();
            unit.Initialize(points);
            pathLine.useWorldSpace = true;
            pathLine.positionCount = points.Length;
            pathLine.SetPositions(points);
        }

        private void Update()
        {
            if (unit != null && unit.Motion != null)
                unit.Simulate(Time.deltaTime);
        }

        private void OnGUI()
        {
            if (unit == null || unit.Motion == null)
                return;
            GUI.Box(new Rect(24, Screen.height - 100, 570, 76), "T02 / Fixed route movement");
            GUI.Label(new Rect(40, Screen.height - 74, 540, 48),
                $"{unit.UnitId} | Speed {unit.Motion.Speed:F2} | {unit.Motion.DistanceTravelled:F2}/{unit.Motion.TotalLength:F2}\n" +
                (unit.Motion.HasArrived ? "Reached the blue endpoint. No gate damage yet." : "Following waypoints. No deployment or combat yet."));
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null)
                return;
            Gizmos.color = Color.cyan;
            for (int index = 0; index < waypoints.Length; index++)
            {
                Vector3 point = transform.TransformPoint(waypoints[index]);
                Gizmos.DrawWireSphere(point, 0.15f);
                if (index > 0)
                    Gizmos.DrawLine(transform.TransformPoint(waypoints[index - 1]), point);
            }
        }
    }
}
