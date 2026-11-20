using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    [Serializable]
    public struct FormationBattleTerrainRegion
    {
        public RectInt cells;
        public float height;
        public bool allowDefender;

        public FormationBattleTerrainRegion(RectInt cells, float height, bool allowDefender)
        { this.cells = cells; this.height = height; this.allowDefender = allowDefender; }
    }

    /// <summary>
    /// Immutable snapshot of the saved board geometry. The small visibility graph is built once;
    /// combat only queries its fixed obstacle corners. No scene objects or physics are consulted.
    /// Heights affect presentation only; combat keeps its existing planar attack distances.
    /// </summary>
    public sealed class FormationBattleTerrainLayout
    {
        // Leaves room for a body at the platform edge, within the existing 0.8-cell melee range.
        public const float BodyClearance = .14f;
        const float Epsilon = .00005f;
        readonly FormationBattleTerrainRegion[] regions;
        readonly Rect[] obstacles;
        readonly Vector2[] corners;
        readonly float[,] distances;
        readonly int[,] nextCorner;
        public IReadOnlyList<FormationBattleTerrainRegion> Regions { get; }

        public FormationBattleTerrainLayout(IEnumerable<FormationBattleTerrainRegion> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            regions = new List<FormationBattleTerrainRegion>(source).ToArray();
            Regions = Array.AsReadOnly(regions);
            obstacles = new Rect[regions.Length];
            for (int i = 0; i < regions.Length; i++)
            {
                var r = regions[i];
                if (r.cells.width <= 0 || r.cells.height <= 0 || r.cells.xMin < FormationBattleModel.DeploymentColumns + 2 ||
                    r.cells.xMax > FormationBattleModel.Width - 1 || r.cells.yMin < 0 ||
                    r.cells.yMax > FormationBattleModel.Height || float.IsNaN(r.height) ||
                    float.IsInfinity(r.height) || r.height <= 0)
                    throw new ArgumentException("Formation terrain must stay inside the right guard area with a positive height.", nameof(source));
                for (int j = 0; j < i; j++)
                    if (r.cells.Overlaps(regions[j].cells))
                        throw new ArgumentException("Formation terrain regions must not overlap.", nameof(source));
                obstacles[i] = Rect.MinMaxRect(r.cells.xMin - .5f - BodyClearance,
                    r.cells.yMin - .5f - BodyClearance, r.cells.xMax - .5f + BodyClearance,
                    r.cells.yMax - .5f + BodyClearance);
            }

            var points = new List<Vector2>();
            foreach (var r in obstacles)
            {
                AddCorner(points, new Vector2(r.xMin, r.yMin));
                AddCorner(points, new Vector2(r.xMin, r.yMax));
                AddCorner(points, new Vector2(r.xMax, r.yMin));
                AddCorner(points, new Vector2(r.xMax, r.yMax));
            }
            corners = points.ToArray();
            distances = new float[corners.Length, corners.Length];
            nextCorner = new int[corners.Length, corners.Length];
            for (int i = 0; i < corners.Length; i++)
                for (int j = 0; j < corners.Length; j++)
                {
                    bool visible = CanTravel(corners[i], corners[j]);
                    distances[i, j] = visible ? Vector2.Distance(corners[i], corners[j]) : float.PositiveInfinity;
                    nextCorner[i, j] = visible ? j : -1;
                }
            for (int k = 0; k < corners.Length; k++)
                for (int i = 0; i < corners.Length; i++)
                    for (int j = 0; j < corners.Length; j++)
                    {
                        float through = distances[i, k] + distances[k, j];
                        if (through + Epsilon >= distances[i, j]) continue;
                        distances[i, j] = through; nextCorner[i, j] = nextCorner[i, k];
                    }
        }

        void AddCorner(List<Vector2> points, Vector2 point)
        {
            if (IsWalkable(point) && !points.Contains(point)) points.Add(point);
        }

        public float HeightAt(Vector2 point)
        {
            for (int i = 0; i < regions.Length; i++)
                if (OnFootprint(regions[i].cells, point)) return regions[i].height;
            return 0;
        }

        public bool CanPlaceDefender(Vector2 cell)
        {
            if (!InsideBoard(cell)) return false;
            foreach (var region in regions)
                if (OnFootprint(region.cells, cell)) return region.allowDefender;
            return true;
        }

        public bool IsWalkable(Vector2 point)
        {
            if (!InsideBoard(point)) return false;
            foreach (var r in obstacles)
                if (point.x > r.xMin + Epsilon && point.x < r.xMax - Epsilon &&
                    point.y > r.yMin + Epsilon && point.y < r.yMax - Epsilon) return false;
            return true;
        }

        public bool CanTravel(Vector2 from, Vector2 to)
        {
            if (!IsWalkable(from) || !IsWalkable(to)) return false;
            foreach (var r in obstacles)
            {
                float first = 0, last = 1;
                Vector2 delta = to - from;
                if (ClipAxis(from.x, delta.x, r.xMin + Epsilon, r.xMax - Epsilon, ref first, ref last) &&
                    ClipAxis(from.y, delta.y, r.yMin + Epsilon, r.yMax - Epsilon, ref first, ref last) &&
                    last >= first) return false;
            }
            return true;
        }

        static bool ClipAxis(float origin, float delta, float min, float max, ref float first, ref float last)
        {
            if (Mathf.Abs(delta) < 1e-7f) return origin >= min && origin <= max;
            float a = (min - origin) / delta, b = (max - origin) / delta;
            if (a > b) { float swap = a; a = b; b = swap; }
            first = Mathf.Max(first, a); last = Mathf.Min(last, b);
            return first <= last;
        }

        static bool InsideBoard(Vector2 point) => !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
            point.x >= 0 && point.x <= FormationBattleModel.Width - 1 &&
            point.y >= 0 && point.y <= FormationBattleModel.Height - 1;

        static bool OnFootprint(RectInt cells, Vector2 point) => point.x >= cells.xMin - .5f &&
            point.x <= cells.xMax - .5f && point.y >= cells.yMin - .5f && point.y <= cells.yMax - .5f;

        /// <summary>Finds the next fixed corner, including a reachable platform-edge attack point.</summary>
        public bool TryApproach(Vector2 from, Vector2 target, float range, out Vector2 waypoint, out float distance)
        {
            waypoint = from; distance = float.PositiveInfinity;
            if (IsWalkable(target)) return TryRoute(from, target, out waypoint, out distance);
            for (int i = 0; i < regions.Length; i++)
            {
                if (!regions[i].allowDefender || !OnFootprint(regions[i].cells, target)) continue;
                var r = obstacles[i];
                ConsiderApproach(from, target, range, new Vector2(r.xMin, Mathf.Clamp(target.y, r.yMin, r.yMax)), ref waypoint, ref distance);
                ConsiderApproach(from, target, range, new Vector2(r.xMax, Mathf.Clamp(target.y, r.yMin, r.yMax)), ref waypoint, ref distance);
                ConsiderApproach(from, target, range, new Vector2(Mathf.Clamp(target.x, r.xMin, r.xMax), r.yMin), ref waypoint, ref distance);
                ConsiderApproach(from, target, range, new Vector2(Mathf.Clamp(target.x, r.xMin, r.xMax), r.yMax), ref waypoint, ref distance);
                break;
            }
            return !float.IsPositiveInfinity(distance);
        }

        void ConsiderApproach(Vector2 from, Vector2 target, float range, Vector2 goal, ref Vector2 waypoint, ref float distance)
        {
            if (Vector2.Distance(goal, target) > range - .005f) return;
            if (TryRoute(from, goal, out var candidate, out float length) && length < distance - Epsilon)
            { waypoint = candidate; distance = length; }
        }

        // Entry is a location to reach, not an attack approach. Keep its path independent
        // from the body's attack range, including future changes to ranged targeting.
        public bool TryEnterBattleArea(Vector2 from, out Vector2 waypoint) =>
            TryRoute(from, new Vector2(FormationBattleModel.DeploymentColumns + 1, from.y), out waypoint, out _);

        public bool TryExit(Vector2 from, out Vector2 waypoint)
        {
            var direct = new Vector2(FormationBattleModel.Width - 1, from.y);
            if (CanTravel(from, direct)) { waypoint = direct; return true; }
            waypoint = from; float best = float.PositiveInfinity;
            for (int row = 0; row < FormationBattleModel.Height; row++)
                if (TryRoute(from, new Vector2(FormationBattleModel.Width - 1, row), out var candidate, out float length) && length < best - Epsilon)
                { waypoint = candidate; best = length; }
            return !float.IsPositiveInfinity(best);
        }

        bool TryRoute(Vector2 from, Vector2 goal, out Vector2 waypoint, out float distance)
        {
            waypoint = from; distance = float.PositiveInfinity;
            if (!IsWalkable(from) || !IsWalkable(goal)) return false;
            if (CanTravel(from, goal)) { waypoint = goal; distance = Vector2.Distance(from, goal); return true; }
            for (int i = 0; i < corners.Length; i++)
            {
                if (!CanTravel(from, corners[i])) continue;
                float first = Vector2.Distance(from, corners[i]);
                for (int j = 0; j < corners.Length; j++)
                {
                    if (nextCorner[i, j] < 0 || !CanTravel(corners[j], goal)) continue;
                    float length = first + distances[i, j] + Vector2.Distance(corners[j], goal);
                    if (length >= distance - Epsilon) continue;
                    distance = length; waypoint = corners[i];
                    if (first < .001f)
                    {
                        Vector2 following = i == j ? goal : corners[nextCorner[i, j]];
                        if (CanTravel(from, following)) waypoint = following;
                    }
                }
            }
            return !float.IsPositiveInfinity(distance);
        }
    }
}
