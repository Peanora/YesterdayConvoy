using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class FlameSpray
    {
        public string SourceId {get;internal set;}
        public string TargetId {get;internal set;}
        public string Direction {get;internal set;}
        public bool FlipX {get;internal set;}
        public double StartedAt {get;internal set;}
        public double EndedAt {get;internal set;}=double.PositiveInfinity;
        public bool Ended=>!double.IsPositiveInfinity(EndedAt);
        public bool Interrupted {get;internal set;}
        public int Ticks {get;internal set;}
        public float DamageDealt {get;internal set;}
        internal double NextTick;
    }
    public sealed class FuelExplosion
    {
        public string SourceId {get;internal set;}
        public Vector3 Position {get;internal set;}
        public double DetonateAt {get;internal set;}
        public bool Resolved {get;internal set;}
        public int GuardsHit {get;internal set;}
        public int ChainHits {get;internal set;}
    }
    public sealed partial class ManualBattle
    {
        readonly List<FlameSpray> flameSprays=new List<FlameSpray>();
        readonly List<FuelExplosion> fuelExplosions=new List<FuelExplosion>();
        public IReadOnlyList<FlameSpray> FlameSprays=>flameSprays.AsReadOnly();
        public IReadOnlyList<FuelExplosion> FuelExplosions=>fuelExplosions.AsReadOnly();

        // A control entry for this ability, rather than a claim of a general status system.
        public bool TryDisableFlamerazer(string entityId,float duration,bool silence=false)
        {
            if(float.IsNaN(duration)||float.IsInfinity(duration)||duration<=0)throw new ArgumentException("Invalid control duration");
            if(!EngagementEnabled||!CombatSettings.Effects.Flamerazer||Phase!=BattlePhase.Running||IsPaused)return false;
            var u=FindUnit(entityId);if(u==null||!u.Active||u.FuelDying)return false;
            if(silence)u.SpraySilencedUntil=Math.Max(u.SpraySilencedUntil,Clock+duration);
            else u.SprayStunnedUntil=Math.Max(u.SprayStunnedUntil,Clock+duration);
            EndSpray(u,true);return true;
        }
        void EndSpray(DeployedUnitState u,bool interrupted)
        {
            var s=u.Spray;if(s==null||s.Ended)return;
            s.EndedAt=Clock;s.Interrupted=interrupted;
            // The first six seconds continuously clear the skill's cooldown.
            u.NextSprayAt=Clock<s.StartedAt+6?Clock:s.StartedAt+16;
            u.NextAttackTime=Math.Max(u.NextAttackTime,Clock+.5333334);
        }
        bool StepFlamerazer(DeployedUnitState u)
        {
            bool stunned=u.SprayStunnedUntil>Clock,silenced=u.SpraySilencedUntil>Clock;
            var s=u.Spray;
            if(s!=null&&!s.Ended)
            {
                var target=FindDefender(s.TargetId);
                if(stunned||silenced||target==null||!target.Alive||GroundDistance(u.Motion.Position,target.Position)>CombatSettings.UnitRange)
                    EndSpray(u,true);
                else
                {
                    while(s.NextTick<=s.StartedAt+10.6+1e-6&&Clock+1e-6>=s.NextTick&&target.Alive)
                    {
                        float hp=target.Health;
                        DamageDefender(target,CombatSettings.UnitAttack*.12f,CombatDamageType.Arts);
                        ApplyBurn(target,CombatSettings.UnitAttack*.06f);
                        s.DamageDealt+=hp-target.Health;s.Ticks++;s.NextTick+=.5;
                    }
                    if(Clock+1e-6>=s.StartedAt+10.6||!target.Alive)EndSpray(u,!target.Alive);
                }
                u.IsMoving=u.HasCombatTarget=false;return true;
            }
            if(stunned||s!=null&&Clock<s.EndedAt+.5333334)
            {u.IsMoving=u.HasCombatTarget=false;return true;}
            if(silenced||Clock+1e-6<u.NextSprayAt||Clock<u.LastAttackTime+CombatSettings.AttackDuration)return false;
            var d=ChooseDefender(u,CombatSettings.UnitRange);if(d==null)return false;
            var delta=d.Position-u.Motion.Position;
            u.Spray=new FlameSpray{SourceId=u.EntityId,TargetId=d.EntityId,StartedAt=Clock,NextTick=Clock+.5,
                Direction=Math.Abs(delta.z)>Math.Abs(delta.x)?(delta.z>0?"up":"down"):"front",FlipX=delta.x<0};
            flameSprays.Add(u.Spray);u.IsMoving=u.HasCombatTarget=false;return true;
        }
        void StartFuelExplosion(DeployedUnitState u)
        {
            if(u.FuelDying)return;
            EndSpray(u,true);CancelUnitShots(u);
            u.FuelDying=true;u.Health=1;u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;
            u.BlockerId=u.CombatTargetId=null;
            fuelExplosions.Add(new FuelExplosion{SourceId=u.EntityId,Position=u.Motion.Position,DetonateAt=Clock+.1});
        }
        void ResolveFuelExplosions()
        {
            // New chain detonations may append while this list is being resolved.
            for(int i=0;i<fuelExplosions.Count;i++)
            {
                var e=fuelExplosions[i];if(e.Resolved||Clock+1e-6<e.DetonateAt)continue;
                e.Resolved=true;var source=FindUnit(e.SourceId);
                foreach(var d in defenders)
                    if(d.Alive&&GroundDistance(d.Position,e.Position)<=CombatSettings.UnitRange)
                    {DamageDefender(d,CombatSettings.UnitAttack,CombatDamageType.Arts);e.GuardsHit++;}
                foreach(var other in units)
                    if(other!=source&&other.Active&&!other.FuelDying&&other.UnitId==source.UnitId&&GroundDistance(other.Motion.Position,e.Position)<=CombatSettings.UnitRange)
                    {TrueDamage(other,CombatSettings.UnitAttack*100);e.ChainHits++;}
                foreach(var f in facilities)
                    if(f.Settings.Kind==FacilityKind.Pillar&&!f.Collapsed&&Mathf.Abs(Cell(f.Settings.Position).x-Cell(e.Position).x)+Mathf.Abs(Cell(f.Settings.Position).y-Cell(e.Position).y)<=1)
                        CollapsePillar(f,e.Position);
                source.Health=0;source.Resolution=UnitResolution.Dead;source.DeathTime=Clock;
            }
        }
    }
}
