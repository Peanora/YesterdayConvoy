using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved front battle flipbooks, loaded from tracked StreamingAssets. No Spine runtime shipped.</summary>
    public sealed class DefenderArt
    {
        [Serializable] private sealed class Manifest { public int version; public string task; public float ppu; public bool rearVariant,noOrdinaryAttack,auxiliary; public ClipData[] clips; }
        [Serializable] private sealed class ClipData
        { public string key; public int count, cell, columns; public float duration, pivotX, pivotY,pixelsPerUnit; public Page[] pages; }
        [Serializable] private sealed class Page { public string file; public int count, width, height; }
        public sealed class Clip
        {
            public Sprite[] Frames { get; internal set; }
            public float Duration { get; internal set; }
            public Sprite At(double time, bool loop)
            {
                time=Math.Max(0,time); if(loop) time%=Duration;
                return Frames[Mathf.Clamp((int)(time/Duration*Frames.Length),0,Frames.Length-1)];
            }
        }
        private static readonly Dictionary<string,DefenderArt> Cache=new Dictionary<string,DefenderArt>();
        private readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        private bool auxiliary;
        private bool ResourcesAlive
        {
            get
            {
                if(!auxiliary&&Portrait==null)return false;
                foreach(var clip in clips.Values)if(clip.Frames[0]==null||clip.Frames[0].texture==null)return false;
                return clips.Count>0;
            }
        }
        public Sprite Portrait { get; private set; }
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
            if(Cache.TryGetValue(id,out var prior)&&prior.ResourcesAlive) return prior;
            Cache.Remove(id);
            if(string.IsNullOrEmpty(id) || Path.GetFileName(id)!=id || !id.StartsWith("D",StringComparison.Ordinal))
                throw new InvalidDataException("Invalid defender art ID.");
            string root=Path.Combine(Application.streamingAssetsPath,"DefenderArt",id);
            var allocated=new List<UnityEngine.Object>();
            try
            {
                var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root,"manifest.json")));
                if(data==null || data.version!=1 || data.task!=id || !FinitePositive(data.ppu) || data.clips==null)
                    throw new InvalidDataException("Invalid defender art manifest.");
                if(data.auxiliary && id!="D28Chess")throw new InvalidDataException("Unexpected auxiliary defender art.");
                var art=new DefenderArt { auxiliary=data.auxiliary };
                foreach(var item in data.clips)
                {
                    if(item.count<=0 || item.count>600 || !FinitePositive(item.duration) || item.cell<=0 || item.cell>512 ||
                        item.columns<=0 || item.columns>16 || item.pages==null || item.pivotX<0 || item.pivotX>1 ||
                        item.pivotY<0 || item.pivotY>1 || float.IsNaN(item.pivotX) || float.IsNaN(item.pivotY))
                        throw new InvalidDataException("Invalid defender clip.");
                    float ppu=item.pixelsPerUnit==0?data.ppu:item.pixelsPerUnit;
                    if(!FinitePositive(ppu))throw new InvalidDataException("Invalid clip pixels per unit.");
                    var frames=new List<Sprite>();
                    foreach(var page in item.pages)
                    {
                        var texture=LoadPng(root,page.file,allocated);
                        if(texture.width!=page.width || texture.height!=page.height || page.width!=item.columns*item.cell ||
                            page.height%item.cell!=0 || page.count<=0 || page.count>item.columns*(page.height/item.cell))
                            throw new InvalidDataException("Defender atlas layout mismatch.");
                        for(int i=0;i<page.count;i++)
                        {
                            var sprite=Sprite.Create(texture,new Rect(i%item.columns*item.cell,
                                page.height-(i/item.columns+1)*item.cell,item.cell,item.cell),new Vector2(item.pivotX,item.pivotY),ppu,0,SpriteMeshType.FullRect);
                            sprite.name=id+"/"+item.key+"/"+frames.Count; allocated.Add(sprite); frames.Add(sprite);
                        }
                    }
                    if(frames.Count!=item.count) throw new InvalidDataException("Defender frame count mismatch.");
                    art.clips.Add(item.key,new Clip { Frames=frames.ToArray(),Duration=item.duration });
                }
                if(data.rearVariant && !id.EndsWith("B",StringComparison.Ordinal))throw new InvalidDataException("Invalid rear variant.");
                foreach(string key in data.auxiliary?new[]{"start","idle","attack"}:data.noOrdinaryAttack ? (data.rearVariant?new[]{"idle"}:new[]{"idle","death"}) : data.rearVariant ? new[]{"idle","attack"} : new[]{"idle","attack","death"}) if(!art.clips.ContainsKey(key)) throw new InvalidDataException("Missing "+key);
                if(art.clips.ContainsKey("attack_start")!=art.clips.ContainsKey("attack_end")) throw new InvalidDataException("Incomplete attack phases.");
                if(!data.auxiliary && data.rearVariant)art.Portrait=Load(id.Substring(0,id.Length-1)).Portrait;
                else if(!data.auxiliary)
                {
                    var portrait=LoadPng(root,"portrait.png",allocated);
                    art.Portrait=Sprite.Create(portrait,new Rect(0,0,portrait.width,portrait.height),new Vector2(.5f,0),data.ppu);
                    allocated.Add(art.Portrait);
                }
                Cache.Add(id,art); return art;
            }
            catch
            {
                foreach(var value in allocated) if(value!=null)
                { if(Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
                throw;
            }
        }
        private static bool FinitePositive(float n) => n>0 && !float.IsNaN(n) && !float.IsInfinity(n);
        private static Texture2D LoadPng(string root,string file,List<UnityEngine.Object> allocated)
        {
            if(string.IsNullOrEmpty(file) || Path.GetFileName(file)!=file || file.Contains(":") || !file.EndsWith(".png",StringComparison.Ordinal))
                throw new InvalidDataException("Expected local defender PNG.");
            var tex=new Texture2D(2,2,TextureFormat.RGBA32,false); allocated.Add(tex);
            if(!ImageConversion.LoadImage(tex,File.ReadAllBytes(Path.Combine(root,file)),false) || tex.width>2048 || tex.height>2048)
                throw new InvalidDataException("Invalid defender texture.");
            tex.filterMode=FilterMode.Bilinear; tex.wrapMode=TextureWrapMode.Clamp;
            tex.Compress(true); tex.Apply(false,true); return tex;
        }
    }
}
