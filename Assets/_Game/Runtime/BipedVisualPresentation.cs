using UnityEngine;

namespace YesterdayConvoy
{
    [RequireComponent(typeof(BipedVisualRig))]
    public sealed class BipedVisualPresentation : UnitVisualPresentation
    {
        private DemoWalkPuppet puppet;
        public DemoWalkPuppet Puppet
        {
            get
            {
                if (puppet == null) puppet = new DemoWalkPuppet(transform, GetComponent<BipedVisualRig>());
                return puppet;
            }
        }
        public override void ResetVisual() => puppet?.Reset();
        public override float MotionWeight => Puppet.MovementWeight;
        protected override void Apply(UnitVisualFrame frame) => Puppet.Pose(frame.Distance, frame.Time, frame.Direction, frame.Moving, frame.Tint, frame.SortingOrder);
    }
}
