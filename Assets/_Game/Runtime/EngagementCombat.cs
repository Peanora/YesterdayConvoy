using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public readonly struct EngagementSettings
    {
        public readonly float UnitAttack, UnitArmor, UnitRange, UnitInterval;
        public readonly float DefenderHealth, DefenderAttack, DefenderArmor, DefenderRange, DefenderInterval, BlockRadius;
        public readonly int BlockCount, SkillAmmo;
        public readonly bool FireRain, HasSkill, Melee, CanAttack;
        public readonly float AttackDuration, AttackWindup;
        public readonly MonsterEffectSettings Effects;
        public readonly CombatDamageType DefenderDamageType;
        public readonly bool RevealsInvisible, DefenderCanHitAir;
        public readonly float InitialSP, RequiredSP, SPPerSecond, SkillAttack, SkillInterval, SkillRange, ProjectileTime;

        public EngagementSettings(float unitAttack = 14, float unitArmor = 2, float unitRange = 0.65f, float unitInterval = 1.7f,
            float defenderHealth = 180, float defenderAttack = 22, float defenderArmor = 3, float defenderRange = 3.2f,
            float defenderInterval = 1, int blockCount = 1, float blockRadius = 0.65f,
            float initialSP = 8, float requiredSP = 12, float spPerSecond = 1, int skillAmmo = 6,
            float skillAttack = 105, float skillInterval = 3.2f, float skillRange = 2.6f, float projectileTime = 0.22f, bool fireRain = false, bool hasSkill = true, bool melee = false, float attackDuration = 0, float attackWindup = 0, MonsterEffectSettings effects = default, bool canAttack = true,CombatDamageType defenderDamageType=CombatDamageType.Physical,bool revealsInvisible=false,bool defenderCanHitAir=true)
        {
            foreach (float value in new[] { unitAttack, unitRange, unitInterval, defenderHealth, defenderAttack, defenderRange,
                defenderInterval, blockRadius, projectileTime })
                if (!Finite(value) || value <= 0) throw new ArgumentException("Combat values must be finite and positive.");
            if (!Finite(unitArmor) || !Finite(defenderArmor) || !Finite(initialSP) || unitArmor < 0 || defenderArmor < 0 ||
                initialSP < 0 || blockCount < 0)
                throw new ArgumentException("Invalid combat armor, block count or initial SP.");
            if (hasSkill)
            {
                foreach(float value in new[]{requiredSP,spPerSecond,skillAttack,skillInterval,skillRange})
                    if(!Finite(value) || value<=0) throw new ArgumentException("Skill values must be finite and positive.");
                if(initialSP>requiredSP || skillAmmo<=0) throw new ArgumentException("Invalid initial SP or ammo.");
            }
            else if(fireRain || initialSP!=0 || requiredSP!=0 || spPerSecond!=0 || skillAmmo!=0 || skillAttack!=0 || skillInterval!=0 || skillRange!=0)
                throw new ArgumentException("A unit without a skill must have zero skill resources.");
            if(!Finite(attackDuration) || !Finite(attackWindup) || attackDuration<0 || attackDuration>unitInterval || attackWindup<0 || attackWindup>attackDuration)
                throw new ArgumentException("Invalid attack animation timing.");
            if(effects.NervousCreeper&&(canAttack||hasSkill))throw new ArgumentException("Creeper cannot use ordinary attacks or ammo skills.");
            if(effects.Healer && (canAttack || hasSkill)) throw new ArgumentException("Healer uses automatic treatment, not damage attacks or ammunition skills.");
            if(defenderDamageType!=CombatDamageType.Physical && defenderDamageType!=CombatDamageType.Arts) throw new ArgumentException("Invalid defender damage type.");
            if(effects.Patriot&&unitInterval<=.4f)throw new ArgumentException("Patriot requires a positive second-phase attack interval.");
            Effects=effects;
            DefenderDamageType=defenderDamageType;
            RevealsInvisible=revealsInvisible;DefenderCanHitAir=defenderCanHitAir;
            AttackDuration=attackDuration; AttackWindup=attackWindup;
            HasSkill=hasSkill; Melee=melee; CanAttack=canAttack;
            FireRain = fireRain;
            UnitAttack = unitAttack; UnitArmor = unitArmor; UnitRange = unitRange; UnitInterval = unitInterval;
            DefenderHealth = defenderHealth; DefenderAttack = defenderAttack; DefenderArmor = defenderArmor;
            DefenderRange = defenderRange; DefenderInterval = defenderInterval; BlockCount = blockCount; BlockRadius = blockRadius;
            InitialSP = initialSP; RequiredSP = requiredSP; SPPerSecond = spPerSecond; SkillAmmo = skillAmmo;
            SkillAttack = skillAttack; SkillInterval = skillInterval; SkillRange = skillRange; ProjectileTime = projectileTime;
        }
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public sealed class DefenderState
    {
        public string EntityId { get; }
        public DefenderSettings Settings { get; }
        public float MaxHealth => Settings.Health;
        public double LastActionTime { get; internal set; } = double.NegativeInfinity;
        public bool LastActionHealing { get; internal set; }
        public Vector3 Position { get; }
        public float Health { get; internal set; }
        public bool Alive => Health > 0;
        public string TargetId { get; internal set; }
        public bool HighGround {get;internal set;}
        public int BlockedCount { get; internal set; }
        public double LastHitTime { get; internal set; } = double.NegativeInfinity;
        public double DeathTime { get; internal set; } = double.PositiveInfinity;
        public float ArmorReduction { get; internal set; }
        public float BurnAccumulated {get;internal set;}
        public float NeuralAccumulated {get;internal set;}
        public double NeuralUntil {get;internal set;}=double.NegativeInfinity;
        public double LastNeuralBurstAt {get;internal set;}=double.NegativeInfinity;
        public int NeuralBursts {get;internal set;}
        public double BurnUntil {get;internal set;}=double.NegativeInfinity;
        public double ColdUntil { get; internal set; } = double.NegativeInfinity;
        public double FrozenUntil { get; internal set; } = double.NegativeInfinity;
        public double FrostSlowUntil { get; internal set; } = double.NegativeInfinity;
        internal double NextAttackTime;
        internal DefenderState(DefenderSettings settings)
        { Settings=settings; EntityId=settings.Id; Position=settings.Position; Health=settings.Health; }
    }

    public sealed class CombatShot
    {
        public string SourceId { get; }
        public string TargetId { get; }
        public bool FromDefender { get; }
        public bool Empowered { get; }
        public bool FireRain { get; }
        public bool Healing { get; }
        public CombatDamageType DamageType { get; }
        public float SplashRadius { get; }
        public Vector3 Origin { get; }
        public Vector3 Destination { get; }
        public double StartedAt { get; }
        public double ImpactAt { get; }
        public float Attack { get; }
        public float ColdDuration { get; }
        public float BurnDamage {get;internal set;}
        public float NeuralDamage {get;internal set;}
        public bool Resolved { get; internal set; }
        public bool Applied { get; internal set; }
        public bool FrozenBonusApplied { get; internal set; }
        public float DamageDealt { get; internal set; }
        internal CombatShot(string source, string target, bool defender, bool empowered, Vector3 origin, Vector3 destination,
            double start, float duration, float attack, bool fireRain = false, float coldDuration = 0, bool healing = false, CombatDamageType damageType = CombatDamageType.Physical, float splashRadius = 0)
        { ColdDuration=coldDuration; Healing=healing; DamageType=damageType; SplashRadius=splashRadius; FireRain=fireRain; SourceId = source; TargetId = target; FromDefender = defender; Empowered = empowered; Origin = origin;
            Destination = destination; StartedAt = start; ImpactAt = start + duration; Attack = attack; }
    }

    public enum SkillFailure { None, NotRunning, Paused, StaleRun, MissingEntity, NotReady, AlreadyActive, NoSkill }

    public sealed partial class ManualBattle
    {
        private readonly EngagementSettings? engagement;
        private readonly List<CombatShot> combatShots = new List<CombatShot>();
        private readonly Dictionary<long, string> skillRequests = new Dictionary<long, string>();
        public bool EngagementEnabled => engagement.HasValue;
        public EngagementSettings CombatSettings => engagement.Value;
        private readonly Vector3[] defenderPositions;
        private readonly DefenderSettings[] defenderRoster;
        private readonly List<DefenderState> defenders = new List<DefenderState>();
        public IReadOnlyList<DefenderState> Defenders => defenders.AsReadOnly();
        public DefenderState Defender => defenders.Count>0 ? defenders[0] : null;
        public DefenderState FindDefender(string id) => defenders.Find(d=>d.EntityId==id);
        public IReadOnlyList<CombatShot> CombatShots => combatShots.AsReadOnly();
        public int SkillCasts { get; private set; }

        private void ResetEngagement()
        {
            flameSprays.Clear();fuelExplosions.Clear();gazes.Clear();pillarCommands.Clear();creeperPulses.Clear();bubbles.Clear();shackles.Clear();
            combatShots.Clear(); deathBlasts.Clear(); iceRings.Clear(); healingCasts.Clear(); skillRequests.Clear(); SkillCasts = 0;
            defenders.Clear();
            if(!EngagementEnabled) return;
            if(defenderRoster!=null) { foreach(var spec in defenderRoster) defenders.Add(new DefenderState(spec)); return; }
            var c=CombatSettings;
            for(int i=0;i<defenderPositions.Length;i++)
                defenders.Add(new DefenderState(new DefenderSettings(i==0 ? "kroos" : "kroos-"+i,"克洛丝",null,
                    defenderPositions[i],c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,
                    c.BlockCount,damageType:c.DefenderDamageType,blockRadius:c.BlockRadius)));
        }

        public DeployedUnitState FindUnit(string id)
        { foreach (var unit in units) if (unit.EntityId == id) return unit; return null; }

        public bool IsSkillActionPlaying(DeployedUnitState unit) => EngagementEnabled && unit!=null && unit.Active && UnitCombat(unit).HasSkill &&
            (UnitCombat(unit).FireRain ? Clock<unit.FireRainStartedAt+UnitCombat(unit).SkillInterval :
            unit.LastAttackEmpowered && Clock<unit.LastAttackTime+UnitCombat(unit).SkillInterval);

        // Readiness stays visible during a full pause; authorization to cast is checked separately.
        public bool IsSkillReady(string entityId)
        {
            var unit=FindUnit(entityId);
            return EngagementEnabled && Phase==BattlePhase.Running && unit!=null && unit.Active && UnitCombat(unit).HasSkill &&
                unit.SkillAmmo==0 && (!UnitCombat(unit).FireRain || !IsSkillActionPlaying(unit)) && unit.SkillSP+0.00001f>=UnitCombat(unit).RequiredSP;
        }

        public SkillFailure CheckSkill(string entityId)
        {
            if (!EngagementEnabled || Phase != BattlePhase.Running) return SkillFailure.NotRunning;
            if (!CombatSettings.HasSkill) return SkillFailure.NoSkill;
            var unit = FindUnit(entityId);
            if(unit!=null && !UnitCombat(unit).HasSkill)return SkillFailure.NoSkill;
            if (IsPaused) return SkillFailure.Paused;
            if (unit == null || !unit.Active) return SkillFailure.MissingEntity;
            var c=UnitCombat(unit);
            if (unit.SkillAmmo > 0 || c.FireRain && IsSkillActionPlaying(unit)) return SkillFailure.AlreadyActive;
            return unit.SkillSP + 0.00001f >= c.RequiredSP ? SkillFailure.None : SkillFailure.NotReady;
        }

        public SkillFailure TryActivateSkill(int runId, long requestId, string entityId)
        {
            if (runId != RunId) return SkillFailure.StaleRun;
            if (skillRequests.TryGetValue(requestId, out string previous))
                return previous == entityId ? SkillFailure.None : SkillFailure.StaleRun;
            SkillFailure failure = CheckSkill(entityId);
            if (failure != SkillFailure.None) return failure;
            var unit = FindUnit(entityId);
            var c=UnitCombat(unit);
            unit.SkillSP = 0; unit.SkillAmmo = c.SkillAmmo; unit.SkillsUsed++; SkillCasts++;
            unit.NextAttackTime = Clock;
            if(c.FireRain)
            { unit.FireRainStartedAt=Clock; unit.IsMoving=false; unit.NextAttackTime=Clock+c.SkillInterval; }
            skillRequests.Add(requestId, entityId);
            return SkillFailure.None;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0; return Vector3.Distance(a, b); }

        private void StepEngagement(float step)
        {
            EngagementSettings guardSettings=CombatSettings;
            StepFacilities(step);
            ResolveFuelExplosions();ResolvePillarCommands();ResolveCorrosiveBubbles();
            ResolveDeathEffects();ResolveShackles();
            ResolveHealing();
            foreach(var shot in combatShots)
            {
                if(shot.Resolved || Clock+0.000001<shot.ImpactAt) continue;
                shot.Resolved=true;
                if(shot.Healing)
                {
                    var healer=FindDefender(shot.SourceId); var recipient=FindDefender(shot.TargetId);
                    if(healer!=null && healer.Alive && recipient!=null && recipient.Alive)
                    { recipient.Health=Mathf.Min(recipient.MaxHealth,recipient.Health+shot.Attack); shot.Applied=true; }
                    continue;
                }
                var unit=FindUnit(shot.FromDefender ? shot.TargetId : shot.SourceId);
                var defender=FindDefender(shot.FromDefender ? shot.SourceId : shot.TargetId);
                if(unit==null || !unit.Active || unit.IsReviving || defender==null || !defender.Alive) continue;
                if(shot.FromDefender&&UnitAirborne(unit)&&!guardSettings.DefenderCanHitAir)continue;
                shot.Applied=true;
                if(shot.FromDefender)
                {
                    TryDamageUnit(unit.EntityId,shot.Attack,shot.DamageType);
                    if(shot.SplashRadius>0)
                        foreach(var other in units)
                            if(other!=unit && CanDefenderTarget(other) && GroundDistance(other.Motion.Position,unit.Motion.Position)<=shot.SplashRadius)
                                TryDamageUnit(other.EntityId,shot.Attack,shot.DamageType);
                }
                else
                {
                    // Freeze can begin or expire while the projectile is in flight.
                    var source=UnitCombat(unit);
                    shot.FrozenBonusApplied=defender.FrozenUntil>Clock && source.Effects.FrozenAttackMultiplier>1;
                    float attack=shot.Attack*(shot.FrozenBonusApplied ? source.Effects.FrozenAttackMultiplier : 1);
                    float health=defender.Health;
                    DamageDefender(defender,attack,unit.AltarEmpowered?CombatDamageType.Arts:UnitCombat(unit).Effects.DamageType);
                    shot.DamageDealt=health-defender.Health;
                    HealUnit(unit,shot.DamageDealt*source.Effects.LifeSteal);
                    if(defender.Alive) defender.ArmorReduction+=source.Effects.ArmorShred;
                    if(defender.Alive && shot.ColdDuration>0) TryApplyCold(defender.EntityId,shot.ColdDuration);
                    if(defender.Alive && shot.BurnDamage>0)ApplyBurn(defender,shot.BurnDamage);
                    if(defender.Alive && shot.NeuralDamage>0)ApplyNeuralDamage(defender,shot.NeuralDamage);
                }
            }
            foreach(var d in defenders) d.BlockedCount=0;
            foreach(var unit in units)
            {
                var c=UnitCombat(unit);
                if(!unit.Active) { unit.IsBlocked=unit.HasCombatTarget=false; continue; }
                if(unit.FuelDying)continue;
                if(c.Effects.Patriot&&StepPatriotPassive(unit))continue;
                if(c.Effects.Mandragora&&StepMandragoraPassive(unit,step))continue;
                if(c.Effects.Harvester&&StepHarvesterPassive(unit,step))continue;
                if(unit.PillarStunnedUntil>Clock&&!PatriotControlImmune(unit)){unit.IsMoving=unit.IsBlocked=unit.HasCombatTarget=false;continue;}
                if(c.Effects.Flamechaser&&StepFlamechaser(unit,step))continue;
                if(unit.IsReviving)
                {
                    if(Clock+1e-6<unit.ReviveAt) continue;
                    unit.IsReviving=false;unit.Health=Settings.Health;unit.DeathTime=double.PositiveInfinity;
                    unit.NextIceRingAt=Clock+8.5;unit.NextAttackTime=Clock;
                }
                HealUnit(unit,unit.MaxHealth*c.Effects.RegenerationFraction*step*RegenerationMultiplier);
                bool casting=c.FireRain && IsSkillActionPlaying(unit);
                if(c.HasSkill && unit.SkillAmmo==0 && !casting) unit.SkillSP=Mathf.Min(c.RequiredSP,unit.SkillSP+step*c.SPPerSecond);
                unit.IsBlocked=false; unit.BlockerId=null;
                foreach(var d in defenders)
                    if(!d.HighGround && !UnitAirborne(unit) && d.NeuralUntil<=Clock && !DefenderShackled(d) && d.Alive && d.BlockedCount<d.Settings.BlockCount && GroundDistance(unit.Motion.Position,d.Position)<=d.Settings.BlockRadius)
                    { unit.IsBlocked=true; unit.BlockerId=d.EntityId; d.BlockedCount++; break; }
                if(c.Effects.Harvester&&HarvesterActionLocked(unit)){unit.IsMoving=unit.HasCombatTarget=false;continue;}
                if(c.Effects.NervousCreeper&&Clock<unit.LastRetaliationAt+.3){unit.IsMoving=unit.HasCombatTarget=false;continue;}
                if(c.Effects.Patriot&&StepPatriotAction(unit))continue;
                if(c.Effects.Shackler&&StepShackler(unit))continue;
                if(c.Effects.FirstSpeaker&&StepFirstSpeaker(unit))continue;
                if(c.Effects.Mandragora && StepMandragoraAction(unit))continue;
                if(c.Effects.Flamerazer && StepFlamerazer(unit))continue;
                if(c.Effects.FrostNova && StepIceRing(unit) || c.Effects.Healer && StepHealer(unit) || c.Effects.Mudrock && StepMudrock(unit)) continue;
                float range=unit.SkillAmmo>0 && !c.FireRain ? c.SkillRange : c.Effects.Patriot&&unit.PatriotPhase==PatriotPhase.Destruction?.85f:c.UnitRange;
                bool ordinary=c.CanAttack&&(!c.Effects.Patriot||unit.PatriotPhase==PatriotPhase.Destruction||unit.IsBlocked)&&(!c.Effects.Flamerazer||unit.IsBlocked)&&(!c.Effects.Mandragora||unit.MandraPhase==MandragoraPhase.Second||unit.IsBlocked)&&(!c.Effects.Harvester||unit.HarvesterPhase==HarvesterPhase.Combat&&unit.IsBlocked);
                DefenderState target=ordinary ? ChooseDefender(unit,range) : null;
                if(!c.CanAttack) unit.CombatTargetId=null;
                unit.HasCombatTarget=target!=null;
                unit.IsMoving=!unit.IsBlocked && !unit.HasCombatTarget && !IsSkillActionPlaying(unit) && Clock>=unit.LastAttackTime+UnitActionDuration(unit)/unit.LastAttackRate;
                if(unit.IsMoving&&PillarObstructs(unit,step))unit.IsMoving=false;
                if(unit.IsMoving)
                {
                    double lockEnd=c.FireRain ? unit.FireRainStartedAt+c.SkillInterval :
                        unit.LastAttackEmpowered ? unit.LastAttackTime+c.SkillInterval : double.NegativeInfinity;
                    float moveStep=(float)Math.Min(step,Math.Max(0,Clock-lockEnd));
                    if(moveStep>0) unit.Motion.Advance(moveStep*UnitMoveMultiplier(unit));
                }
                if(unit.Motion.HasArrived)
                {
                    unit.Resolution=UnitResolution.Breached; unit.IsMoving=unit.HasCombatTarget=unit.IsBlocked=false;
                    Breaches++; Defense=Math.Max(0,Defense-Settings.GateDamage); continue;
                }
                if(c.FireRain && unit.SkillAmmo>0)
                {
                    // Six timed waves, with an explicit launch delay. Every living defender is hit,
                    // regardless of ordinary range. The caster holds its pose through the full window.
                    int wave=c.SkillAmmo-unit.SkillAmmo;
                    double due=unit.FireRainStartedAt+(wave+.25)*c.SkillInterval/c.SkillAmmo;
                    if(Clock+0.000001>=due)
                    {
                        unit.SkillAmmo--;
                        foreach(var d in defenders) if(d.Alive)
                            combatShots.Add(new CombatShot(unit.EntityId,d.EntityId,false,true,
                                d.Position+Vector3.up*5,d.Position,Clock,c.ProjectileTime,c.SkillAttack,true));
                    }
                }
                target=ordinary ? ChooseDefender(unit,range) : null; unit.HasCombatTarget=target!=null;
                if(target!=null && !casting && Clock+0.000001>=unit.NextAttackTime)
                {
                    bool empowered=!c.FireRain && unit.SkillAmmo>0;
                    if(empowered) unit.SkillAmmo--;
                    else unit.OrdinaryAttacksFired++;
                    float coldDuration=!empowered && c.Effects.ColdAttackEvery>0 &&
                        unit.OrdinaryAttacksFired%c.Effects.ColdAttackEvery==0 ? c.Effects.AttackColdDuration : 0;
                    unit.LastAttackTime=Clock; unit.LastAttackEmpowered=empowered;
                    unit.LastAttackRate=empowered ? 1 : UnitAttackRate(unit);
                    unit.NextAttackTime=Clock+(empowered ? c.SkillInterval : UnitAttackInterval(unit))/unit.LastAttackRate;
                    combatShots.Add(new CombatShot(unit.EntityId,target.EntityId,false,empowered,
                        unit.Motion.Position,target.Position,Clock+UnitAttackWindup(unit)/unit.LastAttackRate,c.ProjectileTime,(empowered ? c.SkillAttack : c.UnitAttack)*UnitAttackMultiplier(unit),coldDuration:coldDuration));
                    if(c.Effects.Patriot&&unit.PatriotPhase==PatriotPhase.March&&!empowered)
                        for(int hit=1;hit<4;hit++)combatShots.Add(new CombatShot(unit.EntityId,target.EntityId,false,false,unit.Motion.Position,target.Position,Clock+c.AttackWindup+hit*.2f,c.ProjectileTime,c.UnitAttack*UnitAttackMultiplier(unit)));
                    if(c.Effects.FirstSpeaker&&!empowered)combatShots[combatShots.Count-1].NeuralDamage=c.UnitAttack*UnitAttackMultiplier(unit)*.4f;
                    if(c.Effects.Harvester&&!empowered)combatShots[combatShots.Count-1].NeuralDamage=c.UnitAttack*UnitAttackMultiplier(unit)*.1f;
                    if(c.Effects.Flamechaser&&NearBurningReeds(unit))combatShots[combatShots.Count-1].BurnDamage=c.UnitAttack*UnitAttackMultiplier(unit);
                    int attackTargets=UnitAttackTargets(unit);
                    if(!empowered && attackTargets>1)
                    {
                        int count=1;
                        foreach(var other in defenders)
                        {
                            if(count>=attackTargets) break;
                            if(!other.Alive || other==target || DefenderCamouflaged(other) || GroundDistance(unit.Motion.Position,other.Position)>range) continue;
                            combatShots.Add(new CombatShot(unit.EntityId,other.EntityId,false,false,unit.Motion.Position,other.Position,
                                Clock+c.AttackWindup/unit.LastAttackRate,c.ProjectileTime,c.UnitAttack*UnitAttackMultiplier(unit),coldDuration:coldDuration));
                            count++;
                        }
                    }
                }
            }
            if(Defense==0) return;
            foreach(var d in defenders)
            {
                if(!d.Alive || d.FrozenUntil>Clock || d.NeuralUntil>Clock || DefenderShackled(d)) continue;
                var spec=d.Settings;
                if(spec.Role!=DefenderRole.Fighter)
                {
                    DefenderState wounded=null;
                    foreach(var ally in defenders)
                        if(ally.Alive && ally.Health<ally.MaxHealth-.001f && GroundDistance(d.Position,ally.Position)<=spec.HealRange &&
                            (wounded==null || ally.Health/ally.MaxHealth<wounded.Health/wounded.MaxHealth)) wounded=ally;
                    if(wounded!=null)
                    {
                        d.TargetId=wounded.EntityId;
                        if(Clock+1e-6>=d.NextAttackTime)
                        {
                            d.LastActionTime=Clock; d.LastActionHealing=true;
                            d.NextAttackTime=Clock+spec.Interval/DefenderAttackRate(d);
                            combatShots.Add(new CombatShot(d.EntityId,wounded.EntityId,true,false,d.Position,wounded.Position,
                                Clock+spec.Windup,guardSettings.ProjectileTime,spec.Attack,healing:true));
                        }
                        continue;
                    }
                    if(spec.Role==DefenderRole.Medic) { d.TargetId=null; continue; }
                }
                var target=FindUnit(d.TargetId);
                bool blocks=d.BlockedCount>0;
                if(!CanDefenderTarget(target) || GroundDistance(target.Motion.Position,d.Position)>spec.Range ||
                    blocks && target.BlockerId!=d.EntityId || HasHigherTauntTarget(d,target,blocks))
                {
                    target=null;
                    foreach(var unit in units)
                    {
                        if(!CanDefenderTarget(unit) || GroundDistance(unit.Motion.Position,d.Position)>spec.Range ||
                            blocks && unit.BlockerId!=d.EntityId) continue;
                        if(target==null || UnitTaunt(unit)>UnitTaunt(target) ||
                            UnitTaunt(unit)==UnitTaunt(target) && unit.Motion.DistanceTravelled>target.Motion.DistanceTravelled) target=unit;
                    }
                    d.TargetId=target?.EntityId;
                }
                if(target==null || Clock+0.000001<d.NextAttackTime) continue;
                d.LastActionTime=Clock; d.LastActionHealing=false;
                ShotsFired++; LastShotTime=Clock; LastShotEntityId=target.EntityId; LastShotPosition=target.Motion.Position;
                d.NextAttackTime=Clock+spec.Interval/DefenderAttackRate(d);
                combatShots.Add(new CombatShot(d.EntityId,target.EntityId,true,false,d.Position,target.Motion.Position,
                    Clock+spec.Windup,guardSettings.ProjectileTime,spec.Attack,damageType:spec.DamageType,splashRadius:spec.SplashRadius));
            }
        }

        public int UnitTaunt(DeployedUnitState unit)=>IsPatriot(unit)?unit.PatriotPhase==PatriotPhase.March?2:0:UnitCombat(unit).Effects.TauntLevel;
        bool HasHigherTauntTarget(DefenderState defender,DeployedUnitState current,bool blocks)
        {
            foreach(var unit in units)
                if(CanDefenderTarget(unit) && GroundDistance(unit.Motion.Position,defender.Position)<=defender.Settings.Range &&
                    (!blocks || unit.BlockerId==defender.EntityId) && UnitTaunt(unit)>UnitTaunt(current)) return true;
            return false;
        }

        private DefenderState ChooseDefender(DeployedUnitState unit,float range)
        {
            var blocker=FindDefender(unit.BlockerId);
            if(blocker!=null && blocker.Alive && GroundDistance(unit.Motion.Position,blocker.Position)<=range)
            { unit.CombatTargetId=blocker.EntityId; return blocker; }
            if(IsRole(unit,"M26"))
            {
                DefenderState protocolTarget=null;float closest=float.PositiveInfinity;
                foreach(var d in defenders)
                {
                    if(!d.Alive||DefenderCamouflaged(d)||GroundDistance(unit.Motion.Position,d.Position)>range||
                        !facilities.Exists(f=>f.Settings.Kind==FacilityKind.Gramophone&&GroundDistance(unit.Motion.Position,f.Settings.Position)<=4&&GroundDistance(d.Position,f.Settings.Position)<=1.5f))continue;
                    float gap=GroundDistance(unit.Motion.Position,d.Position);
                    if(gap<closest){closest=gap;protocolTarget=d;}
                }
                if(protocolTarget!=null){unit.CombatTargetId=protocolTarget.EntityId;return protocolTarget;}
            }
            var previous=FindDefender(unit.CombatTargetId);
            if(previous!=null && previous.Alive && !DefenderCamouflaged(previous) && GroundDistance(unit.Motion.Position,previous.Position)<=range) return previous;
            DefenderState nearest=null; float distance=float.PositiveInfinity;
            foreach(var d in defenders)
            {
                if(!d.Alive || DefenderCamouflaged(d)) continue;
                float gap=GroundDistance(unit.Motion.Position,d.Position);
                if(gap>range) continue;

                if(gap<distance) { nearest=d; distance=gap; }
            }
            unit.CombatTargetId=nearest?.EntityId;
            return nearest;
        }
    }
}
