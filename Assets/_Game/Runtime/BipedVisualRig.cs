using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Authored per-view parts and joint positions saved in a visual prefab.</summary>
    public sealed class BipedVisualRig : MonoBehaviour
    {
        [Serializable] public sealed class Part
        {
            public Transform Bone;
            public SpriteRenderer Renderer;
        }
        [Serializable] public sealed class View
        {
            public Transform Root;
            public List<Part> Parts = new List<Part>();
            public Part Head, Body, Tail, ArmL, HandL, ArmR, HandR, ThighL, BootL, ShoeL, ThighR, BootR, ShoeR;
            public Vector2 HipL, KneeL, AnkleL, FootL, HipR, KneeR, AnkleR, FootR;
            public float Ppu;
            // Sprite.Create sub-assets reload as rectangles in this Unity version. Keep the
            // head-only polygon independently so the original full-body image cannot cover legs.
            public Vector2[] HeadVertices;
            public int[] HeadTriangles;
        }
        public View Front, Back, Side, RearQuarter;
        private readonly List<Sprite> headInstances = new List<Sprite>();

        public void PrepareHeadGeometry()
        {
            if (headInstances.Count != 0) return;
            foreach (View view in Views)
            {
                Sprite head = Instantiate(view.Head.Renderer.sprite);
                head.name = view.Head.Renderer.sprite.name + " (head only)";
                var triangles = new ushort[view.HeadTriangles.Length];
                for (int i = 0; i < triangles.Length; i++) triangles[i] = (ushort)view.HeadTriangles[i];
                var pixels = new Vector2[view.HeadVertices.Length];
                // Sprite.vertices uses local units; OverrideGeometry expects Sprite-rect pixels.
                for (int i = 0; i < pixels.Length; i++) pixels[i] = view.HeadVertices[i] * head.pixelsPerUnit + head.pivot;
                head.OverrideGeometry(pixels, triangles);
                view.Head.Renderer.sprite = head;
                headInstances.Add(head);
            }
        }

        private void OnDestroy()
        {
            foreach (Sprite head in headInstances) Destroy(head);
        }
        public IEnumerable<View> Views
        {
            get { yield return Front; yield return Side; yield return RearQuarter; yield return Back; }
        }
        public bool IsComplete
        {
            get
            {
                foreach (View view in Views)
                {
                    if (view == null || view.Root == null || view.Parts.Count < 13) return false;
                    if (view.HeadVertices == null || view.HeadVertices.Length < 5 ||
                        view.HeadTriangles == null || view.HeadTriangles.Length < 3) return false;
                    foreach (Part part in view.Parts)
                        if (part.Bone == null || part.Renderer == null || part.Renderer.sprite == null) return false;
                }
                return true;
            }
        }
    }
}
