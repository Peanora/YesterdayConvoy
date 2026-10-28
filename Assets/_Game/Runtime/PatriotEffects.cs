using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum PatriotPhase { March, Reborn, Destruction }
    public sealed class PatriotSpear
    {
        public string TargetId {get;internal set;}
        public double StartedAt {get;internal set;}
        public CombatShot Shot {get;internal set;}
        public double EndsAt=>StartedAt+3.4333334;
    }
    public sealed partial class ManualBattle
    {
        public bool IsPatriot(DeployedUnitState u)=>EngagementEnabled&&u!=null&&UnitCombat(u).Effects.Patriot;
        public bool PatriotPresent=>EngagementEnabled&&units.Any(u=>u.Active&&IsPatriot(u));
        public bool PatriotReborn(DeployedUnitState u)=>IsPatriot(u)&&u.Active&&u.PatriotPhase==PatriotPhase.Reborn;
        public bool PatriotControlImmune(DeployedUnitState u)=>IsPatriot(u)&&u.PatriotPhase!=PatriotPhase.March;
        public bool PatriotCasting(DeployedUnitState u)=>IsPatriot(u)&&u.Active&&u.Spear!=null&&Clock<u.Spear.EndsAt;
        public bool IsPatriotSpear(CombatShot s)=>units.Any(u=>u.Spear?.Shot==s);
        public float UnitResistance(DeployedUnitState u)=>Mathf.Min(100,UnitCombat(u).Effects.UnitResistance*(IsPatriot(u)&&u.PatriotPhase==PatriotPhase.March?2:1))-(u.ScorchUntil>Clock?20:0);
        float UnitAttackInterval(DeployedUnitState u)=>UnitCombat(u).UnitInterval-(IsPatriot(u)&&u.PatriotPhase==PatriotPhase.Destruction?.4f:0);
        float UnitAttackWindup(DeployedUnitState u)=>IsPatriot(u)&&u.PatriotPhase==PatriotPhase.Destruction?.6833334f:UnitCombat(u).AttackWindup;
        public bool ConfigureDefenderHighGround(string id,bool value)
        {
            if(!EngagementEnabled||Phase!=BattlePhase.Ready||units.Count!=0)return false;
            var d=FindDefender(id);if(d==null)return false;d.HighGround=value;return true;
        }
        public bool TryDisablePatriot(string id,float duration,bool freeze=false)
        {
            if(float.IsNaN(duration)||float.IsInfinity(duration)||duration<=0)throw new ArgumentException("Invalid control duration.");
            var u=FindUnit(id);
            if(Phase!=BattlePhase.Running||IsPaused||u==null||!u.Active||!IsPatriot(u)||PatriotControlImmune(u)||UnitInvulnerable(u))return false;
            u.PatriotDisabledUntil=Math.Max(u.PatriotDisabledUntil,Clock+duration);CancelUnitShots(u);return true;
        }
        void StartPatriotRebirth(DeployedUnitState u)
        {
            CancelUnitShots(u);u.PatriotPhase=PatriotPhase.Reborn;u.PatriotPhaseStartedAt=Clock;u.Health=0;
            u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;u.Spear=null;
            u.PatriotDisabledUntil=double.NegativeInfinity;u.NextImmolationAt=Clock;PulsePatriot(u);
        }
        void PulsePatriot(DeployedUnitState u)
        {
            u.NextImmolationAt=Clock+.8;u.LastImmolationAt=Clock;u.ImmolationTicks++;
            foreach(var d in defenders)
                if(d.Alive&&GroundDistance(d.Position,u.Motion.Position)<=1.75f)
                {
                    float hp=d.Health;d.Health=Mathf.Max(0,d.Health-UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*.0521f);
                    u.ImmolationDamage+=hp-d.Health;d.LastHitTime=Clock;if(!d.Alive)d.DeathTime=Clock;
                }
        }
        bool StepPatriotPassive(DeployedUnitState u)
        {
            if(u.PatriotPhase==PatriotPhase.Reborn)
            {
                if(Clock+1e-6>=u.PatriotPhaseStartedAt+60)
                {
                    u.PatriotPhase=PatriotPhase.Destruction;u.PatriotPhaseStartedAt=Clock;u.Health=u.MaxHealth;
                    u.PatriotInvulnerableUntil=Clock+15;u.NextSpearAt=Clock+15;u.NextImmolationAt=Clock+.8;
                    u.NextAttackTime=Clock;u.LastAttackTime=double.NegativeInfinity;return true;
                }
                if(Clock+1e-6>=u.NextImmolationAt)PulsePatriot(u);
                u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;return true;
            }
            if(u.PatriotPhase==PatriotPhase.Destruction&&Clock+1e-6>=u.NextImmolationAt)PulsePatriot(u);
            if(u.PatriotDisabledUntil>Clock){u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;return true;}
            return false;
        }
        bool StepPatriotAction(DeployedUnitState u)
        {
            if(PatriotCasting(u)){u.IsMoving=u.HasCombatTarget=false;return true;}
            if(u.PatriotPhase!=PatriotPhase.Destruction||Clock+1e-6<u.NextSpearAt||Clock<u.LastAttackTime+UnitActionDuration(u))return false;
            DefenderState target=null;float farthest=-1;
            foreach(var d in defenders)
            {
                float distance=GroundDistance(d.Position,u.Motion.Position);
                if(!d.Alive||!d.HighGround||DefenderCamouflaged(d)||distance>20||distance<=farthest)continue;
                target=d;farthest=distance;
            }
            if(target==null)return false;
            var shot=new CombatShot(u.EntityId,target.EntityId,false,true,u.Motion.Position,target.Position,Clock+3,.22f,UnitCombat(u).UnitAttack*UnitAttackMultiplier(u)*1.35f);
            combatShots.Add(shot);u.Spear=new PatriotSpear{TargetId=target.EntityId,StartedAt=Clock,Shot=shot};
            u.NextSpearAt=u.Spear.EndsAt+20;u.NextAttackTime=Math.Max(u.NextAttackTime,u.Spear.EndsAt);
            u.IsMoving=u.HasCombatTarget=false;return true;
        }
    }
}
