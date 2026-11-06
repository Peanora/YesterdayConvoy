using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original continuous nozzle effect and fuel detonation, attached to real events.</summary>
    public sealed class FlamerazerEffectView:MonoBehaviour
    {
        Func<ManualBattle> source;Func<double> presentation;
        ManualBattle observed;int run=-1;OriginalCombatVfxLayer layer;
        public int VisibleEffects=>layer?.VisibleCount??0;
        public void Bind(Func<ManualBattle> battle,Func<double> presentationClock=null){source=battle;presentation=presentationClock;}
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;
            if(layer==null)layer=new OriginalCombatVfxLayer(transform);
            if(observed!=b||run!=(b?.RunId??-1)){layer.Clear();observed=b;run=b?.RunId??-1;}
            layer.Begin();
            if(b!=null&&cam!=null)
            {
                double now=presentation?.Invoke()??b.Clock;
                foreach(var e in b.FlameSprays)
                {
                    var u=b.FindUnit(e.SourceId);var d=b.FindDefender(e.TargetId);if(u==null||d==null)continue;
                    double age=now-e.StartedAt-.5;
                    Vector3 from=u.Motion.Position+cam.transform.right*(e.FlipX?-.9f:.9f)+cam.transform.up*1.15f,to=d.Position+cam.transform.up*.8f;
                    if(age>=0&&!e.Ended&&u.Active&&!u.FuelDying&&d.Alive)
                        layer.ShowBeam("monster/enemy_duspfr_2_attack_01_front_start2",age,from,to,cam,3.005432f,28,true);
                    // Each real damage tick owns its target hit. No hit is invented while idle.
                    for(int tick=0;tick<e.Ticks;tick++)
                    {
                        double hitAge=now-(e.StartedAt+.5+tick*.5);
                        if(hitAge>=0)layer.Show("monster/enemy_duspfr_2_attack_01_hit",hitAge,to,cam,false,34);
                    }
                }
                foreach(var e in b.FuelExplosions)
                {
                    layer.ShowGround("monster/enemy_duspfr_2_die_01",now-(e.DetonateAt-.1),e.Position,cam,false,30);
                    if(e.Resolved)layer.ShowGround("monster/enemy_duspfr_2_die_01_hit",now-e.DetonateAt,e.Position,cam,false,32);
                }
            }
            layer.End();
        }
        void OnDisable(){layer?.Clear();}
        void OnDestroy(){layer?.Dispose();}
    }
}
