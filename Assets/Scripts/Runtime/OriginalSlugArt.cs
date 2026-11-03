using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved local slug flipbooks; the game never ships a Spine runtime.</summary>
    internal sealed class OriginalSlugArt
    {
        internal const string CardId="CARD_ORIGINIUM_SLUG";
        private static bool attempted;
        private static OriginalSlugArt cached;
        private readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        internal Sprite Portrait { get; private set; }
        internal float ReferenceSpeed { get; private set; }
        internal Clip this[string key] => clips[key];
        [Serializable] private sealed class Manifest
        { public int version; public string cardId; public float ppu,referenceSpeed,attackContact; public ClipData[] clips; }
        [Serializable] private sealed class ClipData
        { public string key; public float duration,fps,pivotX,pivotY; public int count,cellWidth,cellHeight,columns; public Page[] pages; }
        [Serializable] private sealed class Page { public string file; public int width,height,count; }
        internal sealed class Clip
        {
            internal Sprite[] Frames;
            internal float Duration,Fps;
            internal Sprite At(double time,bool loop,out int index)
            {
                double t=Math.Max(0,time);
                if(loop) t%=Duration;
                index=Mathf.Clamp((int)(t*Fps+1e-5),0,Frames.Length-1);
                return Frames[index];
            }
        }
        internal static OriginalSlugArt For(string cardId)
        {
            if(cardId!=CardId) return null;
            if(attempted) return cached;
            attempted=true;
            string root=Path.Combine(Application.streamingAssetsPath,"OriginiumSlugOriginal");
            string manifest=Path.Combine(root,"manifest.json");
            if(!File.Exists(manifest))
            { Debug.LogWarning("SLUG_ORIGINAL_UNAVAILABLE: install an authorized local package; slug remains pending in the menu."); return null; }
            var allocated=new List<UnityEngine.Object>();
            try
            {
                var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest));
                if(data==null || data.version!=1 || data.cardId!=CardId || !Positive(data.ppu) || !Positive(data.referenceSpeed) || data.clips?.Length!=6)
                    throw new InvalidDataException("Invalid slug manifest.");
                var art=new OriginalSlugArt { ReferenceSpeed=data.referenceSpeed };
                var required=new HashSet<string>(new[]{"idle","walk","start","stop","attack","death"});
                foreach(var item in data.clips)
                {
                    if(!required.Remove(item.key) || !Positive(item.duration) || !Positive(item.fps) || item.count<=0 || item.count>600 ||
                        item.cellWidth<=0 || item.cellHeight<=0 || item.columns<=0 || item.pages?.Length!=1 ||
                        !Finite(item.pivotX) || !Finite(item.pivotY) || item.pivotX<0 || item.pivotX>1 || item.pivotY<0 || item.pivotY>1)
                        throw new InvalidDataException("Invalid slug clip.");
                    var page=item.pages[0];var texture=Load(root,page.file,allocated);
                    if(texture.width!=page.width || texture.height!=page.height || page.width>2048 || page.height>2048 ||
                        page.count!=item.count || (long)item.columns*item.cellWidth!=page.width ||
                        page.height%item.cellHeight!=0 || page.count>(long)item.columns*(page.height/item.cellHeight))
                        throw new InvalidDataException("Slug atlas layout mismatch.");
                    var frames=new Sprite[item.count];
                    for(int i=0;i<frames.Length;i++)
                    {
                        var rect=new Rect(i%item.columns*item.cellWidth,page.height-(i/item.columns+1)*item.cellHeight,item.cellWidth,item.cellHeight);
                        frames[i]=Sprite.Create(texture,rect,new Vector2(item.pivotX,item.pivotY),data.ppu,0,SpriteMeshType.FullRect);
                        frames[i].name="OriginiumSlug/"+item.key+"/"+i.ToString("000");allocated.Add(frames[i]);
                    }
                    art.clips.Add(item.key,new Clip{Frames=frames,Duration=item.duration,Fps=item.fps});
                }
                var portrait=Load(root,"portrait.png",allocated);
                art.Portrait=Sprite.Create(portrait,new Rect(0,0,portrait.width,portrait.height),new Vector2(.5f,0),data.ppu);
                allocated.Add(art.Portrait);cached=art;
                Debug.Log("SLUG_ORIGINAL_LOADED: approved idle/start/walk/stop/attack/death, 380 frames; no active skill.");
                return cached;
            }
            catch(Exception e)
            {
                foreach(var value in allocated) if(value!=null)
                { if(Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
                Debug.LogWarning("SLUG_ORIGINAL_INVALID: "+e.Message+"; slug test is unavailable.");return null;
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value>0;
        private static Texture2D Load(string root,string file,List<UnityEngine.Object> allocated)
        {
            if(string.IsNullOrEmpty(file) || Path.GetFileName(file)!=file || !file.EndsWith(".png",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Local PNG filename required.");
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);allocated.Add(texture);
            if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(Path.Combine(root,file)),false)) throw new InvalidDataException("PNG decode failed.");
            texture.name="OriginiumSlug/"+file;texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            texture.Compress(true);texture.Apply(false,true);return texture;
        }
    }
}
