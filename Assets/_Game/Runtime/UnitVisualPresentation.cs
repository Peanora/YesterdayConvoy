using UnityEngine;

namespace YesterdayConvoy
{
    public abstract class UnitVisualPresentation : MonoBehaviour
    {
        public abstract void Present(UnitVisualFrame frame);
        public abstract void ResetVisual();
    }
}
