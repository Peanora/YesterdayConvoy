using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    public static class FrostSealArt
    {
        [Serializable] sealed class Manifest {public int version,fps;public Data[] clips;}
        [Serializable] sealed class Data {public string key;public float duration,ppu,pivotX,pivotY;public int cell,count,columns;public Page[] pages;}
        [Serializable] sealed class Page {public string file;public int count;}
        public sealed class Clip
        {
            public Sprite[] Frames {get;internal set;}
            public float Duration {get;internal set;}
            public Sprite At(double age)=>age<0||age>=Duration||double.IsNaN(age)?null:Frames[Mathf.Clamp((int)((age+1e-7)*60),0,Frames.Length-1)];
        }
        static readonly Dictionary<string,Clip> cache=new Dictionary<string,Clip>();
        public static Clip Load(string key)
        {
            if(cache.TryGetValue(key,out var found)&&found.Frames[0]!=null&&found.Frames[0].texture!=null)return found;
            var manifest=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("FrostSeal/manifest").text);
            var data=Array.Find(manifest.clips,c=>c.key==key);
            if(data==null||manifest.version!=1||manifest.fps!=60||data.count<=0||data.ppu<=0)throw new InvalidDataException("Invalid frost VFX manifest.");
            var frames=new List<Sprite>();
            foreach(var page in data.pages)
            {
                var texture=Resources.Load<Texture2D>("FrostSeal/"+Path.GetFileNameWithoutExtension(page.file));
                if(texture==null||texture.width!=data.columns*data.cell||texture.height%data.cell!=0||page.count>data.columns*(texture.height/data.cell))throw new InvalidDataException("Invalid frost VFX atlas.");
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(texture,new Rect(i%data.columns*data.cell,texture.height-(i/data.columns+1)*data.cell,data.cell,data.cell),new Vector2(data.pivotX,data.pivotY),data.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="FrostSeal/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=data.count)throw new InvalidDataException("Incomplete frost VFX.");
            found=new Clip{Frames=frames.ToArray(),Duration=data.duration};cache[key]=found;return found;
        }
    }
}
