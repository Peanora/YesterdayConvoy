using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace YesterdayConvoy
{
    public static class MephistoVfxArt
    {
        [Serializable]sealed class Manifest{public int version,fps;public Data[] clips;}
        [Serializable]sealed class Data{public string key;public int width,height,columns,count;public float duration,ppu,pivotX,pivotY;public Page[] pages;}
        [Serializable]sealed class Page{public string file;public int count;}
        [Serializable]sealed class Mounts{public MountClip[] clips;}
        [Serializable]sealed class MountClip{public string name;public MountFrame[] frames;}
        [Serializable]sealed class MountFrame{public Point hand,special0;}
        [Serializable]sealed class Point{public float x,y;}
        public sealed class Clip
        {
            internal Sprite[] frames;
            public float Duration{get;internal set;}
            public int Count=>frames.Length;
            public Sprite At(double age,bool loop=false)
            {
                if(double.IsNaN(age)||double.IsInfinity(age)||age<0||!loop&&age>=Duration)return null;
                if(loop)age%=Duration;return frames[Mathf.Clamp((int)(age*30+1e-6),0,Count-1)];
            }
        }
        static Manifest manifest;static Mounts mounts;
        static readonly Dictionary<string,Clip> cache=new Dictionary<string,Clip>();
        public static Clip Load(string key)
        {
            if(cache.TryGetValue(key,out var clip)&&clip.frames[0]!=null&&clip.frames[0].texture!=null)return clip;
            if(manifest==null){var text=Resources.Load<TextAsset>("MephistoVfx/manifest");if(text==null)throw new InvalidDataException("Missing Mephisto VFX manifest");manifest=JsonUtility.FromJson<Manifest>(text.text);}
            if(manifest.version!=1||manifest.fps!=30||manifest.clips==null)throw new InvalidDataException("Invalid Mephisto VFX manifest");
            var data=Array.Find(manifest.clips,c=>c.key==key);if(data==null)throw new InvalidDataException("Missing Mephisto effect: "+key);
            var frames=new List<Sprite>();
            foreach(var page in data.pages)
            {
                if(Path.GetFileName(page.file)!=page.file)throw new InvalidDataException("Invalid Mephisto page path");
                var tex=Resources.Load<Texture2D>("MephistoVfx/"+Path.GetFileNameWithoutExtension(page.file));
                if(tex==null||data.ppu<=0||data.width<=0||data.height<=0||data.columns<=0||tex.width!=data.columns*data.width||tex.height%data.height!=0||page.count>data.columns*(tex.height/data.height))throw new InvalidDataException("Invalid Mephisto page: "+page.file);
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(tex,new Rect(i%data.columns*data.width,tex.height-(i/data.columns+1)*data.height,data.width,data.height),new Vector2(data.pivotX,data.pivotY),data.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="Mephisto/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=data.count||frames.Count==0||data.duration<=0)throw new InvalidDataException("Incomplete Mephisto effect: "+key);
            clip=new Clip{frames=frames.ToArray(),Duration=data.duration};cache[key]=clip;return clip;
        }
        public static Vector2 Hand(string action,int frame)
        {
            if(mounts==null){var text=Resources.Load<TextAsset>("MephistoVfx/mounts");if(text==null)throw new InvalidDataException("Missing Mephisto mounts");mounts=JsonUtility.FromJson<Mounts>(text.text);}
            var clip=Array.Find(mounts.clips,c=>c.name==action);if(clip==null)throw new InvalidDataException("Missing Mephisto pose mount: "+action);
            var point=clip.frames[Mathf.Clamp(frame,0,clip.frames.Length-1)].hand;return new Vector2(point.x,point.y);
        }
    }
}
