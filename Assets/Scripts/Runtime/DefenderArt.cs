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
            internal float Contact=-1;
            public Sprite[] Frames=>Sequence.Frames;
            public float Duration=>Sequence.Duration;
            public Sprite At(double time,bool loop)
            {time=Math.Max(0,time);if(loop)time%=Duration;return Frames[Mathf.Clamp((int)(time/Duration*Sequence.Count),0,Sequence.Count-1)];}
        }
        [Serializable] sealed class TimingManifest { public TimingClip[] clips; }
        [Serializable] sealed class TimingClip { public string key,source; public TimingEvent[] events; }
        [Serializable] sealed class TimingEvent { public string name; public float time; }
        static readonly Dictionary<string,DefenderArt> Cache=new Dictionary<string,DefenderArt>();
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        ArtCatalog catalog;
        public Sprite Portrait=>catalog.Auxiliary?null:catalog.Rear?ArtCatalog.Read(catalog.Root.Substring(0,catalog.Root.Length-1)).Portrait:catalog.Portrait;
        public static Sprite CardPortrait(string id)=>Load(id).Portrait;
        internal IEnumerable<ArtSequence> Sequences=>catalog.Clips.Values;
        public float StartDuration => clips.TryGetValue("start",out var clip)?clip.Duration:0;
        public float SkillEndDuration => clips.TryGetValue("skill_end",out var clip)?clip.Duration:0;
        public int AttackVariants => clips.ContainsKey("attack_2_b")?4:clips.ContainsKey("attack_c")?3:clips.ContainsKey("attack_b")?2:1;
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
        Clip Find(string key)=>clips.TryGetValue(key,out var clip)?clip:null;
        Clip AttackBegin(bool down)=>Find(down&&clips.ContainsKey("attack_down_begin")?"attack_down_begin":clips.ContainsKey("attack_begin")?"attack_begin":"attack_start");
        Clip AttackEnd=>Find("attack_finish")??Find("attack_end");
        string AttackKey(int variant,bool down)
        {
            if(down&&clips.ContainsKey("attack_down"))return "attack_down";
            int index=Math.Max(0,variant)%AttackVariants;
            return index==0?"attack":index==1?"attack_b":index==2?(AttackVariants==4?"attack_2_a":"attack_c"):"attack_2_b";
        }
        public float ActionDuration => (Find("attack")?.Duration??0)+(AttackBegin(false)?.Duration??0)+(AttackEnd?.Duration??0);
        internal float DurationOf(string key)=>Find(key)?.Duration??0;
        internal Sprite PhaseAt(string key,double age,bool loop=false)=>age>=0&&Find(key) is Clip clip&&(loop||age<clip.Duration)?clip.At(age,loop):null;
        public Clip this[string key] => clips[key];
        public Sprite HealAt(double age) => clips.TryGetValue("heal",out var clip) ? clip.At(age,false) : ActionAt(age);
        public float HealDuration=>clips.TryGetValue("heal",out var clip)?clip.Duration:ActionDuration;
        public Sprite SkillIdleAt(double age)=>clips.TryGetValue("skill",out var clip)&&age<clip.Duration?clip.At(age,false):clips["idle"].At(age,true);
        public Sprite ActionAt(double age) => ActionAt(age,0);
        public Sprite ActionAt(double age,int variant)
        {
            var begin=AttackBegin(false);var attack=Find(AttackKey(variant,false));var end=AttackEnd;
            if(begin!=null){if(age<begin.Duration)return begin.At(age,false);age-=begin.Duration;}
            if(attack==null)return clips["idle"].At(age,true);
            if(age<attack.Duration||end==null)return attack.At(age,false);
            return end.At(age-attack.Duration,false);
        }
        // Retime presentation around the launched shot, without changing its gameplay time.
        internal static double SourceTime(double age,double runtimeDuration,double contactAt,double sourceDuration,double sourceContact)
        {
            if(runtimeDuration<=0||sourceDuration<=0)return Math.Max(0,age);
            age=Math.Max(0,Math.Min(age,runtimeDuration));
            if(sourceContact<=0||sourceContact>=sourceDuration||contactAt<=0||contactAt>=runtimeDuration)return age/runtimeDuration*sourceDuration;
            return age<=contactAt?age/contactAt*sourceContact:sourceContact+(age-contactAt)/(runtimeDuration-contactAt)*(sourceDuration-sourceContact);
        }
        internal Sprite ActionPose(double age,double runtimeDuration,double contactAt,int variant,bool down,bool healing,bool first)
        {
            var heal=healing?Find("heal"):null;
            if(heal!=null)return heal.At(SourceTime(age,runtimeDuration,contactAt,heal.Duration,heal.Contact),false);
            var attack=Find(AttackKey(variant,down));if(attack==null)return IdleAt(age);
            bool nativeFirstOnly=catalog.Root.EndsWith("D35")||catalog.Root.EndsWith("D35B");
            var begin=first||clips.ContainsKey("attack_start")&&!nativeFirstOnly?AttackBegin(down):null;
            var end=AttackEnd;double lead=begin?.Duration??0,tail=end?.Duration??0;
            double t=SourceTime(age,runtimeDuration,contactAt,lead+attack.Duration+tail,attack.Contact<0?-1:lead+attack.Contact);
            if(begin!=null&&t<lead)return begin.At(t,false);t-=lead;
            if(t<attack.Duration||end==null)return attack.At(t,false);
            return end.At(t-attack.Duration,false);
        }
        public Sprite AdvancedPose(double age,double remaining,double sinceEnd,bool action,double actionAge,float actionDuration) => AdvancedPose(age,remaining,sinceEnd,action,actionAge,actionDuration,0);
        public Sprite AdvancedPose(double age,double remaining,double sinceEnd,bool action,double actionAge,float actionDuration,int variant)
            =>AdvancedPose(age,remaining,sinceEnd,action,actionAge,actionDuration,variant,false,-1);
        internal Sprite AdvancedPose(double age,double remaining,double sinceEnd,bool action,double actionAge,double actionDuration,int variant,bool down,double contactAt)
        {
            if(remaining>0)
            {
                var begin=Find(down&&clips.ContainsKey("skill_begin_down")?"skill_begin_down":"skill_begin");if(begin!=null&&age<begin.Duration)return begin.At(age,false);
                if(action&&clips.ContainsKey("skill_attack"))
                {
                    int index=Math.Max(0,variant)%SkillAttackVariants;
                    string key=down&&clips.ContainsKey("skill_attack_down")?"skill_attack_down":index==0?"skill_attack":index==1?"skill_attack_b":"skill_attack_c";
                    var attack=clips[key];return attack.At(SourceTime(actionAge,actionDuration,contactAt,attack.Duration,attack.Contact),false);
                }
                var idle=Find(down&&clips.ContainsKey("skill_idle_down")?"skill_idle_down":"skill_idle");if(idle!=null)return idle.At(age,true);
            }
            else
            {
                var end=Find(down&&clips.ContainsKey("skill_end_down")?"skill_end_down":"skill_end");if(sinceEnd>=0&&end!=null&&sinceEnd<end.Duration)return end.At(sinceEnd,false);
            }
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
            var timing=JsonUtility.FromJson<TimingManifest>(File.ReadAllText(ArtSourcePaths.Resolve(art.catalog.Root+"/manifest.json")));
            foreach(var item in timing?.clips??Array.Empty<TimingClip>())if(art.clips.TryGetValue(item.key,out var clip))
            {
                float contact=float.PositiveInfinity;
                foreach(var marker in item.events??Array.Empty<TimingEvent>())if(marker.name=="OnAttack")contact=Math.Min(contact,marker.time);
                clip.Contact=float.IsPositiveInfinity(contact)?OriginalContact(id,item.source):contact;
            }
            Cache.Add(id,art);return art;
        }
        static float OriginalContact(string id,string source)
        {
            // Existing catalogs predate source event serialization. Values come from the original Spine event audit.
            switch(id.TrimEnd('B'))
            {
                case "D21":return source=="Attack"||source=="Skill_3_Loop"?.4f:-1;
                case "D22":return source=="Attack"||source=="Skill_3_Loop"?.5f:-1;
                case "D23":return source=="Attack"?17f/30:source=="Skill_3_Loop"?2f/3:-1;
                case "D24":return source=="Attack_Loop"||source=="Attack_Down_Loop"?8f/30:source=="Skill_3_Loop"||source=="Skill_Down_3_Loop"?1f/6:source=="Skill_3_Skill"?1f/15:-1;
                case "D25":return source=="Attack"?1f/3:source!=null&&source.StartsWith("Skill_3_Loop")?.5f:-1;
                case "D26":return source=="Attack_A"||source=="Skill_3_Combat"?.5f:source=="Attack_B"?(id.EndsWith("B")?16f/30:17f/30):source=="Attack_C"?16f/30:-1;
                case "D27":return source!=null&&source.StartsWith("Skill_3_Attack")?2f/3:-1;
                case "D28":return source!=null&&source.StartsWith("Attack_1_")?.3f:source!=null&&source.StartsWith("Skill_3_Attack")?31f/30:-1;
                case "D29":return source=="Attack"?17f/30:source=="Attack_2"?14f/30:-1;
                case "D30":return source=="Skill_3_Loop"?16f/30:-1;
                default:return -1;
            }
        }
    }
}
