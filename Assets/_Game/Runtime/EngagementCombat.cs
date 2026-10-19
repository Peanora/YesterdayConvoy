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
        public readonly float InitialSP, RequiredSP, SPPerSecond, SkillAttack, SkillInterval, SkillRange, ProjectileTime;

        public EngagementSettings(float unitAttack = 14, float unitArmor = 2, float unitRange = 0.65f, float unitInterval = 1.7f,
            float defenderHealth = 180, float defenderAttack = 22, float defenderArmor = 3, float defenderRange = 3.2f,
            float defenderInterval = 1, int blockCount = 1, float blockRadius = 0.65f,
            float initialSP = 8, float requiredSP = 12, float spPerSecond = 1, int skillAmmo = 6,
            float skillAttack = 105, float skillInterval = 3.2f, float skillRange = 2.6f, float projectileTime = 0.22f)
        {
            foreach (float value in new[] { unitAttack, unitRange, unitInterval, defenderHealth, defenderAttack, defenderRange,
                defenderInterval, blockRadius, requiredSP, spPerSecond, skillAttack, skillInterval, skillRange, projectileTime })
                if (!Finite(value) || value <= 0) throw new ArgumentException("Combat values must be finite and positive.");
            if (!Finite(unitArmor) || !Finite(defenderArmor) || !Finite(initialSP) || unitArmor < 0 || defenderArmor < 0 ||
                initialSP < 0 || initialSP > requiredSP || blockCount < 0 || skillAmmo <= 0)
                throw new ArgumentException("Invalid combat armor, block count or initial SP.");
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
        public Vector3 Position { get; }
        public float Health { get; internal set; }
        public bool Alive => Health > 0;
        public string TargetId { get; internal set; }
        public int BlockedCount { get; internal set; }
        public double LastHitTime { get; internal set; } = double.NegativeInfinity;
        public double DeathTime { get; internal set; } = double.PositiveInfinity;
        internal double NextAttackTime;
        internal DefenderState(Vector3 position, float health) { Position = position; Health = health; }
    }

    public sealed class CombatShot
    {
        public string SourceId { get; }
        public string TargetId { get; }
        public bool FromDefender { get; }
        public bool Empowered { get; }
        public Vector3 Origin { get; }
        public Vector3 Destination { get; }
        public double StartedAt { get; }
        public double ImpactAt { get; }
        public float Attack { get; }
        public bool Resolved { get; internal set; }
        public bool Applied { get; internal set; }
        internal CombatShot(string source, string target, bool defender, bool empowered, Vector3 origin, Vector3 destination,
            double start, float duration, float attack)
        { SourceId = source; TargetId = target; FromDefender = defender; Empowered = empowered; Origin = origin;
            Destination = destination; StartedAt = start; ImpactAt = start + duration; Attack = attack; }
    }

    public enum SkillFailure { None, NotRunning, Paused, StaleRun, MissingEntity, NotReady, AlreadyActive }

    public sealed partial class ManualBattle
    {
        private readonly EngagementSettings? engagement;
        private readonly List<CombatShot> combatShots = new List<CombatShot>();
        private readonly Dictionary<long, string> skillRequests = new Dictionary<long, string>();
        public bool EngagementEnabled => engagement.HasValue;
        public EngagementSettings CombatSettings => engagement.Value;
        public DefenderState Defender { get; private set; }
        public IReadOnlyList<CombatShot> CombatShots => combatShots.AsReadOnly();
        public int SkillCasts { get; private set; }

        private void ResetEngagement()
        {
            combatShots.Clear(); skillRequests.Clear(); SkillCasts = 0;
            Defender = EngagementEnabled ? new DefenderState(towerPosition, CombatSettings.DefenderHealth) : null;
        }

        public DeployedUnitState FindUnit(string id)
        { foreach (var unit in units) if (unit.EntityId == id) return unit; return null; }

        public bool IsSkillActionPlaying(DeployedUnitState unit) => EngagementEnabled && unit!=null && unit.Active &&
            unit.LastAttackEmpowered && Clock<unit.LastAttackTime+CombatSettings.SkillInterval;

        // Readiness stays visible during a full pause; authorization to cast is checked separately.
        public bool IsSkillReady(string entityId)
        {
            var unit=FindUnit(entityId);
            return EngagementEnabled && Phase==BattlePhase.Running && unit!=null && unit.Active &&
                unit.SkillAmmo==0 && unit.SkillSP+0.00001f>=CombatSettings.RequiredSP;
        }

        public SkillFailure CheckSkill(string entityId)
        {
            if (!EngagementEnabled || Phase != BattlePhase.Running) return SkillFailure.NotRunning;
            if (IsPaused) return SkillFailure.Paused;
            var unit = FindUnit(entityId);
            if (unit == null || !unit.Active) return SkillFailure.MissingEntity;
            if (unit.SkillAmmo > 0) return SkillFailure.AlreadyActive;
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
            skillRequests.Add(requestId, entityId);
            return SkillFailure.None;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0; return Vector3.Distance(a, b); }

        private void StepEngagement(float step)
        {
            EngagementSettings c = CombatSettings;
            // A launched projectile settles once. Dead sources/targets cancel effects; no corpse casts.
            foreach (var shot in combatShots)
            {
                if (shot.Resolved || Clock + 0.000001 < shot.ImpactAt) continue;
                shot.Resolved = true;
                var unit = FindUnit(shot.FromDefender ? shot.TargetId : shot.SourceId);
                if (unit == null || !unit.Active || !Defender.Alive) continue;
                shot.Applied = true;
                if (shot.FromDefender)
                {
                    unit.Health = Mathf.Max(0, unit.Health - Mathf.Max(1, shot.Attack - c.UnitArmor));
                    unit.LastHitTime = Clock;
                    if (unit.Health == 0) { unit.Resolution = UnitResolution.Dead; unit.IsMoving = false; unit.IsBlocked = false; }
                }
                else
                {
                    Defender.Health = Mathf.Max(0, Defender.Health - Mathf.Max(1, shot.Attack - c.DefenderArmor));
                    Defender.LastHitTime = Clock;
                    if (!Defender.Alive) { Defender.DeathTime = Clock; Defender.TargetId = null; }
                }
            }

            Defender.BlockedCount = 0;
            foreach (var unit in units)
            {
                if (!unit.Active) { unit.IsBlocked = unit.HasCombatTarget = false; continue; }
                if (unit.SkillAmmo == 0) unit.SkillSP = Mathf.Min(c.RequiredSP, unit.SkillSP + step * c.SPPerSecond);
                unit.IsBlocked = Defender.Alive && Defender.BlockedCount < c.BlockCount &&
                    GroundDistance(unit.Motion.Position, Defender.Position) <= c.BlockRadius;
                if (unit.IsBlocked) Defender.BlockedCount++;
                float range = unit.SkillAmmo > 0 ? c.SkillRange : c.UnitRange;
                unit.HasCombatTarget = Defender.Alive && GroundDistance(unit.Motion.Position, Defender.Position) <= range;
                unit.IsMoving = !unit.IsBlocked && !unit.HasCombatTarget && !IsSkillActionPlaying(unit);
                if (unit.IsMoving)
                {
                    // A step straddling the action end may only move for its unlocked remainder.
                    float moveStep=unit.LastAttackEmpowered ?
                        (float)Math.Min(step,Math.Max(0,Clock-unit.LastAttackTime-c.SkillInterval)) : step;
                    if(moveStep>0) unit.Motion.Advance(moveStep);
                }
                if (unit.Motion.HasArrived)
                {
                    unit.Resolution = UnitResolution.Breached; unit.IsMoving = false; unit.HasCombatTarget = unit.IsBlocked = false;
                    Breaches++; Defense = Math.Max(0, Defense - Settings.GateDamage);
                    continue;
                }
                unit.HasCombatTarget = Defender.Alive && GroundDistance(unit.Motion.Position, Defender.Position) <= range;
                if (unit.HasCombatTarget && Clock + 0.000001 >= unit.NextAttackTime)
                {
                    bool empowered = unit.SkillAmmo > 0;
                    if (empowered) unit.SkillAmmo--;
                    unit.LastAttackTime = Clock;
                    unit.LastAttackEmpowered = empowered;
                    unit.NextAttackTime = Clock + (empowered ? c.SkillInterval : c.UnitInterval);
                    combatShots.Add(new CombatShot(unit.EntityId, "kroos", false, empowered, unit.Motion.Position, Defender.Position,
                        Clock, c.ProjectileTime, empowered ? c.SkillAttack : c.UnitAttack));
                }
            }
            if (!Defender.Alive || Defense == 0) return;
            var target = FindUnit(Defender.TargetId);
            bool blocksExist = Defender.BlockedCount > 0;
            if (target == null || !target.Active || GroundDistance(target.Motion.Position, Defender.Position) > c.DefenderRange ||
                (blocksExist && !target.IsBlocked))
            {
                target = null;
                foreach (var unit in units)
                {
                    if (!unit.Active || GroundDistance(unit.Motion.Position, Defender.Position) > c.DefenderRange ||
                        (blocksExist && !unit.IsBlocked)) continue;
                    if (target == null || unit.Motion.DistanceTravelled > target.Motion.DistanceTravelled) target = unit;
                }
                Defender.TargetId = target?.EntityId;
            }
            if (target == null || Clock + 0.000001 < Defender.NextAttackTime) return;
            ShotsFired++; LastShotTime = Clock; LastShotEntityId = target.EntityId; LastShotPosition = target.Motion.Position;
            Defender.NextAttackTime = Clock + c.DefenderInterval;
            combatShots.Add(new CombatShot("kroos", target.EntityId, true, false, Defender.Position, target.Motion.Position,
                Clock, c.ProjectileTime, c.DefenderAttack));
        }
    }
}
