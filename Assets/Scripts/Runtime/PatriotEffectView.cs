using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Read-only V2 presentation. All damage, targeting and phase changes remain in ManualBattle.</summary>
    public sealed class PatriotEffectView:MonoBehaviour
    {
        [Serializable] sealed class Catalog {public int fps;public float worldSize,anchorX,anchorY;public Clip[] clips;}
        [Serializable] sealed class Clip {public string name;public int frames;public string[] pages;public float worldSize,anchorX,anchorY;}
        sealed class Page {public Texture2D Texture;public Sprite Sprite;public int LastUse;}
        sealed class View {public SpriteRenderer Renderer;public readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();}
        Func<ManualBattle> battle;Func<double> presentationClock;
        ManualBattle previous;long runId;Material material;Catalog catalog;int used;
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        readonly Dictionary<string,Page> pages=new Dictionary<string,Page>();
        readonly List<View> views=new List<View>();readonly List<string> stale=new List<string>();
        static readonly int Frame=Shader.PropertyToID("_Frame");
        public int VisibleEffects {get;private set;}
        public int LoadedPages=>pages.Count;
        public void Bind(Func<ManualBattle> getter,Func<double> clock=null){battle=getter;presentationClock=clock;}
        void Load()
        {
            if(material!=null)return;
            var data=Resources.Load<TextAsset>("PatriotVfx/catalog");var shader=Resources.Load<Shader>("PatriotVfx/PatriotPortable");
            if(data==null||shader==null)throw new InvalidOperationException("Patriot runtime effect catalog/shader missing.");
            catalog=JsonUtility.FromJson<Catalog>(data.text);foreach(var c in catalog.clips)clips.Add(c.name,c);
            material=new Material(shader){name="Patriot original portable effects"};
        }
        void LateUpdate()
        {
            var b=battle?.Invoke();used=0;var cam=Camera.main;
            if(b!=previous||b!=null&&runId!=b.RunId){Clear();previous=b;runId=b?.RunId??0;}
            if(b!=null&&cam!=null)foreach(var u in b.Units)
            {
                if(!b.IsPatriot(u))continue;
                double clock=presentationClock?.Invoke()??b.Clock;
                if(!u.Active)
                {
                    if(u.Resolution==UnitResolution.Dead)Show("die_01_start",clock-u.DeathTime,u.Motion.Position,.85f,cam);
                    continue;
                }
                double phaseAge=b.Clock-u.PatriotPhaseStartedAt;
                if(u.PatriotPhase==PatriotPhase.Reborn)
                {
                    Show("buff_02_start",phaseAge,u.Motion.Position,.85f,cam);
                    Show("buff_02_start_02",phaseAge-.2,u.Motion.Position,.85f,cam);
                    double rise=phaseAge-(60-PatriotTiming.RiseDuration);
                    ShowRise(rise,u.Motion.Position,cam);
                }
                else if(u.PatriotPhase==PatriotPhase.Destruction)
                    ShowRise(phaseAge+PatriotTiming.RiseDuration,u.Motion.Position,cam);
                if(u.PatriotPhase!=PatriotPhase.March)
                {
                    Show("buff_03_start",b.Clock-u.LastImmolationAt,u.Motion.Position,.9f,cam);
                    Show("buff_03_range",phaseAge%3.2,u.Motion.Position,.9f,cam);
                }
                if(u.PatriotPhase==PatriotPhase.Reborn||u.PatriotDisabledUntil>b.Clock)continue;
                var spear=u.Spear;
                if(spear!=null&&b.Clock-spear.StartedAt<6&&!spear.Shot.Erased&&(!spear.Shot.Resolved||spear.Shot.Applied))
                {
                    double t=b.Clock-spear.StartedAt;var target=b.FindDefender(spear.TargetId);
                    Vector3 destination=target?.Position??spear.Shot.Destination;bool mirror=Mirror(spear.Shot.Origin,destination,cam);
                    Show("skill_01_start",t,spear.Shot.Origin+cam.transform.up*1.55f,.65f,cam,mirror,spear.Shot.Origin);
                    if(b.Clock<spear.Shot.ImpactAt)Show("skill_01_hit",t,destination,.9f,cam,ground:destination,order:50);
                    Show("skill_01_start_02",b.Clock-spear.Shot.StartedAt,spear.Shot.Origin+cam.transform.up*2,.75f,cam,mirror,spear.Shot.Origin);
                    Show("skill_01_hit_02",b.Clock-spear.Shot.StartedAt,destination+cam.transform.up*.7f,.75f,cam,ground:destination,order:50);
                    if(spear.Shot.Applied)Show("skill_01_end",b.Clock-spear.Shot.ImpactAt,destination,.8f,cam);
                }
                double attackAge=(b.Clock-u.LastAttackTime)*u.LastAttackRate;
                if(attackAge>=0&&attackAge<3.6&&!b.PatriotCasting(u))
                {
                    var target=b.FindDefender(u.CombatTargetId);bool mirror=target!=null&&Mirror(u.Motion.Position,target.Position,cam);
                    bool second=u.PatriotPhase==PatriotPhase.Destruction;
                    Show(second?"attack_02_start":"attack_01_start",attackAge,u.Motion.Position+cam.transform.up*(second?1.1f:.92f),.65f,cam,mirror,u.Motion.Position);
                }
                foreach(var shot in b.CombatShots)
                {
                    if(shot.FromDefender||shot.SourceId!=u.EntityId||shot.Empowered||shot.Erased||shot.Resolved&&!shot.Applied)continue;
                    var target=b.FindDefender(shot.TargetId);Vector3 position=(target?.Position??shot.Destination)+cam.transform.up*.7f;
                    bool second=u.PatriotPhase==PatriotPhase.Destruction;
                    if(second)Show("attack_02_start_02",(b.Clock-shot.StartedAt)*u.LastAttackRate,position,.65f,cam,Mirror(shot.Origin,shot.Destination,cam),position-cam.transform.up*.7f,50);
                    if(shot.Applied)Show(second?"attack_02_hit":"attack_01_hit",(b.Clock-shot.ImpactAt)*u.LastAttackRate,position,second?.7f:.6f,cam,ground:position-cam.transform.up*.7f,order:50);
                }
            }
            for(int i=used;i<views.Count;i++)views[i].Renderer.enabled=false;
            VisibleEffects=used;ReleaseUnusedPages();
        }
        void ShowRise(double t,Vector3 p,Camera cam)
        {
            Show("buff_04_start",t,p,.85f,cam);Show("buff_04_start_02",t,p,.85f,cam);
            Show("buff_04_start_03",t,p,.85f,cam);Show("buff_04_start_04",t,p,.85f,cam);
        }
        static bool Mirror(Vector3 origin,Vector3 destination,Camera cam)=>Vector3.Dot(destination-origin,cam.transform.right)<0;
        void Show(string suffix,double age,Vector3 position,float scale,Camera cam,bool mirror=false,Vector3? ground=null,int order=8)
        {
            if(age<0||age>=6)return;Load();var clip=clips["patrt_"+suffix];int frame=(int)(age*catalog.fps);if(frame>=clip.frames)return;
            string name=clip.pages[frame/16];
            if(!pages.TryGetValue(name,out var page))
            {
                var texture=Resources.Load<Texture2D>("PatriotVfx/"+name);
                if(texture==null)throw new InvalidOperationException("Missing Patriot atlas "+name);
                var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(clip.anchorX,clip.anchorY),texture.width,0,SpriteMeshType.FullRect);
                pages.Add(name,page=new Page{Texture=texture,Sprite=sprite});
            }
            page.LastUse=Time.frameCount;
            if(used==views.Count)
            {
                var renderer=new GameObject("Patriot effect / "+suffix,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();renderer.transform.SetParent(transform,false);renderer.sharedMaterial=material;
                views.Add(new View{Renderer=renderer});
            }
            var view=views[used++];var r=view.Renderer;r.name="Patriot effect / "+suffix;r.enabled=true;r.sprite=page.Sprite;
            // Flattening the verified native render preserves its projection. Moving along the view ray
            // prevents a ground plane from cutting the lower half of the original ground particles.
            float lift=Mathf.Max(4,clip.worldSize*clip.anchorY*Mathf.Max(0,cam.transform.up.y)/Mathf.Max(.1f,-cam.transform.forward.y)+.1f);
            r.transform.SetPositionAndRotation(position-cam.transform.forward*(lift*scale),cam.transform.rotation);
            r.transform.localScale=new Vector3((mirror?-1:1)*clip.worldSize*scale,clip.worldSize*scale,1);
            r.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,ground??position)+order;
            view.Properties.SetFloat(Frame,frame%16);r.SetPropertyBlock(view.Properties);
        }
        void ReleaseUnusedPages()
        {
            stale.Clear();foreach(var p in pages)if(Time.frameCount-p.Value.LastUse>6)stale.Add(p.Key);
            foreach(string key in stale){var p=pages[key];foreach(var v in views)if(v.Renderer.sprite==p.Sprite)v.Renderer.sprite=null;Destroy(p.Sprite);Resources.UnloadAsset(p.Texture);pages.Remove(key);}
        }
        void Clear()
        {
            foreach(var v in views)if(v.Renderer!=null){v.Renderer.enabled=false;v.Renderer.sprite=null;}
            foreach(var p in pages.Values){Destroy(p.Sprite);Resources.UnloadAsset(p.Texture);}pages.Clear();VisibleEffects=0;
        }
        void OnDestroy(){Clear();foreach(var v in views)if(v.Renderer!=null)Destroy(v.Renderer.gameObject);if(material!=null)Destroy(material);}
    }
}
