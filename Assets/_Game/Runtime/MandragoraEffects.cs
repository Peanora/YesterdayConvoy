using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum MandragoraPhase { Ground, Reborn, Second }
    public sealed class PetrifyingGaze
    {
        public string SourceId {get;internal set;}
        public string TargetId {get;internal set;}
        public double StartedAt {get;internal set;}
        public double EndedAt {get;internal set;}=double.PositiveInfinity;
        public bool Ended=>!double.IsPositiveInfinity(EndedAt);
        public bool Interrupted {get;internal set;}
        public int Ticks {get;internal set;}
        public float DamageDealt {get;internal set;}
        internal double NextTick;
    }
    public sealed class PillarCommand
    {
        public string SourceId {get;internal set;}
        public FacilityState Pillar {get;internal set;}
        public Vector3 Direction {get;internal set;}
        public double StartedAt {get;internal set;}
        public double DetonateAt=>StartedAt+5;
        public bool Resolved {get;internal set;}
        public bool Applied {get;internal set;}
    }
    public sealed partial class ManualBattle
    {
        readonly List<PetrifyingGaze> gazes=new List<PetrifyingGaze>();
        readonly List<PillarCommand> pillarCommands=new List<PillarCommand>();
        public IReadOnlyList<PetrifyingGaze> Gazes=>gazes.AsReadOnly();
        public IReadOnlyList<PillarCommand> PillarCommands=>pillarCommands.AsReadOnly();
        public bool UnitAirborne(DeployedUnitState u)=>EngagementEnabled&&CombatSettings.Effects.Mandragora&&u!=null&&u.Active&&u.MandraPhase==MandragoraPhase.Second&&u.StoneShield;
        bool HasGazeSlow(DefenderState d)=>gazes.Exists(g=>!g.Ended&&g.TargetId==d.EntityId);
        void EndGaze(DeployedUnitState u,bool interrupted)
        {
            var g=u.Gaze;if(g==null||g.Ended)return;
            var d=FindDefender(g.TargetId);g.EndedAt=Clock;g.Interrupted=interrupted;
            u.NextGazeAt=Clock+15;u.NextAttackTime=Math.Max(u.NextAttackTime,Clock+1.0/3);
            if(d!=null&&!HasGazeSlow(d))d.NextAttackTime=Clock+Math.Max(0,d.NextAttackTime-Clock)*.3;
        }
        void BreakStoneShield(DeployedUnitState u)
        {
            if(!u.Active||!u.StoneShield||u.MandraPhase==MandragoraPhase.Reborn||UnitInvulnerable(u))return;
            u.StoneShield=false;u.StoneShieldBrokenAt=Clock;u.HealthAtShieldBreak=u.Health;u.StoneShieldBreaks++;
            EndGaze(u,true);
        }
        void RestoreStoneShield(DeployedUnitState u)
        {
            u.StoneShield=true;u.StoneShieldRestoredAt=Clock;u.StoneShieldRestores++;
            u.IsBlocked=false;u.BlockerId=null;u.NextStoneAuraAt=Clock+1;
        }
        void StartMandragoraRebirth(DeployedUnitState u)
        {
            EndGaze(u,true);CancelUnitShots(u);u.MandraPhase=MandragoraPhase.Reborn;
            u.MandraPhaseStartedAt=Clock;u.Health=0;u.StoneShield=false;
            u.IsMoving=u.IsBlocked=u.HasCombatTarget=false;u.BlockerId=u.CombatTargetId=null;
        }
        bool StepMandragoraPassive(DeployedUnitState u,float step)
        {
            if(u.MandraPhase==MandragoraPhase.Reborn)
            {
                if(Clock+1e-6<u.MandraPhaseStartedAt+5)return true;
                u.MandraPhase=MandragoraPhase.Second;u.MandraPhaseStartedAt=Clock;
                u.Health=u.MaxHealth=Settings.Health;u.MandraInvulnerableUntil=Clock+3;
                u.StoneShield=true;u.NextGazeAt=Clock+10;u.NextMandraPillarAt=Clock+20;u.NextStoneAuraAt=Clock+1;
                u.LastAttackTime=double.NegativeInfinity;u.NextAttackTime=Clock;return true;
            }
            if(!u.StoneShield)
            {
                u.NextGazeAt+=step;u.NextMandraPillarAt+=step;
                if(Clock+1e-6>=u.StoneShieldBrokenAt+25||u.HealthAtShieldBreak-u.Health>u.MaxHealth*.4f)RestoreStoneShield(u);
            }
            if(u.MandraPhase==MandragoraPhase.Second&&u.StoneShield&&Clock+1e-6>=u.NextStoneAuraAt)
            {
                u.NextStoneAuraAt=Clock+1;
                foreach(var d in defenders)
                    if(d.Alive&&!DefenderCamouflaged(d)&&GroundDistance(d.Position,u.Motion.Position)<=1.7f)
                    {DamageDefender(d,CombatSettings.UnitAttack*.3f,CombatDamageType.Arts);u.StoneAuraHits++;}
            }
            return false;
        }
        DefenderState GazeTarget(DeployedUnitState u)
        {
            // Defenders currently share one attack setting; stable spawn order breaks equal-attack ties.
            foreach(var d in defenders)if(d.Alive&&!DefenderCamouflaged(d)&&GroundDistance(d.Position,u.Motion.Position)<=1.5f)return d;
            return null;
        }
        void LockGaze(PetrifyingGaze g,DefenderState d)
        {
            bool already=HasGazeSlow(d);g.TargetId=d.EntityId;
            if(!already)d.NextAttackTime=Clock+Math.Max(0,d.NextAttackTime-Clock)/.3;
        }
        bool StepMandragoraAction(DeployedUnitState u)
        {
            var g=u.Gaze;
            if(g!=null&&!g.Ended)
            {
                var d=FindDefender(g.TargetId);
                if(d==null||!d.Alive)
                {
                    if(d!=null)sealedTiles.Add(Cell(d.Position));
                    d=GazeTarget(u);
                    if(d!=null)LockGaze(g,d);else EndGaze(u,false);
                }
                if(!g.Ended)
                {
                    if(GroundDistance(u.Motion.Position,d.Position)>1.5f){EndGaze(u,true);return true;}
                    while(g.NextTick<=g.StartedAt+7.1+1e-6&&Clock+1e-6>=g.NextTick&&d.Alive)
                    {
                        float hp=d.Health;DamageDefender(d,CombatSettings.UnitAttack,CombatDamageType.Arts);
                        g.Ticks++;g.DamageDealt+=hp-d.Health;g.NextTick+=1;
                    }
                    if(!d.Alive)
                    {
                        sealedTiles.Add(Cell(d.Position));var next=GazeTarget(u);
                        if(next!=null)LockGaze(g,next);else EndGaze(u,false);
                    }
                    if(Clock+1e-6>=g.StartedAt+7.1)EndGaze(u,false);
                }
                u.IsMoving=u.HasCombatTarget=false;return true;
            }
            if(g!=null&&Clock<g.EndedAt+1.0/3||Clock<u.PillarCommandStartedAt+1.5||Clock<u.StoneShieldRestoredAt+(u.MandraPhase==MandragoraPhase.Ground?1.5:1.3333334))
            {u.IsMoving=u.HasCombatTarget=false;return true;}
            if(!u.StoneShield||Clock<u.LastAttackTime+CombatSettings.AttackDuration)return false;
            if(u.MandraPhase==MandragoraPhase.Second&&Clock+1e-6>=u.NextMandraPillarAt&&TryCommandPillar(u))
            {u.NextMandraPillarAt=Clock+15;u.PillarCommandStartedAt=Clock;u.IsMoving=u.HasCombatTarget=false;return true;}
            if(Clock+1e-6<u.NextGazeAt)return false;
            var target=GazeTarget(u);if(target==null)return false;
            g=new PetrifyingGaze{SourceId=u.EntityId,StartedAt=Clock,NextTick=Clock+1};
            LockGaze(g,target);gazes.Add(g);u.Gaze=g;u.IsMoving=u.HasCombatTarget=false;return true;
        }
        bool TryCommandPillar(DeployedUnitState u)
        {
            FacilityState selected=null;Vector3 direction=Vector3.zero;float nearest=float.PositiveInfinity;
            foreach(var f in facilities)
            {
                if(f.Settings.Kind!=FacilityKind.Pillar||f.Collapsed||pillarCommands.Exists(p=>!p.Resolved&&p.Pillar==f))continue;
                int best=0;Vector3 bestDirection=Vector3.zero;
                foreach(var dir in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
                {
                    int count=0;
                    foreach(var d in defenders)if(d.Alive&&(Cell(d.Position)==Cell(f.Settings.Position+dir)||Cell(d.Position)==Cell(f.Settings.Position+dir*2)))count++;
                    if(count>best){best=count;bestDirection=dir;}
                }
                float gap=GroundDistance(u.Motion.Position,f.Settings.Position);
                if(best>0&&gap<nearest){selected=f;direction=bestDirection;nearest=gap;}
            }
            if(selected==null)return false;
            pillarCommands.Add(new PillarCommand{SourceId=u.EntityId,Pillar=selected,Direction=direction,StartedAt=Clock});return true;
        }
        void ResolvePillarCommands()
        {
            foreach(var command in pillarCommands)
            {
                if(command.Resolved||Clock+1e-6<command.DetonateAt)continue;
                command.Resolved=true;
                if(command.Pillar.Collapsed)continue;
                CollapsePillar(command.Pillar,command.Pillar.Settings.Position-command.Direction);command.Applied=true;
            }
        }
    }
}
