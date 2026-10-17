using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Camera-facing presentation only; it never changes logical unit position or HP.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private InitialDemo movingDemo;
        private int previousShots;
        private float flashUntil;

        public SpriteRenderer Renderer => sprite;

        private void OnEnable()
        {
            previousShots = 0;
            flashUntil = -1;
            transform.localPosition = Vector3.zero;
            if (sprite != null) sprite.color = Color.white;
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null || sprite == null) return;
            transform.rotation = camera.transform.rotation;
            sprite.sortingOrder = 500 - Mathf.RoundToInt(camera.WorldToScreenPoint(transform.parent.position).y);
            if (movingDemo == null) return;
            float time = movingDemo.SimulationTime;
            if (movingDemo.ShotsFired > previousShots) flashUntil = time + 0.18f;
            previousShots = movingDemo.ShotsFired;
            bool moving = movingDemo.Outcome == DemoOutcome.Running;
            transform.localPosition = Vector3.up * (moving ? Mathf.Abs(Mathf.Sin(time * 11)) * 0.045f : 0);
            sprite.color = time < flashUntil ? new Color(1, 0.48f, 0.48f) : Color.white;
        }
    }
}
