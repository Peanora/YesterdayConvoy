using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum CombatDamageType { Physical, Arts }

    public readonly struct MonsterEffectSettings
    {
        public readonly CombatDamageType DamageType;
        public readonly float Resistance, ArmorShred, BlastAttack, BlastRadius, BlastDelay, ColdDuration;
        public MonsterEffectSettings(CombatDamageType damageType=CombatDamageType.Physical, float resistance=0, float armorShred=0,
            float blastAttack=0, float blastRadius=0, float blastDelay=0, float coldDuration=0)
        {
            foreach(float value in new[]{resistance,armorShred,blastAttack,blastRadius,blastDelay,coldDuration})
                if(float.IsNaN(value) || float.IsInfinity(value) || value<0) throw new ArgumentException("Invalid monster effect value.");
            if((damageType!=CombatDamageType.Physical && damageType!=CombatDamageType.Arts) || resistance>100 ||
                blastAttack>0 && blastRadius<=0 || coldDuration>0 && blastAttack<=0)
                throw new ArgumentException("Incomplete monster effect configuration.");
            DamageType=damageType;Resistance=resistance;ArmorShred=armorShred;BlastAttack=blastAttack;
            BlastRadius=blastRadius;BlastDelay=blastDelay;ColdDuration=coldDuration;
        }
    }

    public sealed class DeathBlast
    {
        public string SourceId { get; }
        public Vector3 Position { get; }
        public double ScheduledAt { get; }
        public double DetonateAt { get; }
        public float Radius { get; }
        public bool Cold { get; }
        public bool Resolved { get; internal set; }
        public int TargetsHit { get; internal set; }
        internal DeathBlast(DeployedUnitState unit, double time, MonsterEffectSettings settings)
        {
            SourceId=unit.EntityId;Position=unit.Motion.Position;ScheduledAt=time;DetonateAt=time+settings.BlastDelay;
            Radius=settings.BlastRadius;Cold=settings.ColdDuration>0;
        }
    }

    public sealed partial class ManualBattle
    {
        private readonly List<DeathBlast> deathBlasts=new List<DeathBlast>();
        public IReadOnlyList<DeathBlast> DeathBlasts => deathBlasts.AsReadOnly();
        public bool HasPendingDeathBlast => deathBlasts.Exists(x=>!x.Resolved);
        public float DefenderArmor(DefenderState defender) => defender.Settings.Armor-defender.ArmorReduction;
        public static float CalculateDamage(float attack,float armor,float resistance,CombatDamageType type) =>
            Mathf.Max(1,type==CombatDamageType.Arts ? attack*(1-resistance/100) : attack-armor);

        private void DamageDefender(DefenderState defender,float attack,CombatDamageType type)
        {
            defender.Health=Mathf.Max(0,defender.Health-CalculateDamage(attack,DefenderArmor(defender),CombatSettings.Effects.Resistance,type));
            defender.LastHitTime=Clock;
            if(!defender.Alive) { defender.DeathTime=Clock;defender.TargetId=null; }
        }
        private void ScheduleDeathEffect(DeployedUnitState unit)
        {
            if(CombatSettings.Effects.BlastAttack>0) deathBlasts.Add(new DeathBlast(unit,Clock,CombatSettings.Effects));
        }
        private void ResolveDeathEffects()
        {
            var effects=CombatSettings.Effects;
            foreach(var blast in deathBlasts)
            {
                if(blast.Resolved || Clock+1e-6<blast.DetonateAt) continue;
                blast.Resolved=true;
                // Source is already dead. A death burst must survive its source and fire once.
                foreach(var defender in defenders)
                {
                    if(!defender.Alive || GroundDistance(blast.Position,defender.Position)>blast.Radius) continue;
                    blast.TargetsHit++;DamageDefender(defender,effects.BlastAttack,CombatDamageType.Physical);
                    if(defender.Alive && effects.ColdDuration>0)
                    {
                        bool cold=defender.ColdUntil>Clock || defender.FrozenUntil>Clock;
                        if(cold) defender.FrozenUntil=Math.Max(defender.FrozenUntil,Clock+effects.ColdDuration);
                        else defender.NextAttackTime=Clock+Math.Max(0,defender.NextAttackTime-Clock)/.7;
                        defender.ColdUntil=Math.Max(defender.ColdUntil,Clock+effects.ColdDuration);
                    }
                }
            }
        }
    }
}
