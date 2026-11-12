using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Native FrostNova caster, ground and target layers. Damage stays in ManualBattle.</summary>
    public sealed class FrostSealView:MonoBehaviour
    {
        Func<ManualBattle> source;Func<double> presentation;
        ManualBattle observed;int run=-1;
        OriginalCombatVfxLayer layer;
        public int VisibleEffects=>layer?.VisibleCount??0;
        public void Bind(Func<ManualBattle> battle,Func<double> presentationClock=null){source=battle;presentation=presentationClock;}
        void Draw(string raw,double age,Vector3 pos,Camera camera,int order,bool loop=false)
        {if(age>=0)layer.ShowGround("monster/"+raw,age,pos,camera,false,order,loop);}
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;
            if(layer==null)layer=new OriginalCombatVfxLayer(transform);
            if(observed!=b||run!=(b?.RunId??-1)){layer.Clear();observed=b;run=b?.RunId??-1;}
            layer.Begin();
            if(b!=null&&cam!=null)
            {
                double now=presentation?.Invoke()??b.Clock;
                foreach(var u in b.Units)
                {
                    if(u.CardId!="CARD_M14"||BossRosterEffectView.OwnsCard(u.CardId))continue;
                    double ringAge=now-u.IceRingStartedAt;
                    if(u.Active&&!u.IsReviving&&ringAge>=0&&ringAge<3)
                        Draw("firstar_skill_01_buff",ringAge,u.Motion.Position,cam,18,true);
                    if(u.IsReviving)
                    {
                        // Preserve all three original delayed layers, fitted to this game's5s revive.
                        double nativeAge=(now-(u.ReviveAt-5))*MonsterOriginalVfx.Duration("firstar_buff_03_start",9.266667f)/5;
                        Draw("firstar_buff_01_start",nativeAge,u.Motion.Position,cam,24);
                        Draw("firstar_buff_02_start",nativeAge,u.Motion.Position,cam,25);
                        Draw("firstar_buff_03_start",nativeAge,u.Motion.Position,cam,26);
                    }
                }
                if(!BossRosterEffectView.OwnsCard("CARD_M14"))foreach(var e in b.IceRings)Draw("firstar_skill_01_range",now-e.Time,e.Position,cam,24);
                // The five-kind snow synergy is a separate, retained effect source.
                foreach(var e in b.SnowstormPulses)
                    foreach(var id in e.Targets)
                    {var d=b.FindDefender(id);if(d!=null)Draw("firstar_skill_01_range",now-e.At,d.Position,cam,24);}
                foreach(var e in b.FrostSeals)
                {
                    if(e.Cancelled)continue;
                    double castAge=now-e.StartedAt;
                    var caster=b.FindUnit(e.SourceId);
                    if(caster!=null&&caster.Active&&!caster.IsReviving&&!BossRosterEffectView.OwnsCard(caster.CardId))
                    {
                        Draw("firstar_skill_02_start",castAge,e.Position,cam,20);
                        Draw("firstar_skill_02_trail",castAge,e.Position,cam,21);
                    }
                    if(!e.Resolved)continue;
                    double age=now-e.ImpactAt;
                    Draw("firstar_skill_01_range",age,e.Position,cam,25);
                    if(age<0||age>=FrostSealCast.FreezeDuration)continue;
                    foreach(var id in e.FrozenTargets)
                    {
                        var d=b.FindDefender(id);if(d==null)continue;
                        Draw("firstar_skill_02_range",now-e.StartedAt,d.Position,cam,27);
                        Draw("firstar_skill_02_range_02",age,d.Position,cam,28,true);
                    }
                }
            }
            layer.End();
        }
        void OnDisable(){layer?.Clear();}
        void OnDestroy(){layer?.Dispose();}
    }
}
