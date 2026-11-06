using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Visual-only original Flamechaser phase attachments; body clips remain unchanged.</summary>
    public sealed class MonsterOriginalEffectView:MonoBehaviour
    {
        Func<ManualBattle> source;Func<double> presentation;ManualBattle observed;
        sealed class Tail{internal string Entity;internal double At;internal Vector3 Position;}
        readonly List<Tail> reforms=new List<Tail>();
        OriginalCombatVfxLayer layer;int run=-1;
        public int VisibleEffects=>layer?.VisibleCount??0;
        public void Bind(Func<ManualBattle> battle,Func<double> presentationClock=null){source=battle;presentation=presentationClock;}
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;
            if(layer==null)layer=new OriginalCombatVfxLayer(transform);
            if(observed!=b||run!=(b?.RunId??-1)){layer.Clear();reforms.Clear();observed=b;run=b?.RunId??-1;}
            layer.Begin();
            if(b!=null&&cam!=null)
            {
                double now=presentation?.Invoke()??b.Clock;
                foreach(var u in b.Units)
                {
                    if(u.CardId!="CARD_M30"||!u.Active)continue;
                    double age=now-u.FlamePhaseStartedAt;
                    if(u.FlamePhase==FlamechaserPhase.Ember)
                    {
                        layer.ShowGround("monster/enemy_duskls_revive_2",age,u.Motion.Position,cam,false,22);
                        layer.ShowGround("monster/enemy_duskls_idle_2",age,u.Motion.Position,cam,false,20,true);
                    }
                    else if(u.FlamePhase==FlamechaserPhase.Reforming)
                    {
                        if(!reforms.Exists(e=>e.Entity==u.EntityId&&e.At==u.FlamePhaseStartedAt))
                        {
                            reforms.Add(new Tail{Entity=u.EntityId,At=u.FlamePhaseStartedAt,Position=u.Motion.Position});
                            MonsterOriginalVfx.RecordPhaseTail(b,u.FlamePhaseStartedAt,"enemy_duskls_revive_3");
                        }
                    }
                    else if(u.FlamePhase==FlamechaserPhase.Soldier&&b.NearBurningReeds(u))
                        layer.ShowGround("monster/enemy_duskls_weapon_buff",now-u.SpawnTime,u.Motion.Position,cam,false,18,true);
                }
                for(int i=reforms.Count-1;i>=0;i--)
                {
                    var e=reforms[i];double age=now-e.At;
                    if(age>=MonsterOriginalVfx.Duration("enemy_duskls_revive_3")){reforms.RemoveAt(i);continue;}
                    layer.ShowGround("monster/enemy_duskls_revive_3",age,e.Position,cam,false,23);
                    layer.ShowGround("monster/enemy_duskls_revive_4",age,e.Position,cam,false,24);
                }
            }
            layer.End();
        }
        void OnDisable(){layer?.Clear();reforms.Clear();}
        void OnDestroy(){layer?.Dispose();}
    }
}
