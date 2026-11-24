using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        internal static bool IsArchivedSignatureGuard(string id) => id is "D33" or "D34" or "D35";

        static void ConfigureArchivedGuard(FormationDefender guard)
        {
            switch (guard.ArtId)
            {
                case "D31":
                    guard.MaxHealth = 120000;
                    guard.Attack = 140;
                    guard.Armor = 10;
                    guard.Range = 7;
                    guard.Interval = 5;
                    break;
                case "D33":
                    guard.Range = 3.5f;
                    guard.Interval = 1.5f;
                    break;
                case "D34":
                    guard.Range = 2.5f;
                    guard.Interval = 1.2f;
                    break;
                case "D35":
                    guard.Range = 3.5f;
                    guard.Interval = 3;
                    break;
            }
        }

        static Vector2 ArchivedGuardCardinal(Vector2 facing) =>
            Mathf.Abs(facing.x) >= Mathf.Abs(facing.y)
                ? new Vector2(facing.x < 0 ? -1 : 1, 0)
                : new Vector2(0, facing.y < 0 ? -1 : 1);

        internal static bool ArchivedGuardContains(string id, Vector2 origin, Vector2 facing, Vector2 position)
        {
            Vector2 delta = position - origin;
            float forward = Vector2.Dot(delta, facing);
            float side = Mathf.Abs(Vector2.Dot(delta, new Vector2(-facing.y, facing.x)));
            if (id == "D34") return side <= .50001f && forward >= -.50001f && forward <= 3.50001f;
            if (id != "D33") return delta.sqrMagnitude <= 3.5f * 3.5f;
            if (side > 2.50001f || forward < -.50001f) return false;
            float far = side <= .50001f ? 4.50001f : side <= 1.50001f ? 3.50001f : 2.50001f;
            return forward <= far;
        }

        internal Vector2 ArchivedGuardFacing(FormationDefender guard) => Kit(guard).ArchivedFacing;

        void TickArchivedGuard(FormationDefender guard, GuardKit kit)
        {
            if (Clock + 1e-8 < guard.ActionUntil) return;
            bool active = GuardSkillActive(guard);
            bool medical = guard.ArtId == "D35";
            var candidates = GuardTargets()
                .Where(unit => active && !medical
                    ? ArchivedGuardContains(guard.ArtId, guard.Position, kit.ArchivedFacing, unit.Position)
                    : GuardInRange(guard.Position, unit, guard.Range))
                .OrderBy(unit => (unit.Position - guard.Position).sqrMagnitude).ThenBy(unit => unit.Id).ToArray();
            var target = candidates.FirstOrDefault();
            var patient = medical ? WoundedGuards(guard.Position, guard.Range, 1, guard).FirstOrDefault() : null;
            if (active && kit.ArchivedAmmo <= 0)
            {
                kit.ActiveUntil = Clock;
                guard.SkillCharge = 0;
                guard.NextAttackAt = Clock;
                BeginArchivedGuardMotion(guard, true, false, "skill_end", ArchivedGuardTransition(guard.ArtId), 0);
                return;
            }
            if (!active && guard.ArtId != "D31" && guard.SkillCharge + 1e-5 >= guard.SkillRequired &&
                (target != null || patient != null))
            {
                Face(guard, target?.Position ?? patient.Position);
                kit.ArchivedFacing = ArchivedGuardCardinal(guard.Facing);
                kit.LastCast = Clock;
                kit.Casts++;
                kit.ActiveUntil = double.PositiveInfinity;
                kit.ArchivedAmmo = guard.ArtId == "D33" ? 31 : 10;
                guard.SkillCharge = 0;
                BeginArchivedGuardMotion(guard, true, false, "skill_begin", ArchivedGuardTransition(guard.ArtId), 0);
                return;
            }
            if (Clock + 1e-8 < guard.NextAttackAt) return;
            if (medical)
            {
                if (active && target != null) LaunchArchivedMedicine(guard, kit, target, true);
                else if (patient != null) LaunchArchivedMedicine(guard, kit, patient, active);
                return;
            }
            if (target == null) return;
            Face(guard, target.Position);
            guard.TargetId = target.Id;
            if (guard.ArtId == "D31")
            {
                AttackAsSupernova(guard, target);
                return;
            }
            AttackAsArchivedSignature(guard, kit, target, candidates, active);
        }

        void AttackAsSupernova(FormationDefender guard, FormationFighter target)
        {
            float rate = guard.Health < guard.MaxHealth * .5f ? 2 : 1;
            var strike = BeginArchivedGuardMotion(guard, false, false, "attack", 5, 8d / 15, target, rate);
            strike.ContactAt = strike.LaunchedAt + .02 / FormationBattleController.FormationVisualRate;
            guard.NextAttackAt = Clock + guard.Interval / rate;
            GuardContact(guard, strike, () =>
            {
                foreach (var unit in GuardVictims(strike.To, .85f, int.MaxValue))
                    CombatDamage(guard, unit, guard.Attack, 0, false, false);
            }, trackedTarget: target);
        }

        void AttackAsArchivedSignature(FormationDefender guard, GuardKit kit, FormationFighter target,
            FormationFighter[] candidates, bool active)
        {
            bool angelina = guard.ArtId == "D33";
            double windup = angelina ? (active ? .5 : .4) : (active ? 13d / 30 : 11d / 30);
            var action = BeginArchivedGuardMotion(guard, active, false, active ? "skill_attack" : "attack",
                angelina ? 1.5 : 1.2, windup, target);
            guard.NextAttackAt = Clock + guard.Interval;
            if (active && angelina) kit.ArchivedAmmo--;
            var victims = active ? candidates.Take(3).ToArray() : new[] { target };
            foreach (var victim in victims)
            {
                var shot = victim == target || !angelina ? action : ArchivedExtraShot(action, victim);
                if (angelina) ApplyArchivedProjectile(shot);
                GuardContact(guard, shot, () =>
                {
                    if (!victim.Alive) return;
                    float scale = active ? (angelina ? 1.3f * 3.05f : 2f) : 1f;
                    float healthLost = CombatDamage(guard, victim, guard.Attack * scale, 0, active, false);
                    if (active && !angelina && healthLost > 0)
                        AccumulateArchivedCorrosion(guard, kit, victim, healthLost);
                }, trackedTarget: angelina ? victim : null);
            }
        }

        // The accepted seventh-batch encounters use the same authored .65 s flight.
        static void ApplyArchivedProjectile(FormationPresentationAction action)
        {
            action.ProjectileMovement = "AdvancedMovement";
            action.ContactAt = action.LaunchedAt + .65 / action.Rate;
        }

        static double ArchivedGuardTransition(string id) => id == "D33" ? 1 : id == "D34" ? .2 : 2d / 3;

        FormationPresentationAction BeginArchivedGuardMotion(FormationDefender guard, bool skill, bool heal,
            string motion, double sourceDuration, double sourceWindup, FormationActor target = null, float attackRate = 1)
        {
            float rate = FormationBattleController.FormationVisualRate * attackRate;
            double duration = sourceDuration / rate;
            var action = BeginGuardAction(guard, skill, duration, target, heal, motion);
            action.Rate = guard.PresentationRate = rate;
            guard.PresentationDuration = duration;
            action.EndAt = Clock + duration;
            action.ContactAt = action.LaunchedAt = Clock + sourceWindup / rate;
            return action;
        }

        FormationPresentationAction ArchivedExtraShot(FormationPresentationAction primary, FormationActor target)
        {
            var shot = new FormationPresentationAction
            {
                Sequence = ++presentationSequence, ParentSequence = primary.Sequence, ActorId = primary.ActorId,
                TargetId = target.Id, At = primary.At, LaunchedAt = primary.LaunchedAt,
                ContactAt = primary.ContactAt, EndAt = primary.EndAt, Rate = primary.Rate,
                From = primary.From, To = target.Position, Skill = primary.Skill, Animation = primary.Animation,
                ProjectileOnly = true
            };
            presentationActions.Add(shot);
            return shot;
        }

        void AccumulateArchivedCorrosion(FormationDefender guard, GuardKit kit, FormationFighter target, float damage)
        {
            if (kit.ArchivedAmmo <= 0 || Buff(target, "guard_corrosion_immunity") > 0) return;
            float stored = Stack(target, "guard_corrosion_meter") + damage * .2f;
            float threshold = target.ArtId.StartsWith("B", StringComparison.Ordinal) ? 200 : 100;
            if (stored < threshold)
            {
                State(target).Stacks["guard_corrosion_meter"] = stored;
                return;
            }
            State(target).Stacks["guard_corrosion_meter"] = 0;
            State(target).Stacks["guard_corrosion_defense"] = Stack(target, "guard_corrosion_defense") + 12;
            GiveBuff(target, "guard_corrosion_immunity", 1, 8);
            kit.ArchivedAmmo--;
            CombatDamage(guard, target, 500, 2, true, false);
            Vector2 origin = target.Position;
            presentationActions.Add(new FormationPresentationAction
            {
                Sequence = ++presentationSequence, ActorId = guard.Id, TargetId = -1, At = Clock, ContactAt = Clock,
                EndAt = Clock + 1, Skill = true, Rate = FormationBattleController.FormationVisualRate,
                Animation = "corrosion_burst", From = guard.Position, To = origin
            });
            for (int index = 0; index < 3; index++)
            {
                bool last = index == 2;
                ScheduleOwned(guard, Clock + index * .1, () =>
                {
                    foreach (var unit in GuardTargets().Where(unit => ArchivedCorrosionCross(origin, unit.Position)))
                    {
                        CombatDamage(guard, unit, guard.Attack * 2.7f, 0, true, false);
                        if (last) CombatDamage(guard, unit, guard.Attack * 2, 2, true, false);
                    }
                });
            }
        }

        static bool ArchivedCorrosionCross(Vector2 origin, Vector2 position)
        {
            var delta = position - origin;
            float x = Mathf.Abs(delta.x), y = Mathf.Abs(delta.y);
            return x <= .5f && y <= 1.5f || y <= .5f && x <= 1.5f;
        }

        void LaunchArchivedMedicine(FormationDefender guard, GuardKit kit, FormationActor target, bool active)
        {
            bool heal = target is FormationDefender;
            Face(guard, target.Position);
            guard.TargetId = target.Id;
            var action = BeginArchivedGuardMotion(guard, active, heal, active ? "skill_attack" : "attack",
                active ? 3 : 11d / 3, active ? 8d / 15 : 14d / 15, target);
            ApplyArchivedProjectile(action);
            guard.NextAttackAt = Clock + guard.Interval;
            if (active) kit.ArchivedAmmo--;
            GuardContact(guard, action, () =>
            {
                if (!active)
                {
                    if (target is FormationDefender patient) HealGuard(guard, patient, guard.Attack, false);
                    return;
                }
                action.Animation = "medicine_detonation";
                Vector2 origin = action.To;
                ResolveArchivedMedicine(guard, origin, heal, true);
                ScheduleOwned(guard, Clock + .4 / action.Rate, () => ResolveArchivedMedicine(guard, origin, !heal, false));
            }, trackedTarget: target);
        }

        void ResolveArchivedMedicine(FormationDefender guard, Vector2 origin, bool heal, bool primary)
        {
            const float radius = 1.5f;
            float power = guard.Attack * 2.25f;
            if (heal)
            {
                foreach (var patient in WoundedGuards(origin, radius, int.MaxValue, guard))
                    HealGuard(guard, patient, power * 1.7f, true);
                return;
            }
            foreach (var target in GuardVictims(origin, radius, int.MaxValue))
            {
                CombatDamage(guard, target, power * 3.5f, 2, true, false);
                if (primary && target.Alive) GiveBuff(target, "slow", .6f, 5);
            }
        }
    }
}
