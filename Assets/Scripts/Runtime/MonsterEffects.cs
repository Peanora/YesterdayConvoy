using System;
using System.Collections.Generic;
using System.Linq;
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
        public readonly int TauntLevel;
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
        public readonly bool Flamechaser, Flamerazer, Mandragora, NervousCreeper, Harvester, FirstSpeaker, Shackler, TacticalCommander, OrderRunner, OrderSniper, Patriot, TalulahSecondPhase;
        public MonsterEffectSettings(CombatDamageType damageType=CombatDamageType.Physical, float resistance=0, float armorShred=0,
            float blastAttack=0, float blastRadius=0, float blastDelay=0, float coldDuration=0, float frozenAttackMultiplier=0,
            int coldAttackEvery=0,float attackColdDuration=0,bool frostNova=false,float regenerationFraction=0,int attackTargets=0,float lifeSteal=0,bool healer=false,
            bool artsBarrier=false,float unitResistance=0,bool mudrock=false,bool phalanx=false,bool flamechaser=false,bool flamerazer=false,bool mandragora=false,bool nervousCreeper=false,bool harvester=false,bool firstSpeaker=false,bool shackler=false,bool tacticalCommander=false,bool orderRunner=false,bool orderSniper=false,int tauntLevel=0,bool patriot=false,bool talulahSecondPhase=false)
        {
            foreach(float value in new[]{resistance,armorShred,blastAttack,blastRadius,blastDelay,coldDuration,frozenAttackMultiplier,attackColdDuration,regenerationFraction,lifeSteal,unitResistance})
                if(float.IsNaN(value) || float.IsInfinity(value) || value<0) throw new ArgumentException("Invalid monster effect value.");
            if((damageType!=CombatDamageType.Physical && damageType!=CombatDamageType.Arts) || resistance>100 ||
                blastAttack>0 && blastRadius<=0 || coldDuration>0 && blastAttack<=0 ||
                frozenAttackMultiplier>0 && frozenAttackMultiplier<1 || coldAttackEvery<0 ||
                (coldAttackEvery>0)!=(attackColdDuration>0) || regenerationFraction>1 || attackTargets<0 || attackTargets>3 || unitResistance>100 || tauntLevel < -2 || tauntLevel > 2)
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
            Flamechaser=flamechaser;Flamerazer=flamerazer;Mandragora=mandragora;NervousCreeper=nervousCreeper;Harvester=harvester;FirstSpeaker=firstSpeaker;Shackler=shackler;TacticalCommander=tacticalCommander;OrderRunner=orderRunner;OrderSniper=orderSniper;TauntLevel=tauntLevel;Patriot=patriot;TalulahSecondPhase=talulahSecondPhase;
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
        // Independent skill growth comes from its own v2 row. Team modifiers apply
        // once; native ordinary-attack phase/ramp multipliers must not apply again.
        float NativeSkillDamage(DeployedUnitState unit,string skill,string key,float legacy)
        {
            float value=V2Skill(unit,skill,key,legacy);
            return CampaignV2Enabled&&unit!=null&&!unit.SynergySupport&&unit.CardId!=SilverWolfCombat.CardId?
                value*V2IndependentAttackMultiplier(unit):value;
        }
        // One model entry for direct and projectile damage; damage cannot mutate a paused/finished battle.
        public bool TryDamageUnit(string entityId,float attack,CombatDamageType type,float ignoreResistance=0,bool ranged=false,DefenderState sourceDefender=null,bool accuracyChecked=false)
        {
            if(float.IsNaN(ignoreResistance)||float.IsInfinity(ignoreResistance)||ignoreResistance<0||float.IsNaN(attack) || float.IsInfinity(attack) || attack<=0 ||
                type!=CombatDamageType.Physical && type!=CombatDamageType.Arts) throw new ArgumentException("Invalid incoming damage.");
            if(!EngagementEnabled || Phase!=BattlePhase.Running || IsPaused) return false;
            var unit=FindUnit(entityId);if(unit==null || !unit.Active || unit.IsReviving || StrongholdInTransit(unit.EntityId) || UnitInvulnerable(unit)) return false;
            if(!accuracyChecked&&sourceDefender!=null&&!BossRosterDefenderAttackHits(sourceDefender,type))return false;
            BossRosterBeginIncomingDamage(unit,sourceDefender);
            if(unit.FlamePhase==FlamechaserPhase.Ember)return ApplyUnitHealthDamage(unit,1);
            // Penetration stops at zero; existing resistance debuffs may remain negative.
            float resistance=UnitResistance(unit);
            if(resistance>0)resistance=Mathf.Max(0,resistance-ignoreResistance);
            float damage=CalculateDamage(attack,UnitArmor(unit),resistance,type)*SixthVulnerability(unit)*(type==CombatDamageType.Arts?UnitArtsVulnerability(unit):1);
            damage=AbsorbContractShield(unit,damage);if(damage<=0)return true;
            if(unit.SynergyShieldUntil>Clock){float absorbed=Mathf.Min(damage,unit.SynergyShield);unit.SynergyShield-=absorbed;damage-=absorbed;RecordUnitShieldAbsorption(unit,absorbed);if(damage<=0)return true;}
            if(UnitCombat(unit).Effects.Mandragora&&unit.StoneShield)damage*=V2Skill(unit,"stoneShield","incomingDamageMultiplier",.4f);
            if(type==CombatDamageType.Arts && unit.ArtsBarrierRemaining>0)
            {
                float absorbed=Mathf.Min(unit.ArtsBarrierRemaining,damage);unit.ArtsBarrierRemaining-=absorbed;damage-=absorbed;RecordUnitShieldAbsorption(unit,absorbed);
                if(unit.ArtsBarrierRemaining<=.00001f)
                {
                    unit.BarrierBrokenAt=Clock;unit.ArtsBarrierRemaining=0;unit.MaxHealth=UnitBaseHealth(unit);unit.Health=Mathf.Min(unit.Health,unit.MaxHealth);
                    unit.NextAttackTime=Clock+Math.Max(0,unit.NextAttackTime-Clock)*1.5;
                }
            }
            damage=BossRosterAdjustIncomingDamage(unit,damage,type);
            if(damage<=0){unit.LastHitTime=Clock;return true;}
            OnMarineContact(unit);
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
            public IReadOnlyList<string> AppliedTargetIds { get; internal set; } = Array.Empty<string>();
        }
        private readonly List<HealingCast> healingCasts=new List<HealingCast>();
        public IReadOnlyList<HealingCast> HealingCasts => healingCasts.AsReadOnly();
        public float RegenerationMultiplier
        {get{var healer=units.Find(u=>u.Active&&!u.IsReviving&&UnitCombat(u).Effects.Healer);return healer==null?1:V2Skill(healer,"hostAura","regenerationMultiplier",2);}}
        private float HealUnit(DeployedUnitState unit,float amount)
        {
            if(!unit.Active || TalulahTransitionLocked(unit) || unit.FuelDying || unit.MandraPhase==MandragoraPhase.Reborn || PatriotReborn(unit) || unit.IsReviving || unit.FlamePhase!=FlamechaserPhase.Soldier || amount<=0) return 0;
            float restored=Mathf.Min(amount,unit.MaxHealth-unit.Health);
            if(restored<=0) return 0;
            unit.Health+=restored;unit.HealthRecovered+=restored;unit.LastHealTime=Clock;
            combatFeedback.Add(Clock,CombatFeedbackTarget.Monster,unit.EntityId,unit.Motion.Position,CombatFeedbackKind.HealthRestored,restored);
            return restored;
        }
        private void ResolveHealing()
        {
            foreach(var cast in healingCasts)
            {
                if(cast.Resolved || Clock+1e-6<cast.ImpactAt) continue;
                cast.Resolved=true;var source=FindUnit(cast.SourceId);
                if(source==null || !source.Active || source.IsReviving) continue;
                var applied=new List<string>();
                foreach(string id in cast.TargetIds)
                {
                    var target=FindUnit(id);if(target==null) continue;
                    float restored=HealUnit(target,V2Skill(source,"healing","healPerTarget",UnitCombat(source).UnitAttack));
                    cast.HealthRestored+=restored;if(restored>0)applied.Add(id);
                }
                cast.AppliedTargetIds=applied.AsReadOnly();
                source.HealingDone+=cast.HealthRestored;
            }
        }
        private bool StepHealer(DeployedUnitState source)
        {
            if(Clock<source.HealingStartedAt+UnitCombat(source).AttackDuration)
            {source.IsMoving=source.HasCombatTarget=false;return true;}
            if(Clock+1e-6<source.NextHealAt) return false;
            var targets=new List<DeployedUnitState>();
            foreach(var target in units)
                if(target.Active && !target.IsReviving && !TalulahTransitionLocked(target) && target.Health<target.MaxHealth &&
                    GroundDistance(source.Motion.Position,target.Motion.Position)<=UnitCombat(source).UnitRange) targets.Add(target);
            targets.Sort((a,b)=> {int health=(a.Health/a.MaxHealth).CompareTo(b.Health/b.MaxHealth);return health!=0 ? health : string.CompareOrdinal(a.EntityId,b.EntityId);});
            if(targets.Count==0) return false;
            var ids=new List<string>();for(int i=0;i<Math.Min((int)V2Skill(source,"healing","maximumTargets",3),targets.Count);i++) ids.Add(targets[i].EntityId);
            source.HealingStartedAt=Clock;source.NextHealAt=Clock+UnitCombat(source).UnitInterval;
            source.IsMoving=source.HasCombatTarget=false;
            healingCasts.Add(new HealingCast {SourceId=source.EntityId,TargetIds=ids.AsReadOnly(),StartedAt=Clock,ImpactAt=Clock+UnitCombat(source).AttackWindup});
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
        public float UnitAttackMultiplier(DeployedUnitState unit) => BossRosterUnitAttackMultiplier(unit)*(TacticalAttackMultiplier+ContractAttackBonus(unit)+ReunionMemberBonus(unit)-(IsPatriot(unit)&&unit.PatriotPhase!=PatriotPhase.March?V2Skill(unit,"dominion","attackBonus",.2f):0))*(IsPatriot(unit)&&unit.PatriotPhase==PatriotPhase.March?V2Skill(unit,"march","marchMultiplier",1.5f):1)*(UnitCombat(unit).Effects.Mudrock ? 1+Math.Min((int)V2Skill(unit,"ramp","maximumStacks",6),unit.OrdinaryAttacksFired)*V2Skill(unit,"ramp","attackBonusPerStack",.6f) :
            UnitCombat(unit).Effects.FrostNova && unit.ReviveUsed ? V2Skill(unit,"revive","secondLifeBasicAttackMultiplier",1.5f) : 1)*InfectionAttack(unit.EntityId)*(TalulahSecondForm(unit)?V2Skill(unit,"threeStarPhase","secondFormAttackMultiplier",1.5f):1);
        public float UnitActionDuration(DeployedUnitState unit)=>IsPatriot(unit)&&unit.PatriotPhase==PatriotPhase.Destruction?V2Skill(unit,"rebirth","destructionActionDuration",1.3666667f):unit.AltarEmpowered&&IsRole(unit,"M19")?1.6333334f:UnitCombat(unit).AttackDuration;
        public float UnitAttackRate(DeployedUnitState unit) => BossRosterUnitAttackRate(unit)*(unit.ArtsBarrierRemaining>0 ? 1.5f : 1)*InfectionRate(unit.EntityId);
        public float UnitArmor(DeployedUnitState unit)
        {
            float armor=UnitCombat(unit).UnitArmor+TacticalArmorBonus-SeventhArmorReduction(unit)-(IsPatriot(unit)&&unit.PatriotPhase!=PatriotPhase.March?V2Skill(unit,"dominion","armorBonus",2):0);
            if(IsPatriot(unit)&&unit.PatriotPhase==PatriotPhase.March)armor*=3;
            if(!UnitCombat(unit).Effects.Phalanx || unit==null || !unit.Active) return armor;
            foreach(var other in units)
                if(other!=unit && other.Active && !other.IsReviving && other.UnitId==unit.UnitId && GroundDistance(other.Motion.Position,unit.Motion.Position)<=V2Skill(unit,"phalanx","radius",1.5f))
                    armor+=UnitCombat(unit).UnitArmor*V2Skill(unit,"phalanx","armorFractionPerAlly",200f/300);
            return armor;
        }
        private bool StepMudrock(DeployedUnitState unit)
        {
            if(Clock<unit.BarrierRefreshStartedAt+MudrockRefreshDuration)
            {
                if(!unit.BarrierRefreshApplied&&Clock+1e-6>=unit.BarrierRefreshStartedAt+V2Skill(unit,"refresh","applyAt",.7333334f))
                {
                    bool broken=unit.ArtsBarrierRemaining<=0;
                    unit.ArtsBarrierRemaining=unit.ArtsBarrierLayerSize*3;unit.MaxHealth=UnitBaseHealth(unit)*1.5f;
                    if(broken)unit.NextAttackTime=Clock+Math.Max(0,unit.NextAttackTime-Clock)/1.5;
                    unit.BarrierRefreshApplied=true;unit.BarrierRefreshes++;
                }
                unit.IsMoving=unit.HasCombatTarget=false;return true;
            }
            if(Clock+1e-6<unit.NextBarrierRefreshAt||IsSkillActionPlaying(unit)||Clock<unit.LastAttackTime+UnitCombat(unit).AttackDuration/unit.LastAttackRate)return false;
            unit.BarrierRefreshApplied=false;unit.BarrierRefreshStartedAt=Clock;unit.NextBarrierRefreshAt=Clock+MudrockRefreshDuration+V2Skill(unit,"refresh","afterActionCooldown",17);
            unit.NextAttackTime=Math.Max(unit.NextAttackTime,Clock+MudrockRefreshDuration);
            unit.IsMoving=unit.HasCombatTarget=false;return true;
        }
        private float DefenderAttackRate(DefenderState defender) => BossRosterDefenderAttackRate(defender)*
            (defender.ColdUntil>Clock ? .7f : 1)*(defender.FrostSlowUntil>Clock ? .5f : 1)*(HasGazeSlow(defender)? .3f:1)*
            (DefenderSkillActive(defender)?defender.Settings.Skill.AttackSpeedMultiplier:1)*InfectionRate(defender.EntityId)*(defender.Settings.Skill.Kind==DefenderSkillKind.ChenDawnstreak?1.13f:1)+BossRosterDefenderAttackSpeedBonus(defender);
        private void DefeatUnit(DeployedUnitState unit)
        {
            if(BossRosterPreventDeath(unit))return;
            if(TryStartTalulahFall(unit))return;
            if(IsPatriot(unit)&&unit.PatriotPhase==PatriotPhase.March){StartPatriotRebirth(unit);return;}
            if(UnitCombat(unit).Effects.Shackler)EndShackle(unit,true);
            if(UnitCombat(unit).Effects.Mandragora&&unit.MandraPhase==MandragoraPhase.Ground){StartMandragoraRebirth(unit);return;}
            if(UnitCombat(unit).Effects.Mandragora)EndGaze(unit,true);
            if(UnitCombat(unit).Effects.Flamerazer){StartFuelExplosion(unit);return;}
            if(UnitCombat(unit).Effects.Flamechaser&&unit.FlamePhase==FlamechaserPhase.Soldier){StartEmber(unit);return;}
            unit.DeathTime=Clock;unit.IsMoving=unit.IsBlocked=unit.HasCombatTarget=false;
            unit.CombatTargetId=unit.BlockerId=null;
            // First defeat interrupts this life, including projectiles already in flight.
            foreach(var shot in combatShots)
                if(!shot.Resolved && (shot.SourceId==unit.EntityId || shot.TargetId==unit.EntityId)) shot.Resolved=true;
            if(UnitCombat(unit).Effects.FrostNova && !unit.ReviveUsed)
            {
                unit.ReviveUsed=true;unit.IsReviving=true;unit.ReviveAt=Clock+V2Skill(unit,"revive","delay",5);
                unit.IceRingStartedAt=unit.FrostSealStartedAt=double.NegativeInfinity;return;
            }
            unit.Resolution=UnitResolution.Dead;ScheduleDeathEffect(unit);OnAdvancedSynergyDeath(unit);OnRemainingDeath(unit);
        }
        private bool StepIceRing(DeployedUnitState unit)
        {
            if(Clock<unit.IceRingStartedAt+V2Skill(unit,"iceRing","actionDuration",3))
            {unit.IsMoving=unit.HasCombatTarget=false;return true;}
            if(Clock+1e-6<unit.NextIceRingAt) return false;
            if(AutomaticSkillsEnabled&&!AutomaticLegacyBodyAvailable(unit))return false;
            var target=ChooseDefender(unit,V2Skill(unit,"iceRing","searchRange",2.5f));if(target==null) return false;
            unit.IceRingStartedAt=Clock;unit.NextIceRingAt=Clock+AutomaticLegacyIceRingCooldown(unit);
            unit.NextAttackTime=Clock+V2Skill(unit,"iceRing","actionDuration",3);unit.IsMoving=unit.HasCombatTarget=false;
            var ring=new IceRing {Position=unit.Motion.Position,Time=Clock};iceRings.Add(ring);
            BossCoreOnIceRingStarted(unit);
            foreach(var defender in defenders)
            {
                if(!defender.Alive || defender!=target && GroundDistance(unit.Motion.Position,defender.Position)>BossCoreIceRingRadius(unit,V2Skill(unit,"iceRing","splashRadius",2))) continue;
                float beforeHealth=defender.Health,beforeDebt=defender.Debt,beforeTotal=defenders.Sum(d=>d.Health);
                ring.TargetsHit++;DamageDefenderFrom(defender,NativeSkillDamage(unit,"iceRing","damage",UnitCombat(unit).UnitAttack*UnitAttackMultiplier(unit)*1.5f)*BossCoreIceRingMultiplier(unit,defender),CombatDamageType.Arts,unit);
                BossCoreOnIceRingHit(unit,defender,defender.Health<beforeHealth||defender.Debt>beforeDebt||defenders.Sum(d=>d.Health)<beforeTotal);
                if(!defender.Alive) continue;
                if(defender.FrostSlowUntil<=Clock) defender.NextAttackTime=Clock+Math.Max(0,defender.NextAttackTime-Clock)*2;
                defender.FrostSlowUntil=Math.Max(defender.FrostSlowUntil,Clock+V2Skill(unit,"iceRing","slowDuration",8));
            }
            return true;
        }
        private readonly List<DeathBlast> deathBlasts=new List<DeathBlast>();
        public IReadOnlyList<DeathBlast> DeathBlasts => deathBlasts.AsReadOnly();
        public bool HasPendingDeathBlast => HasPendingDeepBurn||herdQueue.Count>0||infectionBursts.Exists(x=>!x.Resolved)||deathBlasts.Exists(x=>!x.Resolved)||fuelExplosions.Exists(x=>!x.Resolved)||pillarCommands.Exists(x=>!x.Resolved);
        public float DefenderArmor(DefenderState defender) => defender.Settings.Armor*(defender.Settings.Skill.Kind==DefenderSkillKind.PramanixSnow&&!DefenderSkillActive(defender)?3:1)*
            (DefenderSkillActive(defender)?defender.Settings.Skill.DefenseMultiplier:1)-defender.ArmorReduction;
        public static float CalculateDamage(float attack,float armor,float resistance,CombatDamageType type) =>
            Mathf.Max(1,type==CombatDamageType.Arts ? attack*(1-resistance/100) : attack-armor);

        private void DamageDefender(DefenderState defender,float attack,CombatDamageType type,bool derived=false,DeployedUnitState source=null,float damageScale=1)
            =>ApplyDefenderAttack(defender,attack,type,derived,source,damageScale,source);
        // Statistics attribution is independent of on-hit gameplay procs. Native abilities
        // which previously had no proc source must retain that behavior.
        void DamageDefenderFrom(DefenderState defender,float attack,CombatDamageType type,DeployedUnitState creditSource,bool derived=false,float damageScale=1)
            =>ApplyDefenderAttack(defender,attack,type,derived,null,damageScale,creditSource);
        void ApplyDefenderAttack(DefenderState defender,float attack,CombatDamageType type,bool derived,DeployedUnitState source,float damageScale,DeployedUnitState creditSource)
        {
            if(TryChenDawnDodge(defender,type))return;
            float healthBefore=defender.Health,debtBefore=defender.Debt;bool alive=defender.Alive,burning=DeepBurning(defender);
            ApplyRankedDefenderLoss(defender,CalculateDamage(attack,DefenderArmor(defender),Mathf.Max(0,CombatSettings.Effects.Resistance+(defender.Settings.Skill.Kind==DefenderSkillKind.PramanixSnow&&!DefenderSkillActive(defender)?20:0)-(defender.BurnUntil>Clock?20:0)),type)*damageScale*SeventhDamageMultiplier(defender,source),creditSource,true);
            bool deepHit=alive&&!derived&&IsDeepHit(source)&&(defender.Health<healthBefore||defender.Debt>debtBefore);
            if(deepHit&&defender.Alive)ApplyDeepBurn(defender);
            if(alive&&!defender.Alive&&!derived)OnDeepKill(defender,burning||deepHit);
            if(source!=null&&!derived&&defender.Health<healthBefore)OnMarineContact(source);
            defender.LastHitTime=Clock;
            if(!defender.Alive) { defender.DeathTime=Clock;defender.TargetId=null; }
        }
        private void ScheduleDeathEffect(DeployedUnitState unit)
        {
            var effects=UnitCombat(unit).Effects;
            if(effects.BlastAttack>0) deathBlasts.Add(new DeathBlast(unit,Clock,effects));
        }
        public bool TryApplyCold(string defenderId,float duration)
        {
            if(float.IsNaN(duration) || float.IsInfinity(duration) || duration<=0)
                throw new ArgumentException("Cold duration must be finite and positive.");
            if(!EngagementEnabled || Phase!=BattlePhase.Running || IsPaused) return false;
            var defender=FindDefender(defenderId);
            if(defender==null || !defender.Alive) return false;
            if(synergyEnabled)
            {
                if(defender.FrozenUntil>Clock){RefreshCombatFeedbackStatuses();return true;}
                if(defender.FrozenUntil>0&&Clock<defender.FrozenUntil+1.2)return false;
                if(defender.ColdUntil>Clock){defender.FrozenUntil=Clock+1.2;defender.ColdUntil=defender.FrozenUntil;RefreshCombatFeedbackStatuses();return true;}
            }
            bool cold=defender.ColdUntil>Clock || defender.FrozenUntil>Clock;
            if(cold) defender.FrozenUntil=Math.Max(defender.FrozenUntil,Clock+duration);
            else defender.NextAttackTime=Clock+Math.Max(0,defender.NextAttackTime-Clock)/.7;
            defender.ColdUntil=Math.Max(defender.ColdUntil,Clock+duration);
            RefreshCombatFeedbackStatuses();return true;
        }
        private void ResolveDeathEffects()
        {
            ResolveInfectionBursts();
            foreach(var blast in deathBlasts)
            {
                if(blast.Resolved || Clock+1e-6<blast.DetonateAt) continue;
                blast.Resolved=true;var effects=UnitCombat(FindUnit(blast.SourceId)).Effects;
                // Source is already dead. A death burst must survive its source and fire once.
                foreach(var defender in defenders)
                {
                    if(!defender.Alive || GroundDistance(blast.Position,defender.Position)>blast.Radius) continue;
                    blast.TargetsHit++;DamageDefenderFrom(defender,effects.BlastAttack,CombatDamageType.Physical,FindUnit(blast.SourceId));
                    if(defender.Alive && effects.ColdDuration>0) TryApplyCold(defender.EntityId,effects.ColdDuration);
                }
            }
        }
    }
}
