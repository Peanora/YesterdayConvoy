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
        sealed class Clip{internal Data Data;internal Sprite[] Frames;}
        sealed class View{internal SpriteRenderer Renderer;internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();}
        static Manifest manifest;
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
            if(clips.TryGetValue(key,out var clip)&&clip.Frames[0]!=null)return clip;
            if(manifest==null)
            {
                var text=Resources.Load<TextAsset>("TalulahNative/manifest");
                if(text==null)throw new InvalidDataException("Missing Talulah native VFX manifest");
                manifest=JsonUtility.FromJson<Manifest>(text.text);
            }
            if(manifest.version!=1||manifest.fps!=30)throw new InvalidDataException("Unsupported Talulah native VFX format");
            var d=Array.Find(manifest.clips,c=>c.key==key);
            if(d==null||d.count<=0||d.ppu<=0||d.width<=0||d.height<=0||d.duration<=0||d.hdrRange<=0||d.columns<=0)
                throw new InvalidDataException("Invalid Talulah native clip: "+key);
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
            clip=new Clip{Data=d,Frames=frames.ToArray()};clips[key]=clip;return clip;
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
            Show("attack-A-caster",sourceTime,shot.Origin,camera,flip,false,8);
            if(shot.Applied&&!shot.Erased)
                Show("attack-A-hit",clock-shot.ImpactAt,shot.Destination+camera.transform.up*.65f,camera,flip,false,35);
        }
        public void PresentUnit(ManualBattle battle,DeployedUnitState unit,Camera camera,bool flip)
        {
            if(!unit.Active)return;
            var p=unit.Motion.Position+unit.VisualOffset;
            double age=battle.Clock-unit.SpawnTime;
            Show("ambient-A-foot",age,p,camera,flip,true,-2);
            if(unit.IsMoving)Show("movement-A-foot",age,p,camera,flip,true,-1);
            if(!battle.IsSkillActionPlaying(unit)||!battle.UnitCombat(unit).FireRain)return;
            double elapsed=battle.Clock-unit.FireRainStartedAt,total=battle.UnitCombat(unit).SkillInterval;
            double raise=Math.Min(.8,total*.2),lower=Math.Min(.6,total*.2),hold=1.1666667;
            double time=elapsed<raise?elapsed/raise*hold:elapsed<total-lower?hold:
                hold+(elapsed-(total-lower))/lower*(1.9666667-hold);
            Show("firecast-A-hand",time,p,camera,flip,false,6);
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
            views.Clear();used=0;
        }
        public void Dispose(){Clear();UnityEngine.Object.Destroy(material);}
    }
}
