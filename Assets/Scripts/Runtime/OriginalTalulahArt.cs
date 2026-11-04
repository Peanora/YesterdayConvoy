using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    internal sealed class OriginalTalulahArt
    {
        internal const string CardId="CARD_TALULAH_TRIAL";
        static OriginalTalulahArt cached;
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
            {time=Math.Max(0,time);if(loop)time%=Duration;index=Mathf.Clamp((int)(time/Duration*Sequence.Count+1e-5),0,Sequence.Count-1);return Frames[index];}
        }
        internal static bool Available(CardDefinition card)=>card!=null&&card.CardId==CardId&&For(card)!=null;
        internal static Sprite BattleSprite(CardDefinition card)=>For(card)?["idle"].Frames[0]??card.BattleSprite;
        internal static Sprite CardPortrait(CardDefinition card)=>For(card)?.Portrait??card.Portrait;
        internal static OriginalTalulahArt For(CardDefinition card)
        {
            if(card==null||card.CardId!=CardId)return null;
            if(cached!=null)return cached;
            try
            {
                var art=new OriginalTalulahArt{catalog=ArtCatalog.Read("TalulahOriginal")};
                foreach(var pair in art.catalog.Clips){pair.Value.Name="TalulahOriginal/"+pair.Key;art.clips.Add(pair.Key,new Clip{Sequence=pair.Value});}
                cached=art;return art;
            }
            catch(Exception e){Debug.LogWarning("TALULAH_ORIGINAL_UNAVAILABLE: "+e.Message);return null;}
        }
    }
}
