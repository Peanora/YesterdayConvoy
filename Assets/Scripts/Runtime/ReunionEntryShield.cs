using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        // Incoming facilities and self-loss use their existing entries. Only a valid
        // defender hit starts the elite support's first-engagement shield here.
        bool TryDefenderDamageUnit(DeployedUnitState unit,float attack,CombatDamageType type,float ignoreResistance=0,bool ranged=false)
        {
            if(float.IsNaN(ignoreResistance)||float.IsInfinity(ignoreResistance)||ignoreResistance<0||float.IsNaN(attack)||float.IsInfinity(attack)||attack<=0||
                type!=CombatDamageType.Physical&&type!=CombatDamageType.Arts)throw new ArgumentException("Invalid incoming damage.");
            if(!EngagementEnabled||Phase!=BattlePhase.Running||IsPaused||unit==null||!unit.Active||unit.IsReviving||StrongholdInTransit(unit.EntityId)||UnitInvulnerable(unit))return false;
            TryActivateReunionEntryShield(unit);
            return TryDamageUnit(unit.EntityId,attack,type,ignoreResistance,ranged);
        }

        // True damage retains every other synergy shield's existing bypass rule.
        // The approved elite-support shield is the only ordinary shield added here.
        float AbsorbReunionEliteShield(DeployedUnitState unit,float damage)
        {
            if(!unit.ReunionEliteSupport||damage<=0||unit.SynergyShield<=0||unit.SynergyShieldUntil<=Clock)return damage;
            float absorbed=Mathf.Min(damage,unit.SynergyShield);
            unit.SynergyShield-=absorbed;
            RecordUnitShieldAbsorption(unit,absorbed);
            return damage-absorbed;
        }
    }
}
