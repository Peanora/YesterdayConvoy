using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original FX consume launched attacks and existing skill/entity states. No combat writes.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class DefenderSkillVfxView : MonoBehaviour
    {
        sealed class ShotView { public double Group,LaunchGroup; public int Sequence; public float Rate; public int Variant; public Vector3 Impact,ProjectileOrigin; public bool HasProjectileOrigin,HasImpact,Empowered,Rear,Down; }
        sealed class Group { public double Time,Launch; public int Variant; }
        sealed class ChessEvent { public SixthCell Cell; public Vector3 Position; public double Created,Trigger; public string SourceId; }
        sealed class Erasure { public Vector3 Position; public double At; }
        Func<ManualBattle> source;
        Func<double> presentationClock;
        OriginalCombatVfxLayer native;
        YuFirewallVfx yuWall;
        OriginalNativeFxLayer original;
        struct PlacementSnapshot { internal Vector3 Position; internal Quaternion Basis; internal Vector3 Direction; internal bool Flip; }
        readonly Dictionary<string,PlacementSnapshot> placementSnapshots=new Dictionary<string,PlacementSnapshot>(StringComparer.Ordinal);
        readonly List<OriginalNativeFxPathPoint> wavePath=new List<OriginalNativeFxPathPoint>();
        ManualBattle frameBattle; double frameClock; string effectContext,effectOwner,effectRangeSource;
        Vector3 effectDirection=Vector3.right; int nextShot;
        static string Stamp(double at)=>at.ToString("R",CultureInfo.InvariantCulture);
        void Context(string token,string owner=null,Vector3 direction=default,string rangeSource=null)
        {effectContext=token;effectOwner=owner;effectRangeSource=rangeSource??owner;effectDirection=direction.sqrMagnitude>1e-8f?direction:Vector3.right;}
        readonly Dictionary<string,Vector3> facingDirections=new Dictionary<string,Vector3>(StringComparer.Ordinal);
        Vector3 Direction(ManualBattle b,DefenderState d)
        {
            if(d.Settings.ArtId=="D32"&&d.SkillCasts>0)return b.ChenDawnFacing(d);
            var u=b.FindUnit(d.TargetId);var ally=b.FindDefender(d.TargetId);
            if(u==null&&ally==null&&facingDirections.TryGetValue(d.EntityId,out var retained))return retained;
            Vector3 initial=b.Lanes.Count>0?b.Lanes[0].Entry:d.Position+Vector3.right;
            foreach(var lane in b.Lanes)if((lane.Entry-d.Position).sqrMagnitude<(initial-d.Position).sqrMagnitude)initial=lane.Entry;
            Vector3 delta=(u?.Motion.Position??ally?.Position??initial)-d.Position;
            Vector3 result=delta.sqrMagnitude>1e-8f?delta.normalized:Vector3.right;
            facingDirections[d.EntityId]=result;return result;
        }
        SpriteRenderer Body(string owner)=>UnityEngine.Object.FindAnyObjectByType<DefenderRosterView>()?.FindVisual(owner);
        bool TryPoint(string entity,string point,out Vector3 position,out float roll)
        {
            position=default;roll=0;if(string.IsNullOrEmpty(entity))return false;
            var defender=frameBattle?.FindDefender(entity);
            if(defender!=null)return DefenderNativeMounts.TryResolvePoint(defender.Settings.ArtId,point,Body(entity),out position,out roll);
            var unit=frameBattle?.FindUnit(entity);if(unit==null||point!="Hit")return false;
            foreach(var visual in UnityEngine.Object.FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None))
                if(visual.EntityId==entity)return OriginalMonsterMounts.TryHit(unit.CardId,visual.Renderer,out position);
            return false;
        }
        static string SpawnPoint(int value)
        {
            switch(value){case 2:return "Hit";case 3:case 6:return "Muzzle";case 4:return "Head";}
            int index=value>=8&&value<=11?value-8:value>=13&&value<=16?value-9:value>=18&&value<=25?value-10:-1;
            return index>=0?"SpecialPoint"+index:null;
        }
        static string FollowPoint(int value)
        {
            switch(value){case 1:return "Hit";case 2:return "Muzzle";case 3:return "Head";case 5:return "SpecialPoint0";case 6:return "SpecialPoint1";}
            return value>=8&&value<=21?"SpecialPoint"+(value-6):null;
        }
        static bool UsesOriginalSource(string id)=>id=="D21"||id=="D22"||id=="D23"||id=="D24"||id=="D25"||id=="D26"||id=="D28"||id=="D29"||id=="D30"||id=="D32"||id=="D33"||id=="D34"||id=="D35";
        bool ShowOriginal(string id,string raw,string key,double age,Vector3 position,Camera camera,bool flip,bool loop,Vector3 direction,bool ground,string ownerId,string recipientId,double stopAge)
        {
            if(!OriginalNativeFxCatalog.TryGetPlacement(key,out var source))return false;
            string owner=ownerId??effectOwner,instance=(effectContext??"unscoped")+"/"+key+(recipientId==null?"":"/recipient/"+recipientId);
            if(!string.IsNullOrEmpty(source.BackEffect)&&UnityEngine.Object.FindAnyObjectByType<DefenderRosterView>()?.FacingBack(owner)==true)
            {
                if(source.BackEffect=="common_empty")return false;
                key=Key(id,source.BackEffect.ToLowerInvariant());raw=source.BackEffect;
                if(!OriginalNativeFxCatalog.TryGetPlacement(key,out source))return false;
            }
            Vector3 aim=direction.sqrMagnitude>1e-8f?direction:effectDirection;float roll=0;Vector2 localScale=Vector2.one;
            if(DefenderSkillFootprints.IsMarkedEffect(id,raw)&&frameBattle?.FindDefender(effectRangeSource) is DefenderState rangeSource)
                aim=frameBattle.DefenderSkillFacing(rangeSource,frameBattle.FindDefender(owner)??rangeSource);
            if(source.HasParticleDriver&&source.HoldByOwner&&source.RotateType==1)
            {var ownerBody=Body(owner);if(ownerBody!=null)flip=ownerBody.flipX;}
            bool mounted=DefenderNativeMounts.RequiresMount(id,raw);
            if(mounted)
            {
                var body=Body(owner);if(body==null||!DefenderNativeMounts.TryResolve(id,raw,body,out position,out roll,out localScale))return false;
                flip=body.flipX;
            }
            else if(source.HasFollowMountPoint)
            {
                var body=Body(owner);if(body==null)return false;
                string point=FollowPoint(source.MountPoint);
                if(point==null)
                {if(source.MountPoint!=0&&source.MountPoint!=7)return false;position=body.transform.position;}
                else if(!TryPoint(owner,point,out position,out roll))return false;
                if(source.KeepOriginRotation)roll=0;
                if(source.FollowScale)return false; // A new scale-following binding needs the signed source track contract.
                mounted=true;flip=body.flipX;
            }
            else if(source.HasParticleDriver)
            {
                string point=SpawnPoint(source.SpawnLocation);
                string anchor=source.SpawnLocation==2?recipientId:owner;
                if(point!=null&&TryPoint(anchor,point,out var mount,out var angle))
                {position=mount;if(source.SpawnLocation!=6)roll=angle;}
                if(source.SpawnLocation==7)position.y=0;
                if(source.SpawnLocation==12)position=camera.transform.position;
            }
            bool groundAnchor=source.SpawnLocation==5||source.SpawnLocation==7||source.SpawnLocation==17||source.SpawnLocation==26;
            Quaternion basis=(ground||groundAnchor||source.UseBodyRotation||source.HasProjectileDriver)?Quaternion.Euler(90,0,0):camera.transform.rotation;
            if(mounted||source.SpawnLocation==3)basis=camera.transform.rotation*Quaternion.AngleAxis(roll,Vector3.forward);
            // Unheld contacts snapshot the original target point at settlement. A projectile,
            // unlike a ParticleEffect, updates its root along the real simulation path.
            if(source.HasParticleDriver&&!source.HoldByOwner)
            {
                if(!placementSnapshots.TryGetValue(instance,out var saved))
                {saved=new PlacementSnapshot{Position=position,Basis=basis,Direction=aim,Flip=flip};placementSnapshots.Add(instance,saved);}
                position=saved.Position;basis=saved.Basis;aim=saved.Direction;flip=saved.Flip;
            }
            return original.TryShow(key,instance,age,new OriginalNativeFxPose(position,basis,aim,flip,new Vector3(localScale.x,localScale.y,1)),source.HasParticleDriver?source.IsLoop:loop,stopAge);
        }
        ManualBattle observedBattle;
        int observedRun=-1;
        readonly Dictionary<CombatShot,ShotView> shots=new Dictionary<CombatShot,ShotView>();
        readonly Dictionary<(string,Vector3),Group> groups=new Dictionary<(string,Vector3),Group>();
        readonly Dictionary<string,int> variants=new Dictionary<string,int>();
        readonly HashSet<(string,double,Vector3)> castSeen=new HashSet<(string,double,Vector3)>();
        readonly Dictionary<SixthCell,ChessEvent> chess=new Dictionary<SixthCell,ChessEvent>();
        readonly Dictionary<CombatShot,Erasure> erased=new Dictionary<CombatShot,Erasure>();
        readonly List<SixthCell> staleChess=new List<SixthCell>();
        readonly Dictionary<string,double> chenReadyAt=new Dictionary<string,double>();
        public double PendingUntil {get;private set;}=double.NegativeInfinity;
        public bool EffectPending=>source?.Invoke() is ManualBattle b&&
            (b.Phase==BattlePhase.Won||b.Phase==BattlePhase.Lost)&&(presentationClock?.Invoke()??b.Clock)<PendingUntil;
        public int VisibleCount=>(native?.VisibleCount??0)+(yuWall?.VisibleCount??0)+(original?.VisibleCount??0);
        public void Bind(Func<ManualBattle> battle)=>Bind(battle,null);
        public void Bind(Func<ManualBattle> battle,Func<double> clock){source=battle;presentationClock=clock;}
        static bool Owns(string id)=>id=="D03"||id=="D05"||id=="D11"||id=="D12"||
            id=="D21"||id=="D22"||id=="D23"||id=="D24"||id=="D25"||id=="D26"||id=="D27"||id=="D28"||id=="D29"||id=="D30"||id=="D32"||id=="D33"||id=="D34"||id=="D35";
        static float Distance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static string Key(string id,string raw)=>raw.StartsWith("common/",StringComparison.Ordinal)?raw:"defender/"+id+"/"+raw;
        bool Show(string id,string raw,double age,Vector3 p,Camera cam,bool flip=false,int order=32,bool loop=false,bool directional=false,Vector3 direction=default,bool ground=false,bool belowActors=false,string ownerId=null,string recipientId=null,double stopAge=double.PositiveInfinity)
        {
            string key=Key(id,raw);
            if(UsesOriginalSource(id))return OriginalNativeFxCatalog.Supports(key)&&ShowOriginal(id,raw,key,age,p,cam,flip,loop,direction,ground||belowActors,ownerId,recipientId,stopAge);
            // Missing/empty native control nodes are tracked as gaps, never replaced with a generic ring.
            if(!OriginalCombatVfxArt.TryGet(key,out var clip))return false;
            float mountRoll=0;Vector2 mountScale=Vector2.one;
            bool mounted=DefenderNativeMounts.RequiresMount(id,raw);
            if(mounted)
            {
                var roster=UnityEngine.Object.FindAnyObjectByType<DefenderRosterView>();var body=roster?.FindVisual(ownerId??id);
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
            if(UsesOriginalSource(id))return;
            if(double.IsNaN(started)||double.IsInfinity(started))return;
            if(OriginalCombatVfxArt.TryGet(Key(id,raw),out var clip))PendingUntil=Math.Max(PendingUntil,started+clip.Duration/Math.Max(.0001f,rate));
        }
        void LateUpdate()
        {
            var b=source?.Invoke();var cam=Camera.main;
            if(native==null)native=new OriginalCombatVfxLayer(transform);
            if(yuWall==null)yuWall=new YuFirewallVfx(transform);
            if(original==null)original=new OriginalNativeFxLayer(transform);
            if(b!=observedBattle||observedRun!=(b?.RunId??-1))
            {
                native.Clear();yuWall.Clear();original.Clear();placementSnapshots.Clear();facingDirections.Clear();nextShot=0;shots.Clear();groups.Clear();variants.Clear();chess.Clear();erased.Clear();chenReadyAt.Clear();
                observedBattle=b;observedRun=b?.RunId??-1;PendingUntil=double.NegativeInfinity;
            }
            frameBattle=b;frameClock=presentationClock?.Invoke()??b?.Clock??0;
            native.Begin();yuWall.Begin(frameClock,cam);original.Begin(frameClock,cam);castSeen.Clear();
            if(b!=null&&cam!=null)
            {
                double clock=presentationClock?.Invoke()??b.Clock;
                PresentFields(b,cam,clock);PresentShots(b,cam,clock);PresentSeventhEvents(b,cam,clock);PresentSnowContacts(b,cam,clock);PresentChess(b,cam,clock);PresentChessImpacts(b,cam,clock);PresentErasure(b,cam,clock);PresentChenDawnWaves(b,cam,clock);PresentSilverAshWaves(b,cam,clock);

            }
            native.End();yuWall.End();original.End();PendingUntil=Math.Max(PendingUntil,original.PendingUntil);
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
                Context("field/"+d.EntityId+"/"+Stamp(d.SkillStartedAt),d.EntityId,Direction(b,d));
                double age=clock-d.SkillStartedAt,endAge=clock-d.SkillEndedAt;
                if(id=="D32")PresentChenDawnTalent(d,cam,clock,running);
                Context("field/"+d.EntityId+"/"+Stamp(d.SkillStartedAt),d.EntityId,Direction(b,d));
                if(running&&d.Alive&&id=="D21"&&!b.DefenderSkillActive(d))
                {
                    Context("ordinary-token/"+d.EntityId+"/"+d.SkillCasts,d.EntityId,Direction(b,d));
                    double begun=double.IsNegativeInfinity(d.SkillEndedAt)?0:d.SkillEndedAt;
                    Show(id,"whitw2_token_01",clock-begun,d.Position,cam,loop:true,ownerId:d.EntityId);
                    Context("field/"+d.EntityId+"/"+Stamp(d.SkillStartedAt),d.EntityId,Direction(b,d));
                }
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
                            Show(id,d.ProtectionUntil>b.Clock?"hsgma2_skill_03_range_02":"hsgma2_skill_03_range_01",d.ProtectionUntil>b.Clock?clock-d.SkillActiveUntil:age,d.Position,cam,order:27,loop:true,ground:true);
                            break;
                        case "D26":
                            Show(id,"svash2_skill_03_buff_01",age,d.Position,cam,loop:true,ground:true);
                            Show(id,"svash2_skill_03_buff_weapon_01",age,d.Position,cam,loop:true);
                            // Original BirdAura_Self / BirdAura each hold one infinite
                            // AttachToTarget projectile until their actual skill ends.
                            Show(id,"svash2_skill_03_range_01",age,d.Position,cam,loop:true,ground:true,ownerId:d.EntityId);
                            var partner=b.FindDefender(d.PartnerId);
                            if(partner!=null&&partner.Alive)
                            {
                                Context("bird-aura/"+d.EntityId+"/"+Stamp(d.SkillStartedAt)+"/"+partner.EntityId,partner.EntityId,b.DefenderSkillFacing(d,partner),d.EntityId);
                                Show(id,"svash2_skill_03_range_01",age,partner.Position,cam,loop:true,ground:true,ownerId:partner.EntityId);
                                Show(id,"svash2_skill_03_deploy_01",clock-d.LastReinforcementAt,partner.Position,cam,ground:true,ownerId:partner.EntityId);
                                Show(id,"svash2_eagle_fly_01",age,partner.Position,cam,loop:true,ownerId:partner.EntityId);
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
                            foreach(var u in b.Units)if(u.Active&&b.DefenderSkillContains(d,u.Motion.Position))
                                Show(id,"demkni_skill_03_buff",age,u.Motion.Position,cam,order:31,loop:true,ground:true,recipientId:u.EntityId);
                            break;
                        case "D32":
                            bool dawnLeft=FacingLeft(b,d,cam);
                            Show(id,dawnLeft?"chen3_skill_03_buff_l_01":"chen3_skill_03_buff_r_01",age,d.Position,cam,loop:true,ownerId:d.EntityId);
                            Show(id,"chen3_skill_03_weapon_01",age,d.Position,cam,loop:true,ownerId:d.EntityId);
                            Show(id,ChenDirection("chen3_skill_03_range_01",b.ChenDawnFacing(d)),age,d.Position,cam,order:27,loop:true,ground:true);
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
                Context("end/"+d.EntityId+"/"+Stamp(d.SkillEndedAt),d.EntityId,Direction(b,d));
                if(id=="D34"&&endAge>=0){Show(id,"clemnt_skill_03_range_end",endAge,d.Position,cam,order:27,belowActors:true);Tail(id,"clemnt_skill_03_range_end",d.SkillEndedAt);}
                if(id=="D23"&&d.LastSelfSkillHealAt>double.NegativeInfinity)
                {Context("self-heal/"+d.EntityId+"/"+Stamp(d.LastSelfSkillHealAt),d.EntityId);Show(id,"monstr_skill_03_heal",clock-d.LastSelfSkillHealAt,d.LastSelfSkillHealPosition,cam,recipientId:d.EntityId);Tail(id,"monstr_skill_03_heal",d.LastSelfSkillHealAt);}
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
                Context("drone/"+drone.SourceId+"/"+drone.Index+"/"+Stamp(d.SkillStartedAt),d.EntityId);
                var target=b.FindUnit(drone.TargetId);Vector3 direction=target!=null?target.Motion.Position-drone.Position:Vector3.right;
                Show("D21","whitw2_skill_03_token_trail_01",clock-d.SkillStartedAt,drone.Position,cam,
                    order:33,loop:true,directional:true,direction:direction);
            }
        }
        readonly List<Vector3> flightBoundary=new List<Vector3>();
        readonly Dictionary<Vector2Int,Vector2Int> boundaryEdges=new Dictionary<Vector2Int,Vector2Int>();
        readonly HashSet<Vector2Int> flightCells=new HashSet<Vector2Int>();
        void PresentFlightRange(ManualBattle b,DefenderState d,Camera cam,double age)
        {
            const string key="defender/D33/aglna2_skill_03_range_line_01";
            if(!OriginalNativeFxCatalog.Supports(key))return;
            // Preserve the original LineRenderer, material, width and UV animation.
            // The marked cells and actual selection share the same footprint.
            flightCells.Clear();boundaryEdges.Clear();flightBoundary.Clear();
            foreach(var cell in b.DefenderSkillCells(d))flightCells.Add(cell);
            Vector3 facing=b.DefenderSkillFacing(d),side=new Vector3(-facing.z,0,facing.x);
            foreach(var cell in flightCells)
            {
                int x=cell.x*2,y=cell.y*2;
                if(!flightCells.Contains(cell+Vector2Int.down))boundaryEdges.Add(new Vector2Int(x-1,y-1),new Vector2Int(x+1,y-1));
                if(!flightCells.Contains(cell+Vector2Int.right))boundaryEdges.Add(new Vector2Int(x+1,y-1),new Vector2Int(x+1,y+1));
                if(!flightCells.Contains(cell+Vector2Int.up))boundaryEdges.Add(new Vector2Int(x+1,y+1),new Vector2Int(x-1,y+1));
                if(!flightCells.Contains(cell+Vector2Int.left))boundaryEdges.Add(new Vector2Int(x-1,y+1),new Vector2Int(x-1,y-1));
            }
            if(boundaryEdges.Count==0)return;
            Vector2Int first=boundaryEdges.Keys.OrderBy(p=>p.x).ThenBy(p=>p.y).First(),current=first;
            for(int i=0;i<=boundaryEdges.Count;i++)
            {
                flightBoundary.Add(d.Position+facing*(current.x*.5f)+side*(current.y*.5f)+Vector3.up*.025f);
                if(!boundaryEdges.TryGetValue(current,out current)||current==first)break;
            }
            flightBoundary.Add(flightBoundary[0]);
            original.TrySetLine(key,"flight-range/"+d.EntityId+"/"+Stamp(d.SkillStartedAt),age,flightBoundary,
                new OriginalNativeFxPose(d.Position,Quaternion.Euler(90,0,0),facing),true);
        }
        static string ChenDirection(string raw,Vector3 direction)=>raw+"_dir_"+
            (Mathf.Abs(direction.x)>=Mathf.Abs(direction.z)?direction.x<0?"left":"right":direction.z>0?"up":"down");
        void PresentChenDawnTalent(DefenderState defender,Camera camera,double clock,bool running)
        {
            string owner=defender.EntityId;
            Context("talent/"+owner+"/"+Stamp(defender.ChenDawnLastTalentAt),owner,Direction(frameBattle,defender));
            if(!running||!defender.Alive){chenReadyAt.Remove(owner);return;}
            if(defender.ChenDawnDodgeReady)
            {
                if(!chenReadyAt.TryGetValue(owner,out double at)){at=defender.ChenDawnLastTalentAt;chenReadyAt.Add(owner,at);}
                Show("D32","chen3_shield_buff_01",clock-at,defender.Position,camera,loop:true,ownerId:owner);
            }
            else chenReadyAt.Remove(owner);
            if(double.IsNegativeInfinity(defender.ChenDawnLastTalentAt))return;
            // Original timer event creates start01 and heal02 together. Consuming
            // the evade buff only removes shield01; it creates no extra hit flash.
            double age=clock-defender.ChenDawnLastTalentAt;
            Show("D32","chen3_buff_start_01",age,defender.Position,camera,ownerId:owner);
            Show("D32","chen3_shield_buff_02",age,defender.Position,camera,ownerId:owner);
            Tail("D32","chen3_buff_start_01",defender.ChenDawnLastTalentAt);Tail("D32","chen3_shield_buff_02",defender.ChenDawnLastTalentAt);
        }
        void PresentChenDawnWaves(ManualBattle battle,Camera camera,double clock)
        {
            foreach(var wave in battle.ChenDawnWaves)
            {
                if(clock<wave.StartedAt||wave.History.Count==0)continue;
                var owner=battle.FindDefender(wave.SourceId);
                bool live=wave.Active&&owner!=null&&owner.Alive&&battle.Phase==BattlePhase.Running&&clock<wave.EndsAt;
                double stopped=live?double.PositiveInfinity:Math.Min(wave.EndedAt,Math.Min(wave.EndsAt,battle.Clock));
                if(owner!=null&&!owner.Alive)stopped=Math.Min(stopped,owner.DeathTime);
                string key=Key("D32","chen3_skill_03_trail_01_dir_right");
                if(!OriginalNativeFxCatalog.Supports(key))continue;
                wavePath.Clear();foreach(var point in wave.History)wavePath.Add(new OriginalNativeFxPathPoint(point.Position,point.At,point.Direction,point.Turn));
                var pose=new OriginalNativeFxPose(wave.Position,Quaternion.Euler(90,0,0),wave.Direction);
                original.TryShowPath(key,"dawn-wave/"+wave.Sequence,clock-wave.StartedAt,wavePath,pose,true,stopped-wave.StartedAt);
            }
        }
        readonly List<OriginalNativeFxPathPoint> silverPath=new List<OriginalNativeFxPathPoint>();
        void PresentSilverAshWaves(ManualBattle battle,Camera camera,double clock)
        {
            foreach(var wave in battle.SilverAshWaves)
            {
                if(wave.Cancelled||clock<wave.ActionAt)continue;
                var caster=battle.FindDefender(wave.SourceId);if(caster==null||caster.Settings.ArtId!="D26")continue;
                bool self=wave.OwnerId==wave.SourceId,left=Vector3.Dot(wave.Direction,camera.transform.right)<0;
                Context("silver-wave/"+battle.RunId+"/"+wave.Sequence,wave.OwnerId,wave.Direction,wave.SourceId);
                if(self)
                    foreach(string raw in new[]{"svash2_skill_03_combat_start_01","svash2_skill_03_combat_start_01_2","svash2_skill_03_combat_start_02"})
                        Cast("D26",raw,wave.ActionAt,wave.ActionRate,clock,wave.Origin,camera,left,ownerId:wave.OwnerId);
                else Cast("D26","svash2_skill_03_combat_start_02",wave.ActionAt,wave.ActionRate,clock,wave.Origin,camera,left,ownerId:wave.OwnerId);
                if(!wave.Launched||clock<wave.StartedAt)continue;
                string key=Key("D26",self?"svash2_skill_03_trail_02":left?"svash2_skill_03_trail_left_01":"svash2_skill_03_trail_right_01");
                double at=System.Math.Min(clock,wave.EndsAt);
                var position=wave.Origin+wave.Direction*(float)((at-wave.StartedAt)*10);
                silverPath.Clear();silverPath.Add(new OriginalNativeFxPathPoint(wave.Origin,wave.StartedAt,wave.Direction));
                silverPath.Add(new OriginalNativeFxPathPoint(position,at,wave.Direction));
                original.TryShowPath(key,"silver-wave/"+battle.RunId+"/"+wave.Sequence+"/flight",clock-wave.StartedAt,silverPath,
                    new OriginalNativeFxPose(position,Quaternion.Euler(90,0,0),wave.Direction),true,.3);
                if(clock<wave.EndsAt)PendingUntil=System.Math.Max(PendingUntil,wave.EndsAt);
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
            result=new ShotView{Sequence=++nextShot,Group=at,LaunchGroup=group.Launch,Rate=Math.Max(.0001f,shot.DefenderActionRate),Variant=group.Variant,Rear=z>.18f||Math.Abs(z)<=.18f&&FacingRear(d),Down=z<-.18f&&Math.Abs(z)>=Math.Abs(x),
                Empowered=shot.Empowered||(shot.DefenderSkillWasActive??(shot.StartedAt>=d.SkillStartedAt&&shot.StartedAt<d.SkillActiveUntil))};
            shots.Add(shot,result);return result;
        }
        void Cast(string id,string raw,double at,float rate,double clock,Vector3 origin,Camera cam,bool flip=false,int order=32,bool ground=false,bool below=false,string ownerId=null)
        {
            string prior=effectContext;effectContext="cast/"+(ownerId??effectOwner)+"/"+Stamp(at);
            double stopAge=double.PositiveInfinity;float actionRate=rate;
            if(UsesOriginalSource(id)&&OriginalNativeFxCatalog.TryGetPlacement(Key(id,raw),out var source)&&source.HasParticleDriver)
            {
                rate=source.UsePlaybackSpeed?Math.Min(rate,source.LimitedPlaybackSpeed>0?source.LimitedPlaybackSpeed:rate):1;
                var actor=frameBattle.FindDefender(ownerId??effectOwner);
                if(source.IsLoop&&actor!=null)stopAge=actor.Settings.ActionDuration/Math.Max(.0001f,actionRate)*rate;
            }
            Show(id,raw,(clock-at)*rate,origin,cam,flip,order,ground:ground,belowActors:below,ownerId:ownerId,stopAge:stopAge);
            effectContext=prior;Tail(id,raw,at,rate);
        }
        void PresentShots(ManualBattle b,Camera cam,double clock)
        {
            foreach(var shot in b.CombatShots)
            {
                if(!shot.FromDefender||shot.Erased||clock-shot.ImpactAt>5)continue;
                var d=b.FindDefender(shot.SourceId);if(d==null||!Owns(d.Settings.ArtId))continue;
                string id=d.Settings.ArtId;var view=State(b,shot,d);if(clock<view.Group)continue;
                Context("shot/"+d.EntityId+"/"+view.Sequence,d.EntityId,shot.Destination-shot.Origin);
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
                    if(id=="D32"&&shot.ChenDawnWaveSequence>0&&shot.Applied)
                    {Show(id,"chen3_skill_03_hit_04",hitAge,destination,cam,left,35,recipientId:shot.TargetId);Tail(id,"chen3_skill_03_hit_04",shot.ImpactAt);}
                    if(id=="D34"&&!shot.TrueDamage&&shot.Applied){Show(id,"clemnt_skill_03_hit_01",hitAge,destination,cam,left,35,recipientId:shot.TargetId);Tail(id,"clemnt_skill_03_hit_01",shot.ImpactAt);}
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
                        if(shot.Drone){hit=empowered?"whitw2_skill_03_token_hit_01":"whitw2_token_hit_01";}
                        else {cast=empowered?"whitw2_skill_03_start_01":"whitw2_attack_01_start";trail=empowered?"whitw2_skill_03_trail_01":"whitw2_attack_01_trail";hit="whitw2_hit_01";}
                        break;
                    case "D22":cast=empowered?"yu_skill3_attack_01":"yu_attack_01";hit=empowered?"yu_skill3_hit_01":"yu_hit_01";break;
                    case "D23":
                        if(shot.Healing&&empowered)
                        {
                            if(age>=0&&!shot.Resolved)
                            {
                                string lineKey=Key(id,"monstr_heal_line_02");
                                Vector3 from=shot.Origin,to=destination;
                                foreach(var sender in b.Defenders)if((sender.Position-shot.Origin).sqrMagnitude<1e-6f)
                                    {if(TryPoint(sender.EntityId,"Hit",out var point,out _))from=point;break;}
                                if(TryPoint(shot.TargetId,"Hit",out var end,out _))to=end;
                                original.TrySetLine(lineKey,effectContext+"/heal-line",age,new[]{from,to},
                                    new OriginalNativeFxPose(from,Quaternion.Euler(90,0,0),to-from));
                            }
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
                        if(empowered)
                        {
                            if(shot.SilverAshWaveSequence>0)hit="svash2_skill_03_hit_01";
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
                                Cast(id,"chen3_skill_03_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left,ownerId:d.EntityId);
                                Cast(id,ChenDirection("chen3_skill_03_start_02",b.ChenDawnFacing(d)),view.Group,view.Rate,clock,shot.Origin,cam);
                            }
                            else
                                Cast(id,"chen3_attack_start_01",view.Group,view.Rate,clock,shot.Origin,cam,left,ownerId:d.EntityId);
                        }
                        hit=empowered?"chen3_skill_03_hit_0"+(Mathf.Clamp(shot.ChenDawnHitIndex,0,2)+1):"chen3_attack_hit_01";break;
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
                if(cast!=null&&first)Cast(id,cast,id=="D24"?view.LaunchGroup:view.Group,view.Rate,clock,shot.Origin,cam,left);
                if(trail!=null&&age>=0&&!shot.Resolved&&hitAge<0)
                {
                    float progress=Mathf.Clamp01((float)(age/Math.Max(.0001,shot.ImpactAt-shot.StartedAt)));
                    Vector3 visualOrigin=shot.Origin;
                    if(id=="D24")
                    {
                        // RangedAttack._mountPointType=MUZZLE is explicit in both
                        // original normal and S3 emitters. Snapshot each launched
                        // bullet at that frame; target tracking keeps this game's
                        // existing destination contract, separate from HIT particles.
                        if(!view.HasProjectileOrigin)
                        {
                            view.ProjectileOrigin=TryPoint(d.EntityId,"Muzzle",out var muzzle,out _)?muzzle:shot.Origin;
                            view.HasProjectileOrigin=true;
                        }
                        visualOrigin=view.ProjectileOrigin;
                    }
                    Show(id,trail,age,Vector3.Lerp(visualOrigin,destination,progress),cam,order:33,loop:true,directional:true,direction:destination-visualOrigin);
                    PendingUntil=Math.Max(PendingUntil,shot.ImpactAt);
                }
                if(hit!=null&&shot.Applied)
                {Show(id,hit,hitAge,destination,cam,left,35,belowActors:id=="D27",recipientId:shot.TargetId);Tail(id,hit,shot.ImpactAt);}
            }
        }
        void PresentSeventhEvents(ManualBattle b,Camera cam,double clock)
        {
            foreach(var e in b.SeventhDefenderEvents)
            {
                if(e.RunId!=b.RunId)continue;var d=b.FindDefender(e.SourceId);if(d==null)continue;
                string id=d.Settings.ArtId;double age=clock-e.At;
                Context("event/"+e.SourceId+"/"+e.Kind+"/"+Stamp(e.At)+"/"+e.TargetId,d.EntityId,e.Position-d.Position);
                switch(e.Kind)
                {
                    case SeventhDefenderEventKind.StickyGround:
                        // The misidentified water-Chen trial has been replaced.
                        // Never reuse its water/slime captures for Dawnstreak.
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
                        Show(id,range,age,e.Position,cam,order:27,ground:true);
                        Show(id,indicator,age,e.Position,cam,order:29,ground:true);
                        Show(id,delayed,age,e.Position,cam,order:28,ground:true);
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
                {e=new ChessEvent{Cell=cell,SourceId=cell.SourceId,Position=cell.Position,Created=cell.CreatedAt,Trigger=double.NegativeInfinity};chess.Add(cell,e);}
                if(cell.Time>0)e.Trigger=cell.Time;
                Context("chess/"+e.SourceId+"/"+Stamp(e.Created)+"/"+e.Position,e.SourceId);
                if(cell.Time>0||clock<cell.CreatedAt)continue;
                if(!cell.VirtualStone)
                {
                    // Real token owns its original birth loop throughout its life.
                    Show("D28","token_wang_birth_01_buff",clock-cell.CreatedAt,cell.Position,cam,order:27,loop:true,ground:true);
                    if(cell.Active&&clock>=cell.ActivatedAt)Show("D28","token_wang_01_buff",clock-cell.ActivatedAt,cell.Position,cam,order:27,loop:true,ground:true);
                }
                else if(clock>=cell.AvailableAt)
                    Show("D28","wang_passive_01_buff",clock-cell.AvailableAt,cell.Position,cam,order:27,loop:true,ground:true);
            }
            staleChess.Clear();
            foreach(var pair in chess)
            {
                var e=pair.Value;Context("chess-trigger/"+e.SourceId+"/"+Stamp(e.Trigger)+"/"+e.Position,e.SourceId);
                if(!double.IsNegativeInfinity(e.Trigger))
                {
                    // Exact original stone projectile's trigger marker. Original chess actor is drawn separately.
                    var caster=b.FindDefender(e.SourceId);bool skill=caster!=null&&e.Trigger>=caster.SkillStartedAt&&e.Trigger<caster.SkillActiveUntil;
                    if(skill)Show("D28","token_wang_skill_03_hit",clock-e.Trigger,e.Position,cam,order:32,ground:true);
                    if(e.Cell.VirtualStone||!skill)Show("D28","wang_passive_01_buff_02",clock-e.Trigger,e.Position,cam,order:32,ground:true);
                    if(clock-e.Trigger>4)staleChess.Add(pair.Key);
                }
                else if(!b.ChessPieces.Contains(e.Cell))staleChess.Add(pair.Key);
            }
            foreach(var cell in staleChess)chess.Remove(cell);
        }
        void PresentChessImpacts(ManualBattle battle,Camera camera,double clock)
        {
            foreach(var impact in battle.WangChessImpacts)
            {
                if(impact.RunId!=battle.RunId||!impact.Empowered||clock<impact.At)continue;
                Context("chess-impact/"+impact.Sequence,impact.SourceId);
                Show("D28","token_wang_skill_03_hit_02",clock-impact.At,impact.Position,camera,order:35,recipientId:impact.TargetId);
            }
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
                Context("erasure/"+Stamp(e.At)+"/"+e.Position);
                Show("D22","yu_skill3_annex_01",clock-e.At,e.Position,cam,order:35);
                Tail("D22","yu_skill3_annex_01",e.At);
            }
        }
        void OnDestroy(){native?.Dispose();yuWall?.Dispose();original?.Dispose();}
    }
}
