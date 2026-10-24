using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum BattlePhase { Ready, Running, Won, Lost }
    public enum UnitResolution { Active, Dead, Breached }
    public enum DeployFailure { None, NotStarted, Finished, Paused, WrongEntry, NoDP, NoUses, CoolingDown, EntryBusy, StaleRun }

    /// <summary>An immutable snapshot. Changing an authored asset cannot mutate a battle in progress.</summary>
    public readonly struct ManualBattleSettings
    {
        public readonly string CardId, UnitId;
        public readonly int InitialDP, InitialDefense, DeploymentLimit, DeploymentCost, GateDamage;
        public readonly float Cooldown, Health, Speed, TowerRange, TowerDamage, ShotInterval;

        public ManualBattleSettings(string cardId, string unitId, int initialDP, int defense, int uses, int cost,
            float cooldown, float health, float speed, int gateDamage, float towerRange, float towerDamage, float shotInterval)
        {
            if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(unitId) || initialDP < 0 || defense <= 0 || uses <= 0 || cost <= 0 || gateDamage <= 0)
                throw new ArgumentException("Invalid manual battle IDs or integer settings.");
            foreach (float value in new[] { cooldown, health, speed, towerRange, towerDamage, shotInterval })
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                    throw new ArgumentException("Manual battle values must be finite and positive.");
            CardId = cardId; UnitId = unitId; InitialDP = initialDP; InitialDefense = defense;
            DeploymentLimit = uses; DeploymentCost = cost; Cooldown = cooldown; Health = health;
            Speed = speed; GateDamage = gateDamage; TowerRange = towerRange; TowerDamage = towerDamage; ShotInterval = shotInterval;
        }
    }

    public sealed class DeployedUnitState
    {
        public string EntityId { get; }
        public string UnitId { get; }
        public double SpawnTime { get; }
        public RouteMotion Motion { get; }
        public float Health { get; internal set; }
        public float MaxHealth { get; internal set; }
        public float ArtsBarrierRemaining { get; internal set; }
        public float ArtsBarrierLayerSize { get; internal set; }
        public int ArtsBarrierLayers => ArtsBarrierLayerSize>0 ? Mathf.CeilToInt(ArtsBarrierRemaining/ArtsBarrierLayerSize) : 0;
        public float LastAttackRate { get; internal set; } = 1;
        public double BarrierRefreshStartedAt { get; internal set; } = double.NegativeInfinity;
        public int BarrierRefreshes { get; internal set; }
        internal double NextBarrierRefreshAt;
        public double LastHitTime { get; internal set; } = double.NegativeInfinity;
        public double DeathTime { get; internal set; } = double.PositiveInfinity;
        public UnitResolution Resolution { get; internal set; }
        public bool Active => Resolution == UnitResolution.Active;
        public bool IsBlocked { get; internal set; }
        public bool HasCombatTarget { get; internal set; }
        internal string CombatTargetId, BlockerId;
        public bool IsMoving { get; internal set; } = true;
        public float SkillSP { get; internal set; }
        public int SkillAmmo { get; internal set; }
        public int SkillsUsed { get; internal set; }
        public int OrdinaryAttacksFired { get; internal set; }
        public float HealthRecovered { get; internal set; }
        public double LastHealTime { get; internal set; } = double.NegativeInfinity;
        public float HealingDone { get; internal set; }
        public double HealingStartedAt { get; internal set; } = double.NegativeInfinity;
        internal double NextHealAt;
        public bool ReviveUsed { get; internal set; }
        public double ReviveAt { get; internal set; } = double.NegativeInfinity;
        public bool IsReviving { get; internal set; }
        public double IceRingStartedAt { get; internal set; } = double.NegativeInfinity;
        internal double NextIceRingAt;
        public double LastAttackTime { get; internal set; } = double.NegativeInfinity;
        public bool LastAttackEmpowered { get; internal set; }
        public double FireRainStartedAt { get; internal set; } = double.NegativeInfinity;
        internal double NextAttackTime;

        internal DeployedUnitState(string id, string unitId, RouteMotion motion, float health, double spawnTime)
        { EntityId = id; UnitId = unitId; Motion = motion; Health = MaxHealth = health; SpawnTime = spawnTime; }
    }

    public readonly struct DeploymentReceipt
    {
        public readonly DeployFailure Failure;
        public readonly string EntityId;
        public bool Success => Failure == DeployFailure.None;
        internal DeploymentReceipt(DeployFailure failure, string entityId = null) { Failure = failure; EntityId = entityId; }
    }

    /// <summary>Owns one card's transactions and all entity state. No Unity frame clock, asset writes or scene objects.</summary>
    public sealed partial class ManualBattle
    {
        public const float EntryRadius = 0.65f;
        public const float EntryClearance = 0.32f;
        private readonly Vector3[] route;
        private readonly Vector3 towerPosition;
        private readonly List<DeployedUnitState> units = new List<DeployedUnitState>();
        private readonly Dictionary<long, DeploymentReceipt> committed = new Dictionary<long, DeploymentReceipt>();
        private readonly IReadOnlyList<DeployedUnitState> visibleUnits;
        private double nextDeploymentTime, nextShotTime;
        private int sequence;
        public ManualBattleSettings Settings { get; }
        public IReadOnlyList<DeployedUnitState> Units => visibleUnits;
        public int RunId { get; private set; }
        public string CardInstanceId => $"run-{RunId}/{Settings.CardId}/0";
        public BattlePhase Phase { get; private set; }
        public bool IsPaused { get; private set; }
        public double Clock { get; private set; }
        public int DP { get; private set; }
        public int RemainingUses { get; private set; }
        public int Defense { get; private set; }
        public int ShotsFired { get; private set; }
        public int Breaches { get; private set; }
        public string LastShotEntityId { get; private set; }
        public Vector3 LastShotPosition { get; private set; }
        public double LastShotTime { get; private set; }
        public Vector3 Entry => route[0];
        public double CooldownRemaining => Math.Max(0, nextDeploymentTime - Clock);
        public int ActiveCount { get { int count = 0; foreach (var unit in units) if (unit.Active) count++; return count; } }

        public ManualBattle(ManualBattleSettings settings, IReadOnlyList<Vector3> waypoints, Vector3 tower, EngagementSettings? combat = null, IReadOnlyList<Vector3> extraDefenders = null)
        {
            Settings = settings;
            engagement = combat;
            defenderPositions = new Vector3[1 + (extraDefenders?.Count ?? 0)];
            defenderPositions[0] = tower;
            for (int i=1;i<defenderPositions.Length;i++) defenderPositions[i]=extraDefenders[i-1];
            // Reuse the existing route validation and take our own route snapshot.
            _ = new RouteMotion(waypoints, settings.Speed);
            route = new Vector3[waypoints.Count];
            for (int i = 0; i < route.Length; i++) route[i] = waypoints[i];
            towerPosition = tower;
            visibleUnits = units.AsReadOnly();
            Restart();
        }

        public void Restart()
        {
            RunId++;
            units.Clear(); committed.Clear(); sequence = 0;
            DP = Settings.InitialDP; RemainingUses = Settings.DeploymentLimit; Defense = Settings.InitialDefense;
            Clock = nextDeploymentTime = nextShotTime = 0;
            ShotsFired = Breaches = 0; LastShotEntityId = null; LastShotTime = double.NegativeInfinity;
            LastShotPosition = Entry; IsPaused = false; Phase = BattlePhase.Ready;
            ResetEngagement();
        }

        public void Begin()
        {
            if (Phase != BattlePhase.Ready) return;
            Phase = BattlePhase.Running;
            EvaluateEnd();
        }

        public void SetPaused(bool paused) { if (Phase == BattlePhase.Running) IsPaused = paused; }

        public bool IsEntry(Vector3 position)
        {
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.z) || float.IsInfinity(position.z)) return false;
            Vector3 offset = position - Entry; offset.y = 0;
            return offset.sqrMagnitude <= EntryRadius * EntryRadius;
        }

        public DeployFailure CheckDeployment(Vector3 position, bool allowWhilePaused = false)
        {
            if (Phase == BattlePhase.Won || Phase == BattlePhase.Lost) return DeployFailure.Finished;
            if (IsPaused && !allowWhilePaused) return DeployFailure.Paused;
            if (!IsEntry(position)) return DeployFailure.WrongEntry;
            if (RemainingUses == 0) return DeployFailure.NoUses;
            if (DP < Settings.DeploymentCost) return DeployFailure.NoDP;
            if (Clock + 0.000001 < nextDeploymentTime) return DeployFailure.CoolingDown;
            foreach (var unit in units)
                if (unit.Active && Vector3.Distance(unit.Motion.Position, Entry) < EntryClearance) return DeployFailure.EntryBusy;
            return Phase == BattlePhase.Ready ? DeployFailure.NotStarted : DeployFailure.None;
        }

        public DeploymentReceipt TryDeploy(int runId, long requestId, Vector3 position, bool allowWhilePaused = false)
        {
            if (runId != RunId) return new DeploymentReceipt(DeployFailure.StaleRun);
            if (committed.TryGetValue(requestId, out var previous)) return previous;
            DeployFailure failure = CheckDeployment(position, allowWhilePaused);
            if (failure != DeployFailure.None) return new DeploymentReceipt(failure);
            var motion = new RouteMotion(route, Settings.Speed);
            string id = $"run-{RunId}/unit-{sequence + 1}";
            var unit = new DeployedUnitState(id, Settings.UnitId, motion, Settings.Health, Clock);
            if (EngagementEnabled) unit.SkillSP = CombatSettings.InitialSP;
            unit.NextIceRingAt=Clock+8.5;
            if(EngagementEnabled && (CombatSettings.Effects.ArtsBarrier || CombatSettings.Effects.Mudrock))
            {
                unit.MaxHealth=unit.Health=Settings.Health*1.5f;
                unit.ArtsBarrierLayerSize=Settings.Health*(CombatSettings.Effects.Mudrock ? 5500f/45000 : .2f);
                unit.ArtsBarrierRemaining=unit.ArtsBarrierLayerSize*3;unit.NextBarrierRefreshAt=Clock+17;
            }
            // Commit together only after all preconditions and entity initialization succeed.
            units.Add(unit); sequence++; DP -= Settings.DeploymentCost; RemainingUses--;
            nextDeploymentTime = Clock + Settings.Cooldown;
            var receipt = new DeploymentReceipt(DeployFailure.None, id);
            committed.Add(requestId, receipt);
            return receipt;
        }

        public void Advance(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
            if (Phase != BattlePhase.Running || IsPaused || delta == 0) return;
            double remaining = delta;
            // Bound combat steps so a long frame cannot skip the tower's range or fire interval.
            while (remaining > 0.00000001 && Phase == BattlePhase.Running && !IsPaused)
            {
                float step = (float)Math.Min(0.05, remaining);
                remaining -= step; Clock += step;
                if (EngagementEnabled)
                {
                    StepEngagement(step);
                    EvaluateEnd();
                    if(Phase==BattlePhase.Won || Phase==BattlePhase.Lost)
                        foreach(var shot in combatShots) if(!shot.Resolved) shot.Resolved=true;
                    continue;
                }
                foreach (var unit in units)
                {
                    if (!unit.Active) continue;
                    unit.Motion.Advance(step);
                    if (unit.Motion.HasArrived)
                    {
                        unit.Resolution = UnitResolution.Breached; Breaches++;
                        Defense = Math.Max(0, Defense - Settings.GateDamage);
                    }
                }
                // Resolve arrivals before exhaustion and before firing at already departed entities.
                if (Defense == 0) { Phase = BattlePhase.Won; IsPaused = false; break; }
                if (Clock + 0.000001 >= nextShotTime)
                {
                    DeployedUnitState target = null;
                    foreach (var unit in units)
                    {
                        if (!unit.Active) continue;
                        Vector3 offset = unit.Motion.Position - towerPosition; offset.y = 0;
                        if (offset.sqrMagnitude > Settings.TowerRange * Settings.TowerRange) continue;
                        if (target == null || unit.Motion.DistanceTravelled > target.Motion.DistanceTravelled) target = unit;
                    }
                    if (target != null)
                    {
                        target.Health = Mathf.Max(0, target.Health - Settings.TowerDamage);
                        target.LastHitTime = LastShotTime = Clock; LastShotEntityId = target.EntityId;
                        LastShotPosition = target.Motion.Position; ShotsFired++; nextShotTime = Clock + Settings.ShotInterval;
                        if (target.Health == 0) { target.Resolution = UnitResolution.Dead; target.DeathTime=Clock; }
                    }
                }
                EvaluateEnd();
            }
        }

        private void EvaluateEnd()
        {
            if (Phase != BattlePhase.Running) return;
            if (Defense == 0) { Phase = BattlePhase.Won; IsPaused = false; }
            else if (!HasPendingDeathBlast && ActiveCount == 0 && (RemainingUses == 0 || DP < Settings.DeploymentCost))
            { Phase = BattlePhase.Lost; IsPaused = false; }
            // Affordable uses remain: wait for cooldown/input. There is no timeout or passive DP recovery.
        }
    }
}
