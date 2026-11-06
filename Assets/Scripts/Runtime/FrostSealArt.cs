using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Compatibility names now resolve the shared original-effect package, never the retired generated VFX.</summary>
    public static class FrostSealArt
    {
        public sealed class Clip
        {
            internal OriginalCombatVfxArt.Clip Original;
            public Sprite[] Frames=>Original.Sequence.Frames;
            public float Duration=>Original.Duration;
            public Sprite At(double age)=>Original.At(age);
        }
        static readonly Dictionary<string,Clip> cache=new Dictionary<string,Clip>(StringComparer.Ordinal);
        public static Clip Load(string key)
        {
            if(cache.TryGetValue(key,out var clip))return clip;
            string raw=key=="storm"?"firstar_skill_01_range":key=="ice"?"firstar_skill_02_range_02":key;
            clip=new Clip{Original=OriginalCombatVfxArt.Get("monster/"+raw)};cache.Add(key,clip);return clip;
        }
    }
}
