using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    internal sealed class SilverAshWaveState
    {
        internal int Sequence;
        internal string SourceId,OwnerId;
        internal Vector3 Origin,Direction,Position,RangeFacing;
        internal double ActionAt,StartedAt,EndsAt,AdvancedUntil;
        internal float ActionRate,Attack,Vulnerability;
        internal bool Launched,Cancelled;
        internal readonly HashSet<string> HitTargets=new HashSet<string>();
    }
    public sealed partial class ManualBattle
    {
        // Original bird/self projectiles: FIXED_DIRECTION, speed10, lifetime .3,
        // radius .8, pass through walls, once per target; each owner emits one.
        readonly List<SilverAshWaveState> silverAshWaves=new List<SilverAshWaveState>();
        int silverAshWaveSequence;
        internal IReadOnlyList<SilverAshWaveState> SilverAshWaves=>silverAshWaves;
        void LaunchSilverAshWave(DefenderState caster,DefenderState owner,DeployedUnitState target)
        {
            var direction=target.Motion.Position-owner.Position;direction.y=0;
            if(direction.sqrMagnitude<1e-8f)direction=DefenderSkillFacing(caster,owner);else direction.Normalize();
            double release=Clock+caster.Settings.Windup/caster.LastActionRate;
            silverAshWaves.Add(new SilverAshWaveState{Sequence=++silverAshWaveSequence,SourceId=caster.EntityId,OwnerId=owner.EntityId,
                Origin=owner.Position,Position=owner.Position,Direction=direction,RangeFacing=DefenderSkillFacing(caster,owner),ActionAt=Clock,ActionRate=caster.LastActionRate,
                StartedAt=release,EndsAt=release+.3,AdvancedUntil=release,Attack=DefenderSkillAttack(caster),Vulnerability=V2DefenderSkill(caster,"vulnerability",.25f)});
        }
        static float WaveSegmentDistance(Vector3 point,Vector3 a,Vector3 b)
        {
            point.y=a.y=b.y=0;var delta=b-a;
            float t=delta.sqrMagnitude<1e-12f?0:Mathf.Clamp01(Vector3.Dot(point-a,delta)/delta.sqrMagnitude);
            return Vector3.Distance(point,a+delta*t);
        }
        void StepSilverAshWaves()
        {
            foreach(var wave in silverAshWaves)
            {
                if(wave.Cancelled||Clock+1e-7<wave.StartedAt||wave.AdvancedUntil>=wave.EndsAt)continue;
                var caster=FindDefender(wave.SourceId);var owner=FindDefender(wave.OwnerId);
                if(caster==null||owner==null){wave.Cancelled=true;continue;}
                if(!wave.Launched&&(!caster.Alive||!owner.Alive)){wave.Cancelled=true;continue;}
                wave.Launched=true;
                double end=Math.Min(Clock,wave.EndsAt);
                var previous=wave.Origin+wave.Direction*(float)((wave.AdvancedUntil-wave.StartedAt)*10);
                wave.Position=wave.Origin+wave.Direction*(float)((end-wave.StartedAt)*10);
                foreach(var unit in units)
                {
                    if(!CanDefenderTarget(unit)||wave.HitTargets.Contains(unit.EntityId)||
                        !DefenderSkillFootprints.Contains(DefenderSkillCells(caster),wave.Origin,wave.RangeFacing,unit.Motion.Position)||WaveSegmentDistance(unit.Motion.Position,previous,wave.Position)>.8f)continue;
                    wave.HitTargets.Add(unit.EntityId);
                    QueueDefenderShot(caster,new CombatShot(caster.EntityId,unit.EntityId,true,true,wave.Origin,unit.Motion.Position,Clock,0,wave.Attack,damageType:CombatDamageType.Physical)
                        {Vulnerability=wave.Vulnerability,SilverAshWaveSequence=wave.Sequence,PersistAfterSourceDeath=true},wave.ActionAt,wave.ActionRate);
                    caster.SixthHits++;
                }
                wave.AdvancedUntil=end;
            }
            silverAshWaves.RemoveAll(w=>w.Cancelled||Clock>w.EndsAt+5);
        }
    }
}
