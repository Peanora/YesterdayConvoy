using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class FormationCadejoBlink
    {
        public int ActorId { get; internal set; }
        public int Sequence { get; internal set; }
        public Vector2 From { get; internal set; }
        public Vector2 To { get; internal set; }
        public Vector2 Facing { get; internal set; }
        public double StartedAt { get; internal set; }
        public double ArrivedAt { get; internal set; } = double.PositiveInfinity;
        public bool Cancelled { get; internal set; }
    }

    public sealed partial class FormationBattleModel
    {
        const float CadejoExitHopLength = 1.15f;
        // The existing .2 source-second departure plays at the project's base 2x rate.
        const double CadejoBlinkWindup = .1;
        const float CadejoLandingClearance = .35f;

        sealed class CadejoMovement
        {
            internal FormationFighter Unit;
            internal int LifeVersion;
            internal float DistanceBudget;
            internal double LastMoveAt = double.NegativeInfinity;
            internal double LastArrivalAt = double.NegativeInfinity;
            internal FormationDefender Target;
            internal FormationCadejoBlink Pending;
        }

        readonly Dictionary<int, CadejoMovement> cadejoMovements = new Dictionary<int, CadejoMovement>();
        readonly List<FormationCadejoBlink> cadejoBlinks = new List<FormationCadejoBlink>();
        int cadejoBlinkSequence;
        public IReadOnlyList<FormationCadejoBlink> CadejoBlinks => cadejoBlinks;

        void ResetCadejoBlinks()
        {
            cadejoMovements.Clear();
            cadejoBlinks.Clear();
        }

        static void CancelCadejoBlink(CadejoMovement movement)
        {
            if (movement.Pending == null) return;
            movement.Pending.Cancelled = true;
            movement.Pending = null;
            movement.Target = null;
        }

        void CancelPendingCadejoBlinks()
        {
            foreach (var movement in cadejoMovements.Values) CancelCadejoBlink(movement);
        }

        void FinishCadejoBlinkTick()
        {
            foreach (var movement in cadejoMovements.Values)
            {
                // Interruptions and reaching attack range stop movement credit. No
                // scheduled callback can move the body during a normal movement lock.
                if (movement.LastMoveAt == Clock && movement.Unit.Alive &&
                    movement.LifeVersion == State(movement.Unit).LifeVersion) continue;
                CancelCadejoBlink(movement);
                movement.DistanceBudget = 0;
            }
            cadejoBlinks.RemoveAll(blink => (blink.Cancelled ? blink.StartedAt : blink.ArrivedAt) < Clock - 1);
        }

        CadejoMovement GetCadejoMovement(FormationFighter unit)
        {
            var state = State(unit);
            if (state.RidingCarrier != 0 || Buff(unit, "stun") > 0 || Buff(unit, "freeze") > 0 || unit.MoveSpeed <= 0)
                return null;

            if (!cadejoMovements.TryGetValue(unit.Id, out var movement) || movement.LifeVersion != state.LifeVersion)
            {
                if (movement != null) CancelCadejoBlink(movement);
                movement = new CadejoMovement { Unit = unit, LifeVersion = state.LifeVersion };
                cadejoMovements[unit.Id] = movement;
            }
            return movement;
        }

        void StartCadejoBlink(CadejoMovement movement, Vector2 landing, FormationDefender target = null)
        {
            var unit = movement.Unit;
            var blink = new FormationCadejoBlink
            {
                ActorId = unit.Id,
                Sequence = ++cadejoBlinkSequence,
                From = unit.Position,
                To = landing,
                Facing = (landing - unit.Position).normalized,
                StartedAt = Clock
            };
            movement.Target = target;
            movement.Pending = blink;
            cadejoBlinks.Add(blink);
            unit.Facing = blink.Facing;
        }

        void LandCadejoBlink(CadejoMovement movement)
        {
            var pending = movement.Pending;
            pending.ArrivedAt = Clock;
            movement.Unit.Position = pending.To;
            movement.Unit.Facing = pending.Facing;
            movement.LastArrivalAt = Clock;
            movement.Pending = null;
            movement.Target = null;
        }

        void AdvanceCadejoEngagement(FormationFighter unit, FormationDefender preferred)
        {
            var movement = GetCadejoMovement(unit);
            if (movement == null) return;
            movement.LastMoveAt = Clock;
            movement.DistanceBudget = 0;
            if (movement.LastArrivalAt == Clock) return;

            // The destination is tied to a real guard. Recheck the guard and its
            // occupied landing before committing, then try another guard if needed.
            if (movement.Pending != null)
            {
                if (CanLandNearCadejoTarget(unit, movement.Target, movement.Pending.To))
                {
                    unit.TargetId = movement.Target.Id;
                    if (Clock + 1e-8 >= movement.Pending.StartedAt + CadejoBlinkWindup) LandCadejoBlink(movement);
                    return;
                }
                CancelCadejoBlink(movement);
            }

            if (TryCadejoTargetLanding(unit, preferred, out var destination))
            {
                StartCadejoBlink(movement, destination, preferred);
                return;
            }
            foreach (var guard in Defenders.Where(d => d.Alive && d != preferred)
                .OrderBy(d => (d.Position - unit.Position).sqrMagnitude).ThenBy(d => d.Id))
            {
                if (!TryCadejoTargetLanding(unit, guard, out destination)) continue;
                unit.TargetId = guard.Id;
                StartCadejoBlink(movement, destination, guard);
                return;
            }
        }

        bool TryCadejoTargetLanding(FormationFighter unit, FormationDefender target, out Vector2 landing)
        {
            landing = default;
            float radius = unit.Range - .02f;
            if (target == null || !target.Alive || radius < CadejoLandingClearance) return false;

            Vector2 fromTarget = (unit.Position - target.Position).normalized;
            if (fromTarget.sqrMagnitude < .01f) fromTarget = Vector2.left;
            // Start on the approach side near melee distance. If occupied, sample
            // both sides out to the unit's current attack range, never onto the target.
            float preferredRadius = Mathf.Min(.76f, radius);
            for (int ring = 0; ring < 2; ring++)
            {
                float distance = ring == 0 ? preferredRadius : radius;
                for (int spoke = 0; spoke < 16; spoke++)
                {
                    float angle = ((spoke + 1) / 2) * Mathf.PI / 8f * (spoke % 2 == 0 ? -1 : 1);
                    float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                    var direction = new Vector2(fromTarget.x * cos - fromTarget.y * sin, fromTarget.x * sin + fromTarget.y * cos);
                    var candidate = target.Position + direction * distance;
                    if (!CanLandNearCadejoTarget(unit, target, candidate)) continue;
                    landing = candidate;
                    return true;
                }
            }
            return false;
        }

        bool CanLandNearCadejoTarget(FormationFighter unit, FormationDefender target, Vector2 landing)
        {
            if (target == null || !target.Alive || landing.x < BattleAreaStart || landing.x >= Width - 1 ||
                (target.Position - landing).sqrMagnitude > unit.Range * unit.Range || !IsFreeCadejoLanding(unit, landing)) return false;
            // A teleport has no intervening cells, but it must end on accessible
            // ground and leave a legal route to the gate after the battle.
            return Terrain == null || Terrain.TryExit(landing, out _);
        }

        void AdvanceCadejoExit(FormationFighter unit, Vector2 waypoint, float dt)
        {
            var movement = GetCadejoMovement(unit);
            if (movement == null) return;
            if (movement.LastMoveAt != Clock)
                movement.DistanceBudget = Mathf.Min(CadejoExitHopLength, movement.DistanceBudget + unit.MoveSpeed * dt);
            movement.LastMoveAt = Clock;
            if (movement.LastArrivalAt == Clock) return;

            if (movement.Pending != null && (movement.Target != null || !CanLandCadejoExit(unit, movement.Pending.To)))
                CancelCadejoBlink(movement);

            if (movement.Pending == null)
            {
                var direction = waypoint - unit.Position;
                float distance = direction.magnitude;
                if (distance <= .00001f) return;
                distance = Mathf.Min(CadejoExitHopLength, distance);
                if (movement.DistanceBudget + unit.MoveSpeed * CadejoBlinkWindup + .00001 < distance) return;

                if (TryCadejoExitLanding(unit, waypoint, direction.normalized, distance, out var landing))
                    StartCadejoBlink(movement, landing);
                return;
            }

            var pending = movement.Pending;
            float hopLength = Vector2.Distance(pending.From, pending.To);
            if (Clock + 1e-8 < pending.StartedAt + CadejoBlinkWindup || movement.DistanceBudget + .00001f < hopLength) return;

            movement.DistanceBudget = Mathf.Max(0, movement.DistanceBudget - hopLength);
            LandCadejoBlink(movement);
        }

        bool TryCadejoExitLanding(FormationFighter unit, Vector2 waypoint, Vector2 direction, float distance, out Vector2 landing)
        {
            Vector2 origin = unit.Position;
            landing = distance + .00001f >= (waypoint - origin).magnitude ? waypoint : origin + direction * distance;
            if (CanLandCadejoExit(unit, landing)) return true;

            // If another body occupies the planned point, prefer a forward side step.
            // The same wall validation applies to both sides and to the direct segment.
            float side = (unit.Id & 1) == 0 ? 1 : -1;
            Vector2 lateral = new Vector2(-direction.y, direction.x);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                landing = origin + (direction * .8f + lateral * (side * .6f)) * distance;
                if ((landing - waypoint).sqrMagnitude < (origin - waypoint).sqrMagnitude && CanLandCadejoExit(unit, landing)) return true;
                side = -side;
            }
            return false;
        }

        bool CanLandCadejoExit(FormationFighter unit, Vector2 landing) =>
            IsFreeCadejoLanding(unit, landing) && (Terrain == null || Terrain.CanTravel(unit.Position, landing));

        bool IsFreeCadejoLanding(FormationFighter unit, Vector2 landing)
        {
            if (landing.x < -1e-5f || landing.x > Width - 1 + 1e-5f || landing.y < -1e-5f || landing.y > Height - 1 + 1e-5f)
                return false;
            if (Terrain != null && !Terrain.IsWalkable(landing)) return false;
            float clearanceSquared = CadejoLandingClearance * CadejoLandingClearance;
            int passenger = State(unit).Passenger;
            foreach (var other in Units)
                if (other != unit && other.Id != passenger && other.Enabled && other.Alive &&
                    (other.Position - landing).sqrMagnitude < clearanceSquared) return false;
            foreach (var summon in Summons)
                if (summon.Alive && (summon.Position - landing).sqrMagnitude < clearanceSquared) return false;
            foreach (var defender in Defenders)
                if (defender.Alive && (defender.Position - landing).sqrMagnitude < clearanceSquared) return false;
            return true;
        }
    }
}
