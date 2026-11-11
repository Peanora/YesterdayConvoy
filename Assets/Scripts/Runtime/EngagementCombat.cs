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
        public readonly bool FireRain, FrostSeal, MudrockCapture, HasSkill, Melee, CanAttack;
        public readonly float AttackDuration, AttackWindup;
        public readonly MonsterEffectSettings Effects;
        public readonly CombatDamageType DefenderDamageType;
        public readonly bool RevealsInvisible, DefenderCanHitAir;
        public readonly float InitialSP, RequiredSP, SPPerSecond, SkillAttack, SkillInterval, SkillRange, ProjectileTime;

        public EngagementSettings(float unitAttack = 14, float unitArmor = 2, float unitRange = 0.65f, float unitInterval = 1.7f,
            float defenderHealth = 180, float defenderAttack = 22, float defenderArmor = 3, float defenderRange = 3.2f,
            float defenderInterval = 1, int blockCount = 1, float blockRadius = 0.65f,
            float initialSP = 8, float requiredSP = 12, float spPerSecond = 1, int skillAmmo = 6,
            float skillAttack = 105, float skillInterval = 3.2f, float skillRange = 2.6f, float projectileTime = 0.22f, bool fireRain = false, bool hasSkill = true, bool melee = false, float attackDuration = 0, float attackWindup = 0, MonsterEffectSettings effects = default, bool canAttack = true,CombatDamageType defenderDamageType=CombatDamageType.Physical,bool revealsInvisible=false,bool defenderCanHitAir=true,bool frostSeal=false,bool mudrockCapture=false)
        {
            foreach (float value in new[] { unitRange, unitInterval, defenderHealth, defenderAttack, defenderRange,
                defenderInterval, blockRadius, projectileTime })
                if (!Finite(value) || value <= 0) throw new ArgumentException("Combat values must be finite and positive.");
            if(!Finite(unitAttack)||unitAttack<0||canAttack&&unitAttack==0)throw new ArgumentException("An attacking unit requires positive attack; non-attacking units may have zero attack.");
            if (!Finite(unitArmor) || !Finite(defenderArmor) || !Finite(initialSP) || unitArmor < 0 || defenderArmor < 0 ||
                initialSP < 0 || blockCount < 0)
                throw new ArgumentException("Invalid combat armor, block count or initial SP.");
            if (hasSkill)
            {
                foreach(float value in new[]{requiredSP,spPerSecond,skillAttack,skillInterval,skillRange})
                    if(!Finite(value) || value<=0) throw new ArgumentException("Skill values must be finite and positive.");
                if(mudrockCapture&&(!effects.Mudrock||frostSeal||fireRain||skillInterval!=1.5f))throw new ArgumentException("Invalid Mudrock capture skill.");
                if(initialSP>requiredSP || ((frostSeal||mudrockCapture) ? skillAmmo!=0 : skillAmmo<=0) || frostSeal && (!effects.FrostNova || fireRain || skillInterval!=3)) throw new ArgumentException("Invalid initial SP or ammo.");
            }
            else if(mudrockCapture || frostSeal || fireRain || initialSP!=0 || requiredSP!=0 || spPerSecond!=0 || skillAmmo!=0 || skillAttack!=0 || skillInterval!=0 || skillRange!=0)
                throw new ArgumentException("A unit without a skill must have zero skill resources.");
            if(!Finite(attackDuration) || !Finite(attackWindup) || attackDuration<0 || attackDuration>unitInterval || attackWindup<0 || attackWindup>attackDuration)
                throw new ArgumentException("Invalid attack animation timing.");
            if(effects.NervousCreeper&&(canAttack||hasSkill))throw new ArgumentException("Creeper cannot use ordinary attacks or ammo skills.");
            if(effects.Healer && (canAttack || hasSkill)) throw new ArgumentException("Healer uses automatic treatment, not damage attacks or ammunition skills.");
            if(defenderDamageType!=CombatDamageType.Physical && defenderDamageType!=CombatDamageType.Arts) throw new ArgumentException("Invalid defender damage type.");
            if(effects.TalulahSecondPhase&&(!fireRain||!hasSkill||!canAttack))throw new ArgumentException("Talulah second phase requires her ordinary attack and manual fire rain.");
            if(effects.Patriot&&unitInterval<=.4f)throw new ArgumentException("Patriot requires a positive second-phase attack interval.");
            Effects=effects;
            DefenderDamageType=defenderDamageType;
            RevealsInvisible=revealsInvisible;DefenderCanHitAir=defenderCanHitAir;
            AttackDuration=attackDuration; AttackWindup=attackWindup;
            HasSkill=hasSkill; Melee=melee; CanAttack=canAttack;
            FireRain = fireRain; FrostSeal=frostSeal; MudrockCapture=mudrockCapture;
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
        public float MaxHealth {get;internal set;}
        public double LastActionTime { get; internal set; } = double.NegativeInfinity;
        public bool LastActionHealing { get; internal set; }
        public float LastActionRate {get;internal set;}=1;
        public float SkillSP {get;internal set;}
        public int SkillCasts {get;internal set;}
        public double SkillStartedAt {get;internal set;}=double.NegativeInfinity;
        public double SkillActiveUntil {get;internal set;}=double.NegativeInfinity;
        public float SkillHealthRestored {get;internal set;}
        public int CriticalHits {get;internal set;}
        public int ExtraHeals {get;internal set;}
        public bool LastAttackSkill {get;internal set;}
        public double SkillActionUntil {get;internal set;}=double.NegativeInfinity;
        public bool AdvancedSkillWasActive {get;internal set;}
        public double ChargeStartedAt {get;internal set;}
        public double NextAuraAt {get;internal set;}
        public double SkillEndedAt {get;internal set;}=double.NegativeInfinity;
        public float LiberatorBonus {get;internal set;}
        public float DroneRamp {get;internal set;}=.2f;
        public string DroneTargetId {get;internal set;}
        public int DroneHits {get;internal set;}
        public int FearApplications {get;internal set;}
        public int AuraHeals {get;internal set;}
        public int BurnApplications {get;internal set;}
        public int ErasedProjectiles {get;internal set;}
        public int LiberatorKills {get;internal set;}
        public Vector3 WallNormal {get;internal set;}=Vector3.right;
        private uint randomState=2166136261;
        internal float Roll(){randomState^=randomState<<13;randomState^=randomState>>17;randomState^=randomState<<5;return(randomState&0xffffff)/16777216f;}
        public Vector3 Position { get; internal set; }
        public float Health { get; internal set; }
        public int ReserveCost {get;internal set;}
        public int SixthHits {get;internal set;}
        public int ChessStock {get;internal set;}
        public int ChessTriggers {get;internal set;}
        public int SnowHits {get;internal set;}
        public int Lures {get;internal set;}
        public bool CostsExchanged {get;internal set;}
        public string PartnerId {get;internal set;}
        public double NextSixthAt {get;internal set;}
        public double NextSnowAt {get;internal set;}=10;
        public float BudgetReceived {get;internal set;}
        public bool Reserve {get;internal set;}
        public bool Departed {get;internal set;}
        public bool DebtLife {get;internal set;}
        public float Debt {get;internal set;}
        public int Ammo {get;internal set;}
        public int Transfers {get;internal set;}
        public int Reinforcements {get;internal set;}
        public int ProtectedHits {get;internal set;}
        public int TrueHits {get;internal set;}
        public double LastReceivedDamage {get;internal set;}=double.NegativeInfinity;
        public double ProtectionUntil {get;internal set;}=double.NegativeInfinity;
        public bool FifthActive {get;internal set;}
        public bool Alive => !Reserve&&!Departed&&(Health>0||DebtLife);
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
        { ChessStock=settings.Skill.Kind==DefenderSkillKind.WangGambit?7:0;Settings=settings;ReserveCost=settings.ReserveCost;Reserve=settings.Reserve; EntityId=settings.Id; Position=settings.Position; Health=MaxHealth=settings.Health;SkillSP=settings.Skill.InitialSP;foreach(char c in settings.Id)randomState=(randomState^c)*16777619; }
    }

    public sealed class CombatShot
    {
        public string SourceId { get; }
        public string TargetId { get; }
        public bool FromDefender { get; }
        public bool Empowered { get; }
        public double WolfActionAt {get;internal set;}=double.NegativeInfinity;
        public float WolfActionRate {get;internal set;}=1;
        public WolfBoxKind WolfBox {get;internal set;}
        public string WolfBonusTargetId {get;internal set;}
        internal bool WolfFinished;
        public bool FireRain { get; }
        public float Vulnerability {get;internal set;}
        public float IgnoreResistance {get;internal set;}
        public bool TrueDamage {get;internal set;}
        public bool SynergyOrder {get;internal set;}
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
        public float HinderDuration {get;internal set;}
        public bool WallChecked {get;internal set;}
        public bool Erased {get;internal set;}
        public bool Drone {get;internal set;}
        public bool Resolved { get; internal set; }
        public bool Applied { get; internal set; }
        public bool FrozenBonusApplied { get; internal set; }
        public float DamageDealt { get; internal set; }
        internal CombatShot(string source, string target, bool defender, bool empowered, Vector3 origin, Vector3 destination,
            double start, float duration, float attack, bool fireRain = false, float coldDuration = 0, bool healing = false, CombatDamageType damageType = CombatDamageType.Physical, float splashRadius = 0)
        { ColdDuration=coldDuration; Healing=healing; DamageType=damageType; SplashRadius=splashRadius; FireRain=fireRain; SourceId = source; TargetId = target; FromDefender = defender; Empowered = empowered; Origin = origin;
            Destination = destination; StartedAt = start; ImpactAt = start + duration; Attack = attack; }
    }

    public enum SkillFailure { None, NotRunning, Paused, StaleRun, MissingEntity, NotReady, AlreadyActive, NoSkill, NoTarget }

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
            ResetBossRoster();
            defenderDrones.Clear();snowCells.Clear();chessPieces.Clear();DefenderBudget=30;
            flameSprays.Clear();fuelExplosions.Clear();gazes.Clear();pillarCommands.Clear();creeperPulses.Clear();bubbles.Clear();shackles.Clear();
            combatShots.Clear(); deathBlasts.Clear(); iceRings.Clear(); frostSeals.Clear(); healingCasts.Clear(); skillRequests.Clear(); ResetGramophones(); SkillCasts = 0;
            defenders.Clear();
            if(!EngagementEnabled) return;
            if(defenderRoster!=null)
            {
                foreach(var spec in defenderRoster)defenders.Add(new DefenderState(spec));
                if(CampaignV2Enabled)InitializeV2SixthDefenders();
                return;
            }
            var c=CombatSettings;
            for(int i=0;i<defenderPositions.Length;i++)
                defenders.Add(new DefenderState(new DefenderSettings(i==0 ? "kroos" : "kroos-"+i,Settings.CardId==SilverWolfCombat.CardId?"源石虫":"克洛丝",null,
                    defenderPositions[i],c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,
                    c.BlockCount,damageType:c.DefenderDamageType,blockRadius:c.BlockRadius)));
        }

        public DeployedUnitState FindUnit(string id)
        { foreach (var unit in units) if (unit.EntityId == id) return unit; return null; }

        public bool IsSkillActionPlaying(DeployedUnitState unit) => EngagementEnabled && unit!=null && unit.Active && UnitCombat(unit).HasSkill &&
            (BossRosterMoveLock(unit) ? true : SilverWolfCombat.Transforming(unit,Clock) ? true : UnitCombat(unit).MudrockCapture ? Clock<unit.MudrockCaptureStartedAt+1.5 : UnitCombat(unit).FrostSeal ? !unit.IsReviving && Clock<unit.FrostSealStartedAt+UnitCombat(unit).SkillInterval :
            UnitCombat(unit).FireRain ? Clock<unit.FireRainStartedAt+UnitCombat(unit).SkillInterval :
            unit.LastAttackEmpowered && Clock<unit.LastAttackTime+UnitCombat(unit).SkillInterval);

        // Readiness stays visible during a full pause; authorization to cast is checked separately.
        private bool WolfActionBusy(DeployedUnitState u)=>SilverWolfCombat.IsWolf(u) && IsSkillActionPlaying(u);

        public bool IsSkillReady(string entityId)
        {
            var unit=FindUnit(entityId);
            if(BossRosterCatalog.Contains(unit?.CardId))return BossRosterReady(unit);
            if(UsesBossCoreSkill(unit))return BossCoreSkills(unit)[0].Ready;
            return EngagementEnabled && Phase==BattlePhase.Running && unit!=null && unit.Active && UnitCombat(unit).HasSkill &&
                !unit.IsReviving && !StrongholdInTransit(unit.EntityId) && !ApprovedWaitingAtPortal(unit.EntityId) && !TalulahTransitionLocked(unit) && !WolfActionBusy(unit) && unit.SkillAmmo==0 && (!(UnitCombat(unit).FireRain||UnitCombat(unit).FrostSeal||UnitCombat(unit).MudrockCapture) || !IsSkillActionPlaying(unit)) &&
                (!UnitCombat(unit).FrostSeal || Clock>=unit.IceRingStartedAt+3) && (!UnitCombat(unit).MudrockCapture||Clock>=unit.BarrierRefreshStartedAt+MudrockRefreshDuration) && unit.SkillSP+0.00001f>=UnitCombat(unit).RequiredSP;
        }

        public SkillFailure CheckSkill(string entityId)
        {
            if (!EngagementEnabled || Phase != BattlePhase.Running) return SkillFailure.NotRunning;
            var unit = FindUnit(entityId);
            if(BossRosterCatalog.Contains(unit?.CardId))return CheckBossRosterSkill(unit);
            if(UsesBossCoreSkill(unit))return CheckBossCoreSkill(unit);
            if(!UnitCombat(unit).HasSkill)return SkillFailure.NoSkill;
            if (IsPaused) return SkillFailure.Paused;
            if (unit == null || !unit.Active || unit.IsReviving) return SkillFailure.MissingEntity;
            if(StrongholdInTransit(unit.EntityId)||ApprovedWaitingAtPortal(unit.EntityId)||TalulahTransitionLocked(unit)||WolfActionBusy(unit))return SkillFailure.AlreadyActive;
            var c=UnitCombat(unit);
            if (unit.SkillAmmo > 0 || (c.FireRain||c.FrostSeal||c.MudrockCapture) && IsSkillActionPlaying(unit) || c.FrostSeal && Clock<unit.IceRingStartedAt+3) return SkillFailure.AlreadyActive;
            if(c.MudrockCapture && (Clock<unit.BarrierRefreshStartedAt+MudrockRefreshDuration || ChooseGramophone(unit)==null))return SkillFailure.NoTarget;
            return unit.SkillSP + 0.00001f >= c.RequiredSP ? SkillFailure.None : SkillFailure.NotReady;
        }

        public SkillFailure TryActivateSkill(int runId, long requestId, string entityId)
        {
            if (runId != RunId) return SkillFailure.StaleRun;
            if(BossRosterCatalog.Contains(FindUnit(entityId)?.CardId))return TryActivateBossRosterSkill(runId,requestId,entityId,null);
            if(UsesBossCoreSkill(FindUnit(entityId)))return TryActivateBossCoreSkill(runId,requestId,entityId);
            if (skillRequests.TryGetValue(requestId, out string previous))
                return previous == entityId ? SkillFailure.None : SkillFailure.StaleRun;
            SkillFailure failure = CheckSkill(entityId);
            if (failure != SkillFailure.None) return failure;
            var unit = FindUnit(entityId);
            var c=UnitCombat(unit);
            unit.SkillSP = 0; unit.SkillAmmo = c.SkillAmmo; unit.SkillsUsed++; SkillCasts++;
            unit.NextAttackTime = Clock;
            if(SilverWolfCombat.IsWolf(unit)){unit.WolfTransformAt=Math.Max(Clock, unit.LastAttackTime+UnitActionDuration(unit)/unit.LastAttackRate);unit.IsMoving=false;unit.NextAttackTime=unit.WolfTransformAt+SilverWolfCombat.TransformDuration;}
            if(c.FrostSeal) StartFrostSeal(unit);
            if(c.MudrockCapture) StartMudrockCapture(unit);
            if(c.FireRain)
            { unit.FireRainStartedAt=Clock; unit.IsMoving=false; unit.NextAttackTime=Clock+c.SkillInterval; }
            skillRequests.Add(requestId, entityId);
            BossCoreOnSkillStarted(unit);
            return SkillFailure.None;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0; return Vector3.Distance(a, b); }

        private void StepEngagement(float step)
        {
            StepBossRoster(step);
            StepRemainingSynergies(step);
            EngagementSettings guardSettings=CombatSettings;
            ResolveFrostSeals();StepSnowstorms();
            StepGramophones(step);
            StepAdvancedDefenders(step);
            StepDefenderWalls();
            StepFacilities(step);StepStrongholdMap();StepApprovedHeatPumps(step);
            ResolveFuelExplosions();ResolvePillarCommands();ResolveCorrosiveBubbles();
            ResolveDeathEffects();ResolveShackles();
            ResolveHealing();
            foreach(var shot in combatShots)
            {
                if(shot.Resolved && shot.WolfBox==WolfBoxKind.Sword && !shot.WolfFinished && Clock>=shot.StartedAt+SilverWolfCombat.SwordFinish)
                    FinishWolfSword(shot);
                if(shot.Resolved || Clock+0.000001<shot.ImpactAt) continue;
                shot.Resolved=true;
                if(shot.Healing)
                {
                    var healer=FindDefender(shot.SourceId); var recipient=FindDefender(shot.TargetId);
                    if(healer!=null && healer.Alive && recipient!=null && recipient.Alive)
                    { HealDefender(healer,recipient,shot.Attack); shot.Applied=true; }
                    continue;
                }
                var unit=FindUnit(shot.FromDefender ? shot.TargetId : shot.SourceId);
                var defender=FindDefender(shot.FromDefender ? shot.SourceId : shot.TargetId);
                if(shot.WolfBox!=WolfBoxKind.None){ResolveWolfBox(shot,unit);continue;}
                if(ResolveViviSkillSplash(shot,unit))continue;
                if(unit==null || !unit.Active || unit.IsReviving || StrongholdInTransit(unit.EntityId) || defender==null || !defender.Alive) continue;
                if(shot.FromDefender&&UnitAirborne(unit)&&!guardSettings.DefenderCanHitAir)continue;
                if(shot.FromDefender&&!shot.TrueDamage&&!BossRosterDefenderAttackHits(defender,shot.DamageType))continue;
                shot.Applied=true;
                if(shot.FromDefender)
                {
                    float preHitHealth=unit.Health;bool wasAlive=unit.Active&&!unit.IsReviving;
                    bool damaged=shot.TrueDamage?ApplyFifthTrueDamage(unit,shot.Attack,defender):TryDefenderDamageUnit(unit,shot.Attack,shot.DamageType,shot.IgnoreResistance,defender.Settings.Range>1.2f,defender,true);
                    bool killedByHit=wasAlive&&(!unit.Active||unit.IsReviving);
                    if(damaged&&shot.Vulnerability>0){unit.VulnerableUntil=Clock+2;unit.Vulnerability=Math.Max(unit.Vulnerability,shot.Vulnerability);}
                    if(damaged){if(unit.Health<preHitHealth)OnMarineContact(unit);OnAdvancedDefenderHit(defender,unit,shot.DamageType,shot.Origin,shot.Drone);RecordSynergyContact(unit);}
                    if(killedByHit)OnLiberatorKill(defender);
                    if(damaged&&unit.Active&&shot.HinderDuration>0)unit.HinderedUntil=Math.Max(unit.HinderedUntil,Clock+shot.HinderDuration);
                    if(shot.SplashRadius>0)
                        foreach(var other in units)
                            if(other!=unit && CanDefenderTarget(other) && GroundDistance(other.Motion.Position,unit.Motion.Position)<=shot.SplashRadius)
                            {
                                if(TryDefenderDamageUnit(other,shot.Attack,shot.DamageType,ranged:defender.Settings.Range>1.2f,sourceDefender:defender,accuracyChecked:true))OnAdvancedDefenderHit(defender,other,shot.DamageType,shot.Origin,shot.Drone);
                            }
                }
                else
                {
                    // Freeze can begin or expire while the projectile is in flight.
                    var source=UnitCombat(unit);
                    shot.FrozenBonusApplied=defender.FrozenUntil>Clock && source.Effects.FrozenAttackMultiplier>1;
                    float attack=shot.Attack*(shot.FrozenBonusApplied ? source.Effects.FrozenAttackMultiplier : 1);
                    float health=defender.Health;
                    if(BossCoreUsesTrueOrdinary(unit,shot))SourcedTrueDamage(defender,attack,unit);
                    else ApplyDefenderAttack(defender,attack,unit.AltarEmpowered?CombatDamageType.Arts:UnitCombat(unit).Effects.DamageType,shot.SynergyOrder||unit.SynergySupport,shot.SynergyOrder||unit.SynergySupport?null:unit,1,unit);
                    shot.DamageDealt=health-defender.Health;
                    BossCoreOnUnitHit(unit,defender,shot);
                    if(!shot.SynergyOrder)
                    {
                        RecordSynergyContact(unit);OnGolemHit(unit,defender);
                    HealUnit(unit,shot.DamageDealt*source.Effects.LifeSteal);
                    if(defender.Alive) defender.ArmorReduction+=source.Effects.ArmorShred;
                    if(defender.Alive && shot.ColdDuration>0) TryApplyCold(defender.EntityId,shot.ColdDuration);
                    if(defender.Alive && shot.BurnDamage>0)ApplyBurn(defender,shot.BurnDamage,unit);
                    if(defender.Alive && shot.NeuralDamage>0)ApplyNeuralDamage(defender,shot.NeuralDamage,unit);
                    OnSynergyDirectHit(unit,defender);
                    }
                }
            }
            FlushHerd();
            foreach(var d in defenders) d.BlockedCount=0;
            foreach(var unit in units)
            {
                var c=UnitCombat(unit);
                if(!unit.Active) { unit.IsBlocked=unit.HasCombatTarget=false; continue; }
                if(StepApprovedTransit(unit))continue;
                if(StepTalulahPhase(unit))continue;
                if(StepStrongholdPortal(unit)){unit.IsMoving=unit.IsBlocked=unit.HasCombatTarget=false;continue;}
                if(unit.FuelDying)continue;
                if(c.Effects.Patriot&&StepPatriotPassive(unit))continue;
                if(c.Effects.Mandragora&&StepMandragoraPassive(unit,step))continue;
                if(c.Effects.Harvester&&StepHarvesterPassive(unit,step))continue;
                if(unit.PillarStunnedUntil>Clock&&!UnitControlImmune(unit)){unit.IsMoving=unit.IsBlocked=unit.HasCombatTarget=false;continue;}
                if(c.Effects.Flamechaser&&StepFlamechaser(unit,step))continue;
                if(unit.IsReviving)
                {
                    if(Clock+1e-6<unit.ReviveAt) continue;
                    unit.IsReviving=false;unit.Health=unit.MaxHealth;unit.DeathTime=double.PositiveInfinity;
                    unit.NextIceRingAt=Clock+V2Skill(unit,"iceRing","cooldown",8.5f);unit.NextAttackTime=Clock;
                }
                HealUnit(unit,unit.MaxHealth*c.Effects.RegenerationFraction*step*RegenerationMultiplier);
                bool casting=(c.FireRain||c.FrostSeal||c.MudrockCapture) && IsSkillActionPlaying(unit);
                if(c.HasSkill && unit.SkillAmmo==0 && (!casting||BossCoreMayRegenerateLegacySP(unit)) && !(SilverWolfCombat.IsWolf(unit)&&IsSkillActionPlaying(unit))) unit.SkillSP=Mathf.Min(c.RequiredSP,unit.SkillSP+step*c.SPPerSecond);
                if(StepApprovedPortalWait(unit))continue;
                unit.IsBlocked=false; unit.BlockerId=null;
                double beforeControl=unit.Motion.DistanceTravelled;
                if(StepUnitLure(unit,step)||StepUnitFear(unit,step))
                {
                    ClampApprovedRouteMotion(unit,beforeControl);
                    if(unit.Motion.DistanceTravelled>=beforeControl&&unit.IsMoving)TryBeginApprovedTransit(unit);
                    continue;
                }
                foreach(var d in defenders)
                    if(!d.HighGround && !BossRosterUnblockable(unit) && DefenderCanBlock(d) && !UnitAirborne(unit) && d.NeuralUntil<=Clock && !DefenderShackled(d) && d.Alive && d.BlockedCount<d.Settings.BlockCount && GroundDistance(unit.Motion.Position,d.Position)<=d.Settings.BlockRadius)
                    { unit.IsBlocked=true; unit.BlockerId=d.EntityId; d.BlockedCount++; break; }
                if(SilverWolfCombat.Transforming(unit,Clock)){unit.IsMoving=false;continue;}
                if((c.FrostSeal||c.MudrockCapture) && casting){unit.IsMoving=unit.HasCombatTarget=false;continue;}
                if(c.Effects.Harvester&&HarvesterActionLocked(unit)){unit.IsMoving=unit.HasCombatTarget=false;continue;}
                if(c.Effects.NervousCreeper&&Clock<unit.LastRetaliationAt+.3){unit.IsMoving=unit.HasCombatTarget=false;continue;}
                if(c.Effects.Patriot&&StepPatriotAction(unit))continue;
                if(c.Effects.Shackler&&StepShackler(unit))continue;
                if(c.Effects.FirstSpeaker&&StepFirstSpeaker(unit))continue;
                if(c.Effects.Mandragora && StepMandragoraAction(unit))continue;
                if(c.Effects.Flamerazer && StepFlamerazer(unit))continue;
                if(c.Effects.FrostNova && StepIceRing(unit) || c.Effects.Healer && StepHealer(unit) || c.Effects.Mudrock && StepMudrock(unit)) continue;
                if(StepBossRosterUnit(unit,step))continue;
                float range=unit.SkillAmmo>0 && !c.FireRain ? c.SkillRange : c.Effects.Patriot&&unit.PatriotPhase==PatriotPhase.Destruction?V2Skill(unit,"rebirth","destructionRange",.85f):c.UnitRange;
                bool ordinary=c.CanAttack&&(!c.Effects.Patriot||unit.PatriotPhase==PatriotPhase.Destruction||unit.IsBlocked)&&(!c.Effects.Flamerazer||unit.IsBlocked)&&(!c.Effects.Mandragora||unit.MandraPhase==MandragoraPhase.Second||unit.IsBlocked)&&(!c.Effects.Harvester||unit.HarvesterPhase==HarvesterPhase.Combat&&unit.IsBlocked);
                DefenderState target=ordinary ? ChooseDefender(unit,range) : null;
                if(!c.CanAttack) unit.CombatTargetId=null;
                unit.HasCombatTarget=target!=null;
                unit.IsMoving=!unit.IsBlocked && !unit.HasCombatTarget && !IsSkillActionPlaying(unit) && Clock>=unit.LastAttackTime+UnitActionDuration(unit)/unit.LastAttackRate;
                if(unit.IsMoving&&PillarObstructs(unit,step))unit.IsMoving=false;
                if(unit.IsMoving)
                {
                    double lockEnd=c.MudrockCapture ? unit.MudrockCaptureStartedAt+1.5 : c.FrostSeal ? unit.FrostSealStartedAt+c.SkillInterval : c.FireRain ? unit.FireRainStartedAt+c.SkillInterval :
                        unit.LastAttackEmpowered ? unit.LastAttackTime+c.SkillInterval : double.NegativeInfinity;
                    float moveStep=(float)Math.Min(step,Math.Max(0,Clock-lockEnd));
                    if(moveStep>0)
                    {
                        double beforeMove=unit.Motion.DistanceTravelled;
                        unit.Motion.Advance(moveStep*UnitMoveMultiplier(unit));
                        ClampApprovedRouteMotion(unit,beforeMove);
                        if(TryBeginApprovedTransit(unit))continue;
                    }
                }
                if(unit.Motion.HasArrived)
                {
                    if(BossArenaEnabled)unit.IsMoving=false;
                    else
                    {
                        unit.Resolution=UnitResolution.Breached; unit.IsMoving=unit.HasCombatTarget=unit.IsBlocked=false;
                        RegisterStrongholdBreach(unit); continue;
                    }
                }
                if(c.FireRain && unit.SkillAmmo>0)
                {
                    // Six timed waves, with an explicit launch delay. Every living defender is hit,
                    // regardless of ordinary range. The caster holds its pose through the full window.
                    int wave=c.SkillAmmo-unit.SkillAmmo;
                    double due=unit.FireRainStartedAt+(wave+V2Skill(unit,"manualRain","firstWaveFractionOfCadence",.25f))*c.SkillInterval/c.SkillAmmo;
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
                    TryActivateReunionEntryShield(unit);
                    bool empowered=!c.FireRain && unit.SkillAmmo>0;
                    if(empowered) unit.SkillAmmo--;
                    else unit.OrdinaryAttacksFired++;
                    float coldDuration=!empowered && c.Effects.ColdAttackEvery>0 &&
                        unit.OrdinaryAttacksFired%c.Effects.ColdAttackEvery==0 ? c.Effects.AttackColdDuration : 0;
                    unit.LastAttackTime=Clock; unit.LastAttackEmpowered=empowered;
                    unit.LastAttackRate=empowered ? 1 : UnitAttackRate(unit);
                    unit.NextAttackTime=Clock+(empowered ? c.SkillInterval : UnitAttackInterval(unit))/unit.LastAttackRate;
                    if(empowered && SilverWolfCombat.IsWolf(unit)){StartWolfShot(unit,target,c);continue;}
                    combatShots.Add(new CombatShot(unit.EntityId,target.EntityId,false,empowered,
                        unit.Motion.Position,target.Position,Clock+UnitAttackWindup(unit)/unit.LastAttackRate,
                        c.Effects.Patriot?(unit.PatriotPhase==PatriotPhase.March?0:PatriotTiming.DestructionFlight/unit.LastAttackRate):c.ProjectileTime,
                        (empowered ? c.SkillAttack : c.UnitAttack)*UnitAttackMultiplier(unit),coldDuration:coldDuration,
                        splashRadius:empowered&&ViviCombat.IsVivi(unit.CardId)?ViviCombat.SkillSplashRadius:0));
                    if(SilverWolfCombat.IsWolf(unit)){var attack=combatShots[combatShots.Count-1];attack.WolfActionAt=Clock;attack.WolfActionRate=unit.LastAttackRate;}
                    if(c.Effects.Patriot&&unit.PatriotPhase==PatriotPhase.March&&!empowered)
                        for(int hit=1;hit<(int)V2Skill(unit,"march","hits",4);hit++)combatShots.Add(new CombatShot(unit.EntityId,target.EntityId,false,false,unit.Motion.Position,target.Position,Clock+(V2Skill(unit,"march","firstHitDelay",PatriotTiming.MarchContact)+hit*V2Skill(unit,"march","hitSpacing",PatriotTiming.MarchSpacing))/unit.LastAttackRate,0,c.UnitAttack*UnitAttackMultiplier(unit)));
                    if(c.Effects.FirstSpeaker&&!empowered)combatShots[combatShots.Count-1].NeuralDamage=V2Skill(unit,"neuralHit","neuralPerHit",c.UnitAttack*UnitAttackMultiplier(unit)*.4f);
                    if(c.Effects.Harvester&&!empowered)combatShots[combatShots.Count-1].NeuralDamage=V2Skill(unit,"neuralAura","ordinaryNeuralOnHit",c.UnitAttack*UnitAttackMultiplier(unit)*.1f);
                    if(c.Effects.Flamechaser&&NearBurningReeds(unit))combatShots[combatShots.Count-1].BurnDamage=V2Skill(unit,"reedsBurn","burnAccumulationPerHit",c.UnitAttack*UnitAttackMultiplier(unit));
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
            StepDefenderSkills(step);
            foreach(var d in defenders)
            {
                if(!d.Alive || BossRosterDefenderActionLocked(d) || d.FrozenUntil>Clock || d.NeuralUntil>Clock || DefenderShackled(d)||Clock<d.SkillActionUntil) continue;
                if(StepArenaBossAttack(d))continue;
                var spec=d.Settings;
                if(CanDefenderHeal(d))
                {
                    DefenderState wounded=null;
                    foreach(var ally in defenders)
                        if(ally.Alive && !ally.DebtLife && ally.Health<ally.MaxHealth-.001f && GroundDistance(d.Position,ally.Position)<=DefenderHealRange(d) &&
                            (wounded==null || ally.Health/ally.MaxHealth<wounded.Health/wounded.MaxHealth)) wounded=ally;
                    if(wounded!=null)
                    {
                        d.TargetId=wounded.EntityId;
                        if(Clock+1e-6>=d.NextAttackTime)
                        {
                            ScheduleDefenderHealing(d,wounded,guardSettings.ProjectileTime);
                        }
                        continue;
                    }
                    if(spec.Role==DefenderRole.Medic||SpotHealing(d)) { d.TargetId=null; continue; }
                }
                if(StepAdvancedDefenderAction(d))continue;
                if(!DefenderCanAttack(d)){d.TargetId=null;continue;}
                var target=FindUnit(d.TargetId);
                bool blocks=d.BlockedCount>0;
                if(!CanDefenderTarget(target) || GroundDistance(target.Motion.Position,d.Position)>DefenderRange(d) ||
                    blocks && target.BlockerId!=d.EntityId || HasHigherTauntTarget(d,target,blocks)||
                    spec.Skill.Kind==DefenderSkillKind.StewardStrike&&Clock+0.000001>=d.NextAttackTime)
                {
                    target=null;
                    foreach(var unit in units)
                    {
                        if(!CanDefenderTarget(unit) || GroundDistance(unit.Motion.Position,d.Position)>DefenderRange(d) ||
                            blocks && unit.BlockerId!=d.EntityId) continue;
                        if(PreferDefenderTarget(d,unit,target))target=unit;
                    }
                    d.TargetId=target?.EntityId;
                }
                if(target==null || Clock+0.000001<d.NextAttackTime) continue;
                ScheduleDefenderAttack(d,target,guardSettings.ProjectileTime);
            }
        }

        public int UnitTaunt(DeployedUnitState unit)=>IsPatriot(unit)?unit.PatriotPhase==PatriotPhase.March?2:0:UnitCombat(unit).Effects.TauntLevel;
        bool HasHigherTauntTarget(DefenderState defender,DeployedUnitState current,bool blocks)
        {
            foreach(var unit in units)
                if(CanDefenderTarget(unit) && GroundDistance(unit.Motion.Position,defender.Position)<=DefenderRange(defender) &&
                    (!blocks || unit.BlockerId==defender.EntityId) && UnitTaunt(unit)>UnitTaunt(current)) return true;
            return false;
        }

        private DefenderState ChooseDefender(DeployedUnitState unit,float range)
        {
            var blocker=FindDefender(unit.BlockerId);
            if(blocker!=null && blocker.Alive && UnitCanReachDefender(unit,blocker) && GroundDistance(unit.Motion.Position,blocker.Position)<=range)
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
            if(previous!=null && previous.Alive && UnitCanReachDefender(unit,previous) && !DefenderCamouflaged(previous) && GroundDistance(unit.Motion.Position,previous.Position)<=range) return previous;
            DefenderState nearest=null; float distance=float.PositiveInfinity;
            foreach(var d in defenders)
            {
                if(!d.Alive || !UnitCanReachDefender(unit,d) || DefenderCamouflaged(d)) continue;
                float gap=GroundDistance(unit.Motion.Position,d.Position);
                if(gap>range) continue;

                if(gap<distance) { nearest=d; distance=gap; }
            }
            unit.CombatTargetId=nearest?.EntityId;
            return nearest;
        }
    }
}
