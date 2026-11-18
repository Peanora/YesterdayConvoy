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
        static readonly string[] warmKeys = {"idle","walk","attack_start","attack","attack_end","cast","skill","death",
            "extra_die_2","extra_idle_2","extra_move_2","extra_revive",
            "extra_skill_front_start","extra_skill_front_loop","extra_skill_front_end"};
        const float SerializedFrameRate = 20f; // Existing Wisadel comic player uses twenty frames per second.

        readonly ArtCatalog catalog;
        readonly BossRosterArt native;
        readonly CardDefinition card;
        readonly float height;
        public string Root => native!=null ? "BossRoster/"+native.Task+"/"+native.View : catalog?.Root;
        public float Height => height;
        public Sprite Portrait => native!=null ? native.Portrait : catalog != null ? catalog.Portrait : card != null ? card.Portrait : null;
        internal string NativeTask => native?.Task;
        internal IEnumerable<string> NativeActions => native?.SourceNames ?? Array.Empty<string>();

        FormationUnitArt(BossRosterArt source)
        { native=source; height=source.Height; }

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
            var original = BossRosterArt.For(definition.CardId);
            if(original!=null)
                art=new FormationUnitArt(original);
            else if(definition.ArtRoot==null)
            {
                // Preserve the approved user GIF Sprite fallback if the independently
                // identified original April outfit package is unavailable.
                if(!cards.TryGetValue(definition.CardId,out var source) || source==null)return null;
                art=new FormationUnitArt(source);
            }
            else
            {
                var source=ArtCatalog.Read(definition.ArtRoot);
                if(definition.CardId=="CARD_TALULAH_TRIAL")source.Extras(new[]{"death"});
                if(definition.CardId=="CARD_M30")source.Extras(new[]{"extra_die_2","extra_idle_2","extra_move_2","extra_revive"});
                if(definition.CardId=="CARD_M31")source.Extras(new[]{"extra_skill_front_start","extra_skill_front_loop","extra_skill_front_end"});
                art=new FormationUnitArt(source);
            }
            cache.Add(definition.CardId,art);
            return art;
        }

        public bool HasAction(string action)
        {
            if(string.IsNullOrEmpty(action))return false;
            if(native!=null)return native.HasClip(action);
            if(catalog!=null)return catalog.Clips.ContainsKey(action);
            if(action=="idle")return card!=null && card.BattleSprite!=null;
            var frames=SerializedFrames(action);
            return frames!=null && frames.Count>0;
        }

        /// <summary>Source seconds. Attack includes authored start/main/end phases when present.</summary>
        public float Duration(string action)
        {
            if(!HasAction(action))return 0;
            if(native!=null)return native[action].Duration;
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

        // Native source event times stay in original seconds. The presenter/timing
        // bridge applies the requested visual 2x once; this accessor does no retiming.
        internal IReadOnlyList<float> ContactTimes(string action)
        {
            if(!HasAction(action))return Array.Empty<float>();
            if(native!=null)return native[action].OnAttackTimes;
            if(action=="attack"&&catalog!=null&&catalog.Contact>0)return new[]{catalog.Contact};
            return Array.Empty<float>();
        }
        // NaN explicitly means no precise source marker is retained for this action.
        internal float ContactTime(string action)
        {var times=ContactTimes(action);return times.Count>0?times[0]:float.NaN;}
        internal bool HasOrdinaryAttack=>HasAction("attack");
        internal string SourceAction(string action)
            =>HasAction(action)?native!=null?native[action].Source:action:null;

        /// <summary>Painted per-motion top above the captured foot root, before body scale.</summary>
        public float PresentationTop(string action)
        {
            if(native!=null&&HasAction(action)&&native[action].PresentationTop>0)
                return native[action].PresentationTop;
            return Height;
        }

        /// <summary>Readiness is live: the shared memory cache may evict a previously warmed clip.</summary>
        public bool IsReady
        {
            get
            {
                if(native!=null)
                {
                    foreach(string key in warmKeys)
                        if(native.HasClip(key)&&!native[key].ReadyFor(0,.6,true))return false;
                    return true;
                }
                if(catalog==null)return HasAction("idle");
                foreach(string key in warmKeys)
                    if(catalog.Clips.TryGetValue(key,out var clip) && !clip.Ready)return false;
                return true;
            }
        }

        /// <summary>Only this role's standard presentation clips; no roster-wide or extra-skill warm.</summary>
        public IEnumerator Warm()
        {
            if(native!=null)
            {
                foreach(string key in warmKeys)
                    if(native.HasClip(key))foreach(var sequence in native.SequencesFor(key,0,.6,true))
                        if(!sequence.Ready)yield return sequence.Warm();
                yield break;
            }
            if(catalog==null)yield break; // Serialized comic frames are already Unity Sprite assets.
            foreach(string key in warmKeys)
                if(catalog.Clips.TryGetValue(key,out var clip) && !clip.Ready)
                    yield return clip.Warm();
        }

        /// <summary>Missing actions return null; they are never synthesized from unrelated art.</summary>
        public Sprite Frame(string action, double time, bool loop=true)
        {
            if(!HasAction(action))return null;
            double duration=Duration(action);
            time=double.IsNaN(time) || time<0 ? 0 : double.IsPositiveInfinity(time) ? duration : time;
            time=loop ? time%duration : Math.Min(time,duration);
            if(native!=null)
                return native[action].ReadyFor(time,0,loop) ? native[action].At(time,loop,out _) : null;
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

        internal bool WindowReady(string action,double time,bool loop)
            =>native==null || !HasAction(action) || native[action].ReadyFor(time,.6,loop);
        internal IEnumerator WarmWindow(string action,double time,bool loop)
        {
            if(native==null||!HasAction(action))yield break;
            foreach(var sequence in native.SequencesFor(action,time,.6,loop))
                if(!sequence.Ready)yield return sequence.Warm();
        }
        internal IEnumerable<string> WindowOwners(string action,double time,bool loop)
        {
            if(!HasAction(action))yield break;
            if(native!=null)
            {
                foreach(var sequence in native.SequencesFor(action,time,.6,loop))yield return sequence.CacheKey;
            }
            else if(catalog!=null)yield return catalog.Clips[action].CacheKey;
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
