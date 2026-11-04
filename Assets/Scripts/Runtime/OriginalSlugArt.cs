using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    internal sealed class OriginalSlugArt
    {
        internal const string CardId="CARD_ORIGINIUM_SLUG";
        static OriginalSlugArt cached;
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        ArtCatalog catalog;
        internal Sprite Portrait=>catalog.Portrait;
        internal float ReferenceSpeed=>catalog.Speed;
        internal float AttackContact=>catalog.Contact;
        internal IEnumerable<ArtSequence> Sequences=>catalog.Clips.Values;
        internal Clip this[string key]=>clips[key];
        internal sealed class Clip
        {
            internal ArtSequence Sequence;
            internal Sprite[] Frames=>Sequence.Frames;
            internal float Duration=>Sequence.Duration;
            internal Sprite At(double time,bool loop,out int index)
            {time=Math.Max(0,time);if(loop)time%=Duration;index=Mathf.Clamp((int)(time*Sequence.Fps+1e-5),0,Sequence.Count-1);return Frames[index];}
        }
        internal static OriginalSlugArt For(string cardId)
        {
            if(cardId!=CardId)return null;
            if(cached!=null)return cached;
            try
            {
                var art=new OriginalSlugArt{catalog=ArtCatalog.Read("OriginiumSlugOriginal")};
                foreach(var pair in art.catalog.Clips){pair.Value.Name="OriginiumSlug/"+pair.Key;art.clips.Add(pair.Key,new Clip{Sequence=pair.Value});}
                cached=art;return art;
            }
            catch(Exception e){Debug.LogWarning("SLUG_ORIGINAL_UNAVAILABLE: "+e.Message);return null;}
        }
    }
}
