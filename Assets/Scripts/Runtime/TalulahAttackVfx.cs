using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Native caster and target layers are separate. This renderer never creates combat events.
    internal sealed class TalulahAttackVfx:IDisposable
    {
        [Serializable]sealed class Manifest{public int version,fps;public Data[] clips;}
        [Serializable]sealed class Data
        {
            public string key,status;public int count,width,height,columns;public float ppu,pivotX,pivotY,duration,hdrRange;public Page[] pages;
        }
        [Serializable]sealed class Page{public string file;public int count;}
        sealed class Clip
        {
            internal Data Data;internal Sprite[] ResourceFrames;internal ArtSequence Sequence;
            internal Sprite[] Frames=>Sequence!=null?Sequence.Frames:ResourceFrames;
        }
        sealed class View{internal SpriteRenderer Renderer;internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();}
        static Manifest manifest,phaseManifest;
        static readonly string[] PhaseKeys={"attack-B-caster","attack-B-hit","ambient-B-foot","movement-B-foot","ambient-B-head","ambient-B-wing","first_transition_start","first_transition_end","final_transition_start","final_transition_end","ignite-B-caster"};
        readonly Dictionary<string,bool> secondForms=new Dictionary<string,bool>(),facings=new Dictionary<string,bool>();
        internal static IEnumerable<ArtSequence> SecondPhaseSequences
        {get{foreach(string key in PhaseKeys)yield return Load(key).Sequence;}}
        static readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        readonly List<View> views=new List<View>();
        readonly Transform parent;
        readonly Material material;
        int used;
        const float VerticalProjection=.9f,FootLift=.12f;
        public TalulahAttackVfx(Transform root)
        {
            parent=root;
            var shader=Resources.Load<Shader>("TalulahNative");
            if(shader==null)throw new InvalidOperationException("Missing Talulah native VFX shader");
            material=new Material(shader){name="Talulah original native effects"};
        }
        static Clip Load(string key)
        {
            if(clips.TryGetValue(key,out var clip)&&(clip.Sequence!=null||clip.ResourceFrames[0]!=null))return clip;
            if(manifest==null)
            {
                var text=Resources.Load<TextAsset>("TalulahNative/manifest");
                if(text==null)throw new InvalidDataException("Missing Talulah native VFX manifest");
                manifest=JsonUtility.FromJson<Manifest>(text.text);
            }
            if(manifest.version!=1||manifest.fps!=30)throw new InvalidDataException("Unsupported Talulah native VFX format");
            var d=Array.Find(manifest.clips,c=>c.key==key);
            bool external=d==null;
            if(external)
            {
                if(phaseManifest==null)phaseManifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"TalulahUnboundVfx/manifest.json")));
                if(phaseManifest==null||phaseManifest.version!=1||phaseManifest.fps!=30)throw new InvalidDataException("Invalid Talulah phase VFX manifest");
                d=Array.Find(phaseManifest.clips,c=>c.key==key);
            }
            if(d==null||d.count<=0||d.ppu<=0||d.width<=0||d.height<=0||d.duration<=0||d.hdrRange<=0||d.columns<=0)
                throw new InvalidDataException("Invalid Talulah native clip: "+key);
            if(external)
            {
                // Reuse the same bounded/prepared texture cache as body animation.
                // Atlas dimensions follow this validated source's complete-cell rows.
                var pages=Array.ConvertAll(d.pages,p=>new ArtSequence.Page{file=p.file,count=p.count,width=d.columns*d.width,height=Mathf.CeilToInt(p.count/(float)d.columns)*d.height});
                var sequence=new ArtSequence{Root="TalulahUnboundVfx",Key=d.key,Name="TalulahNative/"+d.key,
                    Count=d.count,Width=d.width,Height=d.height,Columns=d.columns,Ppu=d.ppu,PivotX=d.pivotX,PivotY=d.pivotY,
                    Duration=d.duration,Fps=30,Pages=pages};
                clip=new Clip{Data=d,Sequence=sequence};clips[key]=clip;return clip;
            }
            var frames=new List<Sprite>();
            foreach(var page in d.pages)
            {
                var texture=Resources.Load<Texture2D>("TalulahNative/"+Path.GetFileNameWithoutExtension(page.file));
                if(texture==null)throw new InvalidDataException("Missing Talulah native page: "+page.file);
                if(page.count>d.columns*(texture.height/d.height))throw new InvalidDataException("Invalid Talulah native page grid: "+page.file+" loaded="+texture.width+"x"+texture.height+" cell="+d.width+"x"+d.height+" columns="+d.columns+" frames="+page.count);
                for(int i=0;i<page.count;i++)
                {
                    var sprite=Sprite.Create(texture,new Rect(i%d.columns*d.width,texture.height-(i/d.columns+1)*d.height,d.width,d.height),new Vector2(d.pivotX,d.pivotY),d.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="TalulahNative/"+key+"/"+frames.Count;sprite.hideFlags=HideFlags.DontSave;frames.Add(sprite);
                }
            }
            if(frames.Count!=d.count)throw new InvalidDataException("Incomplete Talulah native clip: "+key);
            clip=new Clip{Data=d,ResourceFrames=frames.ToArray()};clips[key]=clip;return clip;
        }
        public void Begin()=>used=0;
        public void Present(CombatShot shot,double clock,Camera camera,float sourceRecoveryScale)
        {
            double age=clock-shot.StartedAt;if(camera==null||age<0)return;
            double travel=shot.ImpactAt-shot.StartedAt;
            // Match the actor's piecewise source time without moving the existing hit event.
            double sourceTime=age<=travel?age/Math.Max(.001,travel)*1.0333333:
                1.0333333+(clock-shot.ImpactAt)*sourceRecoveryScale;
            bool flip=Vector3.Dot(shot.Destination-shot.Origin,camera.transform.right)<0;
            bool second=secondForms.TryGetValue(shot.SourceId,out bool b)&&b;
            Show(second?"attack-B-caster":"attack-A-caster",sourceTime,shot.Origin,camera,flip,false,8);
            if(shot.Applied&&!shot.Erased)
                Show(second?"attack-B-hit":"attack-A-hit",clock-shot.ImpactAt,shot.Destination+camera.transform.up*.65f,camera,flip,false,35);
        }
        public void PresentUnit(ManualBattle battle,DeployedUnitState unit,Camera camera,bool flip)
        {
            if(!unit.Active||camera==null)return;
            bool second=battle.TalulahSecondForm(unit);secondForms[unit.EntityId]=second;
            bool previous=facings.TryGetValue(unit.EntityId,out bool prior)?prior:flip;
            flip=OriginalTalulahArt.VisualFlip(battle,unit,camera,previous);facings[unit.EntityId]=flip;
            var p=unit.Motion.Position+unit.VisualOffset;
            double phaseAge=battle.Clock-unit.TalulahPhaseStartedAt;
            switch(unit.TalulahPhase)
            {
                case TalulahPhase.FirstFall:
                    Show("first_transition_start",phaseAge,p,camera,flip,false,6);return;
                case TalulahPhase.FirstRain:return;
                case TalulahPhase.FinalFall:
                    Show("final_transition_start",phaseAge,p,camera,flip,false,6);return;
                case TalulahPhase.FinalRain:
                    Show("final_transition_start",battle.Clock-(unit.TalulahAutomaticRainStartedAt-TalulahTiming.FallDuration),p,camera,flip,false,6);return;
                case TalulahPhase.Second:
                    if(phaseAge<TalulahTiming.ReappearanceDuration){Show("first_transition_end",phaseAge,p,camera,flip,false,6);return;}
                    break;
            }
            double age=battle.Clock-unit.SpawnTime;
            Show(second?"ambient-B-foot":"ambient-A-foot",age,p,camera,flip,true,-2);
            if(unit.IsMoving)Show(second?"movement-B-foot":"movement-A-foot",age,p,camera,flip,true,-1);
            if(second)
            {
                Show("ambient-B-wing",age,p,camera,flip,true,-3);
                Show("ambient-B-head",age,p,camera,flip,true,7);
            }
            if(!battle.IsSkillActionPlaying(unit)||!battle.UnitCombat(unit).FireRain)return;
            double elapsed=battle.Clock-unit.FireRainStartedAt,total=battle.UnitCombat(unit).SkillInterval;
            double raise=Math.Min(.8,total*.2),lower=Math.Min(.6,total*.2),hold=1.1666667;
            double time=elapsed<raise?elapsed/raise*hold:elapsed<total-lower?hold:
                hold+(elapsed-(total-lower))/lower*(1.9666667-hold);
            Show(second?"ignite-B-caster":"firecast-A-hand",time,p,camera,flip,false,6);
        }
        // EngagementView only visits active units. The actor's original presenter
        // calls this final tail using PresentationClock after logical final death.
        public void PresentTerminal(DeployedUnitState unit,double clock,Camera camera,bool flip)
        {
            if(unit==null||camera==null||unit.TalulahPhase!=TalulahPhase.Dead)return;
            Show("final_transition_end",clock-unit.DeathTime,unit.Motion.Position+unit.VisualOffset,camera,flip,false,6);
        }
        void Show(string key,double age,Vector3 position,Camera camera,bool flip,bool loop,int order)
        {
            if(age<0||double.IsNaN(age)||double.IsInfinity(age))return;
            var clip=Load(key);if(!loop&&age>=clip.Data.duration)return;
            if(loop)age%=clip.Data.duration;
            int frame=Mathf.Clamp((int)(age*manifest.fps+1e-5),0,clip.Frames.Length-1);
            if(used==views.Count)
            {
                var renderer=new GameObject("Talulah native / "+key,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(parent,false);renderer.sharedMaterial=material;views.Add(new View{Renderer=renderer});
            }
            var view=views[used++];var r=view.Renderer;r.name="Talulah native / "+key;
            r.enabled=true;r.sprite=clip.Frames[frame];r.flipX=flip;r.color=Color.white;
            r.transform.SetPositionAndRotation(position+camera.transform.up*FootLift,camera.transform.rotation);
            r.transform.localScale=new Vector3(1,1/VerticalProjection,1);
            r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+order;
            view.Properties.SetFloat("_HdrRange",clip.Data.hdrRange);r.SetPropertyBlock(view.Properties);
        }
        public void End(){for(int i=used;i<views.Count;i++)views[i].Renderer.enabled=false;}
        public void Clear()
        {
            foreach(var view in views)if(view.Renderer!=null){view.Renderer.enabled=false;UnityEngine.Object.Destroy(view.Renderer.gameObject);}
            views.Clear();secondForms.Clear();facings.Clear();used=0;
        }
        public void Dispose(){Clear();UnityEngine.Object.Destroy(material);}
    }
}
