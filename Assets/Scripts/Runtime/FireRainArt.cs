using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Shared, imported premultiplied native frames. Only loaded when the effect is first needed.
    internal static class FireRainArt
    {
        [Serializable] sealed class Manifest { public int version,fps;public Data[] clips; }
        [Serializable] sealed class Data { public string key;public int width,height,columns,count;public float ppu,pivotX,pivotY;public Page[] pages; }
        [Serializable] sealed class Page { public string file;public int count; }
        internal sealed class Clip
        {
            internal Sprite[] Frames;
            internal Sprite At(double age,bool loop=false)
            {
                if(age<0||double.IsNaN(age)||double.IsInfinity(age))return null;
                int n=(int)(age*30+1e-6);
                return loop?Frames[n%Frames.Length]:n<Frames.Length?Frames[n]:null;
            }
        }
        static Manifest manifest;
        static readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        internal static Clip Load(string key)
        {
            if(clips.TryGetValue(key,out var clip)&&clip.Frames[0]!=null&&clip.Frames[0].texture!=null)return clip;
            if(manifest==null)
            {
                var text=Resources.Load<TextAsset>("TalulahFireRain/manifest");
                if(text==null)throw new InvalidDataException("Missing Talulah fire-rain manifest");
                manifest=JsonUtility.FromJson<Manifest>(text.text);
            }
            if(manifest.version!=1||manifest.fps!=30)throw new InvalidDataException("Unsupported Talulah fire-rain format");
            var d=Array.Find(manifest.clips,x=>x.key==key);
            if(d==null||d.width<=0||d.height<=0||d.columns<=0||d.ppu<=0)throw new InvalidDataException("Invalid Talulah fire-rain clip: "+key);
            var frames=new List<Sprite>();
            foreach(var page in d.pages)
            {
                var texture=Resources.Load<Texture2D>("TalulahFireRain/"+Path.GetFileNameWithoutExtension(page.file));
                if(texture==null||page.count>d.columns*(texture.height/d.height))throw new InvalidDataException("Missing fire-rain page: "+page.file);
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(texture,new Rect(i%d.columns*d.width,texture.height-(i/d.columns+1)*d.height,d.width,d.height),new Vector2(d.pivotX,d.pivotY),d.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="TalulahFireRain/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=d.count||d.count==0)throw new InvalidDataException("Incomplete fire-rain clip: "+key);
            clip=new Clip{Frames=frames.ToArray()};clips[key]=clip;return clip;
        }
    }
}
