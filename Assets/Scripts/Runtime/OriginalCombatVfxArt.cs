using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Metadata is shared. Texture/Sprite ownership stays in the existing bounded actor cache.
    internal static class OriginalCombatVfxArt
    {
        internal const string Encoding="sqrt-linear-rgb-effective-alpha";
        internal const string MonsterRoot="OriginalCombatVfx/Monsters",DefenderRoot="OriginalCombatVfx/Defenders";
        [Serializable] sealed class Manifest {public int version,fps;public string encoding;public Data[] clips;}
        [Serializable] sealed class Data
        {
            public string key;public int count,width,height,columns;
            public float ppu,pivotX,pivotY,duration,hdrRange,forwardDegrees,loopStart,loopEnd;
            public bool loop,directional,groundDepth,belowActors;public ArtSequence.Page[] pages;
        }
        internal sealed class Clip
        {
            internal readonly ArtSequence Sequence;
            internal readonly float HdrRange,ForwardDegrees;
            internal readonly bool Loop,Directional;
            internal readonly bool GroundDepth,BelowActors,ExactRgba;
            internal readonly float LoopStart,LoopEnd;
            internal string Key=>Sequence.Key;
            internal string CacheKey=>Sequence.CacheKey;
            internal float Duration=>Sequence.Duration;
            internal int Count=>Sequence.Count;
            internal Clip(ArtSequence sequence,float hdr,bool loop,bool directional,float forward,float loopStart=0,float loopEnd=0,bool groundDepth=false,bool belowActors=false)
            {
                Sequence=sequence;HdrRange=hdr;Loop=loop;Directional=directional;ForwardDegrees=forward;
                LoopStart=loopStart;LoopEnd=loopEnd>0?loopEnd:sequence.Duration;GroundDepth=groundDepth;BelowActors=belowActors;
                ExactRgba=Array.TrueForAll(sequence.Pages,p=>!string.IsNullOrEmpty(p.rawResource)&&
                    (p.rawResource.StartsWith("PramanixV5/",StringComparison.Ordinal)||p.rawResource.StartsWith("SixStarVfx/",StringComparison.Ordinal)));
            }
            internal Sprite At(double age,bool loop=false)
            {
                if(loop&&!Loop)throw new InvalidOperationException("A one-shot original effect cannot be looped: "+Key);
                if(double.IsNaN(age)||double.IsInfinity(age)||age<0||!loop&&age>=Duration)return null;
                if(loop&&age>=LoopEnd)age=LoopStart+(age-LoopEnd)%(LoopEnd-LoopStart);
                return Sequence.Frames[Mathf.Clamp((int)(age*Sequence.Fps+1e-5),0,Count-1)];
            }
        }
        static readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>(StringComparer.Ordinal);
        static readonly HashSet<string> loaded=new HashSet<string>(StringComparer.Ordinal);
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        static string RootFor(string key)
        {
            if(string.IsNullOrWhiteSpace(key)||key.Contains("..")||key.Contains("\\")||key.Contains("//"))throw new InvalidDataException("Invalid original effect key.");
            if(key.StartsWith("common/",StringComparison.Ordinal)||key.StartsWith("monster/",StringComparison.Ordinal))return MonsterRoot;
            if(key.StartsWith("defender/",StringComparison.Ordinal))return DefenderRoot;
            throw new InvalidDataException("Original effect key needs common/, monster/ or defender/: "+key);
        }
        // Pure parsing also allows tooling to validate metadata without decoding a single PNG.
        static Clip[] ReadPackage(string root,string json)
        {
            var data=JsonUtility.FromJson<Manifest>(json);
            if(data==null||data.version!=1||data.fps!=30||data.encoding!=Encoding||data.clips==null)throw new InvalidDataException("Unsupported original VFX manifest: "+root);
            var result=new List<Clip>();var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var d in data.clips)
            {
                if(d==null||RootFor(d.key)!=root||!keys.Add(d.key)||d.count<=0||d.count>600||d.width<=0||d.height<=0||
                    d.width%4!=0||d.height%4!=0||d.columns<=0||(long)d.columns*d.width>2048||!Finite(d.ppu)||d.ppu<=0||
                    !Finite(d.pivotX)||!Finite(d.pivotY)||!Finite(d.duration)||d.duration<=0||!Finite(d.hdrRange)||d.hdrRange<=0||
                    !Finite(d.forwardDegrees)||!Finite(d.loopStart)||!Finite(d.loopEnd)||d.loopStart<0||d.loopEnd<0||
                    d.loopStart>=(d.loopEnd>0?d.loopEnd:d.duration)||d.loopEnd>d.duration+.0001f||
                    (!d.loop&&(d.loopStart>0||d.loopEnd>0))||d.pages==null||d.pages.Length==0||Mathf.Abs(d.count-d.duration*data.fps)>1.001f)
                    throw new InvalidDataException("Invalid original VFX clip: "+d?.key);
                int total=0;var files=new HashSet<string>(StringComparer.Ordinal);string folder=null;
                var pages=new List<ArtSequence.Page>();
                foreach(var page in d.pages)
                {
                    if(page==null||string.IsNullOrWhiteSpace(page.file)||Path.IsPathRooted(page.file)||page.file.Contains("\\")||
                        Array.Exists(page.file.Split('/'),part=>part==".."||part=="."||part.Length==0)||
                        !page.file.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||!files.Add(page.file)||page.count<=0||
                        page.width!=(long)d.columns*d.width||page.width>2048||page.height<=0||page.height>2048||
                        page.height%d.height!=0||page.count>(long)d.columns*(page.height/d.height))
                        throw new InvalidDataException("Invalid original VFX atlas: "+d.key);
                    string directory=(Path.GetDirectoryName(page.file)??"").Replace('\\','/');
                    if(folder!=null&&folder!=directory)throw new InvalidDataException("A clip's pages need one source folder: "+d.key);
                    folder=directory;
                    pages.Add(new ArtSequence.Page{file=Path.GetFileName(page.file),rawResource=page.rawResource,count=page.count,width=page.width,height=page.height});
                    total+=page.count;
                }
                if(total!=d.count)throw new InvalidDataException("Incomplete original VFX clip: "+d.key);
                var sequence=new ArtSequence{Root=root+(string.IsNullOrEmpty(folder)?"":"/"+folder),Key=d.key,Name="OriginalVfx/"+d.key,Count=d.count,Width=d.width,Height=d.height,
                    Columns=d.columns,Ppu=d.ppu,PivotX=d.pivotX,PivotY=d.pivotY,Duration=d.duration,Fps=data.fps,Pages=pages.ToArray()};
                result.Add(new Clip(sequence,d.hdrRange,d.loop,d.directional,d.forwardDegrees,d.loopStart,d.loopEnd,d.groundDepth,d.belowActors));
            }
            return result.ToArray();
        }
        internal static bool TryGet(string key,out Clip clip)
        {
            if(clips.TryGetValue(key,out clip))return true;
            string root=RootFor(key);
            if(!loaded.Contains(root))
            {
                string file=ArtSourcePaths.Resolve(root+"/manifest.json");
                if(!File.Exists(file))return false;
                // Commit the metadata package only after all entries validate.
                var package=ReadPackage(root,File.ReadAllText(file));
                foreach(var entry in package)if(clips.ContainsKey(entry.Key))throw new InvalidDataException("Duplicate original VFX key: "+entry.Key);
                foreach(var entry in package)clips.Add(entry.Key,entry);
                loaded.Add(root);
            }
            return clips.TryGetValue(key,out clip);
        }
        internal static Clip Get(string key)
        {if(TryGet(key,out var clip))return clip;throw new InvalidDataException("Missing original VFX: "+key);}
        internal static IEnumerable<ArtSequence> SequencesFor(IEnumerable<string> keys)
        {
            if(keys==null)throw new ArgumentNullException(nameof(keys));var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(string key in keys){var sequence=Get(key).Sequence;if(seen.Add(sequence.CacheKey))yield return sequence;}
        }
    }
}
