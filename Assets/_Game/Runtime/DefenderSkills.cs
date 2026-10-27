using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum DefenderSkillKind { None, CardiganRecovery, MidnightEnchant, PlumeSwiftStrike, AnselRange }

    [Serializable]
    public sealed class DefenderSkillDefinition
    {
        public DefenderSkillKind kind;
        public float initialSP, requiredSP, spPerSecond=1, duration;
        public float attackMultiplier=1, attackSpeedMultiplier=1, healFraction, healRangeBonus;
        public DefenderSkillSettings Snapshot()=>new DefenderSkillSettings(kind,initialSP,requiredSP,spPerSecond,duration,
            attackMultiplier,attackSpeedMultiplier,healFraction,healRangeBonus);
    }

    public readonly struct DefenderSkillSettings
    {
        public readonly DefenderSkillKind Kind;
        public readonly float InitialSP,RequiredSP,SPPerSecond,Duration,AttackMultiplier,AttackSpeedMultiplier,HealFraction,HealRangeBonus;
        public bool Enabled=>Kind!=DefenderSkillKind.None;
        public DefenderSkillSettings(DefenderSkillKind kind,float initialSP,float requiredSP,float spPerSecond,float duration,
            float attackMultiplier=1,float attackSpeedMultiplier=1,float healFraction=0,float healRangeBonus=0)
        {
            foreach(float n in new[]{initialSP,requiredSP,spPerSecond,duration,attackMultiplier,attackSpeedMultiplier,healFraction,healRangeBonus})
                if(float.IsNaN(n)||float.IsInfinity(n))throw new ArgumentException("Non-finite defender skill.");
            if(!Enum.IsDefined(typeof(DefenderSkillKind),kind)||initialSP<0||requiredSP<0||initialSP>requiredSP||spPerSecond<0||duration<0||
                attackMultiplier<=0||attackSpeedMultiplier<=0||healFraction<0||healFraction>1||healRangeBonus<0||
                kind!=DefenderSkillKind.None&&(requiredSP<=0||spPerSecond<=0)||
                kind==DefenderSkillKind.CardiganRecovery&&healFraction<=0||
                kind!=DefenderSkillKind.None&&kind!=DefenderSkillKind.CardiganRecovery&&duration<=0)
                throw new ArgumentException("Invalid defender skill.");
            Kind=kind;InitialSP=initialSP;RequiredSP=requiredSP;SPPerSecond=spPerSecond;Duration=duration;
            AttackMultiplier=attackMultiplier;AttackSpeedMultiplier=attackSpeedMultiplier;HealFraction=healFraction;HealRangeBonus=healRangeBonus;
        }
    }

    public sealed partial class ManualBattle
    {
        public bool DefenderSkillActive(DefenderState d)=>d!=null&&d.Alive&&d.Settings.Skill.Enabled&&Clock<d.SkillActiveUntil;
        public float DefenderSkillAttack(DefenderState d)=>d.Settings.Attack*(DefenderSkillActive(d)?d.Settings.Skill.AttackMultiplier:1);
        public float DefenderHealRange(DefenderState d)=>d.Settings.HealRange+(DefenderSkillActive(d)?d.Settings.Skill.HealRangeBonus:0);
        private float DefenderAnimationRate(DefenderState d)=>DefenderSkillActive(d)?d.Settings.Skill.AttackSpeedMultiplier:1;
        private void StepDefenderSkills(float step)
        {
            foreach(var d in defenders)
            {
                var skill=d.Settings.Skill;
                if(!d.Alive||!skill.Enabled)continue;
                // Recovery resumes only after an active skill has ended, including a straddling frame.
                float elapsed=(float)Math.Max(0,Clock-Math.Max(Clock-step,d.SkillActiveUntil));
                d.SkillSP=Math.Min(skill.RequiredSP,d.SkillSP+elapsed*skill.SPPerSecond);
                if(DefenderSkillActive(d)||d.SkillSP+1e-5<skill.RequiredSP||d.FrozenUntil>Clock||d.NeuralUntil>Clock||DefenderShackled(d))continue;
                if(Clock<d.LastActionTime+d.Settings.ActionDuration/d.LastActionRate)continue;
                bool useful=false;
                if(skill.Kind==DefenderSkillKind.CardiganRecovery)useful=d.Health<=d.MaxHealth*.8f;
                else if(skill.Kind==DefenderSkillKind.AnselRange)
                {
                    foreach(var ally in defenders)
                        useful|=ally.Alive&&ally.Health<ally.MaxHealth-.001f&&GroundDistance(d.Position,ally.Position)<=d.Settings.HealRange+skill.HealRangeBonus;
                }
                else foreach(var u in units)useful|=CanDefenderTarget(u)&&GroundDistance(d.Position,u.Motion.Position)<=d.Settings.Range;
                if(!useful)continue;
                d.SkillSP=0;d.SkillStartedAt=Clock;d.SkillActiveUntil=Clock+skill.Duration;d.SkillCasts++;
                if(skill.Kind==DefenderSkillKind.CardiganRecovery)
                {
                    float before=d.Health;d.Health=Math.Min(d.MaxHealth,d.Health+d.MaxHealth*skill.HealFraction);
                    d.SkillHealthRestored+=d.Health-before;
                }
                // Preserve the remaining attack fraction when attack speed changes.
                d.NextAttackTime=Clock+Math.Max(0,d.NextAttackTime-Clock)/skill.AttackSpeedMultiplier;
            }
        }
        private float DefenderOrdinaryDamage(DefenderState d,DeployedUnitState target)
        {
            float attack=DefenderSkillAttack(d);
            if(d.Settings.RangedAttackMultiplier<1&&GroundDistance(d.Position,target.Motion.Position)>d.Settings.BlockRadius)
                attack*=d.Settings.RangedAttackMultiplier;
            if(d.Settings.CriticalChance>0&&d.Roll()<d.Settings.CriticalChance){attack*=d.Settings.CriticalMultiplier;d.CriticalHits++;}
            return attack;
        }
        private void ScheduleDefenderHealing(DefenderState d,DefenderState wounded,float travel)
        {
            var spec=d.Settings;float rate=DefenderAttackRate(d);
            d.LastActionTime=Clock;d.LastActionHealing=true;d.LastActionRate=DefenderAnimationRate(d);
            d.NextAttackTime=Clock+spec.Interval/rate;
            combatShots.Add(new CombatShot(d.EntityId,wounded.EntityId,true,false,d.Position,wounded.Position,
                Clock+spec.Windup/d.LastActionRate,travel,DefenderSkillAttack(d),healing:true));
            if(spec.ExtraHealChance<=0||d.Roll()>=spec.ExtraHealChance)return;
            DefenderState second=null;
            foreach(var ally in defenders)
                if(ally!=wounded&&ally.Alive&&ally.Health<ally.MaxHealth-.001f&&GroundDistance(d.Position,ally.Position)<=DefenderHealRange(d)&&
                    (second==null||ally.Health/ally.MaxHealth<second.Health/second.MaxHealth))second=ally;
            if(second==null)return;
            d.ExtraHeals++;
            combatShots.Add(new CombatShot(d.EntityId,second.EntityId,true,false,d.Position,second.Position,
                Clock+spec.Windup/d.LastActionRate,travel,DefenderSkillAttack(d),healing:true));
        }
    }
}
