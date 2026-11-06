using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class FrostSealCast
    {
        public const float Windup=1.1f, FreezeDuration=3.3f;
        public string SourceId {get; internal set;}
        public Vector3 Position {get; internal set;}
        public double StartedAt {get; internal set;}
        public double ImpactAt=>StartedAt+Windup;
        public bool Resolved {get; internal set;}
        public bool Cancelled {get; internal set;}
        public IReadOnlyList<string> FrozenTargets=>targets.AsReadOnly();
        internal readonly List<string> targets=new List<string>();
    }

    public sealed partial class ManualBattle
    {
        readonly List<FrostSealCast> frostSeals=new List<FrostSealCast>();
        public IReadOnlyList<FrostSealCast> FrostSeals=>frostSeals.AsReadOnly();
        void StartFrostSeal(DeployedUnitState unit)
        {
            unit.FrostSealStartedAt=Clock;
            unit.IsMoving=unit.HasCombatTarget=false;
            unit.NextAttackTime=Clock+UnitCombat(unit).SkillInterval;
            frostSeals.Add(new FrostSealCast{SourceId=unit.EntityId,Position=unit.Motion.Position,StartedAt=Clock});
        }
        void ResolveFrostSeals()
        {
            foreach(var cast in frostSeals)
            {
                if(cast.Resolved)continue;
                var unit=FindUnit(cast.SourceId);
                // Being defeated interrupts this life. Losing the target never interrupts a cast.
                if(unit==null||!unit.Active||unit.IsReviving)
                {cast.Resolved=cast.Cancelled=true;continue;}
                if(Clock+1e-6<cast.ImpactAt)continue;
                cast.Resolved=true;
                var c=UnitCombat(unit);
                var targets=defenders.Where(d=>d.Alive&&GroundDistance(cast.Position,d.Position)<=c.SkillRange)
                    .OrderBy(d=>GroundDistance(cast.Position,d.Position)).ThenBy(d=>d.EntityId,StringComparer.Ordinal).ToArray();
                foreach(var d in targets)DamageDefender(d,c.SkillAttack*UnitAttackMultiplier(unit),CombatDamageType.Arts);
                foreach(var d in targets.Where(d=>d.Alive).Take(2))
                {
                    d.FrozenUntil=Math.Max(d.FrozenUntil,Clock+FrostSealCast.FreezeDuration);
                    // Preserve attack recovery while frozen; expiry cannot release a delayed attack early.
                    d.NextAttackTime=Math.Max(d.NextAttackTime,d.FrozenUntil);
                    cast.targets.Add(d.EntityId);
                }
            }
        }
    }
}
