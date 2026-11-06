using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Profiling;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // Only decoded presentation resources live here. Metadata never holds a texture or Sprite reference.
    public static class ArtMemoryCache
    {
        internal sealed class Resource
        {
            public Sprite[] Frames;
            public readonly List<Object> Objects=new List<Object>();
            public string Owner;
            public long Bytes,Used;
        }
        static readonly Dictionary<string,Resource> entries=new Dictionary<string,Resource>();
        static readonly HashSet<string> pinned=new HashSet<string>();
        static long sequence;
        public static long BudgetBytes {get;set;}=1024L*1024*1024;
        public static long ResidentBytes {get;private set;}
        public static int EntryCount=>entries.Count;
        public static int TextureLoads {get;private set;}
        public static int PreparedLoads {get;private set;}
        public static int RuntimePngLoads {get;private set;}
        public static int Evictions {get;private set;}
        internal static bool InvalidateDestroyed(string key)
        {
            if(!entries.TryGetValue(key,out var value)||value.Frames[0]!=null&&value.Frames[0].texture!=null)return false;
            Remove(key,value);return true;
        }
        internal static bool TryGet(string key,out Resource value)
        {
            if(entries.TryGetValue(key,out value)&&value.Frames.Length>0&&value.Frames[0]!=null&&value.Frames[0].texture!=null)
            {value.Used=++sequence;return true;}
            if(value!=null)Remove(key,value);value=null;return false;
        }
        internal static void Put(string key,Resource value)
        {
            if(entries.TryGetValue(key,out var old))Remove(key,old);
            foreach(var o in value.Objects)if(o!=null)value.Bytes+=Profiler.GetRuntimeMemorySizeLong(o);
            value.Used=++sequence;entries[key]=value;ResidentBytes+=value.Bytes;
        }
        internal static void Dispose(Resource value)
        {foreach(var o in value.Objects)if(o!=null){if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}}
        static void Remove(string key,Resource value){entries.Remove(key);ResidentBytes-=value.Bytes;Dispose(value);}
        public static void Pin(IEnumerable<string> owners){pinned.Clear();foreach(string owner in owners)pinned.Add(owner);}
        // Called at transition boundaries, never while iterating a GUI or a live renderer list.
        public static void Trim(bool clearUnused=false)
        {
            var remove=new List<KeyValuePair<string,Resource>>();
            foreach(var pair in entries)
            {
                bool active=pinned.Contains(pair.Value.Owner);
                if(!active)foreach(string owner in pinned)if(pair.Value.Owner.StartsWith(owner+"/",StringComparison.Ordinal)){active=true;break;}
                if(!active)remove.Add(pair);
            }
            remove.Sort((a,b)=>a.Value.Used.CompareTo(b.Value.Used));
            foreach(var pair in remove)
            {if(!clearUnused&&ResidentBytes<=BudgetBytes)break;Remove(pair.Key,pair.Value);Evictions++;}
        }
        public static void Clear(){pinned.Clear();Trim(true);}
        internal static Texture2D Texture(string relative,Resource owner,bool readable=false)
        {
            string source=ArtSourcePaths.Resolve(relative);
            string prepared=Application.isEditor?Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/PreparedArt",relative+".ycart")):source+".ycart";
            Texture2D texture;
            if(File.Exists(prepared)&&!readable)
            {
                using(var file=File.OpenRead(prepared))using(var reader=new BinaryReader(file))
                {
                    if(reader.ReadInt32()!=0x59434131)throw new InvalidDataException("Invalid prepared art: "+relative);
                    int width=reader.ReadInt32(),height=reader.ReadInt32();var format=(TextureFormat)reader.ReadInt32();int length=reader.ReadInt32();
                    if(width<=0||height<=0||width>4096||height>4096||length<=0||length>67108864)throw new InvalidDataException("Invalid prepared art dimensions.");
                    byte[] bytes=new byte[length];using(var zip=new DeflateStream(file,CompressionMode.Decompress))
                    {int offset=0;while(offset<bytes.Length){int n=zip.Read(bytes,offset,bytes.Length-offset);if(n==0)throw new EndOfStreamException();offset+=n;}}
                    texture=new Texture2D(width,height,format,false);owner.Objects.Add(texture);texture.LoadRawTextureData(bytes);texture.Apply(false,true);PreparedLoads++;
                }
            }
            else
            {
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false);owner.Objects.Add(texture);
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(source),false))throw new InvalidDataException("Invalid art PNG: "+relative);
                texture.Compress(true);texture.Apply(false,!readable);RuntimePngLoads++;
            }
            TextureLoads++;texture.name=relative;texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;return texture;
        }
    }
}
