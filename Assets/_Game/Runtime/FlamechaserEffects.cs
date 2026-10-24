using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum FlamechaserPhase { Soldier, Transforming, Ember, Reforming }
    public sealed partial class ManualBattle
    {
        public bool UnitInvulnerable(DeployedUnitState u)=>u!=null&&u.Active&&
            (u.MandraPhase==MandragoraPhase.Reborn||Clock<u.MandraInvulnerableUntil||u.FuelDying||u.FlamePhase==FlamechaserPhase.Transforming||u.FlamePhase==FlamechaserPhase.Reforming);
        public bool UnitInvisible(DeployedUnitState u)=>u!=null&&u.Active&&u.FlamePhase==FlamechaserPhase.Ember&&!u.IsBlocked&&Clock>=u.EmberRevealedUntil;
        bool CanDefenderTarget(DeployedUnitState u)=>u!=null&&u.Active&&!u.IsReviving&&!UnitInvulnerable(u)&&
            (!UnitInvisible(u)||CombatSettings.RevealsInvisible)&&(!UnitAirborne(u)||CombatSettings.DefenderCanHitAir);
        public bool NearBurningReeds(DeployedUnitState u)=>u!=null&&u.Active&&u.FlamePhase==FlamechaserPhase.Soldier&&
            facilities.Exists(f=>f.Settings.Kind==FacilityKind.Reeds&&f.Burning&&GroundDistance(u.Motion.Position,f.Settings.Position)<=1.5f);
        void CancelUnitShots(DeployedUnitState u)
        {
            foreach(var s in combatShots)if(!s.Resolved&&(s.SourceId==u.EntityId||s.TargetId==u.EntityId))s.Resolved=true;
        }
        void StartEmber(DeployedUnitState u)
        {
            CancelUnitShots(u);u.FlamePhase=FlamechaserPhase.Transforming;u.FlamePhaseStartedAt=Clock;
            u.EmberTransformations++;u.MaxHealth=u.Health=5;u.DeathTime=Clock;
            u.EmberRevealedUntil=double.NegativeInfinity;
            u.IsMoving=u.HasCombatTarget=u.IsBlocked=false;u.BlockerId=u.CombatTargetId=null;
        }
        // Returns true while this phase owns the unit's action. All transitions share the battle clock.
        bool StepFlamechaser(DeployedUnitState u,float step)
        {
            if(u.FlamePhase==FlamechaserPhase.Soldier)return false;
            if(u.MandraPhase==MandragoraPhase.Reborn||Clock<u.MandraInvulnerableUntil||u.FuelDying||u.FlamePhase==FlamechaserPhase.Transforming)
            {
                if(Clock+1e-6<u.FlamePhaseStartedAt+1)return true;
                u.FlamePhase=FlamechaserPhase.Ember;u.FlamePhaseStartedAt+=1;
            }
            if(u.FlamePhase==FlamechaserPhase.Ember)
            {
                if(Clock+1e-6>=u.FlamePhaseStartedAt+10)
                {
                    CancelUnitShots(u);u.FlamePhase=FlamechaserPhase.Reforming;u.FlamePhaseStartedAt=Clock;
                    u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;return true;
                }
                u.IsBlocked=false;u.BlockerId=null;u.HasCombatTarget=false;u.CombatTargetId=null;
                foreach(var d in defenders)
                    if(d.Alive&&d.BlockedCount<CombatSettings.BlockCount&&GroundDistance(u.Motion.Position,d.Position)<=CombatSettings.BlockRadius)
                    {u.IsBlocked=true;u.BlockerId=d.EntityId;u.EmberRevealedUntil=Clock+3;d.BlockedCount++;break;}
                u.IsMoving=!u.IsBlocked;
                if(u.IsMoving&&PillarObstructs(u,step))u.IsMoving=false;
                if(u.IsMoving)u.Motion.Advance(step);
                if(u.Motion.HasArrived)
                {u.Resolution=UnitResolution.Breached;u.IsMoving=u.IsBlocked=false;Breaches++;Defense=Math.Max(0,Defense-Settings.GateDamage);}
                return true;
            }
            if(Clock+1e-6<u.FlamePhaseStartedAt+1)return true;
            u.FlamePhase=FlamechaserPhase.Soldier;u.FlamePhaseStartedAt=Clock;u.EmberRevivals++;
            u.MaxHealth=u.Health=Settings.Health;u.DeathTime=double.PositiveInfinity;
            u.LastAttackTime=double.NegativeInfinity;u.NextAttackTime=Clock;return false;
        }
        bool ApplyUnitHealthDamage(DeployedUnitState u,float damage)
        {
            if(!u.Active||u.IsReviving||UnitInvulnerable(u)||damage<=0)return false;
            if(u.FlamePhase==FlamechaserPhase.Ember)damage=1;
            u.Health=Mathf.Max(0,u.Health-damage);u.LastHitTime=Clock;
            if(u.Health<=0)DefeatUnit(u);
            else if(CombatSettings.Effects.NervousCreeper)StepCreeperDamage(u);
            return true;
        }
        void ApplyBurn(DefenderState d,float amount)
        {
            if(!d.Alive||amount<=0||d.BurnUntil>Clock)return;
            d.BurnAccumulated+=amount;
            if(d.BurnAccumulated<100)return;
            d.BurnAccumulated=0;d.BurnUntil=Clock+10;BurnBursts++;
            DamageDefender(d,120,CombatDamageType.Arts);
        }
    }
}
