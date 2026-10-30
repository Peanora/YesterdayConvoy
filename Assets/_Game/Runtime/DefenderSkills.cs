using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum DefenderSkillKind { None, CardiganRecovery, MidnightEnchant, PlumeSwiftStrike, AnselRange,
        BeagleGuard,SpotHealingMode,MelanthaStrength,KroosDoubleShot,StewardStrike,LavaChant,HibiscusHealing,OrchidSwiftStrike,LapplandHavoc,YuKitchen,SariaCalcification,MlynarGlory,Mon3trMeltdown,ExusiaiDelivery,HoshigumaInferno,SilverAshReign,PramanixSnow,WangGambit }

    [Serializable]
    public sealed class DefenderSkillDefinition
    {
        public DefenderSkillKind kind;
        public float initialSP, requiredSP, spPerSecond=1, duration;
        public float attackMultiplier=1, attackSpeedMultiplier=1, healFraction, healRangeBonus;
        public float defenseMultiplier=1,healingInterval;
        public float healthMultiplier=1,effectRadius,damageAmplification=1,moveMultiplier=1,elementFraction,eraseChance,chargeSeconds;
        public int targets=1;
        public DefenderSkillSettings Snapshot()=>new DefenderSkillSettings(kind,initialSP,requiredSP,spPerSecond,duration,
            attackMultiplier,attackSpeedMultiplier,healFraction,healRangeBonus,defenseMultiplier,healingInterval,
            healthMultiplier,effectRadius,damageAmplification,moveMultiplier,elementFraction,eraseChance,chargeSeconds,targets);
    }

    public readonly struct DefenderSkillSettings
    {
        public readonly DefenderSkillKind Kind;
        public readonly float InitialSP,RequiredSP,SPPerSecond,Duration,AttackMultiplier,AttackSpeedMultiplier,HealFraction,HealRangeBonus;
        public readonly float DefenseMultiplier,HealingInterval,HealthMultiplier,EffectRadius,DamageAmplification,MoveMultiplier,ElementFraction,EraseChance,ChargeSeconds;
        public readonly int Targets;
        public bool Enabled=>Kind!=DefenderSkillKind.None;
        public bool AttackRecovery=>Kind==DefenderSkillKind.KroosDoubleShot||Kind==DefenderSkillKind.StewardStrike||Kind==DefenderSkillKind.Mon3trMeltdown;
        public DefenderSkillSettings(DefenderSkillKind kind,float initialSP,float requiredSP,float spPerSecond,float duration,
            float attackMultiplier=1,float attackSpeedMultiplier=1,float healFraction=0,float healRangeBonus=0,float defenseMultiplier=1,float healingInterval=0,
            float healthMultiplier=1,float effectRadius=0,float damageAmplification=1,float moveMultiplier=1,float elementFraction=0,float eraseChance=0,float chargeSeconds=0,int targets=1)
        {
            foreach(float n in new[]{initialSP,requiredSP,spPerSecond,duration,attackMultiplier,attackSpeedMultiplier,healFraction,healRangeBonus,defenseMultiplier,healingInterval,healthMultiplier,effectRadius,damageAmplification,moveMultiplier,elementFraction,eraseChance,chargeSeconds})
                if(float.IsNaN(n)||float.IsInfinity(n))throw new ArgumentException("Non-finite defender skill.");
            if(!Enum.IsDefined(typeof(DefenderSkillKind),kind)||initialSP<0||requiredSP<0||initialSP>requiredSP||spPerSecond<0||duration<0||
                attackMultiplier<=0||attackSpeedMultiplier<=0||healFraction<0||healFraction>1||healRangeBonus<0||defenseMultiplier<=0||healingInterval<0||healthMultiplier<1||effectRadius<0||damageAmplification<1||moveMultiplier<=0||moveMultiplier>1||
                elementFraction<0||eraseChance<0||eraseChance>1||chargeSeconds<0||targets<1||targets>5||
                kind!=DefenderSkillKind.None&&(requiredSP<=0||(spPerSecond<=0&&kind!=DefenderSkillKind.KroosDoubleShot&&kind!=DefenderSkillKind.StewardStrike))||
                kind==DefenderSkillKind.CardiganRecovery&&healFraction<=0||
                kind!=DefenderSkillKind.None&&kind!=DefenderSkillKind.CardiganRecovery&&kind!=DefenderSkillKind.KroosDoubleShot&&kind!=DefenderSkillKind.StewardStrike&&duration<=0||
                (kind==DefenderSkillKind.KroosDoubleShot||kind==DefenderSkillKind.StewardStrike)&&(spPerSecond!=0||duration!=0))
                throw new ArgumentException("Invalid defender skill.");
            Kind=kind;InitialSP=initialSP;RequiredSP=requiredSP;SPPerSecond=spPerSecond;Duration=duration;
            AttackMultiplier=attackMultiplier;AttackSpeedMultiplier=attackSpeedMultiplier;HealFraction=healFraction;HealRangeBonus=healRangeBonus;
            DefenseMultiplier=defenseMultiplier;HealingInterval=healingInterval;HealthMultiplier=healthMultiplier;EffectRadius=effectRadius;DamageAmplification=damageAmplification;
            MoveMultiplier=moveMultiplier;ElementFraction=elementFraction;EraseChance=eraseChance;ChargeSeconds=chargeSeconds;Targets=targets;
        }
    }

    public sealed partial class ManualBattle
    {
        public bool DefenderSkillActive(DefenderState d)=>d!=null&&d.Alive&&d.Settings.Skill.Enabled&&Clock<d.SkillActiveUntil;
        public float DefenderSkillAttack(DefenderState d)=>d.Settings.Attack*(DefenderSkillActive(d)||IsFifthActive(d)?d.Settings.Skill.AttackMultiplier:1)*LiberatorAttackMultiplier(d);
        public float DefenderHealRange(DefenderState d)=>d.Settings.HealRange+(DefenderSkillActive(d)?d.Settings.Skill.HealRangeBonus:0);
        private float DefenderAnimationRate(DefenderState d)=>DefenderSkillActive(d)?d.Settings.Skill.AttackSpeedMultiplier:1;
        private bool SpotHealing(DefenderState d)=>d.Settings.Skill.Kind==DefenderSkillKind.SpotHealingMode&&DefenderSkillActive(d);
        private bool CanDefenderHeal(DefenderState d)=>!(d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown&&DefenderSkillActive(d))&&d.Settings.Role!=DefenderRole.Fighter&&
            (d.Settings.Skill.Kind!=DefenderSkillKind.SpotHealingMode||DefenderSkillActive(d))&&
            (d.Settings.Skill.Kind!=DefenderSkillKind.SariaCalcification||!DefenderSkillActive(d));
        private void StepDefenderSkills(float step)
        {
            foreach(var d in defenders)
            {
                var skill=d.Settings.Skill;
                if(!d.Alive||!skill.Enabled||skill.AttackRecovery&&skill.Kind!=DefenderSkillKind.Mon3trMeltdown)continue;
                // Recovery resumes only after an active skill has ended, including a straddling frame.
                float elapsed=(float)Math.Max(0,Clock-Math.Max(Clock-step,d.SkillActiveUntil));
                d.SkillSP=Math.Min(skill.RequiredSP,d.SkillSP+(skill.AttackRecovery?0:elapsed*skill.SPPerSecond));
                if(DefenderSkillActive(d)||IsFifthActive(d)||d.SkillSP+1e-5<skill.RequiredSP||d.FrozenUntil>Clock||d.NeuralUntil>Clock||DefenderShackled(d))continue;
                // Match attack scheduling tolerance so float rounding cannot starve a ready skill.
                if(Clock+1e-6<d.LastActionTime+d.Settings.ActionDuration/d.LastActionRate)continue;
                bool useful=AdvancedSkillUseful(d);
                if(skill.Kind==DefenderSkillKind.Mon3trMeltdown)useful=d.SkillSP>=skill.RequiredSP&&units.Exists(u=>CanDefenderTarget(u));
                if(skill.Kind==DefenderSkillKind.CardiganRecovery)useful=d.Health<=d.MaxHealth*.8f;
                else if(skill.Kind==DefenderSkillKind.AnselRange||skill.Kind==DefenderSkillKind.SpotHealingMode||skill.Kind==DefenderSkillKind.HibiscusHealing)
                {
                    foreach(var ally in defenders)
                        useful|=ally.Alive&&ally.Health<ally.MaxHealth-.001f&&GroundDistance(d.Position,ally.Position)<=d.Settings.HealRange+skill.HealRangeBonus;
                }
                else foreach(var u in units)useful|=CanDefenderTarget(u)&&(GroundDistance(d.Position,u.Motion.Position)<=DefenderRange(d)||skill.Kind==DefenderSkillKind.LapplandHavoc||u.BlockerId==d.EntityId);
                if(!useful)continue;
                d.SkillSP=0;d.SkillStartedAt=Clock;d.SkillActiveUntil=Clock+skill.Duration;d.SkillCasts++;
                BeginAdvancedDefenderSkill(d);
                if(skill.Kind==DefenderSkillKind.BeagleGuard)d.SkillActionUntil=Clock+1;
                if(skill.Kind==DefenderSkillKind.CardiganRecovery)
                {
                    float before=d.Health;d.Health=Math.Min(d.MaxHealth,d.Health+d.MaxHealth*skill.HealFraction);
                    d.SkillHealthRestored+=d.Health-before;
                }
                // Preserve the remaining attack fraction when attack speed changes.
                d.NextAttackTime=Clock+Math.Max(0,d.NextAttackTime-Clock)/skill.AttackSpeedMultiplier;
                if(skill.Kind==DefenderSkillKind.BeagleGuard)d.NextAttackTime=Math.Max(d.NextAttackTime,d.SkillActionUntil);
            }
        }
        private float DefenderOrdinaryDamage(DefenderState d,DeployedUnitState target,float bonus=1)
        {
            float attack=DefenderSkillAttack(d)*bonus;
            if(d.Settings.RangedAttackMultiplier<1&&GroundDistance(d.Position,target.Motion.Position)>d.Settings.BlockRadius)
                attack*=d.Settings.RangedAttackMultiplier;
            if(d.Settings.CriticalChance>0&&d.Roll()<d.Settings.CriticalChance){attack*=d.Settings.CriticalMultiplier;d.CriticalHits++;}
            return attack;
        }
        private void ScheduleDefenderHealing(DefenderState d,DefenderState wounded,float travel)
        {
            var spec=d.Settings;float rate=DefenderAttackRate(d);
            d.LastActionTime=Clock;d.LastActionHealing=true;d.LastActionRate=DefenderAnimationRate(d);
            d.NextAttackTime=Clock+(SpotHealing(d)&&spec.Skill.HealingInterval>0?spec.Skill.HealingInterval:spec.Interval)/rate;
            combatShots.Add(new CombatShot(d.EntityId,wounded.EntityId,true,false,d.Position,wounded.Position,
                Clock+spec.Windup/d.LastActionRate,travel,DefenderSkillAttack(d),healing:true));
            if(d.Settings.Skill.Kind==DefenderSkillKind.Mon3trMeltdown){ScheduleMon3trChain(d,wounded,travel);d.SkillSP=Math.Min(spec.Skill.RequiredSP,d.SkillSP+1);}
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

        private void ScheduleDefenderAttack(DefenderState d,DeployedUnitState target,float travel)
        {
            if(ScheduleSixthAttack(d,target,travel)||ScheduleFifthAttack(d,target,travel))return;
            var spec=d.Settings;var skill=spec.Skill;
            bool empowered=skill.AttackRecovery&&d.SkillSP+1e-5>=skill.RequiredSP;
            if(empowered){d.SkillSP=0;d.SkillCasts++;d.SkillStartedAt=Clock;d.SkillActiveUntil=Clock;}
            d.LastAttackSkill=empowered;d.LastActionTime=Clock;d.LastActionHealing=false;d.LastActionRate=DefenderAnimationRate(d);
            d.NextAttackTime=Clock+spec.Interval/DefenderAttackRate(d);
            int hits=empowered&&skill.Kind==DefenderSkillKind.KroosDoubleShot?2:1;
            ShotsFired++;LastShotTime=Clock;LastShotEntityId=target.EntityId;LastShotPosition=target.Motion.Position;
            for(int i=0;i<hits;i++)
            {
                var shot=new CombatShot(d.EntityId,target.EntityId,true,empowered,d.Position,target.Motion.Position,
                    Clock+spec.Windup/d.LastActionRate+i*.1,travel,DefenderOrdinaryDamage(d,target,empowered?skill.AttackMultiplier:1),
                    damageType:DefenderSkillActive(d)&&skill.Kind==DefenderSkillKind.MidnightEnchant?CombatDamageType.Arts:spec.DamageType,splashRadius:spec.SplashRadius);
                if(skill.Kind==DefenderSkillKind.OrchidSwiftStrike)shot.HinderDuration=.8f;
                combatShots.Add(shot);
            }
            if(skill.Kind==DefenderSkillKind.LapplandHavoc&&!DefenderSkillActive(d))ScheduleOrdinaryDrone(d,target,travel);
            if(skill.Kind==DefenderSkillKind.MlynarGlory&&DefenderSkillActive(d))
            {
                int count=1;foreach(var other in units)
                {
                    if(count>=skill.Targets)break;
                    if(other==target||!CanDefenderTarget(other)||GroundDistance(d.Position,other.Motion.Position)>DefenderRange(d))continue;
                    combatShots.Add(new CombatShot(d.EntityId,other.EntityId,true,true,d.Position,other.Motion.Position,Clock+spec.Windup,travel,DefenderOrdinaryDamage(d,other)));count++;
                }
            }
            // An attack is one SP event, regardless of hit count; the consuming skill attack is locked out.
            if(skill.AttackRecovery&&!empowered)d.SkillSP=Math.Min(skill.RequiredSP,d.SkillSP+1);
        }

        private bool PreferDefenderTarget(DefenderState d,DeployedUnitState candidate,DeployedUnitState current)
        {
            if(current==null)return true;
            int taunt=UnitTaunt(candidate).CompareTo(UnitTaunt(current));if(taunt!=0)return taunt>0;
            if(d.Settings.Skill.Kind==DefenderSkillKind.StewardStrike)
            {
                float delta=UnitArmor(candidate)-UnitArmor(current);
                if(Math.Abs(delta)>.0001f)return delta>0;
            }
            return candidate.Motion.DistanceTravelled>current.Motion.DistanceTravelled;
        }
    }
}
