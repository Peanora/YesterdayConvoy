using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original FX consume launched attacks and existing skill/entity states. No combat writes.</summary>
    public sealed class DefenderSkillVfxView : MonoBehaviour
    {
        sealed class ShotView { public double Group; public int Variant; public Vector3 Impact; public bool HasImpact,Empowered; }
        sealed class Group { public double Time; public int Variant; }
        sealed class ChessEvent { public SixthCell Cell; public Vector3 Position; public double Created,Trigger; public string SourceId; }
        sealed class Erasure { public Vector3 Position; public double At; }
        Func<ManualBattle> source;
        Func<double> presentationClock;
        OriginalCombatVfxLayer native;
        YuFirewallVfx yuWall;
        ManualBattle observedBattle;
        int observedRun=-1;
        readonly Dictionary<CombatShot,ShotView> shots=new Dictionary<CombatShot,ShotView>();
        readonly Dictionary<(string,Vector3),Group> groups=new Dictionary<(string,Vector3),Group>();
        readonly Dictionary<string,int> variants=new Dictionary<string,int>();
        readonly HashSet<(string,double,Vector3)> castSeen=new HashSet<(string,double,Vector3)>();
        readonly Dictionary<SixthCell,ChessEvent> chess=new Dictionary<SixthCell,ChessEvent>();
        readonly Dictionary<CombatShot,Erasure> erased=new Dictionary<CombatShot,Erasure>();
        readonly List<SixthCell> staleChess=new List<SixthCell>();
        public double PendingUntil {get;private set;}=double.NegativeInfinity;
        public bool EffectPending=>source?.Invoke() is ManualBattle b&&
            (b.Phase==BattlePhase.Won||b.Phase==BattlePhase.Lost)&&(presentationClock?.Invoke()??b.Clock)<PendingUntil;
        public int VisibleCount=>(native?.VisibleCount??0)+(yuWall?.VisibleCount??0);
        public void Bind(Func<ManualBattle> battle)=>Bind(battle,null);
        public void Bind(Func<ManualBattle> battle,Func<double> clock){source=battle;presentationClock=clock;}
        static bool Owns(string id)=>id=="D03"||id=="D05"||id=="D11"||id=="D12"||
            id=="D21"||id=="D22"||id=="D23"||id=="D24"||id=="D25"||id=="D26"||id=="D27"||id=="D28"||id=="D29"||id=="D30";
        static float Distance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static string Key(string id,string raw)=>raw.StartsWith("common/",StringComparison.Ordinal)?raw:"defender/"+id+"/"+raw;
        bool Show(string id,string raw,double age,Vector3 p,Camera cam,bool flip=false,int order=32,bool loop=false,bool directional=false,Vector3 direction=default,bool ground=false)
        {
            string key=Key(id,raw);
            // Missing/empty native control nodes are tracked as gaps, never replaced with a generic ring.
            if(!OriginalCombatVfxArt.TryGet(key,out var clip))return false;
            if(ground)return native.ShowGround(key,age,p,cam,flip,order,loop&&clip.Loop);
            if(directional&&clip.Directional)return native.ShowOriented(key,age,p,cam,direction,order,loop&&clip.Loop);
            if(directional)flip=Vector3.Dot(direction,cam.transform.right)<0;
            return native.Show(key,age,p,cam,flip,order,loop&&clip.Loop);
        }
        void Tail(string id,string raw,double started)
        {
            if(double.IsNaN(started)||double.IsInfinity(started))return;
            if(OriginalCombatVfxArt.TryGet(Key(id,raw),out var clip))PendingUntil=Math.Max(PendingUntil,started+clip.Duration);
        }
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;
            if(native==null)native=new OriginalCombatVfxLayer(transform);
            if(yuWall==null)yuWall=new YuFirewallVfx(transform);
            if(b!=observedBattle||observedRun!=(b?.RunId??-1))
            {
                native.Clear();yuWall.Clear();shots.Clear();groups.Clear();variants.Clear();chess.Clear();erased.Clear();
                observedBattle=b;observedRun=b?.RunId??-1;PendingUntil=double.NegativeInfinity;
            }
            native.Begin();yuWall.Begin();castSeen.Clear();
            if(b!=null&&cam!=null)
            {
                double clock=presentationClock?.Invoke()??b.Clock;
                PresentFields(b,cam,clock);PresentShots(b,cam,clock);PresentChess(b,cam,clock);PresentErasure(b,cam,clock);
            }
            native.End();yuWall.End();
        }
        void PresentFields(ManualBattle b,Camera cam,double clock)
        {
            bool running=b.Phase==BattlePhase.Running||b.Phase==BattlePhase.Ready;
            foreach(var d in b.Defenders)
            {
                string id=d.Settings.ArtId;if(!Owns(id))continue;
                double age=clock-d.SkillStartedAt,endAge=clock-d.SkillEndedAt;
                if(id=="D22")yuWall.Present(b,d,cam,clock);
                if(running&&d.Alive&&(b.DefenderSkillActive(d)||b.IsFifthActive(d)))
                {
                    switch(id)
                    {
                        case "D21":
                            Show(id,"whitw2_skill_03_buff_start",age,d.Position,cam,ground:true);
                            break;
                        case "D22":Show(id,"yu_skill3_buff_01",age,d.Position,cam,loop:true,ground:true);break;
                        case "D23":
                            Show(id,"monstr_skill_03_begin_01",age,d.Settings.Position,cam,ground:true);
                            Show(id,"monstr_skill_03_begin_02",age,d.Position,cam);
                            Show(id,"monstr_skill_03_begin_03",age,d.Position,cam,loop:true,ground:true);
                            for(int i=1;i<=5;i++)Show(id,"monstr_skill_03_buff_0"+i,age,d.Position,cam,order:i==1?29:31,loop:true,ground:i==1||i==3||i==4);
                            break;
                        case "D24":
                            Show(id,"angel2_skill_3_begin_start_001",age,d.Position,cam);
                            Show(id,"angel2_skill_3_buff_01",age,d.Position,cam,loop:true,ground:true);
                            if(d.Reinforcements>0)
                            {
                                if(age>=0&&age<.7)
                                    Show(id,"angel2_skill_3_begin_trail_01",age,Vector3.Lerp(d.Position,d.Settings.RelayPosition,(float)(age/.7)),cam,
                                        order:33,loop:true,directional:true,direction:d.Settings.RelayPosition-d.Position);
                                Show(id,"angel2_skill_3_begin_range_01",age,d.Settings.RelayPosition,cam,order:34,ground:true);
                                Show(id,"angel2_skill_3_ground_01",age,d.Settings.RelayPosition,cam,order:27,loop:true,ground:true);
                            }
                            break;
                        case "D25":
                            Show(id,"hsgma2_skill_03_buff_01",age,d.Position,cam,loop:true,ground:true);
                            Show(id,"hsgma2_skill_03_buff_02",age,d.Position,cam,loop:true,ground:true);
                            if(d.DebtLife)Show(id,"hsgma2_skill_03_buff_03",age,d.Position,cam,loop:true,ground:true);
                            Show(id,d.ProtectionUntil>b.Clock?"hsgma2_skill_03_range_02":"hsgma2_skill_03_range_01",age,d.Position,cam,order:27,loop:true,ground:true);
                            break;
                        case "D26":
                            Show(id,"svash2_skill_03_buff_01",age,d.Position,cam,loop:true,ground:true);
                            Show(id,"svash2_skill_03_buff_weapon_01",age,d.Position,cam,loop:true);
                            var partner=b.FindDefender(d.PartnerId);
                            if(partner!=null&&partner.Alive)
                            {
                                Show(id,"svash2_skill_03_deploy_01",age,partner.Position,cam,ground:true);
                                Show(id,"svash2_eagle_fly_01",age,partner.Position,cam,loop:true);
                                Show(id,"svash2_skill_03_range_01",age,partner.Position,cam,order:27,loop:true,ground:true);
                            }
                            else Show(id,"svash2_skill_03_range_01",age,d.Position,cam,order:27,loop:true,ground:true);
                            break;
                        // Snow wind belongs to launched attacks. Passive snow remains a rule marker.
                        case "D27":break;
                        case "D28":
                            Show(id,"wang_skill_03_range_start",age,d.Position,cam,order:27,ground:true);
                            Show(id,"wang_skill_03_buff_02",age,d.Position,cam,loop:true,ground:true);
                            Show(id,"wang_skill_03_range",age,d.Position,cam,order:27,loop:true,ground:true);
                            break;
                        case "D29":
                            Show(id,"demkni_skill_03_self",age,d.Position,cam,order:29,loop:true,ground:true);
                            Show(id,"demkni_skill_03_range",age,d.Position,cam,order:27,loop:true,ground:true);
                            foreach(var u in b.Units)if(u.Active&&Distance(u.Motion.Position,d.Position)<=b.DefenderRange(d))
                                Show(id,"demkni_skill_03_buff",age,u.Motion.Position,cam,order:31,loop:true,ground:true);
                            break;
                        case "D30":
                            Show(id,"mlynar_skill_03_buff_02",age,d.Position,cam,loop:true,ground:true);
                            Show(id,"mlynar_skill_03_buff_03",age,d.Position,cam,order:27,loop:true,ground:true);
                            break;
                    }
                }
                if(id=="D23"&&endAge>=0)
                {
                    Show(id,"monstr_skill_03_end_01",endAge,d.Position,cam);
                    Show(id,"monstr_skill_03_end_02",endAge,d.Settings.RelayPosition,cam,ground:true);
                    Tail(id,"monstr_skill_03_end_01",d.SkillEndedAt);Tail(id,"monstr_skill_03_end_02",d.SkillEndedAt);
                }
            }
            if(!running)return;
            foreach(var drone in b.DefenderDrones)
            {
                var d=b.FindDefender(drone.SourceId);if(!b.DefenderSkillActive(d))continue;
                var target=b.FindUnit(drone.TargetId);Vector3 direction=target!=null?target.Motion.Position-drone.Position:Vector3.right;
                Show("D21","whitw2_skill_03_token_trail_01",clock-d.SkillStartedAt+drone.Index*.17,drone.Position,cam,
                    order:33,loop:true,directional:true,direction:direction);
            }
        }
        ShotView State(ManualBattle b,CombatShot shot,DefenderState d)
        {
            if(shots.TryGetValue(shot,out var result))return result;
            var key=(shot.SourceId,shot.Origin);string id=d.Settings.ArtId;
            bool clustered=id=="D25"||id=="D30"||id=="D26"||id=="D27"||id=="D23";
            if(!groups.TryGetValue(key,out var group)||Math.Abs(shot.StartedAt-group.Time)>(clustered?.3:1e-6))
            {
                variants.TryGetValue(shot.SourceId,out int variant);group=new Group{Time=shot.StartedAt,Variant=variant};
                groups[key]=group;variants[shot.SourceId]=variant+1;
            }
            result=new ShotView{Group=group.Time,Variant=group.Variant,
                Empowered=shot.Empowered||shot.StartedAt>=d.SkillStartedAt&&shot.StartedAt<d.SkillActiveUntil};
            shots.Add(shot,result);return result;
        }
        void PresentShots(ManualBattle b,Camera cam,double clock)
        {
            foreach(var shot in b.CombatShots)
            {
                if(!shot.FromDefender||shot.Erased||clock<shot.StartedAt||clock-shot.ImpactAt>5)continue;
                var d=b.FindDefender(shot.SourceId);if(d==null||!Owns(d.Settings.ArtId))continue;
                string id=d.Settings.ArtId;var view=State(b,shot,d);
                var unit=b.FindUnit(shot.TargetId);var ally=b.FindDefender(shot.TargetId);
                Vector3 destination=shot.Healing?ally?.Position??shot.Destination:unit?.Motion.Position??shot.Destination;
                if(shot.Applied&&!view.HasImpact){view.HasImpact=true;view.Impact=destination;}
                if(view.HasImpact)destination=view.Impact;
                double age=clock-shot.StartedAt,castAge=clock-view.Group,hitAge=clock-shot.ImpactAt;
                Vector3 delta=destination-shot.Origin;bool left=Vector3.Dot(delta,cam.transform.right)<0;
                string cast=null,trail=null,hit=null;bool empowered=view.Empowered;
                switch(id)
                {
                    case "D03":trail=shot.Healing?"spot_skill_01_trail":null;hit=shot.Healing?"spot_skill_01_hit":"common/common_hit_01";break;
                    case "D05":
                        string prefix=empowered||shot.DamageType==CombatDamageType.Arts?"midn_skill_01":"midn_attack_01";
                        if(Distance(shot.Origin,destination)>1.2f)trail=prefix+"_trail";hit=prefix+"_hit";break;
                    case "D11":trail="ansel_attack_01_trail";hit="common/common_heal_hit_01";break;
                    case "D12":trail="orchid_attack_01_trail";hit="orchid_attack_01_hit";break;
                    case "D21":
                        if(shot.Drone){trail=empowered?null:"whitw2_token_01";hit=empowered?"whitw2_skill_03_token_hit_01":"whitw2_token_hit_01";}
                        else {cast=empowered?"whitw2_skill_03_start_01":"whitw2_attack_01_start";trail=empowered?"whitw2_skill_03_trail_01":"whitw2_attack_01_trail";hit="whitw2_hit_01";}
                        break;
                    case "D22":cast=empowered?"yu_skill3_attack_01":"yu_attack_01";hit=empowered?"yu_skill3_hit_01":"yu_hit_01";break;
                    case "D23":
                        if(shot.Healing&&empowered){cast="monstr_skill_03_heal";hit="monstr_skill_03_heal";}
                        else if(empowered){cast="monstr_skill_03_start_01";hit="monstr_skill_03_hit";}
                        else {cast="monstr_attack_start_01";trail="monstr_attack_trail";hit="monstr_attack_hit_01";}
                        break;
                    case "D24":
                        cast=empowered?"angel2_skill_3_loop_start_01":"angel2_attack_loop_start_01";
                        trail=empowered?"angel2_skill_3_loop_trail_01":"angel2_attack_loop_trail_01";
                        hit=empowered?"angel2_skill_3_loop_hit_01":"angel2_attack_loop_hit_01";break;
                    case "D25":
                        bool protectedPhase=d.ProtectionUntil>shot.StartedAt&&shot.StartedAt>=d.SkillActiveUntil;
                        cast=empowered?"hsgma2_skill_03_start_0"+(1+view.Variant%3+(protectedPhase?3:0)):"hsgma2_attack_01_start_01";
                        int contact=Mathf.Clamp(Mathf.RoundToInt((float)((shot.StartedAt-view.Group)/.08)),0,3);
                        hit=!empowered?"hsgma2_attack_01_hit_01":protectedPhase?"hsgma2_skill_03_hit_02"+(contact==0?"":"_"+contact):"hsgma2_skill_03_hit_01"+(contact==0?"":"_1");break;
                    case "D26":
                        if(empowered)
                        {
                            if(castSeen.Add((shot.SourceId,view.Group,shot.Origin)))
                            {
                                foreach(string raw in new[]{"svash2_skill_03_combat_start_01","svash2_skill_03_combat_start_01_2","svash2_skill_03_combat_start_02"})
                                {Show(id,raw,castAge,shot.Origin,cam,left);Tail(id,raw,view.Group);}
                            }
                        }
                        else
                        {
                            cast="svash2_attack_start_"+(view.Variant%3==0?"a":view.Variant%3==1?"b":"c")+"_01";hit="svash2_attack_hit_01";
                            if(view.Variant%3==0)Show(id,"svash2_attack_start_a_02",castAge,shot.Origin,cam,left);
                        }
                        break;
                    case "D27":
                        if(empowered&&castSeen.Add((shot.SourceId,shot.StartedAt,shot.Origin))&&age<1.2)
                        {
                            // Explicit presentation adaptation: retain the complete native tail inside one 1.2s burst.
                            double sourceAge=age*OriginalCombatVfxArt.Get(Key(id,"sbell2_skill_03_start_01")).Duration/1.2;
                            Show(id,"sbell2_skill_03_start_01",sourceAge,shot.Origin,cam,left,29,ground:true);
                            PendingUntil=Math.Max(PendingUntil,shot.StartedAt+1.2);
                        }
                        hit="sbell2_skill_03_hit";break;
                    case "D28":cast="wang_attack_01_start";trail="wang_attack_01_trail";hit="wang_attack_01_hit";break;
                    case "D29":hit=shot.Healing?"common/common_heal_hit_01":"demkni_attack_01_hit";break;
                    case "D30":
                        if(empowered&&castSeen.Add((shot.SourceId,view.Group,shot.Origin)))
                            foreach(string raw in new[]{"mlynar_skill_03_buff_01","mlynar_skill_03_range_01","mlynar_skill_03_start_01"})
                            {Show(id,raw,castAge,shot.Origin,cam,left,33,ground:raw=="mlynar_skill_03_range_01"||raw=="mlynar_skill_03_buff_01");Tail(id,raw,view.Group);}
                        hit="mlynar_skill_03_hit_01";break;
                }
                if(cast!=null&&castSeen.Add((shot.SourceId,view.Group,shot.Origin)))
                {Show(id,cast,castAge,shot.Origin,cam,left);Tail(id,cast,view.Group);}
                if(trail!=null&&!shot.Resolved&&hitAge<0)
                {
                    float p=Mathf.Clamp01((float)(age/Math.Max(.0001,shot.ImpactAt-shot.StartedAt)));
                    Show(id,trail,age,Vector3.Lerp(shot.Origin,destination,p),cam,order:33,loop:true,directional:true,direction:delta);
                    PendingUntil=Math.Max(PendingUntil,shot.ImpactAt);
                }
                if(hit!=null&&shot.Applied)
                {Show(id,hit,hitAge,destination,cam,left,35);Tail(id,hit,shot.ImpactAt);}
            }
        }
        void PresentChess(ManualBattle b,Camera cam,double clock)
        {
            foreach(var cell in b.ChessPieces)
            {
                if(!chess.TryGetValue(cell,out var e))
                {e=new ChessEvent{Cell=cell,SourceId=cell.SourceId,Position=cell.Position,Created=clock,Trigger=double.NegativeInfinity};chess.Add(cell,e);}
                if(cell.Time>0)e.Trigger=cell.Time;
                Show("D28","wang_passive_01_start",clock-e.Created,cell.Position,cam,order:27,ground:true);
                if(cell.Time==0&&cell.Active)Show("D28","wang_passive_01_buff",clock,cell.Position,cam,order:27,loop:true,ground:true);
            }
            staleChess.Clear();
            foreach(var pair in chess)
            {
                var e=pair.Value;
                if(!double.IsNegativeInfinity(e.Trigger))
                {
                    // Exact original stone projectile's trigger marker. Original chess actor is drawn separately.
                    Show("D28","wang_passive_01_buff_02",clock-e.Trigger,e.Position,cam,order:32,ground:true);
                    Tail("D28","wang_passive_01_buff_02",e.Trigger);
                    if(clock-e.Trigger>4)staleChess.Add(pair.Key);
                }
                else if(!b.ChessPieces.Contains(e.Cell))staleChess.Add(pair.Key);
            }
            foreach(var cell in staleChess)chess.Remove(cell);
        }
        void PresentErasure(ManualBattle b,Camera cam,double clock)
        {
            foreach(var shot in b.CombatShots)
            {
                if(shot.FromDefender||!shot.Erased)continue;
                if(!erased.TryGetValue(shot,out var e))
                {
                    foreach(var d in b.Defenders)
                    {
                        if(d.Settings.Skill.Kind!=DefenderSkillKind.YuKitchen||
                            !ManualBattle.CrossesDefenderWall(shot.Origin,shot.Destination,b.DefenderWallPoint(d),d.WallNormal))continue;
                        float a=Vector3.Dot(shot.Origin-b.DefenderWallPoint(d),d.WallNormal),z=Vector3.Dot(shot.Destination-b.DefenderWallPoint(d),d.WallNormal);
                        float p=a/(a-z);double at=shot.StartedAt+(shot.ImpactAt-shot.StartedAt)*p;
                        if(at<d.SkillStartedAt+1.6||at>d.SkillActiveUntil)continue;
                        e=new Erasure{At=at,Position=Vector3.Lerp(shot.Origin,shot.Destination,p)};erased.Add(shot,e);break;
                    }
                }
                if(e==null)continue;
                Show("D22","yu_skill3_annex_01",clock-e.At,e.Position,cam,order:35);
                Tail("D22","yu_skill3_annex_01",e.At);
            }
        }
        void OnDestroy(){native?.Dispose();yuWall?.Dispose();}
    }
}
