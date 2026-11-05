using System;
using UnityEngine;

namespace YesterdayConvoy
{
    // Native JT8-3 phase lengths. +2s first wave is this project's documented
    // adaptation: the source proves ten waves at 2s intervals, not their first offset.
    internal static class TalulahTiming
    {
        internal const string CardId="CARD_TALULAH_TRIAL";
        internal const double FallDuration=5, RainDuration=28, FirstWave=2, WaveInterval=2;
        internal const double ReappearanceDuration=2.7666667, ProtectionDuration=15;
        internal const int AutomaticWaves=10;
    }

    public enum TalulahPhase { First, FirstFall, FirstRain, Second, FinalFall, FinalRain, Dead }

    public sealed partial class ManualBattle
    {
        public bool IsTalulah(DeployedUnitState unit)=>EngagementEnabled&&unit!=null&&
            unit.CardId==TalulahTiming.CardId&&UnitCombat(unit).FireRain;

        bool HasTalulahSecondPhase(DeployedUnitState unit)=>IsTalulah(unit)&&
            UnitCombat(unit).Effects.TalulahSecondPhase;

        public bool TalulahSecondForm(DeployedUnitState unit)=>HasTalulahSecondPhase(unit)&&
            (unit.TalulahPhase==TalulahPhase.Second||unit.TalulahPhase==TalulahPhase.FinalFall||
             unit.TalulahPhase==TalulahPhase.FinalRain||unit.TalulahPhase==TalulahPhase.Dead);

        // The 2.7667s native return animation is inside the 15s protection window.
        // It owns the action, but later protected B combat can move, attack and use SP.
        public bool TalulahTransitionLocked(DeployedUnitState unit)=>HasTalulahSecondPhase(unit)&&unit.Active&&
            (unit.TalulahPhase==TalulahPhase.FirstFall||unit.TalulahPhase==TalulahPhase.FirstRain||
             unit.TalulahPhase==TalulahPhase.FinalFall||unit.TalulahPhase==TalulahPhase.FinalRain||
             unit.TalulahPhase==TalulahPhase.Second&&Clock<unit.TalulahPhaseStartedAt+TalulahTiming.ReappearanceDuration);

        public bool TalulahAutomaticRainActive(DeployedUnitState unit)=>HasTalulahSecondPhase(unit)&&unit.Active&&
            (unit.TalulahPhase==TalulahPhase.FirstRain||unit.TalulahPhase==TalulahPhase.FinalRain)&&
            Clock>=unit.TalulahAutomaticRainStartedAt&&Clock<unit.TalulahAutomaticRainEndsAt;

        void HoldTalulah(DeployedUnitState unit)
        {
            unit.IsMoving=unit.IsBlocked=unit.HasCombatTarget=false;
            unit.BlockerId=unit.CombatTargetId=null;
        }

        bool TryStartTalulahFall(DeployedUnitState unit)
        {
            if(!HasTalulahSecondPhase(unit)||!unit.Active)return false;
            bool first=unit.TalulahPhase==TalulahPhase.First;
            if(!first&&unit.TalulahPhase!=TalulahPhase.Second)return false;
            // Match ordinary caster defeat: pending projectiles stop; past damage stays.
            foreach(var shot in combatShots)
                if(!shot.Resolved&&!shot.Applied&&shot.SourceId==unit.EntityId)shot.Erased=true;
            CancelUnitShots(unit);HoldTalulah(unit);
            unit.SkillAmmo=0;unit.FireRainStartedAt=double.NegativeInfinity;
            unit.NextAttackTime=Clock;unit.LastAttackTime=double.NegativeInfinity;
            unit.LastAttackEmpowered=false;unit.Health=0;
            unit.DeathTime=double.PositiveInfinity;
            if(first)unit.TalulahFirstPhaseMaxHealth=unit.MaxHealth;
            unit.TalulahPhase=first?TalulahPhase.FirstFall:TalulahPhase.FinalFall;
            unit.TalulahPhaseStartedAt=Clock;
            return true;
        }

        void BeginTalulahAutomaticRain(DeployedUnitState unit,bool final,double began)
        {
            unit.TalulahPhase=final?TalulahPhase.FinalRain:TalulahPhase.FirstRain;
            unit.TalulahPhaseStartedAt=began;
            unit.TalulahAutomaticRainStartedAt=began;
            unit.TalulahAutomaticRainEndsAt=began+TalulahTiming.RainDuration;
            unit.TalulahAutomaticWavesFired=0;
        }

        void ResolveTalulahAutomaticRain(DeployedUnitState unit)
        {
            // Automatic waves own no manual ammo/SP and cannot recursively earn
            // on-hit resources. Their actual area damage uses the existing loss path.
            while(unit.TalulahAutomaticWavesFired<TalulahTiming.AutomaticWaves&&
                  Clock+1e-6>=unit.TalulahAutomaticRainStartedAt+TalulahTiming.FirstWave+
                    unit.TalulahAutomaticWavesFired*TalulahTiming.WaveInterval)
            {
                unit.TalulahAutomaticWavesFired++;
                float attack=UnitCombat(unit).UnitAttack*UnitAttackMultiplier(unit);
                foreach(var defender in defenders)
                    if(defender.Alive)DamageDefender(defender,attack,CombatDamageType.Arts,derived:true);
            }
        }

        // Runs before regeneration, SP, blocking, attacks and movement. True means
        // the phase owns this unit's entire step; Resolution stays Active until final.
        bool StepTalulahPhase(DeployedUnitState unit)
        {
            if(!HasTalulahSecondPhase(unit)||!unit.Active)return false;
            if(unit.TalulahPhase==TalulahPhase.First||unit.TalulahPhase==TalulahPhase.Dead)return false;
            if(unit.TalulahPhase==TalulahPhase.Second)
            {
                if(!TalulahTransitionLocked(unit))return false;
                HoldTalulah(unit);return true;
            }
            HoldTalulah(unit);
            if(unit.TalulahPhase==TalulahPhase.FirstFall||unit.TalulahPhase==TalulahPhase.FinalFall)
            {
                if(Clock+1e-6<unit.TalulahPhaseStartedAt+TalulahTiming.FallDuration)return true;
                bool final=unit.TalulahPhase==TalulahPhase.FinalFall;
                BeginTalulahAutomaticRain(unit,final,unit.TalulahPhaseStartedAt+TalulahTiming.FallDuration);
            }
            ResolveTalulahAutomaticRain(unit);
            if(Clock+1e-6<unit.TalulahAutomaticRainEndsAt)return true;
            double ended=unit.TalulahAutomaticRainEndsAt;
            if(unit.TalulahPhase==TalulahPhase.FirstRain)
            {
                unit.TalulahPhase=TalulahPhase.Second;unit.TalulahPhaseStartedAt=ended;
                unit.MaxHealth=unit.Health=unit.TalulahFirstPhaseMaxHealth*1.5f;
                unit.TalulahInvulnerableUntil=ended+TalulahTiming.ProtectionDuration;
                unit.DeathTime=double.PositiveInfinity;unit.LastAttackTime=double.NegativeInfinity;
                unit.NextAttackTime=ended+TalulahTiming.ReappearanceDuration;
                return true;
            }
            unit.TalulahPhase=TalulahPhase.Dead;unit.TalulahPhaseStartedAt=ended;
            // The regular final-death path schedules death effects and synergy hooks
            // once. It cannot restart this state because Dead is not First/Second.
            DefeatUnit(unit);
            return true;
        }
    }
}
