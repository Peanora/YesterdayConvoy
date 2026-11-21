using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        const double SnowBossTransitionSeconds = 2;
        const double SnowBossS2SnowGrowthSeconds = 5.5, SnowBossS3SnowGrowthSeconds = 1.5;
        const double SnowBossS3OpeningDamageSeconds = 8;
        const int SnowBossMaxSnowCells = 20;
        const int SnowBossMaxSnowLayers = 5;
        static readonly IReadOnlyDictionary<Vector2Int, int> NoSnowBossSnow = new Dictionary<Vector2Int, int>();
        static readonly Vector2Int[] SnowSpreadDirections = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        static readonly string[] SnowBossS3Attacks = { "skill_attack", "skill_attack_b", "skill_attack_c" };
        readonly HashSet<int> snowBossFrozen = new HashSet<int>();
        double snowBossFrozenUntil;

        public bool SnowBossTransitionActive(FormationDefender guard) =>
            Phase == FormationPhase.Fighting && guard.IsSnowBoss && guard.SnowBossTransitionUntil > Clock;

        public double SnowBossFreezeRemaining(FormationFighter unit) =>
            Phase == FormationPhase.Fighting && snowBossFrozenUntil > Clock && unit.Enabled && unit.Alive && snowBossFrozen.Contains(unit.Id)
                ? snowBossFrozenUntil - Clock : 0;

        public IReadOnlyDictionary<Vector2Int, int> SnowBossSnow(FormationDefender guard) =>
            guard.IsSnowBoss && guard.Alive && guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && kit.SnowTiles != null
                ? kit.SnowTiles : NoSnowBossSnow;

        public double SnowBossS2StartedAt(FormationDefender guard) =>
            guard.IsSnowBoss && guard.Alive && guard.SnowBossPhase == 1 && !SnowBossTransitionActive(guard) &&
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && kit.Casts > 0 ? kit.LastCast : -1;

        void ResetSnowBoss(FormationDefender guard, GuardKit kit)
        {
            guard.SnowBossPhase = 1;
            guard.SnowBossTransitionStartedAt = guard.SnowBossTransitionUntil = 0;
            guard.BossHealthRefillAt = double.NegativeInfinity;
            guard.PhaseStage = 0;
            kit.SnowTiles = new Dictionary<Vector2Int, int>();
            kit.InSnow = new HashSet<int>();
            kit.SnowInduction = new Dictionary<int, Vector2Int>();
            snowBossFrozenUntil = 0;
        }

        bool BeginSnowBossTransition(FormationDefender guard)
        {
            if (!guard.IsSnowBoss || guard.SnowBossPhase != 1 || SnowBossTransitionActive(guard)) return false;
            guard.SnowBossTransitionStartedAt = Clock;
            guard.SnowBossTransitionUntil = snowBossFrozenUntil = Clock + SnowBossTransitionSeconds;
            snowBossFrozen.UnionWith(Units.Concat(Summons).Where(unit => unit.Enabled && unit.Alive).Select(unit => unit.Id));

            var kit = Kit(guard);
            kit.SnowTiles.Clear();
            kit.InSnow.Clear();
            kit.Effect = null;
            kit.ActiveUntil = 0;
            foreach (var action in presentationActions.Where(action => action.ActorId == guard.Id &&
                (action.EndAt > Clock || action.ContactAt >= Clock)))
                action.Cancelled = true;
            guard.TargetId = -1;
            guard.PresentationAction = null;
            guard.PresentationDuration = 0;
            guard.ActionUntil = guard.NextAttackAt = guard.SnowBossTransitionUntil;
            var state = State(guard);
            state.Buffs.Clear();
            state.Shields.Clear();
            guard.Shield = 0;
            BattleEvents.Add($"{Clock:0.00}:圣聆初雪:转阶段");
            return true;
        }

        void CompleteSnowBossTransition()
        {
            if (snowBossFrozenUntil == 0 || Clock + 1e-8 < snowBossFrozenUntil) return;
            snowBossFrozenUntil = 0;
            snowBossFrozen.Clear();
            var guard = Defenders.FirstOrDefault(defender => defender.IsSnowBoss);
            if (guard == null || !guard.Alive) return;
            guard.SnowBossPhase = 2;
            guard.PhaseStage = 1;
            var state = State(guard);
            guard.Health = state.LowestHealth = guard.MaxHealth;
            guard.BossHealthRefillAt = Clock;
            state.Buffs.Clear();
            guard.Range = state.BaseRange + .75f;
            Kit(guard).Casts = 0;
            BattleEvents.Add($"{Clock:0.00}:圣聆初雪:群山俯首");
        }

        FormationPresentationAction SnowBossAction(FormationDefender guard, FormationFighter target,
            string motion, double sourceDuration, double sourceContact = 0, float speed = 2)
        {
            double duration = sourceDuration / speed;
            var action = BeginGuardAction(guard, true, duration, target, motion: motion);
            guard.PresentationDuration = duration;
            guard.PresentationRate = speed;
            action.Rate = speed;
            action.EndAt = action.At + duration;
            action.ContactAt = action.LaunchedAt = action.At + sourceContact / speed;
            return action;
        }

        void TickSnowBoss(FormationDefender guard, GuardKit kit)
        {
            if (SnowBossTransitionActive(guard)) return;
            bool firstPhase = guard.SnowBossPhase == 1;
            if (kit.SnowTiles.Count > 0 && Clock + 1e-8 >= kit.NextSnowGrowth)
                GrowSnowBossField(guard, kit);
            if (firstPhase)
                TickSnowBossFloor(guard, kit);
            else
                TickSnowBossS3Floor(guard, kit);
            if (Clock + 1e-8 < guard.ActionUntil || Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0) return;
            var target = GuardVictims(guard.Position, guard.Range, 1).FirstOrDefault();
            if (target != null)
            {
                guard.TargetId = target.Id;
                Face(guard, target.Position);
            }
            if (firstPhase && kit.Casts == 0 && target != null)
            {
                kit.Casts = 1;
                kit.LastCast = Clock;
                kit.NextSnowTick = Clock + 1;
                GrowSnowBossField(guard, kit);
                SnowBossAction(guard, target, "skill_2_begin", 50d / 60);
                return;
            }
            if (!firstPhase && kit.Casts == 0)
            {
                StartSnowBossS3(guard, kit, target);
                return;
            }
            if (target == null || Clock + 1e-8 < guard.NextAttackAt) return;
            if (firstPhase)
                StrikeSnowBossS2(guard, kit, target);
            else if (GuardSkillActive(guard))
                StrikeSnowBossS3(guard, kit, target);
        }

        void StrikeSnowBossS2(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            // D27 source attack event is frame 42 at 60 fps at normal playback.
            var action = SnowBossStrike(guard, target, "skill_2_attack", 42d / 60, 0);
            GuardContact(guard, action, () =>
            {
                foreach (var unit in GuardVictims(guard.Position, guard.Range, 4))
                {
                    if (!guard.Alive || SnowBossTransitionActive(guard)) return;
                    GrowSnowBossFloor(kit, Vector2Int.RoundToInt(unit.Position));
                    CombatDamage(guard, unit, guard.Attack * 2.50f, 1, true, false);
                }
            });
        }

        bool CanSpreadBossSnow(Vector2Int cell) => cell.x >= DeploymentColumns + 1 && cell.x < Width - 1 &&
            cell.y >= 0 && cell.y < Height && (Terrain == null || Terrain.HeightAt(cell) == 0 && Terrain.IsWalkable(cell));

        void GrowSnowBossField(FormationDefender guard, GuardKit kit)
        {
            bool skillThree = guard.SnowBossPhase == 2;
            kit.NextSnowGrowth = Clock + (skillThree ? SnowBossS3SnowGrowthSeconds : SnowBossS2SnowGrowthSeconds);
            var center = Vector2Int.RoundToInt(guard.Position);
            // Source ranges x-1 and x-2: a two-cell diamond and a 5×5 region without the corners.
            for (int x = -2; x <= 2; x++)
                for (int y = -2; y <= 2; y++)
                {
                    if (Math.Abs(x) + Math.Abs(y) > (skillThree ? 3 : 2)) continue;
                    var cell = center + new Vector2Int(x, y);
                    if (!CanSpreadBossSnow(cell)) continue;
                    if (kit.SnowTiles.TryGetValue(cell, out int layers))
                        kit.SnowTiles[cell] = Math.Min(SnowBossMaxSnowLayers, layers + 1);
                    else if (kit.SnowTiles.Count < SnowBossMaxSnowCells)
                        kit.SnowTiles[cell] = 1;
                }
        }

        void GrowSnowBossFloor(GuardKit kit, Vector2Int cell)
        {
            if (!CanSpreadBossSnow(cell)) return;
            var snow = kit.SnowTiles;
            if (!snow.TryGetValue(cell, out int layers) && snow.Count >= SnowBossMaxSnowCells) return;
            snow[cell] = Math.Min(SnowBossMaxSnowLayers, layers + 2);
            if (snow[cell] < SnowBossMaxSnowLayers || snow.Count >= SnowBossMaxSnowCells) return;
            foreach (var direction in SnowSpreadDirections)
            {
                var adjacent = cell + direction;
                if (snow.ContainsKey(adjacent) || !CanSpreadBossSnow(adjacent)) continue;
                snow.Add(adjacent, 1);
                break;
            }
        }

        void TickSnowBossFloor(FormationDefender guard, GuardKit kit)
        {
            if (kit.SnowTiles.Count == 0) return;
            bool damageTick = Clock + 1e-8 >= kit.NextSnowTick;
            if (damageTick) kit.NextSnowTick = Clock + 1;
            var targets = GuardTargets().ToArray();
            kit.InSnow.IntersectWith(targets.Select(unit => unit.Id));
            foreach (var unit in targets)
            {
                if (!guard.Alive || SnowBossTransitionActive(guard)) return;
                bool onSnow = kit.SnowTiles.ContainsKey(Vector2Int.RoundToInt(unit.Position));
                if (onSnow)
                {
                    kit.InSnow.Add(unit.Id);
                    if (damageTick) SnowBossSnowDamage(guard, unit, .30f, "SnowBossS2Tick");
                }
                else if (kit.InSnow.Remove(unit.Id)) Cold(unit);
            }
        }

        void StartSnowBossS3(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            kit.Casts++;
            kit.LastCast = Clock;
            var startup = SnowBossAction(guard, target, "skill_begin", 20d / 60, 20d / 60);
            GuardContact(guard, startup, () =>
            {
                kit.Effect = new FormationGuardEffect { Kind = "snow", Origin = guard.Position, Radius = 2.25f,
                    StartedAt = Clock, EndsAt = double.PositiveInfinity };
                kit.ActiveUntil = kit.Effect.EndsAt;
                kit.NextSnowTick = Clock + 1;
                kit.SnowStrikes = 0;
                GrowSnowBossField(guard, kit);
                InduceSnowBossVictims(guard, kit);
            });
        }

        void StrikeSnowBossS3(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            var action = SnowBossStrike(guard, target, SnowBossS3Attacks[kit.SnowStrikes++ % SnowBossS3Attacks.Length],
                40d / 60, .30f);
            GuardContact(guard, action, () =>
            {
                foreach (var unit in GuardVictims(guard.Position, guard.Range, 5))
                {
                    if (!guard.Alive) return;
                    CombatDamage(guard, unit, guard.Attack * 2.70f, 1, true, false);
                }
            });
        }

        FormationPresentationAction SnowBossStrike(FormationDefender guard, FormationFighter target,
            string motion, double sourceContact, float skillAttackSpeedBonus)
        {
            float haste = Mathf.Max(.1f, 1 + skillAttackSpeedBonus - (Buff(guard, "cold") > 0 ? .2f : 0) - Buff(guard, "nerve_slow"));
            double previousDeadline = guard.NextAttackAt;
            var action = SnowBossAction(guard, target, motion, 2, sourceContact, 2 * haste);
            // Source attack intervals align to 30 Hz. Keep that deadline through a
            // single board tick of latency, but start fresh after a gap in targets.
            double interval = Math.Round(guard.Interval / (double)haste * 30, MidpointRounding.AwayFromZero) / 30;
            double start = Clock - previousDeadline <= StepSeconds + 1e-8 ? previousDeadline : Clock;
            guard.NextAttackAt = start + interval;
            return action;
        }

        void InduceSnowBossVictims(FormationDefender guard, GuardKit kit)
        {
            kit.SnowInduction.Clear();
            var origin = Vector2Int.RoundToInt(guard.Position);
            var anchorCells = Enumerable.Range(-2, 5).SelectMany(x => Enumerable.Range(-2, 5)
                .Where(y => x * x + y * y is > 0 and <= 5).Select(y => origin + new Vector2Int(x, y)))
                .Where(CanSpreadBossSnow).ToArray();
            foreach (var unit in GuardVictims(guard.Position, guard.Range, int.MaxValue))
            {
                // The rider follows the carrier and Cadhejo keeps its authored blink movement.
                if (State(unit).RidingCarrier != 0 || unit.ArtId == "B14") continue;
                foreach (var cell in anchorCells.OrderBy(cell => ((Vector2)cell - unit.Position).sqrMagnitude))
                {
                    if (Terrain != null && !Terrain.TryApproach(unit.Position, cell, unit.Range, out _, out _)) continue;
                    kit.SnowInduction.Add(unit.Id, cell);
                    break;
                }
            }
        }

        bool MoveSnowBossInduced(FormationFighter unit, float dt)
        {
            var guard = Defenders.FirstOrDefault(defender => defender.IsSnowBoss && defender.Alive && defender.SnowBossPhase == 2);
            if (guard == null) return false;
            var kit = Kit(guard);
            if (kit.Effect == null || Clock >= kit.Effect.StartedAt + 3 || !kit.SnowInduction.TryGetValue(unit.Id, out var cell))
                return false;
            if (((Vector2)cell - unit.Position).sqrMagnitude < .06f)
            {
                kit.SnowInduction.Remove(unit.Id);
                return false;
            }
            Move(unit, cell, dt, true);
            return unit.IsMoving;
        }

        void TickSnowBossS3Floor(FormationDefender guard, GuardKit kit)
        {
            var snow = kit.Effect;
            if (snow == null || Clock > snow.StartedAt + SnowBossS3OpeningDamageSeconds + 1e-8 || Clock + 1e-8 < kit.NextSnowTick) return;
            kit.NextSnowTick += 1;
            foreach (var unit in GuardTargets().Where(unit => kit.SnowTiles.ContainsKey(Vector2Int.RoundToInt(unit.Position))).ToArray())
            {
                if (!guard.Alive) return;
                SnowBossSnowDamage(guard, unit, .60f, "SnowBossS3Tick");
            }
        }

        void SnowBossSnowDamage(FormationDefender guard, FormationFighter victim, float multiplier, string motion)
        {
            var context = CaptureActionContext(State(guard));
            context.Presentation = new FormationPresentationAction { ActorId = guard.Id, TargetId = victim.Id,
                At = Clock, ContactAt = Clock, From = guard.Position, To = victim.Position, Skill = true, Animation = motion };
            RunInPresentationContext(guard, context, () => CombatDamage(guard, victim, guard.Attack * multiplier, 1, true, false));
        }
    }
}
