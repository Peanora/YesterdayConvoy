using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>
    /// Lazy presentation access for formation cards. This exposes authored frames, not a migration
    /// of their original combat abilities. Metadata lookup never decodes animation atlases.
    /// </summary>
    public sealed class FormationUnitArt
    {
        static readonly Dictionary<string,FormationUnitArt> cache = new Dictionary<string,FormationUnitArt>(StringComparer.Ordinal);
        static readonly Dictionary<string,CardDefinition> cards = new Dictionary<string,CardDefinition>(StringComparer.Ordinal);
        static readonly string[] warmKeys = {"idle","walk","attack_start","attack","attack_end","cast","skill","death"};
        const float SerializedFrameRate = 20f; // Existing Wisadel comic player uses twenty frames per second.

        readonly ArtCatalog catalog;
        readonly CardDefinition card;
        readonly float height;
        public string Root => catalog?.Root;
        public float Height => height;
        public Sprite Portrait => catalog != null ? catalog.Portrait : card != null ? card.Portrait : null;

        FormationUnitArt(ArtCatalog source)
        {
            catalog=source;
            height=source.Height;
            if(height<=0 && source.Clips.TryGetValue("idle",out var idle))
                height=idle.Height/idle.Ppu;
        }

        FormationUnitArt(CardDefinition source)
        {
            card=source;
            var idle=source.BattleSprite;
            height=idle!=null ? idle.bounds.size.y : 0;
        }

        /// <summary>Register scene references without loading any animation package.</summary>
        public static void RegisterCards(IEnumerable<CardDefinition> definitions)
        {
            if(definitions==null)return;
            foreach(var definition in definitions)
            {
                if(definition==null || string.IsNullOrEmpty(definition.CardId))continue;
                if(cards.TryGetValue(definition.CardId,out var old) && old==definition)continue;
                cards[definition.CardId]=definition;
                // Serialized Sprite packages may be replaced on a new scene; native packages are
                // independent of CardDefinition references and retain their shared metadata cache.
                if(FormationUnitCatalog.Find(definition.CardId)?.ArtRoot==null)
                    cache.Remove(definition.CardId);
            }
        }

        public static FormationUnitArt For(string cardOrArtId)
        {
            var definition=FormationUnitCatalog.Find(cardOrArtId);
            if(definition==null)return null;
            if(cache.TryGetValue(definition.CardId,out var prior))return prior;
            FormationUnitArt art;
            if(definition.ArtRoot==null)
            {
                // Wisadel's approved comic frames are authored Sprite references, not Resources
                // or a MonsterCombatArt manifest. The scene supplies the existing card asset.
                if(!cards.TryGetValue(definition.CardId,out var source) || source==null)return null;
                art=new FormationUnitArt(source);
            }
            else
            {
                var source=ArtCatalog.Read(definition.ArtRoot);
                if(definition.CardId=="CARD_TALULAH_TRIAL")source.Extras(new[]{"death"});
                art=new FormationUnitArt(source);
            }
            cache.Add(definition.CardId,art);
            return art;
        }

        public bool HasAction(string action)
        {
            if(string.IsNullOrEmpty(action))return false;
            if(catalog!=null)return catalog.Clips.ContainsKey(action);
            if(action=="idle")return card!=null && card.BattleSprite!=null;
            var frames=SerializedFrames(action);
            return frames!=null && frames.Count>0;
        }

        /// <summary>Source seconds. Attack includes authored start/main/end phases when present.</summary>
        public float Duration(string action)
        {
            if(!HasAction(action))return 0;
            if(catalog!=null)
            {
                float duration=catalog.Clips[action].Duration;
                if(action=="attack")
                {
                    if(catalog.Clips.TryGetValue("attack_start",out var start))duration+=start.Duration;
                    if(catalog.Clips.TryGetValue("attack_end",out var end))duration+=end.Duration;
                }
                return duration;
            }
            return action=="idle" ? 1f : SerializedFrames(action).Count/SerializedFrameRate;
        }

        /// <summary>Readiness is live: the shared memory cache may evict a previously warmed clip.</summary>
        public bool IsReady
        {
            get
            {
                if(catalog==null)return HasAction("idle");
                foreach(string key in warmKeys)
                    if(catalog.Clips.TryGetValue(key,out var clip) && !clip.Ready)return false;
                return true;
            }
        }

        /// <summary>Only this role's standard presentation clips; no roster-wide or extra-skill warm.</summary>
        public IEnumerator Warm()
        {
            if(catalog==null)yield break; // Serialized comic frames are already Unity Sprite assets.
            foreach(string key in warmKeys)
                if(catalog.Clips.TryGetValue(key,out var clip) && !clip.Ready)
                    yield return clip.Warm();
        }

        /// <summary>Missing actions return null, including Wisadel's unauthored death animation.</summary>
        public Sprite Frame(string action, double time, bool loop=true)
        {
            if(!HasAction(action))return null;
            double duration=Duration(action);
            time=double.IsNaN(time) || time<0 ? 0 : double.IsPositiveInfinity(time) ? duration : time;
            time=loop ? time%duration : Math.Min(time,duration);
            if(catalog==null)
            {
                if(action=="idle")return card.BattleSprite;
                var frames=SerializedFrames(action);
                return frames[Index(time,duration,frames.Count)];
            }
            if(action=="attack" && catalog.Clips.TryGetValue("attack_start",out var start))
            {
                if(time<start.Duration)return Sample(start,time);
                time-=start.Duration;
            }
            var main=catalog.Clips[action];
            if(action=="attack" && catalog.Clips.TryGetValue("attack_end",out var end) && time>=main.Duration)
                return Sample(end,time-main.Duration);
            return Sample(main,time);
        }

        IReadOnlyList<Sprite> SerializedFrames(string action)
        {
            if(card==null)return null;
            switch(action)
            {
                case "walk":return card.WalkFrames;
                case "attack":return card.AttackFrames;
                // A semantic alias of the existing skill frame array, not newly authored motion.
                case "cast":case "skill":return card.SkillFrames;
                default:return null;
            }
        }

        static Sprite Sample(ArtSequence clip,double time) => clip.Frames[Index(time,clip.Duration,clip.Count)];
        static int Index(double time,double duration,int count) => Mathf.Clamp((int)(time/duration*count+1e-5),0,count-1);
    }
}
