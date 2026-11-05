using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class MudrockVfxView:MonoBehaviour
    {
        Func<ManualBattle> source;
        readonly List<SpriteRenderer> pool=new List<SpriteRenderer>();
        readonly List<LineRenderer> beams=new List<LineRenderer>();
        readonly Dictionary<string,SpriteRenderer> devices=new Dictionary<string,SpriteRenderer>();
        Material effectMaterial,lineMaterial;int used,beamUsed;ManualBattle observed;int run=-1;
        public int VisibleEffects=>used;
        public void Bind(Func<ManualBattle> read)=>source=read;
        void Draw(string key,double age,Vector3 position,Camera camera,int order,bool flip=false,bool loop=false)
        {
            var clip=MudrockVfxArt.Load(key);var sprite=clip?.At(age,loop);if(sprite==null)return;
            if(used==pool.Count){var r=new GameObject("Mudrock original / "+key).AddComponent<SpriteRenderer>();r.transform.SetParent(transform,false);r.sharedMaterial=effectMaterial;pool.Add(r);}
            var s=pool[used++];s.sprite=sprite;s.enabled=true;s.color=Color.white;s.flipX=flip;s.transform.SetPositionAndRotation(position,camera.transform.rotation);s.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+order;
        }
        void Both(string key,double age,Vector3 pos,Camera cam,bool flip=false,bool loop=false)
        {Draw(key+"_ground",age,pos,cam,-2,flip,loop);Draw(key+"_over",age,pos,cam,12,flip,loop);}
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;used=beamUsed=0;
            if(effectMaterial==null)effectMaterial=new Material(Shader.Find("Yesterday Convoy/Mudrock Original Baked"));
            if(observed!=b||run!=(b?.RunId??-1)){observed=b;run=b?.RunId??-1;devices.Clear();foreach(var a in FindObjectsByType<GramophoneAnchor>(FindObjectsSortMode.None)){var r=a.GetComponent<SpriteRenderer>();if(r!=null)devices[a.deviceId]=r;}}
            if(b!=null&&cam!=null)
            {
                foreach(var g in b.Gramophones)
                {
                    if(!devices.TryGetValue(g.Settings.Id,out var body))continue;
                    double age=b.Clock-g.ChangedAt;string key=age>=0&&age<.966667?"towerCapture_all":g.Owner==GramophoneOwner.Neutral?"towerIdle_all":"towerOccupied_all";
                    var clip=MudrockVfxArt.Load(key);body.sprite=clip?.At(key=="towerCapture_all"?age:b.Clock,true);body.sharedMaterial=effectMaterial;body.enabled=true;body.transform.rotation=cam.transform.rotation;
                    body.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,g.Settings.Position);body.color=Color.white;
                }
                foreach(var unit in b.Units)
                {
                    if(!unit.Active||unit.IsReviving||!b.UnitCombat(unit).Effects.Mudrock)continue;
                    var pos=unit.Motion.Position;bool flip=unit.Motion.Direction.x<-.01f;
                    if(unit.ArtsBarrierRemaining>0)Both("shield"+Mathf.Clamp(unit.ArtsBarrierLayers,1,3),b.Clock-unit.SpawnTime,pos,cam,flip,true);
                    double broken=b.Clock-unit.BarrierBrokenAt;if(broken>=0&&broken<2.5)Both("shieldBreak",broken,pos,cam,flip);
                    double refresh=b.Clock-unit.BarrierRefreshStartedAt-.7333334;
                    if(refresh>=0&&refresh<2.3)Both("shieldRefresh",refresh,pos,cam,flip);
                    double attack=b.Clock-unit.LastAttackTime;
                    if(attack>=0&&attack<2)
                    {
                        if(unit.OrdinaryAttacksFired>=3)Both("attackStart",attack,pos+Vector3.up*.5f,cam,flip);

                    }
                }
                foreach(var shot in b.CombatShots)
                {
                    if(shot.FromDefender||!shot.Applied)continue;var sourceUnit=b.FindUnit(shot.SourceId);if(sourceUnit==null||!b.UnitCombat(sourceUnit).Effects.Mudrock)continue;
                    double hit=b.Clock-shot.ImpactAt;if(hit>=0&&hit<1.5)Both("attack"+(shot.Attack<b.UnitCombat(sourceUnit).UnitAttack*3?1:shot.Attack<b.UnitCombat(sourceUnit).UnitAttack*4?2:3),hit,shot.Destination,cam,shot.Destination.x<shot.Origin.x);
                }
                foreach(var c in b.MudrockCaptures)
                {
                    if(c.Cancelled)continue;double age=b.Clock-c.StartedAt;if(age<0||age>=3)continue;
                    var unit=b.FindUnit(c.SourceId);if(unit==null||!unit.Active)continue;bool flip=unit.Motion.Direction.x<-.01f;
                    Both("captureStart",age,c.Position+Vector3.up*.6f,cam,flip);Both("captureRange",age,c.Position,cam,flip);
                    if(c.Resolved){var device=b.Gramophones.FirstOrDefault(x=>x.Settings.Id==c.TargetId);if(device!=null)Both("captureHit",b.Clock-c.ImpactAt,device.Settings.Position+Vector3.up*.7f,cam);}
                }
                foreach(var a in b.GramophoneActions)
                {
                    double age=b.Clock-a.StartedAt;if(age<0||age>.7||a.Resolved&&!a.Applied)continue;
                    if(beamUsed==beams.Count){var l=new GameObject("Gramophone attack / heal connection").AddComponent<LineRenderer>();l.transform.SetParent(transform,false);if(lineMaterial==null)lineMaterial=new Material(Shader.Find("Sprites/Default"));l.sharedMaterial=lineMaterial;l.positionCount=2;l.useWorldSpace=true;l.startWidth=l.endWidth=.025f;beams.Add(l);}
                    var beam=beams[beamUsed++];beam.enabled=true;beam.SetPosition(0,a.From+Vector3.up*.8f);beam.SetPosition(1,a.To+Vector3.up*.5f);var color=a.Healing?new Color(.35f,1,.6f):a.Owner==GramophoneOwner.Monsters?new Color(1,.2f,.2f):new Color(.3f,.7f,1);color.a=(float)Math.Sin(Math.PI*age/.7);beam.startColor=beam.endColor=color;beam.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,a.To)+14;
                }
            }
            for(int i=used;i<pool.Count;i++)pool[i].enabled=false;for(int i=beamUsed;i<beams.Count;i++)beams[i].enabled=false;
        }
        void OnGUI()
        {
            var b=source?.Invoke();var cam=Camera.main;if(b==null||cam==null||b.Gramophones.Count==0)return;
            var previous=GUI.matrix;var vp=TacticalViewport.Current;GUI.matrix=vp.Matrix;
            var style=new GUIStyle(GUI.skin.label){font=GetComponent<ManualBattleController>()?.GetComponent<InitialDemo>()?.InterfaceFont,fontSize=14,alignment=TextAnchor.MiddleCenter};
            foreach(var g in b.Gramophones)
            {
                var p=vp.FromScreen(cam.WorldToScreenPoint(g.Settings.Position));var c=g.Owner==GramophoneOwner.Monsters?new Color(1,.3f,.3f):g.Owner==GramophoneOwner.Defenders?new Color(.35f,.75f,1):Color.gray;
                var old=GUI.color;GUI.color=new Color(.05f,.08f,.1f,.9f);GUI.DrawTexture(new Rect(p.x-38,p.y+2,76,5),Texture2D.whiteTexture);GUI.color=new Color(.35f,.75f,1);GUI.DrawTexture(new Rect(p.x-37,p.y+3,74*g.Charge/100,3),Texture2D.whiteTexture);GUI.color=c;
                GUI.Label(new Rect(p.x-60,p.y+9,120,24),g.Owner==GramophoneOwner.Monsters?"进攻方占领":g.Owner==GramophoneOwner.Defenders?"守军占领":"中立",style);GUI.color=old;
            }
            GUI.matrix=previous;
        }
        void OnDestroy(){if(effectMaterial!=null)Destroy(effectMaterial);if(lineMaterial!=null)Destroy(lineMaterial);}
    }
}
