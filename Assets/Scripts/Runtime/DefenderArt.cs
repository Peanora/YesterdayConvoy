using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved front battle flipbooks from classified source art or the compiled Player payload.</summary>
    public sealed class DefenderArt
    {
        public sealed class Clip
        {
            internal ArtSequence Sequence;
            public Sprite[] Frames=>Sequence.Frames;
            public float Duration=>Sequence.Duration;
            public Sprite At(double time,bool loop)
            {time=Math.Max(0,time);if(loop)time%=Duration;return Frames[Mathf.Clamp((int)(time/Duration*Sequence.Count),0,Sequence.Count-1)];}
        }
        static readonly Dictionary<string,DefenderArt> Cache=new Dictionary<string,DefenderArt>();
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        ArtCatalog catalog;
        public Sprite Portrait=>catalog.Auxiliary?null:catalog.Rear?ArtCatalog.Read(catalog.Root.Substring(0,catalog.Root.Length-1)).Portrait:catalog.Portrait;
        public static Sprite CardPortrait(string id)=>Load(id).Portrait;
        internal IEnumerable<ArtSequence> Sequences=>catalog.Clips.Values;
        public float StartDuration => clips.TryGetValue("start",out var clip)?clip.Duration:0;
        public float SkillEndDuration => clips.TryGetValue("skill_end",out var clip)?clip.Duration:0;
        public int AttackVariants => clips.ContainsKey("attack_2_b")?4:clips.ContainsKey("attack_b")?2:1;
        public int SkillAttackVariants => clips.ContainsKey("skill_attack_c")?3:clips.ContainsKey("skill_attack_b")?2:1;
        public Sprite StartAt(double age,bool alternate=false) => clips.TryGetValue(alternate&&clips.ContainsKey("start_2")?"start_2":"start",out var clip)&&age>=0&&age<clip.Duration?clip.At(age,false):null;
        public Sprite IdleAt(double age,int variant=0)
        {
            if(clips.TryGetValue("idle_2",out var alternate))
            {
                if(clips.ContainsKey("attack_2_a")&&variant%4>=2)return alternate.At(age,true);
                // A rare native idle gesture for non-attacking operators, on the same display clock.
                double phase=Math.Max(0,age)%12;
                if(!clips.ContainsKey("attack")&&phase>=10&&phase<10+alternate.Duration)return alternate.At(phase-10,false);
            }
            return clips["idle"].At(age,true);
        }
        public float ActionDuration => (clips.TryGetValue("attack",out var attack)?attack.Duration:0)+(clips.TryGetValue("attack_start",out var start)?start.Duration:0)+(clips.TryGetValue("attack_end",out var end)?end.Duration:0);
        public Clip this[string key] => clips[key];
        public Sprite HealAt(double age) => clips.TryGetValue("heal",out var clip) ? clip.At(age,false) : ActionAt(age);
        public float HealDuration=>clips.TryGetValue("heal",out var clip)?clip.Duration:ActionDuration;
        public Sprite SkillIdleAt(double age)=>clips.TryGetValue("skill",out var clip)&&age<clip.Duration?clip.At(age,false):clips["idle"].At(age,true);
        public Sprite ActionAt(double age) => ActionAt(age,0);
        public Sprite ActionAt(double age,int variant)
        {
            if(AttackVariants>1)
            {
                int index=Math.Max(0,variant)%AttackVariants;
                string key=index==0?"attack":index==1?"attack_b":index==2?"attack_2_a":"attack_2_b";
                return clips[key].At(ActionDuration>0?age/ActionDuration*clips[key].Duration:age,false);
            }
            if(clips.TryGetValue("attack_start",out var start))
            {
                if(age<start.Duration) return start.At(age,false);
                age-=start.Duration; var loop=clips["attack"];
                if(age<loop.Duration) return loop.At(age,false);
                return clips["attack_end"].At(age-loop.Duration,false);
            }
            return clips.TryGetValue("attack",out var attack)?attack.At(age,false):clips["idle"].At(age,true);
        }
        public Sprite AdvancedPose(double age,double remaining,double sinceEnd,bool action,double actionAge,float actionDuration) => AdvancedPose(age,remaining,sinceEnd,action,actionAge,actionDuration,0);
        public Sprite AdvancedPose(double age,double remaining,double sinceEnd,bool action,double actionAge,float actionDuration,int variant)
        {
            if(remaining>0)
            {
                if(clips.TryGetValue("skill_begin",out var begin)&&age<begin.Duration)return begin.At(age,false);
                if(action&&clips.ContainsKey("skill_attack"))
                {
                    int index=Math.Max(0,variant)%SkillAttackVariants;
                    string key=index==0?"skill_attack":index==1?"skill_attack_b":"skill_attack_c";
                    var attack=clips[key];return attack.At(actionDuration>0?actionAge/actionDuration*attack.Duration:actionAge,false);
                }
                if(clips.TryGetValue("skill_idle",out var idle))return idle.At(age,true);
            }
            else if(sinceEnd>=0&&clips.TryGetValue("skill_end",out var end)&&sinceEnd<end.Duration)return end.At(sinceEnd,false);
            return clips["idle"].At(age,true);
        }
        public static DefenderArt Load(string id)
        {
            if(Cache.TryGetValue(id,out var prior))
            {
                bool invalid=false;foreach(var sequence in prior.Sequences)invalid|=ArtMemoryCache.InvalidateDestroyed(sequence.CacheKey);
                if(!invalid)return prior;Cache.Remove(id);
            }
            if(string.IsNullOrEmpty(id)||Path.GetFileName(id)!=id||!id.StartsWith("D",StringComparison.Ordinal))throw new InvalidDataException("Invalid defender art ID.");
            var art=new DefenderArt{catalog=ArtCatalog.Read("DefenderArt/"+id)};
            foreach(var pair in art.catalog.Clips)art.clips.Add(pair.Key,new Clip{Sequence=pair.Value});
            Cache.Add(id,art);return art;
        }
    }
}
