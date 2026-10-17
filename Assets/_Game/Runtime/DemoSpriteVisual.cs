using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Camera-facing presentation only; it never changes logical unit position or HP.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private InitialDemo movingDemo;
        [SerializeField] private Sprite rearPose;
        private Sprite idlePose;
        private int previousShots;
        private float flashUntil;
        private float previousTime;
        private DemoWalkPuppet puppet;

        public SpriteRenderer Renderer => sprite;
        public DemoWalkPuppet Puppet => puppet;

        private void OnEnable()
        {
            previousShots = 0;
            flashUntil = -1;
            previousTime = 0;
            transform.localPosition = Vector3.zero;
            if (sprite != null)
            {
                if (idlePose == null) idlePose = sprite.sprite;
                sprite.sprite = idlePose;
                sprite.color = Color.white;
            }
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null || sprite == null) return;
            transform.rotation = camera.transform.rotation;
            sprite.sortingOrder = (500 - Mathf.RoundToInt(camera.WorldToScreenPoint(transform.parent.position).y)) * 16;
            if (movingDemo == null) return;
            float time = movingDemo.SimulationTime;
            if (time < previousTime) { previousShots = 0; flashUntil = -1; }
            previousTime = time;
            if (movingDemo.ShotsFired > previousShots) flashUntil = time + 0.18f;
            previousShots = movingDemo.ShotsFired;
            bool moving = movingDemo.Outcome == DemoOutcome.Running;
            if (rearPose != null && moving)
            {
                if (puppet == null) puppet = new DemoWalkPuppet(transform, idlePose, rearPose);
                sprite.enabled = false;
                sprite.color = time < flashUntil ? new Color(1, 0.48f, 0.48f) : Color.white;
                transform.localPosition = Vector3.zero;
                puppet.Pose((float)movingDemo.Motion.DistanceTravelled, time, movingDemo.Motion.Direction, moving, sprite.color, sprite.sortingOrder);
                return;
            }
            puppet?.Hide();
            sprite.enabled = true;
            transform.localScale = Vector3.one;
            sprite.color = time < flashUntil ? new Color(1, 0.48f, 0.48f) : Color.white;
        }

        private void OnDestroy() => puppet?.Dispose();
    }
}
