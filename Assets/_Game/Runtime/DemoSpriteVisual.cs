using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Camera-facing presentation only; it never changes logical unit position or HP.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private InitialDemo movingDemo;
        [SerializeField] private Sprite rearPose;
        [SerializeField] private Sprite sidePose;
        [SerializeField] private Sprite rearQuarterPose;
        private Sprite idlePose;
        private int previousShots;
        private float flashUntil;
        private float previousTime;
        [SerializeField] private UnitVisualPresentation presentation;
        private RouteMotion previousMotion;

        public SpriteRenderer Renderer => sprite;
        public DemoWalkPuppet Puppet => presentation is BipedVisualPresentation biped ? biped.Puppet : null;
        public UnitVisualPresentation Presentation => presentation;
        public bool HasSourceViews => rearPose != null && sidePose != null && rearQuarterPose != null;

        private void OnEnable()
        {
            previousShots = 0;
            flashUntil = -1;
            previousTime = 0;
            previousMotion = null;
            presentation?.ResetVisual();
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
            if (movingDemo == null) { FaceCamera(); return; }
            float time = movingDemo.SimulationTime;
            if (movingDemo.Motion != previousMotion || time < previousTime)
            {
                previousShots = 0;
                flashUntil = -1;
                presentation?.ResetVisual();
                previousMotion = movingDemo.Motion;
            }
            previousTime = time;
            if (movingDemo.ShotsFired > previousShots) flashUntil = time + 0.18f;
            previousShots = movingDemo.ShotsFired;
            bool moving = movingDemo.Outcome == DemoOutcome.Running;
            PresentMotion((float)movingDemo.Motion.DistanceTravelled, time, movingDemo.Motion.Direction, moving,
                time < flashUntil ? new Color(1, 0.48f, 0.48f) : Color.white);
        }

        // Read-only movement input. Also used by the isolated art preview; never writes the logical root.
        public void PresentMotion(float distance, float simulationTime, Vector3 direction, bool moving, Color tint)
        {
            if (!FaceCamera()) return;
            sprite.color = tint;
            if (presentation != null)
            {
                sprite.enabled = false;
                transform.localPosition = Vector3.zero;
                presentation.Present(new UnitVisualFrame(distance, simulationTime, direction, moving, tint, sprite.sortingOrder));
                return;
            }
            sprite.enabled = true;
            transform.localScale = Vector3.one;
        }

        private bool FaceCamera()
        {
            Camera camera = Camera.main;
            if (camera == null || sprite == null) return false;
            transform.rotation = camera.transform.rotation;
            Vector3 anchor = transform.parent != null ? transform.parent.position : transform.position;
            sprite.sortingOrder = (500 - Mathf.RoundToInt(camera.WorldToScreenPoint(anchor).y)) * 16;
            return true;
        }

    }
}
