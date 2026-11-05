using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace YesterdayConvoy
{
    public static class MudrockVfxArt
    {
        [Serializable]sealed class Manifest{public int version,fps;public Data[] clips;}
        [Serializable]sealed class Data{public string key;public int width,height,columns,count;public float duration,ppu,pivotX,pivotY;public Page[] pages;}
        [Serializable]sealed class Page{public string file;public int count;}
        public sealed class Clip
        {
            internal Sprite[] frames;
            public float Duration {get;internal set;}
            public int Count=>frames.Length;
            public Sprite At(double age,bool loop=false)
            {if(double.IsNaN(age)||double.IsInfinity(age)||age<0||!loop&&age>=Duration)return null;if(loop)age%=Duration;return frames[Mathf.Clamp((int)(age*60+1e-6),0,frames.Length-1)];}
        }
        static Manifest manifest;
        static readonly Dictionary<string,Clip> cache=new Dictionary<string,Clip>();
        public static Clip Load(string key)
        {
            if(cache.TryGetValue(key,out var found)&&found.frames[0]!=null&&found.frames[0].texture!=null)return found;
            if(manifest==null){var file=Resources.Load<TextAsset>("MudrockVfx/manifest");if(file==null)throw new InvalidDataException("Missing Mudrock VFX manifest");manifest=JsonUtility.FromJson<Manifest>(file.text);}
            if(manifest.version!=1||manifest.fps!=60)throw new InvalidDataException("Invalid Mudrock VFX format.");
            var d=Array.Find(manifest.clips,c=>c.key==key);if(d==null)return null;
            var frames=new List<Sprite>();
            foreach(var p in d.pages)
            {
                var tex=Resources.Load<Texture2D>("MudrockVfx/"+Path.GetFileNameWithoutExtension(p.file));
                if(tex==null||d.ppu<=0||d.width<=0||d.height<=0||p.count>d.columns*(tex.height/d.height))throw new InvalidDataException("Invalid Mudrock VFX page: "+p.file);
                for(int i=0;i<p.count;i++){var sprite=Sprite.Create(tex,new Rect(i%d.columns*d.width,tex.height-(i/d.columns+1)*d.height,d.width,d.height),new Vector2(d.pivotX,d.pivotY),d.ppu,0,SpriteMeshType.FullRect);sprite.name="Mudrock/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);}
            }
            if(frames.Count!=d.count)throw new InvalidDataException("Incomplete Mudrock VFX clip.");
            found=new Clip{frames=frames.ToArray(),Duration=d.duration};cache[key]=found;return found;
        }
    }
}
