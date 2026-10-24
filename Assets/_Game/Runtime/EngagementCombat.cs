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
        public readonly float InitialSP, RequiredSP, SPPerSecond, SkillAttack, SkillInterval, SkillRange, ProjectileTime;

        public EngagementSettings(float unitAttack = 14, float unitArmor = 2, float unitRange = 0.65f, float unitInterval = 1.7f,
            float defenderHealth = 180, float defenderAttack = 22, float defenderArmor = 3, float defenderRange = 3.2f,
            float defenderInterval = 1, int blockCount = 1, float blockRadius = 0.65f,
            float initialSP = 8, float requiredSP = 12, float spPerSecond = 1, int skillAmmo = 6,
            float skillAttack = 105, float skillInterval = 3.2f, float skillRange = 2.6f, float projectileTime = 0.22f, bool fireRain = false, bool hasSkill = true, bool melee = false, float attackDuration = 0, float attackWindup = 0, MonsterEffectSettings effects = default, bool canAttack = true,CombatDamageType defenderDamageType=CombatDamageType.Physical)
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
            if(effects.Healer && (canAttack || hasSkill)) throw new ArgumentException("Healer uses automatic treatment, not damage attacks or ammunition skills.");
            if(defenderDamageType!=CombatDamageType.Physical && defenderDamageType!=CombatDamageType.Arts) throw new ArgumentException("Invalid defender damage type.");
            Effects=effects;
            DefenderDamageType=defenderDamageType;
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
        public Vector3 Position { get; }
        public float Health { get; internal set; }
        public bool Alive => Health > 0;
        public string TargetId { get; internal set; }
        public int BlockedCount { get; internal set; }
        public double LastHitTime { get; internal set; } = double.NegativeInfinity;
        public double DeathTime { get; internal set; } = double.PositiveInfinity;
        public float ArmorReduction { get; internal set; }
        public float BurnAccumulated {get;internal set;}
        public double BurnUntil {get;internal set;}=double.NegativeInfinity;
        public double ColdUntil { get; internal set; } = double.NegativeInfinity;
        public double FrozenUntil { get; internal set; } = double.NegativeInfinity;
        public double FrostSlowUntil { get; internal set; } = double.NegativeInfinity;
        internal double NextAttackTime;
        internal DefenderState(string id, Vector3 position, float health) { EntityId=id; Position = position; Health = health; }
    }

    public sealed class CombatShot
    {
        public string SourceId { get; }
        public string TargetId { get; }
        public bool FromDefender { get; }
        public bool Empowered { get; }
        public bool FireRain { get; }
        public Vector3 Origin { get; }
        public Vector3 Destination { get; }
        public double StartedAt { get; }
        public double ImpactAt { get; }
        public float Attack { get; }
        public float ColdDuration { get; }
        public bool Resolved { get; internal set; }
        public bool Applied { get; internal set; }
        public bool FrozenBonusApplied { get; internal set; }
        public float DamageDealt { get; internal set; }
        internal CombatShot(string source, string target, bool defender, bool empowered, Vector3 origin, Vector3 destination,
            double start, float duration, float attack, bool fireRain = false,float coldDuration=0)
        { ColdDuration=coldDuration;FireRain=fireRain; SourceId = source; TargetId = target; FromDefender = defender; Empowered = empowered; Origin = origin;
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
        private readonly List<DefenderState> defenders = new List<DefenderState>();
        public IReadOnlyList<DefenderState> Defenders => defenders.AsReadOnly();
        public DefenderState Defender => defenders.Count>0 ? defenders[0] : null;
        public DefenderState FindDefender(string id) => defenders.Find(d=>d.EntityId==id);
        public IReadOnlyList<CombatShot> CombatShots => combatShots.AsReadOnly();
        public int SkillCasts { get; private set; }

        private void ResetEngagement()
        {
            combatShots.Clear(); deathBlasts.Clear(); iceRings.Clear(); healingCasts.Clear(); skillRequests.Clear(); SkillCasts = 0;
            defenders.Clear();
            if (EngagementEnabled) for(int i=0;i<defenderPositions.Length;i++)
                defenders.Add(new DefenderState(i==0 ? "kroos" : "kroos-"+i,defenderPositions[i],CombatSettings.DefenderHealth));
        }

        public DeployedUnitState FindUnit(string id)
        { foreach (var unit in units) if (unit.EntityId == id) return unit; return null; }

        public bool IsSkillActionPlaying(DeployedUnitState unit) => EngagementEnabled && CombatSettings.HasSkill && unit!=null && unit.Active &&
            (CombatSettings.FireRain ? Clock<unit.FireRainStartedAt+CombatSettings.SkillInterval :
            unit.LastAttackEmpowered && Clock<unit.LastAttackTime+CombatSettings.SkillInterval);

        // Readiness stays visible during a full pause; authorization to cast is checked separately.
        public bool IsSkillReady(string entityId)
        {
            var unit=FindUnit(entityId);
            return EngagementEnabled && CombatSettings.HasSkill && Phase==BattlePhase.Running && unit!=null && unit.Active &&
                unit.SkillAmmo==0 && (!CombatSettings.FireRain || !IsSkillActionPlaying(unit)) && unit.SkillSP+0.00001f>=CombatSettings.RequiredSP;
        }

        public SkillFailure CheckSkill(string entityId)
        {
            if (!EngagementEnabled || Phase != BattlePhase.Running) return SkillFailure.NotRunning;
            if (!CombatSettings.HasSkill) return SkillFailure.NoSkill;
            if (IsPaused) return SkillFailure.Paused;
            var unit = FindUnit(entityId);
            if (unit == null || !unit.Active) return SkillFailure.MissingEntity;
            if (unit.SkillAmmo > 0 || CombatSettings.FireRain && IsSkillActionPlaying(unit)) return SkillFailure.AlreadyActive;
            return unit.SkillSP + 0.00001f >= CombatSettings.RequiredSP ? SkillFailure.None : SkillFailure.NotReady;
        }

        public SkillFailure TryActivateSkill(int runId, long requestId, string entityId)
        {
            if (runId != RunId) return SkillFailure.StaleRun;
            if (skillRequests.TryGetValue(requestId, out string previous))
                return previous == entityId ? SkillFailure.None : SkillFailure.StaleRun;
            SkillFailure failure = CheckSkill(entityId);
            if (failure != SkillFailure.None) return failure;
            var unit = FindUnit(entityId);
            unit.SkillSP = 0; unit.SkillAmmo = CombatSettings.SkillAmmo; unit.SkillsUsed++; SkillCasts++;
            unit.NextAttackTime = Clock;
            if(CombatSettings.FireRain)
            { unit.FireRainStartedAt=Clock; unit.IsMoving=false; unit.NextAttackTime=Clock+CombatSettings.SkillInterval; }
            skillRequests.Add(requestId, entityId);
            return SkillFailure.None;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0; return Vector3.Distance(a, b); }

        private void StepEngagement(float step)
        {
            EngagementSettings c=CombatSettings;
            StepFacilities(step);
            ResolveDeathEffects();
            ResolveHealing();
            foreach(var shot in combatShots)
            {
                if(shot.Resolved || Clock+0.000001<shot.ImpactAt) continue;
                shot.Resolved=true;
                var unit=FindUnit(shot.FromDefender ? shot.TargetId : shot.SourceId);
                var defender=FindDefender(shot.FromDefender ? shot.SourceId : shot.TargetId);
                if(unit==null || !unit.Active || unit.IsReviving || defender==null || !defender.Alive) continue;
                shot.Applied=true;
                if(shot.FromDefender)
                {
                    TryDamageUnit(unit.EntityId,shot.Attack,c.DefenderDamageType);
                }
                else
                {
                    // Freeze can begin or expire while the projectile is in flight.
                    shot.FrozenBonusApplied=defender.FrozenUntil>Clock && c.Effects.FrozenAttackMultiplier>1;
                    float attack=shot.Attack*(shot.FrozenBonusApplied ? c.Effects.FrozenAttackMultiplier : 1);
                    float health=defender.Health;
                    DamageDefender(defender,attack,unit.AltarEmpowered?CombatDamageType.Arts:c.Effects.DamageType);
                    shot.DamageDealt=health-defender.Health;
                    HealUnit(unit,shot.DamageDealt*c.Effects.LifeSteal);
                    if(defender.Alive) defender.ArmorReduction+=c.Effects.ArmorShred;
                    if(defender.Alive && shot.ColdDuration>0) TryApplyCold(defender.EntityId,shot.ColdDuration);
                }
            }
            foreach(var d in defenders) d.BlockedCount=0;
            foreach(var unit in units)
            {
                if(!unit.Active) { unit.IsBlocked=unit.HasCombatTarget=false; continue; }
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
                    if(d.Alive && d.BlockedCount<c.BlockCount && GroundDistance(unit.Motion.Position,d.Position)<=c.BlockRadius)
                    { unit.IsBlocked=true; unit.BlockerId=d.EntityId; d.BlockedCount++; break; }
                if(c.Effects.FrostNova && StepIceRing(unit) || c.Effects.Healer && StepHealer(unit) || c.Effects.Mudrock && StepMudrock(unit)) continue;
                float range=unit.SkillAmmo>0 && !c.FireRain ? c.SkillRange : c.UnitRange;
                DefenderState target=c.CanAttack ? ChooseDefender(unit,range) : null;
                if(!c.CanAttack) unit.CombatTargetId=null;
                unit.HasCombatTarget=target!=null;
                unit.IsMoving=!unit.IsBlocked && !unit.HasCombatTarget && !IsSkillActionPlaying(unit) && Clock>=unit.LastAttackTime+c.AttackDuration/unit.LastAttackRate;
                if(unit.IsMoving)
                {
                    double lockEnd=c.FireRain ? unit.FireRainStartedAt+c.SkillInterval :
                        unit.LastAttackEmpowered ? unit.LastAttackTime+c.SkillInterval : double.NegativeInfinity;
                    float moveStep=(float)Math.Min(step,Math.Max(0,Clock-lockEnd));
                    if(moveStep>0) unit.Motion.Advance(moveStep);
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
                target=c.CanAttack ? ChooseDefender(unit,range) : null; unit.HasCombatTarget=target!=null;
                if(target!=null && !casting && Clock+0.000001>=unit.NextAttackTime)
                {
                    bool empowered=!c.FireRain && unit.SkillAmmo>0;
                    if(empowered) unit.SkillAmmo--;
                    else unit.OrdinaryAttacksFired++;
                    float coldDuration=!empowered && c.Effects.ColdAttackEvery>0 &&
                        unit.OrdinaryAttacksFired%c.Effects.ColdAttackEvery==0 ? c.Effects.AttackColdDuration : 0;
                    unit.LastAttackTime=Clock; unit.LastAttackEmpowered=empowered;
                    unit.LastAttackRate=empowered ? 1 : UnitAttackRate(unit);
                    unit.NextAttackTime=Clock+(empowered ? c.SkillInterval : c.UnitInterval)/unit.LastAttackRate;
                    combatShots.Add(new CombatShot(unit.EntityId,target.EntityId,false,empowered,
                        unit.Motion.Position,target.Position,Clock+c.AttackWindup/unit.LastAttackRate,c.ProjectileTime,(empowered ? c.SkillAttack : c.UnitAttack)*UnitAttackMultiplier(unit),coldDuration:coldDuration));
                    if(!empowered && c.Effects.AttackTargets>1)
                    {
                        int count=1;
                        foreach(var other in defenders)
                        {
                            if(count>=c.Effects.AttackTargets) break;
                            if(!other.Alive || other==target || GroundDistance(unit.Motion.Position,other.Position)>range) continue;
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
                if(!d.Alive || d.FrozenUntil>Clock) continue;
                var target=FindUnit(d.TargetId);
                bool blocks=d.BlockedCount>0;
                if(target==null || !target.Active || target.IsReviving || GroundDistance(target.Motion.Position,d.Position)>c.DefenderRange ||
                    blocks && target.BlockerId!=d.EntityId)
                {
                    target=null;
                    foreach(var unit in units)
                    {
                        if(!unit.Active || unit.IsReviving || GroundDistance(unit.Motion.Position,d.Position)>c.DefenderRange ||
                            blocks && unit.BlockerId!=d.EntityId) continue;
                        if(target==null || unit.Motion.DistanceTravelled>target.Motion.DistanceTravelled) target=unit;
                    }
                    d.TargetId=target?.EntityId;
                }
                if(target==null || Clock+0.000001<d.NextAttackTime) continue;
                ShotsFired++; LastShotTime=Clock; LastShotEntityId=target.EntityId; LastShotPosition=target.Motion.Position;
                d.NextAttackTime=Clock+c.DefenderInterval/DefenderAttackRate(d);
                combatShots.Add(new CombatShot(d.EntityId,target.EntityId,true,false,d.Position,target.Motion.Position,
                    Clock,c.ProjectileTime,c.DefenderAttack));
            }
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
