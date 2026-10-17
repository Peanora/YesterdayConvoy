using UnityEngine;

namespace YesterdayConvoy
{
    public abstract class UnitVisualPresentation : MonoBehaviour
    {
        public UnitVisualFrame LastFrame { get; private set; }
        public void Present(UnitVisualFrame frame) { LastFrame = frame; Apply(frame); }
        protected abstract void Apply(UnitVisualFrame frame);
        public abstract float MotionWeight { get; }
        public abstract void ResetVisual();
    }
}
