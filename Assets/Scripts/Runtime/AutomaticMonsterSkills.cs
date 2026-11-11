using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class AutomaticMonsterCast
    {
        public string EntityId,CardId,SkillId;public double At;
    }
    public sealed partial class ManualBattle
    {
        sealed class AutomaticMonsterState
        {
            internal int Phase,Region;internal bool HalfReset;
            internal double LastAt,ChargeBlockedUntil;internal long LastAttack=long.MinValue;
            internal readonly Dictionary<string,double> ReadyAt=new Dictionary<string,double>(StringComparer.Ordinal);
            internal readonly Dictionary<string,float> Charge=new Dictionary<string,float>(StringComparer.Ordinal);
            internal readonly Dictionary<string,int> Uses=new Dictionary<string,int>(StringComparer.Ordinal);
        }
        readonly Dictionary<string,AutomaticMonsterState> automaticMonsterStates=new Dictionary<string,AutomaticMonsterState>();
        readonly List<AutomaticMonsterCast> automaticMonsterCasts=new List<AutomaticMonsterCast>();
        public bool AutomaticSkillsEnabled {get;private set;}
        public IReadOnlyList<AutomaticMonsterCast> AutomaticMonsterCasts=>automaticMonsterCasts.AsReadOnly();
        // Production enables the scheduler for every mode. Low-level command fixtures
        // may still isolate an individual ability without running unrelated AI casts.
        public void EnableAutomaticSkills()
        {
            if(AutomaticSkillsEnabled)return;AutomaticSkillsEnabled=true;
            foreach(var u in units)InitializeAutomaticLegacyMonsterSkill(u);
        }
        void ResetAutomaticMonsterSkills()
        {automaticMonsterStates.Clear();automaticMonsterCasts.Clear();ResetAutomaticLegacyMonsterSkills();}
        void RecordAutomaticMonsterCast(DeployedUnitState u,string skill)
        {if(AutomaticSkillsEnabled&&u!=null)automaticMonsterCasts.Add(new AutomaticMonsterCast{EntityId=u.EntityId,CardId=u.CardId,SkillId=skill,At=Clock});}
        int AutomaticMonsterPhase(DeployedUnitState u)=>BossRoster(u)?.Phase??1;
        int AutomaticMonsterRegion(DeployedUnitState u)=>BossRoster(u)?.Region??1;
        AutomaticMonsterState AutomaticState(DeployedUnitState u)
        {
            int phase=AutomaticMonsterPhase(u),region=AutomaticMonsterRegion(u);
            if(!automaticMonsterStates.TryGetValue(u.EntityId,out var state))
            {state=new AutomaticMonsterState();automaticMonsterStates.Add(u.EntityId,state);}
            if(state.Phase!=phase||state.Region!=region)
            {
                // Saint's rite explicitly keeps counting in both ground/flying modes.
                double retainedRite=state.ReadyAt.TryGetValue("rite",out var priorRite)?priorRite:double.NaN;
                double origin=state.Phase==0?u.SpawnTime:Clock;state.Phase=phase;state.Region=region;state.LastAt=origin;
                state.ReadyAt.Clear();state.Charge.Clear();state.Uses.Clear();
                foreach(var timing in AutomaticMonsterTimings.For(u.CardId,phase,region))
                {
                    state.ReadyAt[timing.Id]=u.CardId=="CARD_B13"&&timing.Id=="rite"&&!double.IsNaN(retainedRite)?retainedRite:origin+timing.Initial;
                    state.Charge[timing.Id]=timing.InitialSP;state.Uses[timing.Id]=0;
                }
                // The selected native second-form entry forces one Supervision cast;
                // its regular source 10s initial field is not that entry action.
                if(u.CardId=="CARD_B11"&&phase>=2&&state.ReadyAt.ContainsKey("edict"))state.ReadyAt["edict"]=origin;
            }
            if(u.CardId=="CARD_B01"&&!state.HalfReset&&u.Health<=u.MaxHealth*.5f)
            {state.HalfReset=true;state.ReadyAt["c4"]=Clock;}
            return state;
        }
        void OnAutomaticMonsterSkillStarted(DeployedUnitState u,double endsAt)
        {
            if(!AutomaticSkillsEnabled)return;var state=AutomaticState(u);
            state.ChargeBlockedUntil=Math.Max(state.ChargeBlockedUntil,endsAt);
        }
        void OnAutomaticMonsterOrdinaryAttack(DeployedUnitState u,long activation)
        {
            if(!AutomaticSkillsEnabled)return;var state=AutomaticState(u);if(state.LastAttack==activation)return;state.LastAttack=activation;
            foreach(var timing in AutomaticMonsterTimings.For(u.CardId,state.Phase,state.Region))
                if(timing.AttackSP>0)state.Charge[timing.Id]=Math.Min(timing.AttackSP,state.Charge[timing.Id]+1);
        }
        void AdvanceAutomaticMonsterCharge(DeployedUnitState u,AutomaticMonsterState state,IReadOnlyList<AutomaticMonsterTiming> timings)
        {
            double from=state.LastAt;state.LastAt=Clock;var boss=BossRoster(u);
            foreach(var timing in timings)
            {
                if(timing.SPCost<=0||timing.AttackSP>0)continue;
                double elapsed=Math.Max(0,Clock-Math.Max(from,timing.StartsAfterAction?state.ChargeBlockedUntil:from));
                if(boss.Transitioning||u.CardId=="CARD_B21"&&boss.Present)elapsed=0;
                float rate=timing.RecoveryRate+(u.CardId=="CARD_B21"&&BossLiving().Count()>=6?1.5f:0);
                state.Charge[timing.Id]=Math.Min(timing.SPCost,state.Charge[timing.Id]+(float)elapsed*rate);
            }
        }
        bool AutomaticMonsterCondition(DeployedUnitState u,AutomaticMonsterTiming timing)
        {
            var s=BossRoster(u);
            if(timing.Blocked&&!u.IsBlocked)return false;
            if(timing.Range>0&&!AutomaticLegacyHasTarget(u,timing.Range))return false;
            switch(timing.Condition)
            {
                case AutomaticSkillCondition.GlobalTarget:return BossLiving().Any();
                case AutomaticSkillCondition.InAttackRange:return AutomaticLegacyHasTarget(u,UnitCombat(u).UnitRange);
                case AutomaticSkillCondition.FlamechaserAlly:return BossFriends(u).Any(x=>UnitCombat(x).Effects.Flamechaser&&x.FlamePhase==FlamechaserPhase.Soldier);
                case AutomaticSkillCondition.BurningTarget:return BossLiving().Any(d=>BossInObject("fire",d.Position));
                case AutomaticSkillCondition.ActiveCamera:return bossRosterObjects.Any(o=>o.OwnerId==u.EntityId&&o.Kind=="camera"&&o.Alive&&o.Shield<=0);
                case AutomaticSkillCondition.CameraTarget:return BossLiving().Any(d=>GroundDistance(d.Position,u.Motion.Position)<=UnitCombat(u).UnitRange&&bossRosterObjects.Any(o=>o.Alive&&o.OwnerId==u.EntityId&&o.Kind=="camera"&&o.Shield<=0&&GroundDistance(d.Position,o.Position)<=o.Radius));
                case AutomaticSkillCondition.CannonAvailable:return bossRosterObjects.Any(o=>o.OwnerId==u.EntityId&&o.Kind=="cannon"&&o.Alive&&GroundDistance(o.Position,u.Motion.Position)<=UnitCombat(u).UnitRange);
                case AutomaticSkillCondition.BlackCrownCharge:return s.BlackCrownCharges>0&&BossLiving().Any();
                case AutomaticSkillCondition.AwayModeTarget:return !s.Present&&BossLiving().Any();
                default:return true;
            }
        }
        float AutomaticMonsterApproachRange(DeployedUnitState u,float ordinaryRange)
        {
            if(!AutomaticSkillsEnabled||!automaticMonsterStates.TryGetValue(u.EntityId,out var state))return ordinaryRange;
            float result=ordinaryRange;
            foreach(var timing in AutomaticMonsterTimings.For(u.CardId,state.Phase,state.Region))
            {
                if(timing.MaximumCasts>=0&&state.Uses[timing.Id]>=timing.MaximumCasts)continue;
                float required=timing.AttackSP>0?timing.AttackSP:timing.SPCost;
                if(required>0?state.Charge[timing.Id]+.00001f<required:Clock+1e-6<state.ReadyAt[timing.Id])continue;
                if(timing.Blocked&&!u.IsBlocked)
                {
                    var blocker=defenders.Where(d=>d.Alive&&!d.HighGround&&d.Settings.BlockCount>d.BlockedCount&&UnitCanReachDefender(u,d)&&!DefenderCamouflaged(d))
                        .OrderBy(d=>GroundDistance(d.Position,u.Motion.Position)).FirstOrDefault();
                    if(blocker!=null&&GroundDistance(blocker.Position,u.Motion.Position)<=ordinaryRange+.1f)result=Math.Min(result,Math.Max(.1f,blocker.Settings.BlockRadius-.01f));
                }
                else if(timing.Range>0&&timing.Range<result&&AutomaticLegacyHasTarget(u,ordinaryRange))result=timing.Range;
            }
            return result;
        }
        bool TryAutomaticMonsterSkill(DeployedUnitState u)
        {
            if(!AutomaticSkillsEnabled||!u.Active||u.IsReviving||IsPaused||Phase!=BattlePhase.Running||BossSleep(u))return false;
            if(TryAutomaticLegacyMonsterSkill(u))return true;
            var timings=AutomaticMonsterTimings.For(u.CardId,AutomaticMonsterPhase(u),AutomaticMonsterRegion(u));if(timings.Count==0)return false;
            var state=AutomaticState(u);var boss=BossRoster(u);AdvanceAutomaticMonsterCharge(u,state,timings);
            foreach(var timing in timings.OrderByDescending(t=>t.Priority).ThenBy(t=>state.ReadyAt[t.Id]))
            {
                float required=timing.AttackSP>0?timing.AttackSP:timing.SPCost;
                if(timing.MaximumCasts>=0&&state.Uses[timing.Id]>=timing.MaximumCasts)continue;
                if(required>0?state.Charge[timing.Id]+.00001f<required:Clock+1e-6<state.ReadyAt[timing.Id])continue;
                if(!AutomaticMonsterCondition(u,timing))continue;
                var skill=BossRosterRules.Skills(u.CardId).FirstOrDefault(s=>s.Id==timing.Id);
                if(skill==null||boss==null||CheckBossRosterSkillCore(u,boss,skill,true,true)!=SkillFailure.None)continue;
                boss.SP[skill.Id]=0;u.SkillsUsed++;SkillCasts++;state.Uses[timing.Id]++;state.Charge[timing.Id]=0;
                if(u.CardId=="CARD_B21"&&skill.Id=="insomnia")boss.Consumed.Add("firstAppear");
                BossCast(u,skill,true);state.ReadyAt[timing.Id]=Clock+timing.Interval;
                RecordAutomaticMonsterCast(u,timing.Id);return true;
            }
            return false;
        }
        public string AutomaticSkillStatus(DeployedUnitState u,string skillId=null)
        {
            if(u==null||!u.Active)return "已退场";if(IsPaused)return "暂停中";
            string legacy=AutomaticLegacySkillStatus(u);if(legacy!=null)return legacy;
            var boss=BossRoster(u);if(boss==null)return "天赋按战斗条件自动触发";
            if(boss.Transitioning)return "阶段转换中";
            if(Clock<boss.LockedUntil)return "动作进行中，结束后自动判断";
            var timings=AutomaticMonsterTimings.For(u.CardId,boss.Phase,boss.Region);
            var matches=timings.Where(t=>skillId==null||t.Id==skillId).ToArray();
            if(matches.Length==0)return "按血量、阶段或场上条件自动触发";
            var timing=matches[0];automaticMonsterStates.TryGetValue(u.EntityId,out var state);
            if(state!=null&&state.Phase==boss.Phase&&state.Region==boss.Region)
            {
                if(timing.MaximumCasts>=0&&state.Uses.TryGetValue(timing.Id,out var used)&&used>=timing.MaximumCasts)return "本区域已完成，等待下一阶段";
                if(timing.SPCost>0)
                {
                    float charge=state.Charge.TryGetValue(timing.Id,out var sp)?sp:timing.InitialSP;
                    if(charge+.00001f<timing.SPCost)return (timing.AttackSP>0?"攻击回复 ":"技力 ")+charge.ToString("0.0")+"/"+timing.SPCost.ToString("0");
                }
                else if(state.ReadyAt.TryGetValue(timing.Id,out var at)&&at>Clock+.00001)return "自动冷却 "+(at-Clock).ToString("0.0")+"秒";
            }
            else if(timing.SPCost>0)return (timing.AttackSP>0?"攻击回复 ":"技力 ")+timing.InitialSP.ToString("0.0")+"/"+timing.SPCost.ToString("0");
            else if(u.SpawnTime+timing.Initial>Clock)return "自动冷却 "+(u.SpawnTime+timing.Initial-Clock).ToString("0.0")+"秒";
            return AutomaticMonsterCondition(u,timing)?"条件满足后自动施放":"已就绪，等待触发条件";
        }
        public float AutomaticSkillChargeRatio(DeployedUnitState u)
        {
            if(u==null||!u.Active)return 0;
            if(ViviCombat.IsVivi(u.CardId))
            {if(coreViviSkill=="s3")return Mathf.Clamp01(u.SkillSP/50);return bossCoreStates.TryGetValue(u.EntityId,out var core)?Mathf.Clamp01(core.SP/(coreViviSkill=="s1"?2:25)):coreViviSkill=="s2"?.6f:0;}
            var boss=BossRoster(u);
            if(boss==null)
            {var c=UnitCombat(u);if(u.CardId=="CARD_TALULAH_TRIAL"||u.CardId=="CARD_M14")return 0;return c.HasSkill?Mathf.Clamp01(u.SkillSP/Math.Max(1,LegacyAutomaticRequiredSP(u))):0;}
            var timings=AutomaticMonsterTimings.For(u.CardId,boss.Phase,boss.Region);if(timings.Count==0)return 0;var timing=timings[0];
            if(!automaticMonsterStates.TryGetValue(u.EntityId,out var state)||state.Phase!=boss.Phase||state.Region!=boss.Region)return timing.SPCost>0?Mathf.Clamp01(timing.InitialSP/timing.SPCost):0;
            if(timing.SPCost>0)return state.Charge.TryGetValue(timing.Id,out var charge)?Mathf.Clamp01(charge/timing.SPCost):0;
            if(!state.ReadyAt.TryGetValue(timing.Id,out var at))return 0;float duration=state.Uses.TryGetValue(timing.Id,out var used)&&used>0?timing.Interval:timing.Initial;
            return duration>0?Mathf.Clamp01(1-(float)(at-Clock)/duration):1;
        }
        bool AutomaticSaintReturn(DeployedUnitState u,BossRosterState s)
        {
            if(!AutomaticSkillsEnabled||u.CardId!="CARD_B13"||s.Phase<2)return false;
            bool flying=s.Phase==3;string key=flying?"returnGround":"returnFlying";
            if(Count(s,key)>=2)return false;s.Counts[key]=Count(s,key)+1;s.Generation++;s.Transitioning=true;
            u.Health=1;u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;CancelUnitShots(u);
            double wait=flying?.9666667:1.1666667,entry=flying?1.5833333:1.65;int next=flying?2:3;
            string death=flying?"B_Die_2":"B_Die_1",birth=flying?"B_Start_1":"B_Start_2";
            BossSetAnimation(u,death,false,wait);s.LockedUntil=Clock+wait+entry;
            RecordAutomaticMonsterCast(u,"sacrament");BossEmit(u,"SkillStart","sacrament",u.Motion.Position,animation:death,end:s.LockedUntil);
            // Original waits for two external waves. This route mode has no such waves:
            // preserve the proven minimum wait, exact entry and finite two returns per mode.
            BossAfter(u,wait,()=>{s.Phase=next;BossSetAnimation(u,birth,false,entry);});
            BossAfter(u,wait+entry,()=>{s.Transitioning=false;u.Health=u.MaxHealth;u.DeathTime=double.PositiveInfinity;u.NextAttackTime=Clock;s.LockedUntil=Clock;BossSetAnimation(u,BossIdle(u,s),true);BossEmit(u,"SkillEnd","sacrament",u.Motion.Position,animation:s.Animation);});
            return true;
        }
    }
}
