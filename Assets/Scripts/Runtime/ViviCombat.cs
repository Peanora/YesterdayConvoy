using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // The approved comic GIF faces screen-right. Mirroring owns only its visual child.
    internal static class ViviCombat
    {
        internal const string CardId="CARD_WISADEL_APRIL";
        internal const float SkillSplashRadius=2.2f; // Trial value; world XZ distance, not targeting range.
        internal static bool IsVivi(string cardId)=>cardId==CardId;
        internal static bool SkillShell(DeployedUnitState source,CombatShot shot)=>source!=null&&IsVivi(source.CardId)&&!shot.FromDefender&&shot.Empowered&&!shot.FireRain&&shot.SplashRadius>0;

        internal static bool VisualFlip(ManualBattle battle,DeployedUnitState unit,Camera camera,bool previous)
        {
            if(camera==null||!unit.Active||battle.IsPaused)return previous;
            double age=battle.Clock-unit.LastAttackTime;
            bool action=battle.IsSkillActionPlaying(unit)||age>=0&&age<battle.UnitActionDuration(unit)/unit.LastAttackRate;
            Vector3 direction=unit.Motion.Direction;
            if(action)
            {
                // Lock the fired gesture to its own immutable shot, even after its target dies.
                var shot=battle.CombatShots.LastOrDefault(s=>!s.FromDefender&&!s.Erased&&s.SourceId==unit.EntityId&&s.StartedAt>=unit.LastAttackTime);
                if(shot==null)return previous;
                direction=shot.Destination-shot.Origin;
            }
            else if(unit.IsMoving)
            {
                if(!battle.UnitControlImmune(unit))
                {
                    if(unit.LureUntil>battle.Clock)
                    {if(unit.Motion.DistanceTravelled>unit.LureDistance)direction=-direction;}
                    else if(unit.FearUntil>battle.Clock)direction=-direction;
                }
            }
            else
            {
                var target=battle.FindDefender(unit.CombatTargetId);
                if(target==null)return previous;
                direction=target.Position-unit.Motion.Position;
            }
            float dx=Vector3.Dot(direction,camera.transform.right);
            return Mathf.Abs(dx)>.08f?dx<0:previous;
        }
    }

    public sealed partial class ManualBattle
    {
        // A launched shell lands at its recorded point. Its first target need not still be alive.
        // The normal caster-defeat cancellation remains authoritative before this method runs.
        bool ResolveViviSkillSplash(CombatShot shot,DeployedUnitState source)
        {
            if(!ViviCombat.SkillShell(source,shot))return false;
            if(!source.Active||source.IsReviving||StrongholdInTransit(source.EntityId))return true;
            shot.Applied=true;var combat=UnitCombat(source);shot.DamageDealt=0;
            foreach(var target in defenders.Where(d=>d.Alive&&GroundDistance(d.Position,shot.Destination)<=shot.SplashRadius).ToArray())
            {
                bool frozen=target.FrozenUntil>Clock&&combat.Effects.FrozenAttackMultiplier>1;
                shot.FrozenBonusApplied|=frozen;
                float health=target.Health;
                ApplyDefenderAttack(target,shot.Attack*(frozen?combat.Effects.FrozenAttackMultiplier:1),
                    source.AltarEmpowered?CombatDamageType.Arts:combat.Effects.DamageType,
                    shot.SynergyOrder||source.SynergySupport,shot.SynergyOrder||source.SynergySupport?null:source,1,source);
                float loss=health-target.Health;shot.DamageDealt+=loss;
                if(shot.SynergyOrder)continue;
                RecordSynergyContact(source);OnGolemHit(source,target);
                HealUnit(source,loss*combat.Effects.LifeSteal);
                if(target.Alive)target.ArmorReduction+=combat.Effects.ArmorShred;
                if(target.Alive&&shot.ColdDuration>0)TryApplyCold(target.EntityId,shot.ColdDuration);
                if(target.Alive&&shot.BurnDamage>0)ApplyBurn(target,shot.BurnDamage,source);
                if(target.Alive&&shot.NeuralDamage>0)ApplyNeuralDamageAttributed(target,shot.NeuralDamage,null,source);
                OnSynergyDirectHit(source,target);
            }
            return true;
        }
    }
}
