using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Native Seaborn layers follow actual retaliation, phase, bubble and shackle events.</summary>
    public sealed class NeuralEffectView:MonoBehaviour
    {
        sealed class Impact{internal double At;internal Vector3 Position;}
        readonly List<Impact> impacts=new List<Impact>();
        readonly Dictionary<CorrosiveBubble,double> observedHits=new Dictionary<CorrosiveBubble,double>();
        Func<ManualBattle> source;Func<double> presentation;ManualBattle observed;
        OriginalCombatVfxLayer layer;int run=-1;
        public int VisibleEffects=>layer?.VisibleCount??0;
        public void Bind(Func<ManualBattle> battle,Func<double> presentationClock=null){source=battle;presentation=presentationClock;}
        void Draw(string raw,double age,Vector3 pos,Camera cam,int order,bool loop=false)
        {if(age>=0)layer.ShowGround("monster/"+raw,age,pos,cam,false,order,loop);}
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;
            if(layer==null)layer=new OriginalCombatVfxLayer(transform);
            if(observed!=b||run!=(b?.RunId??-1))
            {layer.Clear();impacts.Clear();observedHits.Clear();observed=b;run=b?.RunId??-1;}
            layer.Begin();
            if(b!=null&&cam!=null)
            {
                double now=presentation?.Invoke()??b.Clock;
                foreach(var e in b.CreeperPulses)
                {
                    Draw("enemy_dsurch_01_start",now-e.Time,e.Position,cam,24);
                    Draw("enemy_dsurch_01_start_02",now-e.Time,e.Position,cam,25);
                }
                foreach(var u in b.Units)
                {
                    if(u.CardId!="CARD_M34"||!u.Active)continue;
                    if(u.HarvesterPhase==HarvesterPhase.Waking)
                        Draw("enemy_dslntc_skill_begin_buff",now-u.HarvesterPhaseStartedAt,u.Motion.Position,cam,22);
                    else if(u.HarvesterPhase==HarvesterPhase.Combat)
                        Draw("enemy_dslntc_buff_01",now-u.HarvesterPhaseStartedAt,u.Motion.Position,cam,22,true);
                }
                foreach(var e in b.Shackles)
                {
                    var u=b.FindUnit(e.SourceId);var d=b.FindDefender(e.TargetId);if(u==null||d==null)continue;
                    double age=now-e.StartedAt;
                    if(!e.Ended&&u.Active&&d.Alive&&b.DefenderShackled(d))
                    {
                        Vector3 start=u.Motion.Position+cam.transform.up*1.1f,end=d.Position+cam.transform.up*.8f;
                        layer.Show("monster/dmage_skill_01_start",age,start,cam,false,22);
                        layer.ShowBeam("monster/dmage_skill_01_line",age,start,end,cam,3.005432f,28,true);
                        // Original projectile_dmage attaches this layer to the target.
                        Draw("dmage_skill_01_buff",age,d.Position,cam,32,true);
                    }
                }
                foreach(var e in b.CorrosiveBubbles)
                {
                    double age=now-e.StartedAt;var u=b.FindUnit(e.SourceId);
                    if(age>=0&&age<5)
                    {
                        Draw("enemy_dsmant_skill_01_start",age,e.Origin,cam,24);
                        if(u!=null&&u.Active)
                        {
                            Draw("enemy_dsmant_skill_01_buff",age,u.Motion.Position,cam,26,true);
                            Draw("enemy_dsmant_skill_01_buff_02",age,u.Motion.Position,cam,27,true);
                        }
                        Draw("enemy_dsmant_skill_01_buff_03",age,e.Anchor,cam,28,true);
                    }
                    if(!double.IsNegativeInfinity(e.LastPulseAt)&&(!observedHits.TryGetValue(e,out double at)||at!=e.LastPulseAt))
                    {
                        observedHits[e]=e.LastPulseAt;var d=b.FindDefender(e.LastTargetId);
                        if(d!=null)impacts.Add(new Impact{At=e.LastPulseAt,Position=d.Position});
                    }
                }
                for(int i=impacts.Count-1;i>=0;i--)
                {
                    var hit=impacts[i];double age=now-hit.At;
                    if(age>=MonsterOriginalVfx.Duration("enemy_dsmant_skill_01_hit")){impacts.RemoveAt(i);continue;}
                    Draw("enemy_dsmant_skill_01_hit",age,hit.Position,cam,34);
                }
                foreach(var d in b.Defenders)
                    if(!double.IsNegativeInfinity(d.LastNeuralBurstAt))
                        layer.ShowGround("common/common_nerve_injury_01_hit",now-d.LastNeuralBurstAt,d.Position,cam,false,38);
            }
            layer.End();
        }
        void OnDisable(){layer?.Clear();impacts.Clear();observedHits.Clear();}
        void OnDestroy(){layer?.Dispose();}
    }
}
