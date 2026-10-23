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
        public readonly bool FireRain, HasSkill, Melee;
        public readonly float AttackDuration, AttackWindup;
        public readonly MonsterEffectSettings Effects;
        public readonly float InitialSP, RequiredSP, SPPerSecond, SkillAttack, SkillInterval, SkillRange, ProjectileTime;

        public EngagementSettings(float unitAttack = 14, float unitArmor = 2, float unitRange = 0.65f, float unitInterval = 1.7f,
            float defenderHealth = 180, float defenderAttack = 22, float defenderArmor = 3, float defenderRange = 3.2f,
            float defenderInterval = 1, int blockCount = 1, float blockRadius = 0.65f,
            float initialSP = 8, float requiredSP = 12, float spPerSecond = 1, int skillAmmo = 6,
            float skillAttack = 105, float skillInterval = 3.2f, float skillRange = 2.6f, float projectileTime = 0.22f, bool fireRain = false, bool hasSkill = true, bool melee = false, float attackDuration = 0, float attackWindup = 0, MonsterEffectSettings effects = default)
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
            Effects=effects;
            AttackDuration=attackDuration; AttackWindup=attackWindup;
            HasSkill=hasSkill; Melee=melee;
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
        public int BlockedCount { get; internal set; }
        public double LastHitTime { get; internal set; } = double.NegativeInfinity;
        public double DeathTime { get; internal set; } = double.PositiveInfinity;
        public float ArmorReduction { get; internal set; }
        public double ColdUntil { get; internal set; } = double.NegativeInfinity;
        public double FrozenUntil { get; internal set; } = double.NegativeInfinity;
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
        public bool Resolved { get; internal set; }
        public bool Applied { get; internal set; }
        internal CombatShot(string source, string target, bool defender, bool empowered, Vector3 origin, Vector3 destination,
            double start, float duration, float attack, bool fireRain = false, bool healing = false, CombatDamageType damageType = CombatDamageType.Physical, float splashRadius = 0)
        { Healing=healing; DamageType=damageType; SplashRadius=splashRadius; FireRain=fireRain; SourceId = source; TargetId = target; FromDefender = defender; Empowered = empowered; Origin = origin;
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
            combatShots.Clear(); deathBlasts.Clear(); skillRequests.Clear(); SkillCasts = 0;
            defenders.Clear();
            if(!EngagementEnabled) return;
            if(defenderRoster!=null) { foreach(var spec in defenderRoster) defenders.Add(new DefenderState(spec)); return; }
            var c=CombatSettings;
            for(int i=0;i<defenderPositions.Length;i++)
                defenders.Add(new DefenderState(new DefenderSettings(i==0 ? "kroos" : "kroos-"+i,"克洛丝",null,
                    defenderPositions[i],c.DefenderHealth,c.DefenderAttack,c.DefenderArmor,c.DefenderRange,c.DefenderInterval,
                    c.BlockCount,blockRadius:c.BlockRadius)));
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
            ResolveDeathEffects();
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
                if(unit==null || !unit.Active || defender==null || !defender.Alive) continue;
                shot.Applied=true;
                if(shot.FromDefender)
                {
                    ApplyDefenderHit(unit,shot);
                    if(shot.SplashRadius>0)
                        foreach(var other in units)
                            if(other!=unit && other.Active && GroundDistance(other.Motion.Position,unit.Motion.Position)<=shot.SplashRadius)
                                ApplyDefenderHit(other,shot);
                }
                else
                {
                    DamageDefender(defender,shot.Attack,c.Effects.DamageType);
                    if(defender.Alive) defender.ArmorReduction+=c.Effects.ArmorShred;
                }
            }
            foreach(var d in defenders) d.BlockedCount=0;
            foreach(var unit in units)
            {
                if(!unit.Active) { unit.IsBlocked=unit.HasCombatTarget=false; continue; }
                bool casting=c.FireRain && IsSkillActionPlaying(unit);
                if(c.HasSkill && unit.SkillAmmo==0 && !casting) unit.SkillSP=Mathf.Min(c.RequiredSP,unit.SkillSP+step*c.SPPerSecond);
                unit.IsBlocked=false; unit.BlockerId=null;
                foreach(var d in defenders)
                    if(d.Alive && d.BlockedCount<d.Settings.BlockCount && GroundDistance(unit.Motion.Position,d.Position)<=d.Settings.BlockRadius)
                    { unit.IsBlocked=true; unit.BlockerId=d.EntityId; d.BlockedCount++; break; }
                float range=unit.SkillAmmo>0 && !c.FireRain ? c.SkillRange : c.UnitRange;
                DefenderState target=ChooseDefender(unit,range);
                unit.HasCombatTarget=target!=null;
                unit.IsMoving=!unit.IsBlocked && !unit.HasCombatTarget && !IsSkillActionPlaying(unit) && Clock>=unit.LastAttackTime+c.AttackDuration;
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
                target=ChooseDefender(unit,range); unit.HasCombatTarget=target!=null;
                if(target!=null && !casting && Clock+0.000001>=unit.NextAttackTime)
                {
                    bool empowered=!c.FireRain && unit.SkillAmmo>0;
                    if(empowered) unit.SkillAmmo--;
                    unit.LastAttackTime=Clock; unit.LastAttackEmpowered=empowered;
                    unit.NextAttackTime=Clock+(empowered ? c.SkillInterval : c.UnitInterval);
                    combatShots.Add(new CombatShot(unit.EntityId,target.EntityId,false,empowered,
                        unit.Motion.Position,target.Position,Clock+c.AttackWindup,c.ProjectileTime,empowered ? c.SkillAttack : c.UnitAttack));
                }
            }
            if(Defense==0) return;
            foreach(var d in defenders)
            {
                if(!d.Alive || d.FrozenUntil>Clock) continue;
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
                            d.NextAttackTime=Clock+spec.Interval/(d.ColdUntil>Clock ? .7 : 1);
                            combatShots.Add(new CombatShot(d.EntityId,wounded.EntityId,true,false,d.Position,wounded.Position,
                                Clock+spec.Windup,c.ProjectileTime,spec.Attack,healing:true));
                        }
                        continue;
                    }
                    if(spec.Role==DefenderRole.Medic) { d.TargetId=null; continue; }
                }
                var target=FindUnit(d.TargetId);
                bool blocks=d.BlockedCount>0;
                if(target==null || !target.Active || GroundDistance(target.Motion.Position,d.Position)>spec.Range ||
                    blocks && target.BlockerId!=d.EntityId)
                {
                    target=null;
                    foreach(var unit in units)
                    {
                        if(!unit.Active || GroundDistance(unit.Motion.Position,d.Position)>spec.Range ||
                            blocks && unit.BlockerId!=d.EntityId) continue;
                        if(target==null || unit.Motion.DistanceTravelled>target.Motion.DistanceTravelled) target=unit;
                    }
                    d.TargetId=target?.EntityId;
                }
                if(target==null || Clock+0.000001<d.NextAttackTime) continue;
                d.LastActionTime=Clock; d.LastActionHealing=false;
                ShotsFired++; LastShotTime=Clock; LastShotEntityId=target.EntityId; LastShotPosition=target.Motion.Position;
                d.NextAttackTime=Clock+spec.Interval/(d.ColdUntil>Clock ? .7 : 1);
                combatShots.Add(new CombatShot(d.EntityId,target.EntityId,true,false,d.Position,target.Motion.Position,
                    Clock+spec.Windup,c.ProjectileTime,spec.Attack,damageType:spec.DamageType,splashRadius:spec.SplashRadius));
            }
        }

        private void ApplyDefenderHit(DeployedUnitState unit,CombatShot shot)
        {
            unit.Health=Mathf.Max(0,unit.Health-CalculateDamage(shot.Attack,CombatSettings.UnitArmor,0,shot.DamageType));
            unit.LastHitTime=Clock;
            if(unit.Health==0) { unit.Resolution=UnitResolution.Dead; unit.DeathTime=Clock; unit.IsMoving=unit.IsBlocked=false; ScheduleDeathEffect(unit); }
        }

        private DefenderState ChooseDefender(DeployedUnitState unit,float range)
        {
            var blocker=FindDefender(unit.BlockerId);
            if(blocker!=null && blocker.Alive && GroundDistance(unit.Motion.Position,blocker.Position)<=range)
            { unit.CombatTargetId=blocker.EntityId; return blocker; }
            var previous=FindDefender(unit.CombatTargetId);
            if(previous!=null && previous.Alive && GroundDistance(unit.Motion.Position,previous.Position)<=range) return previous;
            DefenderState nearest=null; float distance=float.PositiveInfinity;
            foreach(var d in defenders)
            {
                if(!d.Alive) continue;
                float gap=GroundDistance(unit.Motion.Position,d.Position);
                if(gap>range) continue;

                if(gap<distance) { nearest=d; distance=gap; }
            }
            unit.CombatTargetId=nearest?.EntityId;
            return nearest;
        }
    }
}
