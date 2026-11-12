using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Independent package: existing OriginalCombatVfx/Monsters keys and ownership are untouched.
    internal static class BossRosterFxArt
    {
        internal const string Encoding="sqrt-linear-rgb-effective-alpha";
        [Serializable] sealed class Package {public int version,fps;public string task,encoding;public Data[] clips;public Preload[] preload;public Binding[] bindings;}
        [Serializable] sealed class Data
        {
            public string key;public int count,width,height,columns;
            public float ppu,pivotX,pivotY,duration,hdrRange,sourceStart,loopPeriod;
            public bool loop;public ArtSequence.Page[] pages;
        }
        [Serializable] sealed class Preload {public int phase;public string[] keys;}
        [Serializable] sealed class BindingPackage {public int version;public Binding[] bindings;}
        [Serializable] internal sealed class Binding
        {
            public string kind,skill,nativeSkill,key,attach,animation,direction;public int phase=-1,hitIndex=-1,region;
            public bool loop,finishOnEnd,projectileLife,allowUnapplied;public float offsetX,offsetY,offsetZ;
        }
        internal sealed class Clip
        {
            internal readonly ArtSequence Sequence;internal readonly float HdrRange,SourceStart,LoopPeriod;
            internal readonly bool Loop;
            internal string Key=>Sequence.Key;
            internal float Duration=>SourceStart+Sequence.Duration;
            internal Clip(ArtSequence sequence,float hdr,float sourceStart,bool loop,float loopPeriod=0)
            {Sequence=sequence;HdrRange=hdr;SourceStart=sourceStart;Loop=loop;LoopPeriod=loopPeriod;}
            internal Sprite At(double age,bool repeat=false)
            {
                if(repeat&&!Loop)throw new InvalidOperationException("A non-looping Boss FX source cannot be repeated: "+Key);
                if(double.IsNaN(age)||double.IsInfinity(age)||age<SourceStart)return null;
                age-=SourceStart;if(repeat)age%=LoopPeriod>0?LoopPeriod:Sequence.Duration;else if(age>=Sequence.Duration)return null;
                return Sequence.Frames[Mathf.Clamp((int)(age*Sequence.Fps+1e-5),0,Sequence.Count-1)];
            }
        }
        sealed class Loaded {internal Dictionary<string,Clip> Clips;internal Binding[] Bindings;internal Preload[] Preloads;}
        static readonly Dictionary<string,Loaded> packages=new Dictionary<string,Loaded>(StringComparer.Ordinal);
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        static bool TaskValid(string task)=>task!=null&&task.Length==3&&task[0]=='B'&&char.IsDigit(task[1])&&char.IsDigit(task[2]);
        internal static string TaskForCard(string card)
        {if(card=="CARD_WISADEL_APRIL")return "B20";if(card=="CARD_M14")return "B18";
            var task=card!=null&&card.StartsWith("CARD_",StringComparison.Ordinal)?card.Substring(5):null;return TaskValid(task)?task:null;}
        internal static string Key(string task,string raw)=>"boss/"+task+"/"+raw;
        internal static bool OwnsCard(string card)=>card=="CARD_WISADEL_APRIL"||card=="CARD_M14"||BossRosterCatalog.Contains(card);
        internal static string LogicalRoot(string task)
        {if(!TaskValid(task))throw new InvalidDataException("Invalid Boss task: "+task);return "BossRosterFx/"+task;}
        static Loaded Parse(string task,string json,string bindingsJson)
        {
            var root=LogicalRoot(task);var data=JsonUtility.FromJson<Package>(json);
            var bindings=string.IsNullOrEmpty(bindingsJson)?new BindingPackage{version=1,bindings=data?.bindings}:JsonUtility.FromJson<BindingPackage>(bindingsJson);
            if(data==null||data.version!=1||data.task!=task||data.fps!=30||data.encoding!=Encoding||data.clips==null||bindings==null||bindings.version!=1||bindings.bindings==null)
                throw new InvalidDataException("Unsupported Boss FX package: "+task);
            var clips=new Dictionary<string,Clip>(StringComparer.Ordinal);
            foreach(var d in data.clips)
            {
                if(d==null||string.IsNullOrEmpty(d.key)||d.key.Contains("/")||d.key.Contains("\\")||d.key.Contains("..")||clips.ContainsKey(d.key)||
                    d.count<=0||d.count>600||d.width<=0||d.height<=0||d.width%4!=0||d.height%4!=0||d.columns<=0||d.columns*d.width>2048||
                    !Finite(d.ppu)||d.ppu<=0||!Finite(d.pivotX)||!Finite(d.pivotY)||d.pivotX<0||d.pivotX>1||d.pivotY<0||d.pivotY>1||
                    !Finite(d.duration)||d.duration<=0||Mathf.Abs(d.duration*data.fps-d.count)>.01f||!Finite(d.hdrRange)||d.hdrRange<=0||
                    !Finite(d.sourceStart)||d.sourceStart<0||!Finite(d.loopPeriod)||d.loopPeriod<0||d.loopPeriod>d.duration||d.pages==null||d.pages.Length==0)throw new InvalidDataException("Invalid Boss FX clip: "+d?.key);
                int count=0;var files=new HashSet<string>(StringComparer.Ordinal);var pages=new List<ArtSequence.Page>();
                foreach(var page in d.pages)
                {
                    if(page==null||string.IsNullOrWhiteSpace(page.file)||(page.file.Contains("\\")||page.file.Contains("..")||(Path.GetDirectoryName(page.file)??"").Replace('\\','/')!=d.key)||!page.file.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||
                        !files.Add(page.file)||page.count<=0||page.width!=d.width*d.columns||page.width>2048||page.height<=0||page.height>2048||page.height%d.height!=0||
                        page.count>d.columns*(page.height/d.height))throw new InvalidDataException("Invalid Boss FX page: "+d.key);
                    count+=page.count;pages.Add(new ArtSequence.Page{file=Path.GetFileName(page.file),count=page.count,width=page.width,height=page.height});
                }
                if(count!=d.count)throw new InvalidDataException("Incomplete Boss FX frames: "+d.key);
                var sequence=new ArtSequence{Root=root+"/"+d.key,Key=Key(task,d.key),Name="BossRosterFx/"+task+"/"+d.key,Count=d.count,Width=d.width,Height=d.height,
                    Columns=d.columns,Ppu=d.ppu,PivotX=d.pivotX,PivotY=d.pivotY,Duration=d.duration,Fps=data.fps,Pages=pages.ToArray()};
                clips.Add(d.key,new Clip(sequence,d.hdrRange,d.sourceStart,d.loop,d.loopPeriod));
            }
            foreach(var binding in bindings.bindings)
                if(binding==null||string.IsNullOrEmpty(binding.kind)||!clips.ContainsKey(binding.key)||binding.loop&&!clips[binding.key].Loop||
                    Array.IndexOf(new[]{"owner","target","projectile","ground"},binding.attach)<0)throw new InvalidDataException("Invalid Boss FX event binding: "+task);
            foreach(var preload in data.preload??Array.Empty<Preload>())
                if(preload==null||preload.phase<0||preload.keys==null||Array.Exists(preload.keys,k=>!clips.ContainsKey(k)))throw new InvalidDataException("Invalid Boss FX preload: "+task);
            return new Loaded{Clips=clips,Bindings=bindings.bindings,Preloads=data.preload??Array.Empty<Preload>()};
        }
        static Loaded Load(string task)
        {
            if(packages.TryGetValue(task,out var value))return value;
            string root=LogicalRoot(task),file=ArtSourcePaths.Resolve(root+"/manifest.json");
            if(!File.Exists(file))return null;
            string bindingFile=ArtSourcePaths.Resolve(root+"/bindings.json");
            value=Parse(task,File.ReadAllText(file),File.Exists(bindingFile)?File.ReadAllText(bindingFile):null);packages.Add(task,value);return value;
        }
        internal static bool TryGet(string key,out Clip clip)
        {
            clip=null;if(key==null)return false;var parts=key.Split('/');
            if(parts.Length!=3||parts[0]!="boss"||!TaskValid(parts[1]))throw new InvalidDataException("Invalid Boss FX key: "+key);
            var loaded=Load(parts[1]);return loaded!=null&&loaded.Clips.TryGetValue(parts[2],out clip);
        }
        internal static Clip Get(string key)
        {if(TryGet(key,out var clip))return clip;throw new InvalidDataException("Missing Boss FX: "+key);}
        internal static IEnumerable<Binding> Bindings(string task)=>Load(task)?.Bindings??Array.Empty<Binding>();
        internal static IEnumerable<ArtSequence> SequencesFor(IEnumerable<string> cardIds,int phase=0)
        {
            if(cardIds==null)throw new ArgumentNullException(nameof(cardIds));var seen=new HashSet<string>(StringComparer.Ordinal);
            var cards=new List<string>(cardIds);
            foreach(var sequence in BossRosterDeviceView.SequencesFor(cards))if(seen.Add(sequence.CacheKey))yield return sequence;
            foreach(var card in cards)
            {
                string task=TaskForCard(card);if(task==null||!OwnsCard(card)&&card!="CARD_M14")continue;var loaded=Load(task);if(loaded==null)continue;
                foreach(var row in loaded.Preloads)if(row.phase==phase)
                    foreach(var key in row.keys){var sequence=loaded.Clips[key].Sequence;if(seen.Add(sequence.CacheKey))yield return sequence;}
            }
        }
    }
}
