using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    // Read-only presentation. Healing and regeneration remain entirely in ManualBattle.
    [DefaultExecutionOrder(100)]
    public sealed class MephistoEffectView:MonoBehaviour
    {
        sealed class Aura{public bool active;public double started,ended;}
        Func<ManualBattle> source;
        readonly List<SpriteRenderer> pool=new List<SpriteRenderer>();
        readonly Dictionary<string,DemoSpriteVisual> actors=new Dictionary<string,DemoSpriteVisual>();
        readonly Dictionary<string,Aura> auras=new Dictionary<string,Aura>();
        Material material;int used,knownUnits=-1,run=-1;ManualBattle observed;
        public int VisibleEffects=>used;
        public void Bind(Func<ManualBattle> battle){source=battle;}
        static bool Mephi(ManualBattle b,DeployedUnitState u)=>u!=null&&u.CardId=="CARD_M25"&&b.UnitCombat(u).Effects.Healer;
        Vector3 Root(DeployedUnitState u)=>actors.TryGetValue(u.EntityId,out var a)&&a!=null?a.Renderer.transform.position:u.Motion.Position+u.VisualOffset;
        void Draw(string key,double age,Vector3 position,Vector3 ground,Camera cam,bool flip=false,bool loop=false,int order=12)
        {
            var sprite=MephistoVfxArt.Load(key).At(age,loop);if(sprite==null)return;
            if(material==null){material=Resources.Load<Material>("MudrockVfx/OriginalEffect");if(material==null)throw new InvalidOperationException("Missing original premultiplied VFX material");}
            if(used==pool.Count){var r=new GameObject("Mephisto original VFX").AddComponent<SpriteRenderer>();r.transform.SetParent(transform,false);r.sharedMaterial=material;pool.Add(r);}
            var body=pool[used++];body.name="Mephisto / "+key;body.sprite=sprite;body.enabled=true;body.flipX=flip;body.color=Color.white;
            body.transform.SetPositionAndRotation(position,cam.transform.rotation);body.transform.localScale=Vector3.one*.779296875f;
            body.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,ground)+order;
        }
        Vector3 Hand(ManualBattle b,DeployedUnitState u,Vector3 root,Camera cam,out bool flip)
        {
            actors.TryGetValue(u.EntityId,out var actor);flip=actor!=null?actor.Renderer.flipX:Vector3.Dot(u.Motion.Direction,cam.transform.right)<0;
            bool casting=b.Clock<u.HealingStartedAt+b.UnitCombat(u).AttackDuration;
            string action=casting?"Attack":u.IsMoving?"Move":"Idle";
            double age=casting?b.Clock-u.HealingStartedAt:u.IsMoving?u.Motion.DistanceTravelled/.75:b.Clock-u.SpawnTime;
            int frame=actor!=null?actor.FrameIndex/2:(int)(age*30);
            var point=MephistoVfxArt.Hand(action,frame);
            return root+cam.transform.right*(flip?-point.x:point.x)+cam.transform.up*point.y;
        }
        void LateUpdate()
        {
            used=0;var b=source?.Invoke();var cam=Camera.main;
            if(b!=null&&b.EngagementEnabled&&b.Phase==BattlePhase.Running&&cam!=null)
            {
                if(observed!=b||run!=b.RunId){observed=b;run=b.RunId;knownUnits=-1;actors.Clear();auras.Clear();}
                if(knownUnits!=b.Units.Count){knownUnits=b.Units.Count;actors.Clear();foreach(var a in FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None))if(a.EntityId!=null&&a.gameObject.activeInHierarchy)actors[a.EntityId]=a;}
                foreach(var cast in b.HealingCasts)
                {
                    var u=b.FindUnit(cast.SourceId);if(!Mephi(b,u))continue;
                    double age=b.Clock-cast.ImpactAt;
                    if(u.Active&&!u.IsReviving&&age>=0&&age<3)
                    {var root=Root(u);var hand=Hand(b,u,root,cam,out bool flip);Draw("attack_01_start",age,hand,root,cam,flip);}
                    if(!cast.Resolved||age<0||age>=2.5)continue;
                    foreach(var id in cast.AppliedTargetIds)
                    {
                        var target=b.FindUnit(id);if(target==null||!target.Active||target.IsReviving)continue;
                        var root=Root(target);Draw("attack_01_hit",age,root,root,cam,order:24);
                    }
                }
                bool boosting=false;foreach(var u in b.Units)if(Mephi(b,u)&&u.Active&&!u.IsReviving){boosting=true;break;}
                foreach(var u in b.Units)
                {
                    bool active=boosting&&u.Active&&!u.IsReviving&&b.UnitCombat(u).Effects.RegenerationFraction>0;
                    if(!auras.TryGetValue(u.EntityId,out var aura)){if(!active)continue;auras[u.EntityId]=aura=new Aura{started=b.Clock};}
                    if(active&&!aura.active)aura.started=b.Clock;if(!active&&aura.active)aura.ended=b.Clock;aura.active=active;
                    if(!u.Active||u.IsReviving||!active&&b.Clock-aura.ended>=2)continue;
                    // Adapt recipient placement to body height; original recipient Special0 fallback is unavailable.
                    var root=Root(u);var pos=root+cam.transform.up*.8f;
                    Draw(active?"buff_01_start":"buff_01_end",b.Clock-(active?aura.started:aura.ended),pos,root,cam,loop:active,order:20);
                }
            }
            for(int i=used;i<pool.Count;i++)pool[i].enabled=false;
        }
        void OnDisable(){used=0;foreach(var r in pool)if(r!=null)r.enabled=false;auras.Clear();actors.Clear();knownUnits=-1;}
    }
}
