using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Camera-facing presentation only; it never changes logical unit position or HP.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private InitialDemo movingDemo;
        [SerializeField] private Sprite[] stridePoses;
        [SerializeField, Min(0.1f)] private float strideLength = 0.9f;
        private Sprite idlePose;
        private int previousShots;
        private float flashUntil;
        private float previousTime;

        public SpriteRenderer Renderer => sprite;

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
            sprite.sortingOrder = 500 - Mathf.RoundToInt(camera.WorldToScreenPoint(transform.parent.position).y);
            if (movingDemo == null) return;
            float time = movingDemo.SimulationTime;
            if (time < previousTime) { previousShots = 0; flashUntil = -1; }
            previousTime = time;
            if (movingDemo.ShotsFired > previousShots) flashUntil = time + 0.18f;
            previousShots = movingDemo.ShotsFired;
            bool moving = movingDemo.Outcome == DemoOutcome.Running;
            // One left/right stride per travelled distance, independent of rendering FPS.
            // Visual offsets never write back to the authoritative route transform.
            float phase = moving && movingDemo.Motion != null
                ? Mathf.Repeat((float)movingDemo.Motion.DistanceTravelled / strideLength, 1) : 0;
            if (stridePoses != null && stridePoses.Length == 2)
            {
                int beat = Mathf.FloorToInt(phase * 4);
                sprite.sprite = !moving || beat % 2 == 1 ? idlePose : stridePoses[beat / 2];
            }
            float lift = moving ? Mathf.Sin(phase * Mathf.PI * 2) : 0;
            transform.localPosition = Vector3.up * (Mathf.Abs(lift) * 0.025f);
            transform.rotation = camera.transform.rotation * Quaternion.Euler(0, 0, lift * 1.2f);
            sprite.color = time < flashUntil ? new Color(1, 0.48f, 0.48f) : Color.white;
        }
    }
}
