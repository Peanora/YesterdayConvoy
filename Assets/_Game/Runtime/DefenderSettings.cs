using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum DefenderRole { Fighter, Guardian, Medic }

    /// <summary>Immutable, per-entity combat snapshot. Legacy battles still use their old settings.</summary>
    public readonly struct DefenderSettings
    {
        public readonly string Id, Name, ArtId;
        public readonly Vector3 Position;
        public readonly float Health, Attack, Armor, Range, HealRange, Interval, BlockRadius, Windup, ActionDuration;
        public readonly int BlockCount;
        public readonly DefenderRole Role;
        public readonly CombatDamageType DamageType;
        public readonly float SplashRadius;
        public readonly DefenderSkillSettings Skill;
        public readonly bool CanAttack,Kasimierz;
        public readonly float RangedAttackMultiplier,CriticalChance,CriticalMultiplier,ExtraHealChance;

        public DefenderSettings(string id, string name, string artId, Vector3 position, float health,
            float attack, float armor, float range, float interval, int blocks = 0,
            DefenderRole role = DefenderRole.Fighter, CombatDamageType damageType = CombatDamageType.Physical,
            float windup = 0, float actionDuration = 0, float splashRadius = 0, float blockRadius = .65f, float healingRange = 0,
            DefenderSkillSettings skill=default,bool canAttack=true,float rangedAttackMultiplier=1,float criticalChance=0,float criticalMultiplier=1.5f,float extraHealChance=0,bool kasimierz=false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Missing defender ID.");
            foreach (float n in new[] { position.x, position.y, position.z, health, attack, armor, range, interval, windup, actionDuration, splashRadius, blockRadius, healingRange,rangedAttackMultiplier,criticalChance,criticalMultiplier,extraHealChance })
                if (float.IsNaN(n) || float.IsInfinity(n)) throw new ArgumentException("Non-finite defender value.");
            if (health <= 0 || attack <= 0 || armor < 0 || range <= 0 || interval <= 0 || blocks < 0 ||
                blockRadius <= 0 || windup < 0 || actionDuration < windup || interval < actionDuration || splashRadius < 0 || healingRange < 0 ||
                !Enum.IsDefined(typeof(DefenderRole), role) || !Enum.IsDefined(typeof(CombatDamageType), damageType)||rangedAttackMultiplier<=0||rangedAttackMultiplier>1||criticalChance<0||criticalChance>1||criticalMultiplier<1||extraHealChance<0||extraHealChance>1)
                throw new ArgumentException("Invalid defender combat values.");
            Id=id; Name=name; ArtId=artId; Position=position; Health=health; Attack=attack; Armor=armor;
            Range=range; HealRange=healingRange>0 ? healingRange : range; Interval=interval; BlockCount=blocks; Role=role; DamageType=damageType;
            Windup=windup; ActionDuration=actionDuration; SplashRadius=splashRadius; BlockRadius=blockRadius;
            Skill=skill;CanAttack=canAttack;Kasimierz=kasimierz;RangedAttackMultiplier=rangedAttackMultiplier;CriticalChance=criticalChance;CriticalMultiplier=criticalMultiplier;ExtraHealChance=extraHealChance;
        }
    }
}
