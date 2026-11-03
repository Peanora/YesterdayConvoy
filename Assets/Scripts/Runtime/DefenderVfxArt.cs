using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Read-only, imported texture flipbooks. Rendering never schedules a combat event.</summary>
    public static class DefenderVfxArt
    {
        [Serializable] sealed class Manifest { public int version; public Data[] clips; }
        [Serializable] sealed class Data { public string key; public float duration,ppu,pivotX,pivotY; public int cell,count,columns; public bool loop; public Page[] pages; }
        [Serializable] sealed class Page { public string file; public int count; }
        public sealed class Clip
        {
            public Sprite[] Frames { get; internal set; }
            public float Duration { get; internal set; }
            public bool Loop { get; internal set; }
            public Sprite At(double age)
            {
                if(double.IsNaN(age)||double.IsInfinity(age)||age<0||!Loop&&age>=Duration)return null;
                if(Loop)age%=Duration;
                return Frames[Mathf.Clamp((int)((age+1e-7)*60),0,Frames.Length-1)];
            }
        }
        static Manifest manifest;
        static readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        public static bool Supports(DefenderSkillKind kind)=>kind==DefenderSkillKind.LapplandHavoc||kind==DefenderSkillKind.YuKitchen||kind==DefenderSkillKind.PramanixSnow||kind==DefenderSkillKind.WangGambit||kind==DefenderSkillKind.SariaCalcification||kind==DefenderSkillKind.MlynarGlory;
        public static Clip Load(string key)
        {
            if(clips.TryGetValue(key,out var found)&&found.Frames[0]!=null&&found.Frames[0].texture!=null)return found;
            if(manifest==null)manifest=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("DefenderVfx/manifest").text);
            var data=Array.Find(manifest.clips,c=>c.key==key);
            if(data==null||manifest.version!=1||data.duration<=0||data.ppu<=0||data.cell<=0||data.columns<=0)throw new InvalidDataException("Invalid defender VFX: "+key);
            var frames=new List<Sprite>();
            foreach(var page in data.pages)
            {
                var texture=Resources.Load<Texture2D>("DefenderVfx/"+Path.GetFileNameWithoutExtension(page.file));
                if(texture==null)throw new InvalidDataException("Missing defender VFX texture: "+page.file);
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(texture,new Rect(i%data.columns*data.cell,texture.height-(i/data.columns+1)*data.cell,data.cell,data.cell),new Vector2(data.pivotX,data.pivotY),data.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="DefenderVfx/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=data.count)throw new InvalidDataException("Incomplete defender VFX: "+key);
            found=new Clip{Frames=frames.ToArray(),Duration=data.duration,Loop=data.loop};clips[key]=found;return found;
        }
    }
}
