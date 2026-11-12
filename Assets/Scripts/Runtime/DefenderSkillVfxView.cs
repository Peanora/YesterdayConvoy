using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original FX consume launched attacks and existing skill/entity states. No combat writes.</summary>
    public sealed class DefenderSkillVfxView : MonoBehaviour
    {
        sealed class ShotView { public double Group,LaunchGroup; public float Rate; public int Variant; public Vector3 Impact; public bool HasImpact,Empowered,Rear,Down; }
        sealed class Group { public double Time,Launch; public int Variant; }
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
            id=="D21"||id=="D22"||id=="D23"||id=="D24"||id=="D25"||id=="D26"||id=="D27"||id=="D28"||id=="D29"||id=="D30"||id=="D32"||id=="D33"||id=="D34"||id=="D35";
        static float Distance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static string Key(string id,string raw)=>raw.StartsWith("common/",StringComparison.Ordinal)?raw:"defender/"+id+"/"+raw;
        bool Show(string id,string raw,double age,Vector3 p,Camera cam,bool flip=false,int order=32,bool loop=false,bool directional=false,Vector3 direction=default,bool ground=false,bool belowActors=false)
        {
            string key=Key(id,raw);
            // Missing/empty native control nodes are tracked as gaps, never replaced with a generic ring.
            if(!OriginalCombatVfxArt.TryGet(key,out var clip))return false;
            float mountRoll=0;Vector2 mountScale=Vector2.one;
            bool mounted=DefenderNativeMounts.RequiresMount(id,raw);
            if(mounted)
            {
                var roster=UnityEngine.Object.FindAnyObjectByType<DefenderRosterView>();var body=roster?.FindVisual(id);
                if(body==null||!DefenderNativeMounts.TryResolve(id,raw,body,out var mount,out mountRoll,out mountScale))return false;
                p=mount;flip=body.flipX;
            }
            // Direction is applied before ground-band dispatch: calibrated native clips already carry their own ground depth and actor band.
            if(directional&&clip.Directional)return native.ShowOriented(key,age,p,cam,direction,order,loop&&clip.Loop);
            if(belowActors)return native.ShowBelowActors(key,age,p,cam,flip,order,loop&&clip.Loop);
            if(ground)return native.ShowGround(key,age,p,cam,flip,order,loop&&clip.Loop);
            if(directional)flip=Vector3.Dot(direction,cam.transform.right)<0;
            return mounted?native.ShowScaled(key,age,p,cam,flip,order,mountScale,loop&&clip.Loop,mountRoll):
                native.Show(key,age,p,cam,flip,order,loop&&clip.Loop,mountRoll);
        }
        void Tail(string id,string raw,double started,float rate=1)
        {
            if(double.IsNaN(started)||double.IsInfinity(started))return;
            if(OriginalCombatVfxArt.TryGet(Key(id,raw),out var clip))PendingUntil=Math.Max(PendingUntil,started+clip.Duration/Math.Max(.0001f,rate));
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
                PresentFields(b,cam,clock);PresentShots(b,cam,clock);PresentSeventhEvents(b,cam,clock);PresentSnowContacts(b,cam,clock);PresentChess(b,cam,clock);PresentErasure(b,cam,clock);
            }
            native.End();yuWall.End();
        }
        // These captures include a one-time cold start. Replaying frame zero every
        // three seconds extinguishes all three fields; repeat only their warm span.
        internal static double PramanixFieldSampleAge(double skillAge)=>skillAge<3?skillAge:1+(skillAge-3)%2;
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
                            // Original local-XY billboard smoke extends below its foot.
                            // Upright depth lets the real terrain occlude that part.
                            Show(id,"whitw2_skill_03_buff_start",age,d.Position,cam);
                            break;
                        case "D22":Show(id,"yu_skill3_buff_01",age,d.Position,cam,loop:true,ground:true);break;
                        case "D23":
                            Show(id,"monstr_skill_03_begin_01",age,d.Settings.Position,cam,ground:true);
                            Show(id,"monstr_skill_03_begin_02",age,d.Position,cam);
                            Show(id,"monstr_skill_03_begin_03",age,d.Position,cam,loop:true,ground:true);
                            for(int i=1;i<=5;i++)Show(id,"monstr_skill_03_buff_0"+i,age,d.Position,cam,order:i==1?29:31,loop:true);
                            break;
                        case "D24":
                            Show(id,"angel2_skill_3_begin_start_001",age,d.Position,cam);
                            Show(id,"angel2_skill_3_buff_01",age,d.Position,cam,loop:true,ground:true);
                            if(d.LastReinforcementAt>=d.SkillStartedAt)
                            {
                                double deliveryAge=clock-d.LastReinforcementAt;
                                // The model records successful landing here. No unsupported post-landing flight is invented.
                                Show(id,"angel2_skill_3_begin_range_01",deliveryAge,d.Settings.RelayPosition,cam,order:34,ground:true);
                                Show(id,"angel2_skill_3_ground_01",deliveryAge,d.Settings.RelayPosition,cam,order:27,loop:true,ground:true);
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
                                Show(id,"svash2_skill_03_deploy_01",clock-d.LastReinforcementAt,partner.Position,cam,ground:true);
                                Show(id,"svash2_eagle_fly_01",age,partner.Position,cam,loop:true);
                            }
                            break;
                        case "D27":
                            double fieldAge=PramanixFieldSampleAge(age);
                            Show(id,"sbell2_skill_03_buff_01",fieldAge,d.Position,cam,order:28,loop:true,belowActors:true);
                            Show(id,"sbell2_skill_03_range",fieldAge,d.Position,cam,order:27,loop:true,belowActors:true);
                            // Approved C: one owner-bound loop, aged from the real skill start.
                            Show(id,"sbell2_skill_03_flow",fieldAge,d.Position,cam,order:29,loop:true,belowActors:true);
                            break;
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
                        case "D32":
                            bool rear=FacingRear(d);string back=rear?"_b":"";
                            foreach(string raw in new[]{"chen2_skill_03_buff_01","chen2_skill_03_buff_02","chen2_skill_03_buff_03"})Show(id,raw+back,age,d.Position,cam,loop:true);
                            break;
                        case "D33":
                            Show(id,"aglna2_skill_03_range_01",age,d.Position,cam,order:27,loop:true,belowActors:true);
                            Show(id,"aglna2_skill_03_buff_weapon_01",age,d.Position,cam,loop:true);
                            Show(id,FacingLeft(b,d,cam)?"aglna2_skill_03_buff_left_01":"aglna2_skill_03_buff_right_01",age,d.Position,cam,loop:true);
                            PresentFlightRange(b,d,cam,age);
                            if(d.SeventhMoving)
                            {
                                var flightTarget=b.FindUnit(d.SeventhInterceptTarget);Vector3 flightDirection=flightTarget!=null?flightTarget.Motion.Position-d.Position:Vector3.right;
                                Show(id,"aglna2_skill_03_fly_trail_01",clock-d.SeventhMoveStartedAt,d.Position,cam,loop:true,directional:true,direction:flightDirection);
                            }
                            break;
                        case "D34":
                            Show(id,FacingLeft(b,d,cam)?"clemnt_skill_03_buff_01_l":"clemnt_skill_03_buff_01_r",age,d.Position,cam,loop:true);
                            Show(id,"clemnt_skill_03_buff_02",age,d.Position,cam,loop:true);
                            Show(id,"clemnt_skill_03_range",age,d.Position,cam,order:27,loop:true,belowActors:true);
                            break;
                        case "D35":Show(id,"kalts2_skill_02_buff_01",age,d.Position,cam,loop:true);break;
                        case "D30":
                            Show(id,"mlynar_skill_03_buff_02",age,d.Position,cam,loop:true,ground:true);
                            Show(id,"mlynar_skill_03_buff_03",age,d.Position,cam,order:27,loop:true,ground:true);
                            break;
                    }
                }
                if(id=="D34"&&endAge>=0){Show(id,"clemnt_skill_03_range_end",endAge,d.Position,cam,order:27,belowActors:true);Tail(id,"clemnt_skill_03_range_end",d.SkillEndedAt);}
                if(id=="D23"&&d.LastSelfSkillHealAt>double.NegativeInfinity)
                {Show(id,"monstr_skill_03_heal",clock-d.LastSelfSkillHealAt,d.LastSelfSkillHealPosition,cam);Tail(id,"monstr_skill_03_heal",d.LastSelfSkillHealAt);}
                if(id=="D23"&&endAge>=0)
                {
                    var endPosition=d.LastMon3trRebirthDeathAt==d.SkillEndedAt?d.LastMon3trRebirthDeathPosition:d.Position;
                    Show(id,"monstr_skill_03_end_01",endAge,endPosition,cam);
                    Show(id,"monstr_skill_03_end_02",endAge,d.Settings.RelayPosition,cam,ground:true);
                    Tail(id,"monstr_skill_03_end_01",d.SkillEndedAt);Tail(id,"monstr_skill_03_end_02",d.SkillEndedAt);
                }
            }
            if(!running)return;
            // Consume the real accumulated layers, not the preview's fixed 1 -> 2 -> 5 staging.
            foreach(var cell in b.SnowCells)
            {
                var d=b.FindDefender(cell.SourceId);
                if(d==null||!d.Alive||d.Settings.ArtId!="D27"||cell.Layers<=0)continue;
                string raw=cell.Layers==1?"sbell2_ground_01":cell.Layers<5?"sbell2_ground_02":"sbell2_ground_03";
                Show("D27",raw,clock-cell.Time,cell.Position+Vector3.up*.025f,cam,order:26,loop:true,belowActors:true);
            }
            foreach(var drone in b.DefenderDrones)
            {
                var d=b.FindDefender(drone.SourceId);if(!b.DefenderSkillActive(d))continue;
                var target=b.FindUnit(drone.TargetId);Vector3 direction=target!=null?target.Motion.Position-drone.Position:Vector3.right;
                Show("D21","whitw2_skill_03_token_trail_01",clock-d.SkillStartedAt+drone.Index*.17,drone.Position,cam,
                    order:33,loop:true,directional:true,direction:direction);
            }
        }
        void PresentFlightRange(ManualBattle b,DefenderState d,Camera cam,double age)
        {
            const string key="defender/D33/aglna2_skill_03_range_line_01";
            if(!OriginalCombatVfxArt.TryGet(key,out _))return;
            // Source LineRenderer material/width/UV; only the geometry follows this game's authored radial range.
            const int segments=32;float radius=b.DefenderRange(d);
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,z=(i+1)*Mathf.PI*2/segments;
                Vector3 from=d.Position+new Vector3(Mathf.Cos(a)*radius,.4f,Mathf.Sin(a)*radius);
                Vector3 to=d.Position+new Vector3(Mathf.Cos(z)*radius,.4f,Mathf.Sin(z)*radius);
                native.ShowBeam(key,age,from,to,cam,1,27,true);
            }
        }
        static double ActionStart(CombatShot shot)=>double.IsNegativeInfinity(shot.DefenderActionStartedAt)?shot.StartedAt:shot.DefenderActionStartedAt;
        static bool FacingRear(DefenderState d)=>UnityEngine.Object.FindAnyObjectByType<DefenderRosterView>()?.FacingBack(d.EntityId)??false;
        static bool FacingLeft(ManualBattle b,DefenderState d,Camera cam)
        {
            var body=UnityEngine.Object.FindAnyObjectByType<DefenderRosterView>()?.FindVisual(d.EntityId);
            if(body!=null)return body.flipX;
            var target=b.FindUnit(d.TargetId);var ally=b.FindDefender(d.TargetId);
            Vector3 p=target?.Motion.Position??ally?.Position??d.Position+Vector3.right;
            return Vector3.Dot(p-d.Position,cam.transform.right)<0;
        }
        ShotView State(ManualBattle b,CombatShot shot,DefenderState d)
        {
            if(shots.TryGetValue(shot,out var result))return result;
            var key=(shot.SourceId,shot.Origin);double at=ActionStart(shot);
            if(!groups.TryGetValue(key,out var group)||Math.Abs(at-group.Time)>1e-6)
            {variants.TryGetValue(shot.SourceId,out int variant);group=new Group{Time=at,Launch=shot.StartedAt,Variant=variant};groups[key]=group;variants[shot.SourceId]=variant+1;}
            float z=shot.Destination.z-shot.Origin.z,x=shot.Destination.x-shot.Origin.x;
            result=new ShotView{Group=at,LaunchGroup=group.Launch,Rate=Math.Max(.0001f,shot.DefenderActionRate),Variant=group.Variant,Rear=z>.18f||Math.Abs(z)<=.18f&&FacingRear(d),Down=z<-.18f&&Math.Abs(z)>=Math.Abs(x),
                Empowered=shot.Empowered||(shot.DefenderSkillWasActive??(shot.StartedAt>=d.SkillStartedAt&&shot.StartedAt<d.SkillActiveUntil))};
            shots.Add(shot,result);return result;
        }
        void Cast(string id,string raw,double at,float rate,double clock,Vector3 origin,Camera cam,bool flip=false,int order=32,bool ground=false,bool below=false)
        {Show(id,raw,(clock-at)*rate,origin,cam,flip,order,ground:ground,belowActors:below);Tail(id,raw,at,rate);}
        void PresentShots(ManualBattle b,Camera cam,double clock)
        {
            foreach(var shot in b.CombatShots)
            {
                if(!shot.FromDefender||shot.Erased||clock-shot.ImpactAt>5)continue;
                var d=b.FindDefender(shot.SourceId);if(d==null||!Owns(d.Settings.ArtId))continue;
                string id=d.Settings.ArtId;var view=State(b,shot,d);if(clock<view.Group)continue;
                var unit=b.FindUnit(shot.TargetId);var ally=b.FindDefender(shot.TargetId);
                Vector3 destination=shot.Healing?ally?.Position??shot.Destination:unit?.Motion.Position??shot.Destination;
                if(shot.Applied&&!view.HasImpact){view.HasImpact=true;view.Impact=destination;}
                if(view.HasImpact)destination=view.Impact;
                double age=clock-shot.StartedAt,castAge=(clock-view.Group)*view.Rate,hitAge=clock-shot.ImpactAt;
                Vector3 delta=destination-shot.Origin;bool left=Vector3.Dot(delta,cam.transform.right)<0;
                bool first=castSeen.Add((shot.SourceId,view.Group,shot.Origin));
                string cast=null,trail=null,hit=null;bool empowered=view.Empowered;
                // Secondary area recipients are real settlement shots, not another caster action or flying projectile.
                if(shot.SeventhSecondary)
                {
                    if(id=="D34"&&!shot.TrueDamage&&shot.Applied){Show(id,"clemnt_skill_03_hit_01",hitAge,destination,cam,left,35);Tail(id,"clemnt_skill_03_hit_01",shot.ImpactAt);}
                    continue;
                }
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
                        if(shot.Healing&&empowered)
                        {
                            if(age>=0&&OriginalCombatVfxArt.TryGet(Key(id,"monstr_heal_line_02"),out _))
                            {native.ShowBeam(Key(id,"monstr_heal_line_02"),age,shot.Origin,destination,cam,1,33);Tail(id,"monstr_heal_line_02",shot.StartedAt);}
                            hit="monstr_skill_03_heal";
                        }
                        else if(empowered){cast="monstr_skill_03_start_01";hit="monstr_skill_03_hit";}
                        else {cast="monstr_attack_start_01";trail="monstr_attack_trail";hit="monstr_attack_hit_01";}
                        break;
                    case "D24":
                        cast=empowered?(view.Down?"angel2_skill_3_down_loop_start_01":"angel2_skill_3_loop_start_01"):
                            view.Down?"angel2_attack_down_loop_start_01":"angel2_attack_loop_start_01";
                        trail=empowered?"angel2_skill_3_loop_trail_01":"angel2_attack_loop_trail_01";
                        hit=empowered?"angel2_skill_3_loop_hit_01":"angel2_attack_loop_hit_01";break;
                    case "D25":
                        bool protectedPhase=d.ProtectionUntil>shot.StartedAt&&shot.StartedAt>=d.SkillActiveUntil;
                        cast=empowered?"hsgma2_skill_03_start_0"+(1+view.Variant%3+(protectedPhase?3:0)):"hsgma2_attack_01_start_01";
                        int contact=Mathf.Clamp(Mathf.RoundToInt((float)((shot.StartedAt-view.LaunchGroup)/.08)),0,3);
                        hit=!empowered?"hsgma2_attack_01_hit_01":protectedPhase?"hsgma2_skill_03_hit_02"+(contact==0?"":"_"+contact):"hsgma2_skill_03_hit_01"+(contact==0?"":"_1");break;
                    case "D26":
                        if(empowered&&first)
                        {
                            foreach(string raw in new[]{"svash2_skill_03_combat_start_01","svash2_skill_03_combat_start_01_2","svash2_skill_03_combat_start_02"})
                                Cast(id,raw,view.Group,view.Rate,clock,shot.Origin,cam,left);
                            // Original AlignedProjectile is emitted for each real S3 attack; it is not a permanent aura or generic target hit.
                            Show(id,"svash2_skill_03_range_01",(clock-view.LaunchGroup)*view.Rate,shot.Origin,cam,order:27,directional:true,direction:delta);
                            Tail(id,"svash2_skill_03_range_01",view.LaunchGroup,view.Rate);
                        }
                        else if(!empowered)
                        {
                            cast="svash2_attack_start_"+(view.Variant%3==0?"a":view.Variant%3==1?"b":"c")+"_01";hit="svash2_attack_hit_01";
                            if(first&&view.Variant%3==0)Cast(id,"svash2_attack_start_a_02",view.Group,view.Rate,clock,shot.Origin,cam,left);
                        }
                        break;
                    case "D27":
                        if(empowered&&first)Cast(id,"sbell2_skill_03_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left,29,below:true);
                        hit="sbell2_skill_03_hit";break;
                    case "D28":cast="wang_attack_01_start";trail="wang_attack_01_trail";hit="wang_attack_01_hit";break;
                    case "D29":hit=shot.Healing?"common_heal_hit_01":"demkni_attack_01_hit";break;
                    case "D30":
                        if(empowered&&first)
                        {
                            Cast(id,"mlynar_skill_03_buff_01",view.Group,view.Rate,clock,shot.Origin,cam,left,33);
                            Cast(id,"mlynar_skill_03_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left,33);
                            Cast(id,"mlynar_skill_03_range_01",view.LaunchGroup,view.Rate,clock,shot.Origin,cam,left,27,ground:true);
                        }
                        if(empowered)hit="mlynar_skill_03_hit_01";
                        // Default AnimAttack has NeverTrigger in the source. Preloaded buff04 is not invented into an ordinary attack.
                        break;
                    case "D32":
                        if(first)
                        {
                            if(empowered)
                            {
                                string back=view.Rear?"_b":"";
                                Cast(id,"chen2_skill_03_start_01"+back,view.Group,view.Rate,clock,shot.Origin,cam,left);
                                Cast(id,"chen2_skill_03_start_02"+back,view.Group,view.Rate,clock,shot.Origin,cam,left);
                                Cast(id,"chen2_skill_03_start_03",view.LaunchGroup,view.Rate,clock,shot.Origin,cam,left);
                            }
                            else
                            {
                                Cast(id,"chen2_attack_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left);
                                Cast(id,"chen2_attack_start_02",view.LaunchGroup,view.Rate,clock,shot.Origin,cam,left);
                            }
                        }
                        hit=empowered?"chen2_skill_03_hit_01":"chen2_attack_hit";break;
                    case "D33":
                        if(first)
                        {
                            if(empowered)
                            {
                                // This is the real model Attack -> Skill_3_Loop branch. Combat is not guessed from rear facing.
                                Cast(id,"aglna2_skill_03_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left);
                                Cast(id,"aglna2_skill_03_start_02",view.Group,view.Rate,clock,shot.Origin,cam,left);
                            }
                            else Cast(id,"aglna2_attack_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left);
                        }
                        trail=empowered?"aglna2_skill_03_trail_01":"aglna2_attack_trail_01";hit=empowered?"aglna2_skill_03_hit_01":"aglna2_attack_hit_01";break;
                    case "D34":
                        float dx=shot.Destination.x-shot.Origin.x,dz=shot.Destination.z-shot.Origin.z;
                        string suffix=Math.Abs(dz)>Math.Abs(dx)?dz>0&&empowered?"_up":dz<0?"_down":"":"";
                        cast=empowered?"clemnt_skill_03_start_01"+suffix:"clemnt_attack_01_start_01"+(suffix=="_down"?suffix:"");
                        hit=empowered?"clemnt_skill_03_hit_01":"clemnt_attack_01_hit_01";break;
                    case "D35":
                        // S2 medical launch/detonation is driven once by SeventhDefenderEvents; these are ordinary RangedHeal shots only.
                        if(!empowered){cast="kalts2_skill_01_start_01";trail="kalts2_skill_01_trail_01";hit="kalts2_skill_01_hit_01";}
                        break;
                }
                if(cast!=null&&first)Cast(id,cast,view.Group,view.Rate,clock,shot.Origin,cam,left);
                if(trail!=null&&age>=0&&!shot.Resolved&&hitAge<0)
                {
                    float progress=Mathf.Clamp01((float)(age/Math.Max(.0001,shot.ImpactAt-shot.StartedAt)));
                    Show(id,trail,age,Vector3.Lerp(shot.Origin,destination,progress),cam,order:33,loop:true,directional:true,direction:delta);
                    PendingUntil=Math.Max(PendingUntil,shot.ImpactAt);
                }
                if(hit!=null&&shot.Applied)
                {Show(id,hit,hitAge,destination,cam,left,35,belowActors:id=="D27");Tail(id,hit,shot.ImpactAt);}
            }
        }
        void PresentSeventhEvents(ManualBattle b,Camera cam,double clock)
        {
            foreach(var e in b.SeventhDefenderEvents)
            {
                if(e.RunId!=b.RunId)continue;var d=b.FindDefender(e.SourceId);if(d==null)continue;
                string id=d.Settings.ArtId;double age=clock-e.At;
                switch(e.Kind)
                {
                    case SeventhDefenderEventKind.StickyGround:
                        if(age<0||clock>=e.EndsAt)break;
                        Show(id,"chen2_skill_03_range_01",age,e.Position,cam,order:27,loop:true,directional:true,direction:e.Direction,belowActors:true);
                        foreach(var u in b.Units)if(u.Active&&!b.UnitAirborne(u)&&Distance(u.Motion.Position,e.Position)<=d.Settings.Skill.EffectRadius)
                            Show(id,"chen2_skill_03_debuff",age,u.Motion.Position,cam,loop:true);
                        break;
                    case SeventhDefenderEventKind.CorrosionBurst:
                        if(age<0)break;
                        if(clock<e.EndsAt)Show(id,"clemnt_skill_03_mark_all_01",age,e.Position,cam,order:28,loop:true,belowActors:true);
                        PendingUntil=Math.Max(PendingUntil,e.EndsAt);break;
                    case SeventhDefenderEventKind.HealingPulse:
                        if(id!="D35")break;
                        bool enemy=b.FindUnit(e.TargetId)!=null;
                        double actionAt=e.At-8.0/15.0;
                        if(!d.Alive&&d.DeathTime<e.At)break;
                        bool flip=Vector3.Dot(e.Position-d.Position,cam.transform.right)<0;
                        Cast(id,"folnic_skill_02_start",actionAt,1,clock,d.Position,cam,flip);
                        Cast(id,enemy?"kalts2_skill_02_start_damage_01":"kalts2_skill_02_start_heal_01",actionAt,1,clock,d.Position,cam,flip);
                        if(age>=0&&clock<e.EndsAt)
                        {
                            var target=b.FindUnit(e.TargetId);var recipient=b.FindDefender(e.TargetId);
                            Vector3 destination=target?.Motion.Position??recipient?.Position??e.Position,delta=destination-d.Position;
                            float progress=Mathf.Clamp01((float)(age/Math.Max(.0001,e.EndsAt-e.At)));
                            Show(id,enemy?"kalts2_skill_02_trail_damage_01":"kalts2_skill_02_trail_heal_01",age,Vector3.Lerp(d.Position,destination,progress),cam,order:33,directional:true,direction:delta);
                            PendingUntil=Math.Max(PendingUntil,e.EndsAt);
                        }
                        break;
                    case SeventhDefenderEventKind.MedicalDetonation:
                        bool aimedAtEnemy=b.FindUnit(e.TargetId)!=null;
                        // These names intentionally follow original projectile references, including the cross-named sub projectile effects.
                        string range=aimedAtEnemy?"kalts2_skill_02_range_damage_01":"kalts2_skill_02_range_heal_01";
                        string indicator=aimedAtEnemy?"kalts2_skill_02_indicator_damage_01":"kalts2_skill_02_indicator_heal_01";
                        string delayed=aimedAtEnemy?"kalts2_skill_02_range_heal_01_delay":"kalts2_skill_02_range_damage_01_delay";
                        Show(id,range,age,e.Position,cam,order:27,belowActors:true);
                        Show(id,indicator,age,e.Position,cam,order:29,belowActors:true);
                        Show(id,delayed,age,e.Position,cam,order:28,belowActors:true);
                        Tail(id,range,e.At);Tail(id,indicator,e.At);Tail(id,delayed,e.At);break;
                }
            }
        }
        void PresentSnowContacts(ManualBattle b,Camera camera,double clock)
        {
            foreach(var impact in b.PramanixSnowImpacts)
            {
                var caster=b.FindDefender(impact.SourceId);
                if(caster==null||caster.Settings.ArtId!="D27")continue;
                Show("D27","sbell2_buff_01_hit_01",clock-impact.At,impact.Position,camera,order:36,belowActors:true);
                Tail("D27","sbell2_buff_01_hit_01",impact.At);
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
