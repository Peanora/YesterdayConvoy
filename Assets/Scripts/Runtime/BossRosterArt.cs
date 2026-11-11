using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Original source names remain addressable. Metadata never pins decoded frames.
    internal sealed class BossRosterArt
    {
        [Serializable] sealed class Manifest
        {
            public int version;public string task,cardId,name,view,model,skin;
            public float ppu,height,referenceSpeed;public Alias[] aliases;public string[] runtimeSources;
            public ClipData[] clips;public SourceState[] zeroDurationStates;
        }
        [Serializable] sealed class Alias {public string key,source;}
        [Serializable] internal sealed class ClipData
        {
            public string key,source;public int count,cell,columns;public float duration,fps,pivotX,pivotY,pixelsPerUnit;
            public bool loop,transparentWait;public SourceEvent[] events;public SegmentData[] segments;
        }
        [Serializable] internal sealed class SegmentData {public int firstFrame,count;public ArtSequence.Page[] pages;}
        [Serializable] internal sealed class SourceEvent
        {
            public string name;public float time;public int @int;public float @float;public string @string;
        }
        [Serializable] internal sealed class SourceState
        {public string name;public float duration;public bool loop;public float[] bounds;public int verticesMeasured;public SourceEvent[] events;}

        static readonly Dictionary<string,BossRosterArt> cache=new Dictionary<string,BossRosterArt>(StringComparer.Ordinal);
        static readonly Dictionary<string,string> coreTasks=new Dictionary<string,string>(StringComparer.Ordinal)
        {{"CARD_M25","B02"},{"CARD_M32","B06"},{"CARD_TALULAH_TRIAL","B16"},{"CARD_M20","B17"},{"CARD_M14","B18"},{"CARD_M28","B19"},{"CARD_WISADEL_APRIL","B20"}};
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>(StringComparer.Ordinal);
        readonly Dictionary<string,string> aliases=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly List<ArtSequence> runtime=new List<ArtSequence>();
        readonly List<ArtSequence> all=new List<ArtSequence>();
        string root;float ppu;
        internal string Task {get;private set;}
        internal string CardId {get;private set;}
        internal string Model {get;private set;}
        internal string View {get;private set;}
        internal float Height {get;private set;}
        internal float ReferenceSpeed {get;private set;}
        internal bool HasAttack=>HasClip("attack");
        internal float AttackDuration=>HasAttack?this["attack"].Duration:0;
        internal float AttackContact=>HasAttack&&this["attack"].OnAttackTimes.Count>0?this["attack"].OnAttackTimes[0]:0;
        internal float DeathDuration=>HasClip("death")?this["death"].Duration:0;
        internal IReadOnlyList<SourceState> ZeroDurationStates {get;private set;}
        internal IEnumerable<ArtSequence> Sequences=>runtime;
        internal IEnumerable<ArtSequence> AllSequences=>all;
        internal IEnumerable<string> SourceNames=>clips.Keys;
        internal bool HasClip(string key)=>key!=null&&(clips.ContainsKey(key)||aliases.ContainsKey(key));
        internal bool HasZeroDurationState(string sourceName)
        {foreach(var state in ZeroDurationStates)if(state.name==sourceName)return true;return false;}
        internal Clip this[string key]=>clips[aliases.TryGetValue(key,out var source)?source:key];
        internal IEnumerable<ArtSequence> SequencesFor(IEnumerable<string> sourceNames)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(string name in sourceNames)
            {
                // An explicit native zero-duration wait has no texture dependencies.
                if(HasZeroDurationState(name))continue;
                foreach(var sequence in this[name].Sequences)
                    if(seen.Add(sequence.CacheKey))yield return sequence;
            }
        }
        // A cast can prepare only the required original clip/window before its simulation starts.
        // Returning metadata does not upload textures or pin unrelated source motions.
        internal IEnumerable<ArtSequence> SequencesFor(string sourceName,double time,double lookAhead,bool loop=false)
            =>HasZeroDurationState(sourceName)?Array.Empty<ArtSequence>():this[sourceName].SequencesFor(time,lookAhead,loop);
        internal Sprite Portrait
        {
            get
            {
                string key=root+"/portrait";
                if(ArtMemoryCache.TryGet(key,out var value))return value.Frames[0];
                value=new ArtMemoryCache.Resource{Owner="portraits"};
                try
                {
                    string file=root+"/card-preview.png";
                    string prepared=Application.isEditor?Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/PreparedArt",file+".ycart")):ArtSourcePaths.Resolve(file)+".ycart";
                    if(!File.Exists(prepared))file=root+"/portrait.png";
                    var texture=ArtMemoryCache.Texture(file,value);
                    var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,0),ppu,0,SpriteMeshType.FullRect);
                    sprite.name=Task+"/portrait";value.Objects.Add(sprite);value.Frames=new[]{sprite};
                    ArtMemoryCache.Put(key,value);return sprite;
                }
                catch{ArtMemoryCache.Dispose(value);throw;}
            }
        }
        internal sealed class Clip
        {
            internal string Source {get;private set;}
            internal int Count {get;private set;}
            internal float Duration {get;private set;}
            internal float Fps {get;private set;}
            internal float Ppu {get;private set;}
            internal Vector2 Pivot {get;private set;}
            internal bool Loop {get;private set;}
            internal bool TransparentWait {get;private set;}
            internal IReadOnlyList<SourceEvent> Events {get;private set;}
            internal IReadOnlyList<float> OnAttackTimes {get;private set;}
            readonly List<ArtSequence> sequences=new List<ArtSequence>();
            readonly List<int> firstFrames=new List<int>();
            internal IEnumerable<ArtSequence> Sequences=>sequences;
            internal IEnumerable<ArtSequence> SequencesFor(double time,double lookAhead,bool loop=false)
            {
                if(double.IsNaN(lookAhead)||lookAhead<0)throw new ArgumentOutOfRangeException(nameof(lookAhead));
                if(loop&&lookAhead>=Duration)
                {foreach(var sequence in sequences)yield return sequence;yield break;}
                if(double.IsNaN(time)||time<0)time=0;
                if(loop)time=double.IsInfinity(time)?0:time%Duration;
                int from=FrameIndex(time,loop);double end=time+lookAhead;
                bool wraps=loop&&end>=Duration;
                int through=wraps?Count-1:FrameIndex(end,false);
                var seen=new HashSet<ArtSequence>();
                for(int i=0;i<sequences.Count;i++)
                    if(firstFrames[i]<=through&&firstFrames[i]+sequences[i].Count>from&&seen.Add(sequences[i]))yield return sequences[i];
                if(wraps)
                {
                    through=FrameIndex(end-Duration,false);
                    for(int i=0;i<sequences.Count;i++)
                        if(firstFrames[i]<=through&&seen.Add(sequences[i]))yield return sequences[i];
                }
            }
            internal bool ReadyFor(double time,double lookAhead,bool loop=false)
            {foreach(var sequence in SequencesFor(time,lookAhead,loop))if(!sequence.Ready)return false;return true;}
            internal Sprite[] Frames
            {
                get
                {
                    if(sequences.Count==1)return sequences[0].Frames;
                    // Explicit complete-motion access only. Do not retain these Sprite references.
                    var result=new Sprite[Count];
                    for(int s=0;s<sequences.Count;s++)Array.Copy(sequences[s].Frames,0,result,firstFrames[s],sequences[s].Count);
                    return result;
                }
            }
            internal Sprite At(double time,bool loop,out int index)
            {
                index=FrameIndex(time,loop);
                for(int i=sequences.Count-1;i>=0;i--)
                    if(index>=firstFrames[i])return sequences[i].Frames[index-firstFrames[i]];
                throw new InvalidDataException("Original Boss frame is outside its source clip: "+Source);
            }
            internal int FrameIndex(double time,bool loop)
            {
                if(double.IsNaN(time)||time<0)time=0;
                if(loop)time=double.IsInfinity(time)?0:time%Duration;
                if(time>=Duration)return Count-1;
                // Original captures include their terminal sample; do not stretch Count over Duration.
                return Mathf.Clamp((int)Math.Floor(time*Fps+1e-5),0,Count-1);
            }
            static internal Clip Read(string root,ClipData data,float defaultPpu)
            {
                if(data==null||string.IsNullOrEmpty(data.source)||data.source!=data.key||data.count<=0||data.duration<=0||data.fps<=0||data.cell<=0||data.columns<=0||data.columns>16||data.segments==null||data.segments.Length==0)
                    throw new InvalidDataException("Invalid original Boss clip: "+root);
                float clipPpu=data.pixelsPerUnit>0?data.pixelsPerUnit:defaultPpu;
                if(clipPpu<=0||data.pivotX<0||data.pivotX>1||data.pivotY<0||data.pivotY>1)
                    throw new InvalidDataException("Invalid original Boss clip scale/pivot: "+root);
                var result=new Clip{Source=data.source,Count=data.count,Duration=data.duration,Fps=data.fps,Ppu=clipPpu,
                    Pivot=new Vector2(data.pivotX,data.pivotY),Loop=data.loop,TransparentWait=data.transparentWait,Events=data.events??Array.Empty<SourceEvent>()};
                var attacks=new List<float>();
                foreach(var e in result.Events)
                {
                    if(e==null||e.time<0||e.time>data.duration+.0001f)throw new InvalidDataException("Boss source event is outside its clip");
                    if(e.name=="OnAttack")attacks.Add(e.time);
                }
                result.OnAttackTimes=attacks;
                int first=0;
                for(int i=0;i<data.segments.Length;i++)
                {
                    var segment=data.segments[i];
                    if(segment==null||segment.firstFrame!=first||segment.count<=0||segment.count>600||segment.pages==null||segment.pages.Length==0)
                        throw new InvalidDataException("Invalid original Boss cache segment: "+root+"/"+data.source);
                    int count=0;
                    foreach(var page in segment.pages)
                    {
                        if(page==null||string.IsNullOrEmpty(page.file)||Path.GetFileName(page.file)!=page.file||page.count<=0||page.width!=data.columns*data.cell||page.height%data.cell!=0||page.width>2048||page.height>2048||page.count>data.columns*(page.height/data.cell))
                            throw new InvalidDataException("Invalid original Boss page: "+root+"/"+data.source);
                        string path=ArtSourcePaths.Resolve(root+"/"+page.file);
                        if(!File.Exists(path)&&!File.Exists(path+".ycart"))throw new FileNotFoundException("Original Boss page is missing",path);
                        count+=page.count;
                    }
                    if(count!=segment.count)throw new InvalidDataException("Original Boss segment frame count mismatch");
                    string key=data.source+(data.segments.Length>1?"~"+i.ToString("000"):"");
                    result.firstFrames.Add(first);
                    result.sequences.Add(new ArtSequence{Root=root,Key=key,Name=root+"/"+key,Count=segment.count,
                        Duration=data.segments.Length==1?data.duration:Mathf.Max(1/data.fps,Mathf.Min(segment.count/data.fps,data.duration-segment.firstFrame/data.fps)),
                        Fps=data.fps,Ppu=clipPpu,PivotX=data.pivotX,PivotY=data.pivotY,
                        Width=data.cell,Height=data.cell,Columns=data.columns,Pages=segment.pages});
                    first+=count;
                }
                if(first!=data.count)throw new InvalidDataException("Original Boss clip frame count mismatch");
                return result;
            }
        }
        internal static BossRosterArt For(CardDefinition card)=>card==null?null:For(card.CardId);
        internal static bool IsCoreCard(string cardId)=>cardId!=null&&coreTasks.ContainsKey(cardId);
        internal static BossRosterArt For(string cardIdOrTask,string view="main")
        {
            if(cardIdOrTask==null)return null;
            string task=coreTasks.TryGetValue(cardIdOrTask,out var core)?core:cardIdOrTask.StartsWith("CARD_",StringComparison.Ordinal)?cardIdOrTask.Substring(5):cardIdOrTask.StartsWith("AUX_",StringComparison.Ordinal)?cardIdOrTask.Substring(4):cardIdOrTask;
            bool auxiliary=task=="P14"||task=="P14H"||task=="P20"||task=="P26"||task=="P12I"||task=="P15C"||task=="P24D"||task=="P25M";
            if(!auxiliary&&(task.Length!=3||task[0]!='B'||!int.TryParse(task.Substring(1),out int n)||n<1||n>26))return null;
            if(task=="B20"&&view=="main")view="front";
            if(view!="main"&&view!="front"&&view!="back")throw new ArgumentException("Unsupported Boss source view",nameof(view));
            string root="BossRoster/"+task+"/"+view;
            if(cache.TryGetValue(root,out var prior))return prior;
            string manifest=ArtSourcePaths.Resolve(root+"/manifest.json");
            if(!File.Exists(manifest))return null;
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest));
            string expectedCard=auxiliary?"AUX_"+task:"CARD_"+task;
            foreach(var pair in coreTasks)if(pair.Value==task){expectedCard=pair.Key;break;}
            if(data==null||data.version!=1||data.task!=task||data.cardId!=expectedCard||data.view!=view||data.ppu<=0||data.height<=0||data.clips==null||data.runtimeSources==null)
                throw new InvalidDataException("Invalid original Boss identity/manifest: "+root);
            var art=new BossRosterArt{root=root,ppu=data.ppu,Task=task,CardId=data.cardId,Model=data.model,View=view,Height=data.height,
                ReferenceSpeed=data.referenceSpeed,ZeroDurationStates=data.zeroDurationStates??Array.Empty<SourceState>()};
            foreach(var item in data.clips)
            {
                var clip=Clip.Read(root,item,data.ppu);art.clips.Add(clip.Source,clip);
                art.all.AddRange(clip.Sequences);
            }
            foreach(var state in art.ZeroDurationStates)
                if(state==null||string.IsNullOrEmpty(state.name)||state.duration!=0||art.clips.ContainsKey(state.name))
                    throw new InvalidDataException("Invalid original Boss zero-duration state: "+root);
            foreach(var alias in data.aliases??Array.Empty<Alias>())
            {
                if(alias==null||string.IsNullOrEmpty(alias.key)||!art.clips.ContainsKey(alias.source))throw new InvalidDataException("Invalid original Boss alias: "+root);
                art.aliases.Add(alias.key,alias.source);
            }
            foreach(string source in data.runtimeSources)art.runtime.AddRange(art.clips[source].Sequences);
            cache.Add(root,art);return art;
        }
    }
}
