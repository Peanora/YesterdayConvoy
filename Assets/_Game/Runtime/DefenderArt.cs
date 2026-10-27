using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved front battle flipbooks, loaded from tracked StreamingAssets. No Spine runtime shipped.</summary>
    public sealed class DefenderArt
    {
        [Serializable] private sealed class Manifest { public int version; public string task; public float ppu; public bool rearVariant; public ClipData[] clips; }
        [Serializable] private sealed class ClipData
        { public string key; public int count, cell, columns; public float duration, pivotX, pivotY; public Page[] pages; }
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
        public Sprite Portrait { get; private set; }
        public float ActionDuration => clips["attack"].Duration+(clips.TryGetValue("attack_start",out var start)?start.Duration:0)+(clips.TryGetValue("attack_end",out var end)?end.Duration:0);
        public Clip this[string key] => clips[key];
        public Sprite HealAt(double age) => clips.TryGetValue("heal",out var clip) ? clip.At(age,false) : ActionAt(age);
        public Sprite ActionAt(double age)
        {
            if(clips.TryGetValue("attack_start",out var start))
            {
                if(age<start.Duration) return start.At(age,false);
                age-=start.Duration; var loop=clips["attack"];
                if(age<loop.Duration) return loop.At(age,false);
                return clips["attack_end"].At(age-loop.Duration,false);
            }
            return clips["attack"].At(age,false);
        }
        public static DefenderArt Load(string id)
        {
            if(Cache.TryGetValue(id,out var prior)) return prior;
            if(string.IsNullOrEmpty(id) || Path.GetFileName(id)!=id || !id.StartsWith("D",StringComparison.Ordinal))
                throw new InvalidDataException("Invalid defender art ID.");
            string root=Path.Combine(Application.streamingAssetsPath,"DefenderArt",id);
            var allocated=new List<UnityEngine.Object>();
            try
            {
                var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root,"manifest.json")));
                if(data==null || data.version!=1 || data.task!=id || !FinitePositive(data.ppu) || data.clips==null)
                    throw new InvalidDataException("Invalid defender art manifest.");
                var art=new DefenderArt();
                foreach(var item in data.clips)
                {
                    if(item.count<=0 || item.count>600 || !FinitePositive(item.duration) || item.cell<=0 || item.cell>512 ||
                        item.columns<=0 || item.columns>16 || item.pages==null || item.pivotX<0 || item.pivotX>1 ||
                        item.pivotY<0 || item.pivotY>1 || float.IsNaN(item.pivotX) || float.IsNaN(item.pivotY))
                        throw new InvalidDataException("Invalid defender clip.");
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
                                page.height-(i/item.columns+1)*item.cell,item.cell,item.cell),new Vector2(item.pivotX,item.pivotY),data.ppu,0,SpriteMeshType.FullRect);
                            sprite.name=id+"/"+item.key+"/"+frames.Count; allocated.Add(sprite); frames.Add(sprite);
                        }
                    }
                    if(frames.Count!=item.count) throw new InvalidDataException("Defender frame count mismatch.");
                    art.clips.Add(item.key,new Clip { Frames=frames.ToArray(),Duration=item.duration });
                }
                if(data.rearVariant && !id.EndsWith("B",StringComparison.Ordinal))throw new InvalidDataException("Invalid rear variant.");
                foreach(string key in data.rearVariant ? new[]{"idle","attack"} : new[]{"idle","attack","death"}) if(!art.clips.ContainsKey(key)) throw new InvalidDataException("Missing "+key);
                if(art.clips.ContainsKey("attack_start")!=art.clips.ContainsKey("attack_end")) throw new InvalidDataException("Incomplete attack phases.");
                if(data.rearVariant)art.Portrait=Load(id.Substring(0,id.Length-1)).Portrait;
                else
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
