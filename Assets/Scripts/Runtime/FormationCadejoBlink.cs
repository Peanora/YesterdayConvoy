using System.Collections.Generic;
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
        const float CadejoHopLength = 1.15f;
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
            internal bool Avoid;
            internal Vector2 RequestedGoal;
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

        void AdvanceCadejoBlink(FormationFighter unit, Vector2 requested, Vector2 waypoint, float dt, bool avoid)
        {
            var state = State(unit);
            if (state.RidingCarrier != 0 || Buff(unit, "stun") > 0 || Buff(unit, "freeze") > 0 || unit.MoveSpeed <= 0)
                return;

            if (!cadejoMovements.TryGetValue(unit.Id, out var movement) || movement.LifeVersion != state.LifeVersion)
            {
                if (movement != null) CancelCadejoBlink(movement);
                movement = new CadejoMovement { Unit = unit, LifeVersion = state.LifeVersion };
                cadejoMovements[unit.Id] = movement;
            }
            if (movement.LastMoveAt != Clock)
                movement.DistanceBudget = Mathf.Min(CadejoHopLength, movement.DistanceBudget + unit.MoveSpeed * dt);
            movement.LastMoveAt = Clock;
            if (movement.LastArrivalAt == Clock) return;

            if (movement.Pending != null && (movement.Avoid != avoid || movement.RequestedGoal != requested ||
                !CanLandCadejo(unit, movement.Pending.To)))
                CancelCadejoBlink(movement);

            if (movement.Pending == null)
            {
                var direction = waypoint - unit.Position;
                float distance = direction.magnitude;
                if (distance <= .00001f) return;
                if (avoid && HasEnteredBattleArea(unit) && waypoint == requested)
                    distance = Mathf.Max(.00001f, distance - unit.Range + .025f);
                distance = Mathf.Min(CadejoHopLength, distance);
                if (movement.DistanceBudget + unit.MoveSpeed * CadejoBlinkWindup + .00001 < distance) return;

                if (!TryCadejoLanding(unit, waypoint, direction.normalized, distance, out var landing)) return;
                var blink = new FormationCadejoBlink
                {
                    ActorId = unit.Id,
                    Sequence = ++cadejoBlinkSequence,
                    From = unit.Position,
                    To = landing,
                    Facing = (landing - unit.Position).normalized,
                    StartedAt = Clock
                };
                movement.Avoid = avoid;
                movement.RequestedGoal = requested;
                movement.Pending = blink;
                cadejoBlinks.Add(blink);
                unit.Facing = blink.Facing;
                return;
            }

            var pending = movement.Pending;
            float hopLength = Vector2.Distance(pending.From, pending.To);
            if (Clock + 1e-8 < pending.StartedAt + CadejoBlinkWindup || movement.DistanceBudget + .00001f < hopLength) return;

            pending.ArrivedAt = Clock;
            unit.Position = pending.To;
            unit.Facing = pending.Facing;
            movement.DistanceBudget = Mathf.Max(0, movement.DistanceBudget - hopLength);
            movement.LastArrivalAt = Clock;
            movement.Pending = null;
        }

        bool TryCadejoLanding(FormationFighter unit, Vector2 waypoint, Vector2 direction, float distance, out Vector2 landing)
        {
            Vector2 origin = unit.Position;
            landing = distance + .00001f >= (waypoint - origin).magnitude ? waypoint : origin + direction * distance;
            if (CanLandCadejo(unit, landing)) return true;

            // If another body occupies the planned point, prefer a forward side step.
            // The same wall validation applies to both sides and to the direct segment.
            float side = (unit.Id & 1) == 0 ? 1 : -1;
            Vector2 lateral = new Vector2(-direction.y, direction.x);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                landing = origin + (direction * .8f + lateral * (side * .6f)) * distance;
                if ((landing - waypoint).sqrMagnitude < (origin - waypoint).sqrMagnitude && CanLandCadejo(unit, landing)) return true;
                side = -side;
            }
            return false;
        }

        bool CanLandCadejo(FormationFighter unit, Vector2 landing)
        {
            if (landing.x < -1e-5f || landing.x > Width - 1 + 1e-5f || landing.y < -1e-5f || landing.y > Height - 1 + 1e-5f)
                return false;
            if (Terrain != null && !Terrain.CanTravel(unit.Position, landing)) return false;
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
