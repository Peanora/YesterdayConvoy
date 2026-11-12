using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace YesterdayConvoy
{
    // A clip describes frames without decoding them. Cache eviction never invalidates this description.
    internal sealed class ArtSequence
    {
        [Serializable] internal sealed class Page {public string file,rawResource;public int width,height,count;}
        public string Root,Key,Name;
        public float Ppu,PivotX,PivotY,Duration,Fps;
        public int Width,Height,Columns,Count;
        public Page[] Pages;
        string cacheKey;
        public string CacheKey=>cacheKey??(cacheKey=Root+"/"+Key);
        public bool Ready=>ArtMemoryCache.TryGet(CacheKey,out _);
        public Sprite[] Frames
        {
            get
            {
                if(ArtMemoryCache.TryGet(CacheKey,out var value))return value.Frames;
                var routine=Warm();try{while(routine.MoveNext()){}}finally{(routine as IDisposable)?.Dispose();}
                if(!ArtMemoryCache.TryGet(CacheKey,out value))throw new InvalidDataException("Clip not prepared: "+CacheKey);
                return value.Frames;
            }
        }
        public IEnumerator Warm()
        {
            if(Ready)yield break;
            if(Count<=0||Count>600||Width<=0||Height<=0||Columns<=0||Ppu<=0||Duration<=0||Pages==null)throw new InvalidDataException("Invalid clip metadata: "+CacheKey);
            var value=new ArtMemoryCache.Resource{Owner=CacheKey,Frames=new Sprite[Count]};int offset=0;bool committed=false;
            try
            {
                foreach(var page in Pages)
                {
                    if(string.IsNullOrEmpty(page.file)||Path.GetFileName(page.file)!=page.file||page.count<=0||offset+page.count>Count)throw new InvalidDataException("Invalid clip page: "+CacheKey);
                    var texture=string.IsNullOrEmpty(page.rawResource)?ArtMemoryCache.Texture(Path.Combine(Root,page.file),value):RawTexture(page,value);
                    if(texture.width!=page.width||texture.height!=page.height||page.width!=Columns*Width||page.height%Height!=0||page.count>Columns*(page.height/Height))throw new InvalidDataException("Atlas layout mismatch: "+CacheKey);
                    for(int i=0;i<page.count;i++)
                    {
                        var frame=Sprite.Create(texture,new Rect(i%Columns*Width,page.height-(i/Columns+1)*Height,Width,Height),new Vector2(PivotX,PivotY),Ppu,0,SpriteMeshType.FullRect);
                        frame.name=Name+"/"+(offset).ToString("000");value.Objects.Add(frame);value.Frames[offset++]=frame;
                    }
                    yield return null; // One atlas upload per rendered loading frame.
                }
                if(offset!=Count)throw new InvalidDataException("Frame count mismatch: "+CacheKey);
                if(!Ready){ArtMemoryCache.Put(CacheKey,value);committed=true;}
            }
            finally{if(!committed)ArtMemoryCache.Dispose(value);}
        }
        // A few approved HDR captures require exact RGB at alpha zero, which block compression
        // cannot guarantee. Their source RGBA rows are already bottom-to-top for Unity uploads.
        static Texture2D RawTexture(Page page,ArtMemoryCache.Resource owner)
        {
            if(!page.rawResource.StartsWith("PramanixV5/",StringComparison.Ordinal)||page.rawResource.Contains("..")||page.rawResource.Contains("\\"))
                throw new InvalidDataException("Invalid exact native page: "+page.rawResource);
            var asset=Resources.Load<TextAsset>(page.rawResource);
            if(asset==null)throw new FileNotFoundException("Missing exact native RGBA resource",page.rawResource);
            int size=checked(page.width*page.height*4);
            if(size<=0||size>67108864)throw new InvalidDataException("Invalid exact native dimensions.");
            var bytes=new byte[size];
            try
            {
                using(var input=new MemoryStream(asset.bytes,false))using(var zip=new GZipStream(input,CompressionMode.Decompress))
                {
                    int offset=0;while(offset<size){int count=zip.Read(bytes,offset,size-offset);if(count==0)throw new EndOfStreamException(page.rawResource);offset+=count;}
                    if(zip.ReadByte()!=-1)throw new InvalidDataException("Extra exact native RGBA bytes: "+page.rawResource);
                }
            }
            finally{Resources.UnloadAsset(asset);}
            var texture=new Texture2D(page.width,page.height,TextureFormat.RGBA32,false,true){name=page.rawResource,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            owner.Objects.Add(texture);texture.LoadRawTextureData(bytes);texture.Apply(false,true);return texture;
        }
    }
}
