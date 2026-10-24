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
        public readonly float FrozenAttackMultiplier;
        public readonly int ColdAttackEvery;
        public readonly float AttackColdDuration;
        public readonly bool FrostNova;
        public readonly float RegenerationFraction;
        public readonly int AttackTargets;
        public readonly float LifeSteal;
        public readonly bool Healer;
        public readonly bool ArtsBarrier;
        public readonly float UnitResistance;
        public readonly bool Mudrock;
        public readonly bool Phalanx;
        public readonly bool Flamechaser, Flamerazer, Mandragora, NervousCreeper, Harvester, FirstSpeaker, Shackler, TacticalCommander, OrderRunner, OrderSniper;
        public MonsterEffectSettings(CombatDamageType damageType=CombatDamageType.Physical, float resistance=0, float armorShred=0,
            float blastAttack=0, float blastRadius=0, float blastDelay=0, float coldDuration=0, float frozenAttackMultiplier=0,
            int coldAttackEvery=0,float attackColdDuration=0,bool frostNova=false,float regenerationFraction=0,int attackTargets=0,float lifeSteal=0,bool healer=false,
            bool artsBarrier=false,float unitResistance=0,bool mudrock=false,bool phalanx=false,bool flamechaser=false,bool flamerazer=false,bool mandragora=false,bool nervousCreeper=false,bool harvester=false,bool firstSpeaker=false,bool shackler=false,bool tacticalCommander=false,bool orderRunner=false,bool orderSniper=false)
        {
            foreach(float value in new[]{resistance,armorShred,blastAttack,blastRadius,blastDelay,coldDuration,frozenAttackMultiplier,attackColdDuration,regenerationFraction,lifeSteal,unitResistance})
                if(float.IsNaN(value) || float.IsInfinity(value) || value<0) throw new ArgumentException("Invalid monster effect value.");
            if((damageType!=CombatDamageType.Physical && damageType!=CombatDamageType.Arts) || resistance>100 ||
                blastAttack>0 && blastRadius<=0 || coldDuration>0 && blastAttack<=0 ||
                frozenAttackMultiplier>0 && frozenAttackMultiplier<1 || coldAttackEvery<0 ||
                (coldAttackEvery>0)!=(attackColdDuration>0) || regenerationFraction>1 || attackTargets<0 || attackTargets>3 || unitResistance>100)
                throw new ArgumentException("Incomplete monster effect configuration.");
            DamageType=damageType;Resistance=resistance;ArmorShred=armorShred;BlastAttack=blastAttack;
            BlastRadius=blastRadius;BlastDelay=blastDelay;ColdDuration=coldDuration;
            FrozenAttackMultiplier=frozenAttackMultiplier;
            ColdAttackEvery=coldAttackEvery;AttackColdDuration=attackColdDuration;
            FrostNova=frostNova;
            RegenerationFraction=regenerationFraction;
            AttackTargets=attackTargets;LifeSteal=lifeSteal;
            Healer=healer;
            ArtsBarrier=artsBarrier;UnitResistance=unitResistance;
            Mudrock=mudrock;
            Phalanx=phalanx;
            Flamechaser=flamechaser;Flamerazer=flamerazer;Mandragora=mandragora;NervousCreeper=nervousCreeper;Harvester=harvester;FirstSpeaker=firstSpeaker;Shackler=shackler;TacticalCommander=tacticalCommander;OrderRunner=orderRunner;OrderSniper=orderSniper;
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
        // One model entry for direct and projectile damage; damage cannot mutate a paused/finished battle.
        public bool TryDamageUnit(string entityId,float attack,CombatDamageType type)
        {
            if(float.IsNaN(attack) || float.IsInfinity(attack) || attack<=0 ||
                type!=CombatDamageType.Physical && type!=CombatDamageType.Arts) throw new ArgumentException("Invalid incoming damage.");
            if(!EngagementEnabled || Phase!=BattlePhase.Running || IsPaused) return false;
            var unit=FindUnit(entityId);if(unit==null || !unit.Active || unit.IsReviving || UnitInvulnerable(unit)) return false;
            if(unit.FlamePhase==FlamechaserPhase.Ember)return ApplyUnitHealthDamage(unit,1);
            float damage=CalculateDamage(attack,UnitArmor(unit),UnitCombat(unit).Effects.UnitResistance,type);
            if(CombatSettings.Effects.Mandragora&&unit.StoneShield)damage*=.4f;
            if(type==CombatDamageType.Arts && unit.ArtsBarrierRemaining>0)
            {
                float absorbed=Mathf.Min(unit.ArtsBarrierRemaining,damage);unit.ArtsBarrierRemaining-=absorbed;damage-=absorbed;
                if(unit.ArtsBarrierRemaining<=.00001f)
                {
                    unit.ArtsBarrierRemaining=0;unit.MaxHealth=Settings.Health;unit.Health=Mathf.Min(unit.Health,unit.MaxHealth);
                    unit.NextAttackTime=Clock+Math.Max(0,unit.NextAttackTime-Clock)*1.5;
                }
            }
            if(damage<=0){unit.LastHitTime=Clock;return true;}
            return ApplyUnitHealthDamage(unit,damage);
        }
        public sealed class HealingCast
        {
            public string SourceId { get; internal set; }
            public IReadOnlyList<string> TargetIds { get; internal set; }
            public double StartedAt { get; internal set; }
            public double ImpactAt { get; internal set; }
            public bool Resolved { get; internal set; }
            public float HealthRestored { get; internal set; }
        }
        private readonly List<HealingCast> healingCasts=new List<HealingCast>();
        public IReadOnlyList<HealingCast> HealingCasts => healingCasts.AsReadOnly();
        public float RegenerationMultiplier => CombatSettings.Effects.Healer && units.Exists(u=>u.Active && !u.IsReviving) ? 2 : 1;
        private float HealUnit(DeployedUnitState unit,float amount)
        {
            if(!unit.Active || unit.FuelDying || unit.MandraPhase==MandragoraPhase.Reborn || unit.IsReviving || unit.FlamePhase!=FlamechaserPhase.Soldier || amount<=0) return 0;
            float restored=Mathf.Min(amount,unit.MaxHealth-unit.Health);
            if(restored<=0) return 0;
            unit.Health+=restored;unit.HealthRecovered+=restored;unit.LastHealTime=Clock;
            return restored;
        }
        private void ResolveHealing()
        {
            foreach(var cast in healingCasts)
            {
                if(cast.Resolved || Clock+1e-6<cast.ImpactAt) continue;
                cast.Resolved=true;var source=FindUnit(cast.SourceId);
                if(source==null || !source.Active || source.IsReviving) continue;
                foreach(string id in cast.TargetIds)
                {
                    var target=FindUnit(id);if(target==null) continue;
                    cast.HealthRestored+=HealUnit(target,CombatSettings.UnitAttack);
                }
                source.HealingDone+=cast.HealthRestored;
            }
        }
        private bool StepHealer(DeployedUnitState source)
        {
            if(Clock<source.HealingStartedAt+CombatSettings.AttackDuration)
            {source.IsMoving=source.HasCombatTarget=false;return true;}
            if(Clock+1e-6<source.NextHealAt) return false;
            var targets=new List<DeployedUnitState>();
            foreach(var target in units)
                if(target.Active && !target.IsReviving && target.Health<target.MaxHealth &&
                    GroundDistance(source.Motion.Position,target.Motion.Position)<=CombatSettings.UnitRange) targets.Add(target);
            targets.Sort((a,b)=> {int health=(a.Health/a.MaxHealth).CompareTo(b.Health/b.MaxHealth);return health!=0 ? health : string.CompareOrdinal(a.EntityId,b.EntityId);});
            if(targets.Count==0) return false;
            var ids=new List<string>();for(int i=0;i<Math.Min(3,targets.Count);i++) ids.Add(targets[i].EntityId);
            source.HealingStartedAt=Clock;source.NextHealAt=Clock+CombatSettings.UnitInterval;
            source.IsMoving=source.HasCombatTarget=false;
            healingCasts.Add(new HealingCast {SourceId=source.EntityId,TargetIds=ids.AsReadOnly(),StartedAt=Clock,ImpactAt=Clock+CombatSettings.AttackWindup});
            return true;
        }
        public sealed class IceRing
        {
            public Vector3 Position { get; internal set; }
            public double Time { get; internal set; }
            public int TargetsHit { get; internal set; }
        }
        private readonly List<IceRing> iceRings=new List<IceRing>();
        public IReadOnlyList<IceRing> IceRings => iceRings.AsReadOnly();
        public float UnitAttackMultiplier(DeployedUnitState unit) => TacticalAttackMultiplier*(UnitCombat(unit).Effects.Mudrock ? 1+Math.Min(6,unit.OrdinaryAttacksFired)*.6f :
            UnitCombat(unit).Effects.FrostNova && unit.ReviveUsed ? 1.5f : 1);
        public float UnitActionDuration(DeployedUnitState unit)=>unit.AltarEmpowered&&IsRole(unit,"M19")?1.6333334f:UnitCombat(unit).AttackDuration;
        public float UnitAttackRate(DeployedUnitState unit) => unit.ArtsBarrierRemaining>0 ? 1.5f : 1;
        public float UnitArmor(DeployedUnitState unit)
        {
            float armor=UnitCombat(unit).UnitArmor+TacticalArmorBonus;
            if(!CombatSettings.Effects.Phalanx || unit==null || !unit.Active) return armor;
            foreach(var other in units)
                if(other!=unit && other.Active && !other.IsReviving && other.UnitId==unit.UnitId && GroundDistance(other.Motion.Position,unit.Motion.Position)<=1.5f)
                    armor+=CombatSettings.UnitArmor*(200f/300);
            return armor;
        }
        private bool StepMudrock(DeployedUnitState unit)
        {
            if(Clock<unit.BarrierRefreshStartedAt+1.5) {unit.IsMoving=unit.HasCombatTarget=false;return true;}
            if(Clock+1e-6<unit.NextBarrierRefreshAt || Clock<unit.LastAttackTime+CombatSettings.AttackDuration/unit.LastAttackRate) return false;
            bool broken=unit.ArtsBarrierRemaining<=0;
            unit.ArtsBarrierRemaining=unit.ArtsBarrierLayerSize*3;unit.MaxHealth=Settings.Health*1.5f;
            if(broken) unit.NextAttackTime=Clock+Math.Max(0,unit.NextAttackTime-Clock)/1.5;
            unit.BarrierRefreshes++;unit.BarrierRefreshStartedAt=Clock;unit.NextBarrierRefreshAt=Clock+17;
            unit.NextAttackTime=Math.Max(unit.NextAttackTime,Clock+1.5);
            unit.IsMoving=unit.HasCombatTarget=false;return true;
        }
        private float DefenderAttackRate(DefenderState defender) =>
            (defender.ColdUntil>Clock ? .7f : 1)*(defender.FrostSlowUntil>Clock ? .5f : 1)*(HasGazeSlow(defender)? .3f:1);
        private void DefeatUnit(DeployedUnitState unit)
        {
            if(CombatSettings.Effects.Shackler)EndShackle(unit,true);
            if(CombatSettings.Effects.Mandragora&&unit.MandraPhase==MandragoraPhase.Ground){StartMandragoraRebirth(unit);return;}
            if(CombatSettings.Effects.Mandragora)EndGaze(unit,true);
            if(CombatSettings.Effects.Flamerazer){StartFuelExplosion(unit);return;}
            if(CombatSettings.Effects.Flamechaser&&unit.FlamePhase==FlamechaserPhase.Soldier){StartEmber(unit);return;}
            unit.DeathTime=Clock;unit.IsMoving=unit.IsBlocked=unit.HasCombatTarget=false;
            unit.CombatTargetId=unit.BlockerId=null;
            // First defeat interrupts this life, including projectiles already in flight.
            foreach(var shot in combatShots)
                if(!shot.Resolved && (shot.SourceId==unit.EntityId || shot.TargetId==unit.EntityId)) shot.Resolved=true;
            if(CombatSettings.Effects.FrostNova && !unit.ReviveUsed)
            {
                unit.ReviveUsed=true;unit.IsReviving=true;unit.ReviveAt=Clock+5;
                unit.IceRingStartedAt=double.NegativeInfinity;return;
            }
            unit.Resolution=UnitResolution.Dead;ScheduleDeathEffect(unit);
        }
        private bool StepIceRing(DeployedUnitState unit)
        {
            if(Clock<unit.IceRingStartedAt+3)
            {unit.IsMoving=unit.HasCombatTarget=false;return true;}
            if(Clock+1e-6<unit.NextIceRingAt) return false;
            var target=ChooseDefender(unit,2.5f);if(target==null) return false;
            unit.IceRingStartedAt=Clock;unit.NextIceRingAt=Clock+8.5;
            unit.NextAttackTime=Clock+3;unit.IsMoving=unit.HasCombatTarget=false;
            var ring=new IceRing {Position=unit.Motion.Position,Time=Clock};iceRings.Add(ring);
            foreach(var defender in defenders)
            {
                if(!defender.Alive || defender!=target && GroundDistance(unit.Motion.Position,defender.Position)>2) continue;
                ring.TargetsHit++;DamageDefender(defender,CombatSettings.UnitAttack*UnitAttackMultiplier(unit)*1.5f,CombatDamageType.Arts);
                if(!defender.Alive) continue;
                if(defender.FrostSlowUntil<=Clock) defender.NextAttackTime=Clock+Math.Max(0,defender.NextAttackTime-Clock)*2;
                defender.FrostSlowUntil=Math.Max(defender.FrostSlowUntil,Clock+8);
            }
            return true;
        }
        private readonly List<DeathBlast> deathBlasts=new List<DeathBlast>();
        public IReadOnlyList<DeathBlast> DeathBlasts => deathBlasts.AsReadOnly();
        public bool HasPendingDeathBlast => deathBlasts.Exists(x=>!x.Resolved)||fuelExplosions.Exists(x=>!x.Resolved)||pillarCommands.Exists(x=>!x.Resolved);
        public float DefenderArmor(DefenderState defender) => CombatSettings.DefenderArmor-defender.ArmorReduction;
        public static float CalculateDamage(float attack,float armor,float resistance,CombatDamageType type) =>
            Mathf.Max(1,type==CombatDamageType.Arts ? attack*(1-resistance/100) : attack-armor);

        private void DamageDefender(DefenderState defender,float attack,CombatDamageType type)
        {
            defender.Health=Mathf.Max(0,defender.Health-CalculateDamage(attack,DefenderArmor(defender),Mathf.Max(0,CombatSettings.Effects.Resistance-(defender.BurnUntil>Clock?20:0)),type));
            defender.LastHitTime=Clock;
            if(!defender.Alive) { defender.DeathTime=Clock;defender.TargetId=null; }
        }
        private void ScheduleDeathEffect(DeployedUnitState unit)
        {
            if(CombatSettings.Effects.BlastAttack>0) deathBlasts.Add(new DeathBlast(unit,Clock,CombatSettings.Effects));
        }
        public bool TryApplyCold(string defenderId,float duration)
        {
            if(float.IsNaN(duration) || float.IsInfinity(duration) || duration<=0)
                throw new ArgumentException("Cold duration must be finite and positive.");
            if(!EngagementEnabled || Phase!=BattlePhase.Running || IsPaused) return false;
            var defender=FindDefender(defenderId);
            if(defender==null || !defender.Alive) return false;
            bool cold=defender.ColdUntil>Clock || defender.FrozenUntil>Clock;
            if(cold) defender.FrozenUntil=Math.Max(defender.FrozenUntil,Clock+duration);
            else defender.NextAttackTime=Clock+Math.Max(0,defender.NextAttackTime-Clock)/.7;
            defender.ColdUntil=Math.Max(defender.ColdUntil,Clock+duration);
            return true;
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
                    if(defender.Alive && effects.ColdDuration>0) TryApplyCold(defender.EntityId,effects.ColdDuration);
                }
            }
        }
    }
}
