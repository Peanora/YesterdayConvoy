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
        }
        public View Front, Back, Side, RearQuarter;
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
                    foreach (Part part in view.Parts)
                        if (part.Bone == null || part.Renderer == null || part.Renderer.sprite == null) return false;
                }
                return true;
            }
        }
    }
}
