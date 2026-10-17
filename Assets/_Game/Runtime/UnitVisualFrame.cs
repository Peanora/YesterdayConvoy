using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Read-only presentation input, independent of combat and body structure.</summary>
    public readonly struct UnitVisualFrame
    {
        public readonly float Distance, Time;
        public readonly Vector3 Direction;
        public readonly bool Moving;
        public readonly Color Tint;
        public readonly int SortingOrder;
        public UnitVisualFrame(float distance, float time, Vector3 direction, bool moving, Color tint, int sortingOrder)
        {
            Distance = distance; Time = time; Direction = direction; Moving = moving; Tint = tint; SortingOrder = sortingOrder;
        }
    }
}
