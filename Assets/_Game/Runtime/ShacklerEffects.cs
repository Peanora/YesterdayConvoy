using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class ShackleCast
    {
        public string SourceId {get;internal set;}
        public string TargetId {get;internal set;}
        public double StartedAt {get;internal set;}
        public double EndedAt {get;internal set;}=double.PositiveInfinity;
        public bool Ended=>!double.IsPositiveInfinity(EndedAt);
        public bool Interrupted {get;internal set;}
        public int Ticks {get;internal set;}
        public float DamageDealt {get;internal set;}
    }
    public sealed partial class ManualBattle
    {
        readonly List<ShackleCast> shackles=new List<ShackleCast>();
        public IReadOnlyList<ShackleCast> Shackles=>shackles.AsReadOnly();
        public bool DefenderShackled(DefenderState d)=>d!=null&&d.Alive&&shackles.Exists(s=>s.TargetId==d.EntityId&&ShackleActive(s));
        bool ShackleActive(ShackleCast s)
        {
            var u=FindUnit(s.SourceId);var d=FindDefender(s.TargetId);
            return !s.Ended&&u!=null&&u.Active&&d!=null&&d.Alive&&Clock<s.StartedAt+20&&
                Clock>=u.PillarStunnedUntil&&Clock>=u.ShackleStunnedUntil&&Clock>=u.ShackleSilencedUntil;
        }
        void EndShackle(DeployedUnitState u,bool interrupted)
        {
            var s=u.Shackle;if(s==null||s.Ended)return;
            s.EndedAt=Clock;s.Interrupted=interrupted;u.NextAttackTime=Math.Max(u.NextAttackTime,Clock+2.0/3);
        }
        // Ability-scoped control entry, also used by the independent trial's real buttons.
        public bool TryDisableShackler(string entityId,float duration,bool silence=false)
        {
            if(float.IsNaN(duration)||float.IsInfinity(duration)||duration<=0)throw new ArgumentException("Invalid control duration");
            if(!EngagementEnabled||!CombatSettings.Effects.Shackler||Phase!=BattlePhase.Running||IsPaused)return false;
            var u=FindUnit(entityId);if(u==null||!u.Active)return false;
            if(silence)u.ShackleSilencedUntil=Math.Max(u.ShackleSilencedUntil,Clock+duration);
            else u.ShackleStunnedUntil=Math.Max(u.ShackleStunnedUntil,Clock+duration);
            EndShackle(u,true);return true;
        }
        void ResolveShackles()
        {
            foreach(var s in shackles)
            {
                if(s.Ended)continue;
                var u=FindUnit(s.SourceId);var d=FindDefender(s.TargetId);
                if(u==null||!u.Active||d==null||!d.Alive||u.PillarStunnedUntil>Clock||u.ShackleStunnedUntil>Clock||u.ShackleSilencedUntil>Clock)
                {if(u!=null)EndShackle(u,true);continue;}
                while(s.Ticks<20&&Clock+1e-6>=s.StartedAt+s.Ticks+1&&d.Alive)
                {s.Ticks++;float hp=d.Health;DamageDefender(d,CombatSettings.UnitAttack*UnitAttackMultiplier(u)*.35f,CombatDamageType.Arts);s.DamageDealt+=hp-d.Health;}
                if(!d.Alive)EndShackle(u,true);
                else if(Clock+1e-6>=s.StartedAt+20)EndShackle(u,false);
            }
        }
        bool StepShackler(DeployedUnitState u)
        {
            var current=u.Shackle;
            if(u.ShackleStunnedUntil>Clock||current!=null&&(!current.Ended||Clock<current.EndedAt+2.0/3))
            {u.IsMoving=u.HasCombatTarget=false;return true;}
            if(current!=null||u.ShackleSilencedUntil>Clock)return false;
            DefenderState target=null;float distance=float.PositiveInfinity;
            foreach(var d in defenders)
            {
                if(!d.Alive||d.NeuralUntil>Clock||d.FrozenUntil>Clock||DefenderShackled(d)||DefenderCamouflaged(d))continue;
                float gap=GroundDistance(u.Motion.Position,d.Position);
                if(gap<=3&&gap<distance){target=d;distance=gap;}
            }
            if(target==null)return false;
            var s=new ShackleCast{SourceId=u.EntityId,TargetId=target.EntityId,StartedAt=Clock};shackles.Add(s);u.Shackle=s;
            u.IsMoving=u.HasCombatTarget=false;target.BlockedCount=0;return true;
        }
    }
}
