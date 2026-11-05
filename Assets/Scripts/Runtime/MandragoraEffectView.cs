using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    // Read-only director: frames derive from battle events; no damage or simulation writes.
    [DefaultExecutionOrder(100)]
    public sealed class MandragoraEffectView:MonoBehaviour
    {
        Func<ManualBattle> source;Func<double> presentationClock;
        readonly List<SpriteRenderer> pool=new List<SpriteRenderer>();
        readonly Dictionary<string,DemoSpriteVisual> actors=new Dictionary<string,DemoSpriteVisual>();
        Material material;int used,knownUnits=-1,run=-1;ManualBattle observed;
        public int VisibleEffects=>used;
        public void Bind(Func<ManualBattle> battle,Func<double> clock=null){source=battle;presentationClock=clock;}
        SpriteRenderer Draw(string key,double age,Vector3 position,Camera cam,int order=12,bool flip=false,bool loop=false,float scale=1,Vector3? ground=null)
        {
            var sprite=MandragoraVfxArt.Load(key).At(age,loop);if(sprite==null)return null;
            if(material==null){material=Resources.Load<Material>("MudrockVfx/OriginalEffect");if(material==null)throw new InvalidOperationException("Missing original premultiplied VFX material");}
            if(used==pool.Count){var r=new GameObject("Mandragora original VFX").AddComponent<SpriteRenderer>();r.transform.SetParent(transform,false);r.sharedMaterial=material;pool.Add(r);}
            var body=pool[used++];body.name="Mandragora / "+key;body.sprite=sprite;body.enabled=true;body.flipX=flip;body.color=Color.white;
            body.transform.SetPositionAndRotation(position,cam.transform.rotation);body.transform.localScale=Vector3.one*scale;
            body.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,ground??position)+order;return body;
        }
        void Fx(string key,double age,Vector3 pos,Camera cam,bool flip=false,bool loop=false,int order=12,float scale=.95f,Vector3? ground=null)
        {if(age>=0&&age<10||loop)Draw(key,age,pos,cam,order,flip,loop,scale,ground);}
        static Vector3 Mount(string action,double age,bool eye,Vector3 root,Camera cam,bool flip,bool loop=false)
        {var m=MandragoraVfxArt.Mount(action,age,eye,loop);return root+cam.transform.right*(flip?-m.x:m.x)+cam.transform.up*m.y;}
        void Gaze(ManualBattle b,DeployedUnitState u,Vector3 root,Camera cam,bool flip)
        {
            var g=u.Gaze;if(g==null||g.Ended||b.Clock<g.StartedAt)return;
            var target=b.FindDefender(g.TargetId);if(target==null||!target.Alive)return;
            double age=b.Clock-g.StartedAt;bool second=u.MandraPhase==MandragoraPhase.Second;string prefix=second?"C2":"C1";
            var eye=Mount(prefix+"_Skill_1_"+(age<.8333334?"Begin":"Loop"),age<.8333334?age:age-.8333334,true,root,cam,flip,age>=.8333334);
            var halo=eye-cam.transform.right*((second?.291f:.359f)*(flip?-1:1))*.8f-cam.transform.up*(second?.741f:.583f)*.8f;
            Fx(second?"c2_skill_01_beamshield_01":"c1_skill_01_beamshield_01",age,halo,cam,flip,true,14,.8f,root);
            if(age<.8333334)return;
            var end=target.Position+cam.transform.up*.85f;var delta=cam.transform.InverseTransformDirection(end-eye);
            float length=new Vector2(delta.x,delta.y).magnitude;var beam=Draw("c1_skill_01_beam",age-.8333334,eye,cam,24,false,true,1,root);
            beam.transform.rotation=cam.transform.rotation*Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);beam.transform.localScale=new Vector3(length/3,1,1);
            Fx("c1_skill_01_hit",age-.8333334,end,cam,false,true,50,.8f,target.Position);
        }
        void Unit(ManualBattle b,DeployedUnitState u,Camera cam)
        {
            if(!b.UnitCombat(u).Effects.Mandragora)return;
            actors.TryGetValue(u.EntityId,out var actor);bool flip=actor!=null?actor.Renderer.flipX:u.Motion.Direction.x<0;
            var root=actor!=null?actor.Renderer.transform.position:u.Motion.Position+u.VisualOffset;bool second=u.MandraPhase==MandragoraPhase.Second;
            if(!u.Active)
            {
                if(u.Resolution==UnitResolution.Dead){double death=(presentationClock?.Invoke()??b.Clock)-u.DeathTime;Fx("c2_die_01",death,root,cam,flip);}
                return;
            }
            if(u.MandraPhase==MandragoraPhase.Reborn)
            {
                double age=(b.Clock-u.MandraPhaseStartedAt)*7.3333334/5;
                Fx("reborn_start_01",age,root,cam,flip);if(age<4.5){Fx("reborn_loop_01",age,root,cam,flip,true);Fx("reborn_loop_02",age,root,cam,flip,true);}
                Fx("reborn_end_01",age-4.5,root,cam,flip);Fx("reborn_end_02",age-4.5,root,cam,flip);return;
            }
            double restore=b.Clock-u.StoneShieldRestoredAt,broken=b.Clock-u.StoneShieldBrokenAt;
            string phase=second?"c2":"c1";
            if(u.StoneShield&&(double.IsInfinity(restore)||restore>=.7333334))
            {
                double age=b.Clock-u.SpawnTime;
                Fx(phase+"_skill_02_buff_01",age,root,cam,flip,true,-1);
                if(second){Fx("c2_skill_02_buff_02",age,root,cam,flip,true);Fx("c2_skill_02_buff_03",age,root,cam,flip,true,-2);}
            }
            Fx(phase+"_skill_02_begin_01",restore,root,cam,flip);
            Fx(phase+"_skill_02_end_01",broken,root,cam,flip);
            if(second)Fx("c2_skill_02_buff_02_end",broken,root,cam,flip);
            Gaze(b,u,root,cam,flip);
            double attack=(b.Clock-u.LastAttackTime)*u.LastAttackRate;
            if(attack>=0&&attack<1.5)
            {
                var hand=Mount(second?u.StoneShield?"C2_Attack_1":"C2_Attack_2":"C1_Attack",attack,false,root,cam,flip);
                string prefix=second?"c2_attack_01":"c1_attack";
                Fx(prefix+"_start_01",attack,hand,cam,flip,ground:root);Fx(prefix+"_start_02",attack,hand,cam,flip,ground:root);
            }
            double pillar=b.Clock-u.PillarCommandStartedAt;
            if(pillar>=0&&pillar<1.5)
            {
                Fx("c2_skill_03_start_01",pillar,root,cam,flip);Fx("c2_skill_03_start_02",pillar,root,cam,flip);
                Fx("c2_skill_03_hand_01",pillar,Mount("C2_Skill_3",pillar,false,root,cam,flip),cam,flip,ground:root);
            }
        }
        void LateUpdate()
        {
            used=0;var b=source?.Invoke();var cam=Camera.main;
            if(b!=null&&b.EngagementEnabled&&cam!=null)
            {
                if(observed!=b||run!=b.RunId){observed=b;run=b.RunId;knownUnits=-1;actors.Clear();}
                if(knownUnits!=b.Units.Count){knownUnits=b.Units.Count;actors.Clear();foreach(var v in FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None))if(v.EntityId!=null)actors[v.EntityId]=v;}
                bool any=false;foreach(var u in b.Units){if(b.UnitCombat(u).Effects.Mandragora)any=true;Unit(b,u,cam);}
                if(any)
                {
                    foreach(var shot in b.CombatShots)
                    {
                        if(shot.FromDefender||shot.Erased)continue;var u=b.FindUnit(shot.SourceId);if(u==null||!b.UnitCombat(u).Effects.Mandragora)continue;
                        double flight=b.Clock-shot.StartedAt;var end=shot.Destination+cam.transform.up*.85f;bool flip=shot.Destination.x<shot.Origin.x;
                        if(!shot.Resolved&&flight>=0&&b.Clock<shot.ImpactAt&&u.MandraPhase==MandragoraPhase.Second)
                        {
                            var from=shot.Origin+cam.transform.up*.9f;float t=(float)(flight/(shot.ImpactAt-shot.StartedAt));
                            Fx("c2_attack_01_trail_01",flight,Vector3.Lerp(from,end,t),cam,flip,true,24,ground:Vector3.Lerp(shot.Origin,shot.Destination,t));
                        }
                        if(shot.Applied)Fx(u.MandraPhase==MandragoraPhase.Ground?"c1_attack_hit":"c2_attack_01_hit",b.Clock-shot.ImpactAt,end,cam,flip,order:50,ground:shot.Destination);
                    }
                    foreach(var command in b.PillarCommands)if(!command.Resolved)
                    {
                        double age=b.Clock-command.StartedAt;var p=command.Pillar.Settings.Position;
                        Fx("skill_03_pillar_ground_01",age,p,cam,false,true,-2);
                        for(int j=1;j<=2;j++){var target=p+command.Direction*j;Fx("warning_ground_01",age,target,cam,false,true,-1);Fx("warning_head_01",age,target+cam.transform.up*1.6f,cam,false,true,50,ground:target);}
                    }
                    foreach(var f in b.Facilities)if(f.Collapsed&&f.Settings.Kind==FacilityKind.Pillar)
                    {
                        string direction=Mathf.Abs(f.FallDirection.x)>.5f?f.FallDirection.x>0?"right":"left":f.FallDirection.z>0?"up":"down";
                        Fx("stonepillar_collapse_01_"+direction,b.Clock-f.LastActivation,f.Settings.Position,cam,false,false,20);
                        Fx("skill_03_stones_01",b.Clock-f.LastActivation,f.Settings.Position+f.FallDirection,cam);
                    }
                    foreach(var cell in b.SealedTiles)
                    {
                        double created=0;foreach(var d in b.Defenders)if(Mathf.RoundToInt(d.Position.x)==cell.x&&Mathf.RoundToInt(d.Position.z)==cell.y&&!double.IsInfinity(d.DeathTime))created=d.DeathTime;
                        Fx("block_01",Math.Min(1,b.Clock-created),new Vector3(cell.x,.02f,cell.y),cam,false,false,8);
                    }
                }
            }
            for(int i=used;i<pool.Count;i++)pool[i].enabled=false;
        }
    }
}
