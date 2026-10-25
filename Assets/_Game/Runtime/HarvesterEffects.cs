using UnityEngine;

namespace YesterdayConvoy
{
    public enum HarvesterPhase { Dormant, Waking, Combat }
    public sealed partial class ManualBattle
    {
        public float UnitMoveMultiplier(DeployedUnitState u)
        {
            float multiplier=EngagementEnabled&&CombatSettings.Effects.Harvester&&u!=null&&u.HarvesterPhase==HarvesterPhase.Combat?6:1;
            if(EngagementEnabled&&u!=null&&UnitCombat(u).Effects.OrderRunner&&HasTacticalCommand)multiplier*=1+.3f/u.Motion.Speed;
            if(IsPatriot(u)&&u.PatriotPhase==PatriotPhase.Destruction)multiplier*=1+.1f/u.Motion.Speed;
            return multiplier;
        }
        void TryWakeHarvester(DeployedUnitState u)
        {
            if(!u.Active||u.HarvesterPhase!=HarvesterPhase.Dormant||u.PillarStunnedUntil>Clock||u.Health/u.MaxHealth>=.9999f)return;
            u.HarvesterPhase=HarvesterPhase.Waking;u.HarvesterPhaseStartedAt=Clock;
            u.IsMoving=u.HasCombatTarget=false;u.CombatTargetId=null;
        }
        bool StepHarvesterPassive(DeployedUnitState u,float step)
        {
            TryWakeHarvester(u);
            if(u.HarvesterPhase==HarvesterPhase.Waking&&Clock+1e-6>=u.HarvesterPhaseStartedAt+1)
            {
                u.HarvesterPhase=HarvesterPhase.Combat;u.HarvesterPhaseStartedAt=Clock;
                u.NextHarvesterAuraAt=Clock+1;u.NextAttackTime=Clock;
            }
            if(u.HarvesterPhase!=HarvesterPhase.Combat)return false;
            float lost=Mathf.Min(u.Health,u.MaxHealth*.04f*Mathf.Min(step,(float)(Clock-u.HarvesterPhaseStartedAt)));
            double incomingHit=u.LastHitTime;
            ApplyUnitHealthDamage(u,lost);u.LastHitTime=incomingHit;u.HarvesterHealthLost+=lost;
            if(!u.Active)return true;
            if(Clock+1e-6<u.NextHarvesterAuraAt)return false;
            u.NextHarvesterAuraAt=Clock+1;
            bool hit=false;
            foreach(var d in defenders)
            {
                // This is a persistent area aura, including camouflaged defenders.
                if(!d.Alive||GroundDistance(d.Position,u.Motion.Position)>2.5f)continue;
                ApplyNeuralDamage(d,CombatSettings.UnitAttack*.2f);hit=true;
            }
            if(hit){u.HarvesterAuraTicks++;u.LastHarvesterAuraAt=Clock;}
            return false;
        }
        bool HarvesterActionLocked(DeployedUnitState u)=>u.HarvesterPhase==HarvesterPhase.Waking||u.HarvesterPhase==HarvesterPhase.Dormant&&Clock<u.SpawnTime+30;
    }
}
