using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        internal const double LapplandSkillSeconds = 40;
        internal const double SilverAshSkillSeconds = 48;
        internal const string LapplandWolfAttack = "lappland_wolf_attack";
        internal const string LapplandHuntAttack = "lappland_hunt_attack";
        const double LapplandAttackInterval = 1.3;
        const float LapplandScatterSeconds = 1.3f;
        const float LapplandInitialMultiplier = .2f;
        const float LapplandMultiplierStep = .15f;
        const float LapplandMultiplierCap = 1.1f;
        const float SilverAshWaveSpeed = 10;
        const double SilverAshWaveSourceFlight = .3;
        const double SilverAshAttackInterval = 1.2;
        const double SilverAshAttackRelease = .5; // Skill_3_Combat OnAttack, frame 30 at 60 fps.
        const float SilverAshWaveRadius = .8f;

        internal sealed class GuardHuntWolf
        {
            internal Vector2 Position, Direction, ScatterDirection;
            internal FormationFighter Target;
            internal double PursuitAge, NextAttackAt;
            internal float DamageScale = LapplandInitialMultiplier;
            internal bool Locked;
        }

        internal IReadOnlyList<GuardHuntWolf> GuardHuntWolves(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && GuardSkillActive(guard)
                ? kit.HuntWolves ?? Array.Empty<GuardHuntWolf>() : Array.Empty<GuardHuntWolf>();

        bool RecurringGuardCastReady(GuardKit kit, double firstDelay, double cooldown) =>
            Clock >= (kit.Casts == 0 ? kit.FirstContact + firstDelay : kit.ActiveUntil + cooldown);

        float GuardAttackFrequency(FormationDefender guard) =>
            Mathf.Max(.1f, 1 - (Buff(guard, "cold") > 0 ? .2f : 0) - Buff(guard, "nerve_slow"));

        bool TryEndRecurringGuardSkill(FormationDefender guard, GuardKit kit)
        {
            if (kit.Casts == 0 || kit.SignatureEnded || Clock < Math.Max(kit.ActiveUntil, guard.ActionUntil)) return false;
            kit.SignatureEnded = true;
            kit.HuntWolves = null;
            kit.OrdinaryWolfTarget = -1;
            BeginGuardAction(guard, true, guard.ArtId == "D21" ? 1d / 12 : .5, motion: "skill_end");
            return true;
        }

        void QueueLapplandOrdinaryWolf(FormationDefender guard, GuardKit kit, FormationFighter target,
            FormationPresentationAction main)
        {
            var wolf = new FormationPresentationAction
            {
                Sequence = ++presentationSequence, ParentSequence = main.Sequence, ActorId = guard.Id, TargetId = target.Id,
                From = main.From, To = main.To, At = main.At, LaunchedAt = main.LaunchedAt,
                ContactAt = main.ContactAt, EndAt = main.ContactAt, Rate = main.Rate,
                Animation = LapplandWolfAttack, ProjectileOnly = true
            };
            presentationActions.Add(wolf);
            GuardContact(guard, wolf, () =>
            {
                if (kit.OrdinaryWolfTarget != target.Id)
                {
                    kit.OrdinaryWolfTarget = target.Id;
                    kit.OrdinaryWolfDamageScale = LapplandInitialMultiplier;
                }
                CombatDamage(guard, target, guard.Attack * kit.OrdinaryWolfDamageScale, 1, false, false);
                kit.OrdinaryWolfDamageScale = Mathf.Min(LapplandMultiplierCap, kit.OrdinaryWolfDamageScale + LapplandMultiplierStep);
            }, trackedTarget: target);
        }

        void StartGuardHunt(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            var cast = StartGuardCast(guard, kit, target, .5);
            kit.ActiveUntil = Clock + LapplandSkillSeconds;
            kit.SignatureEnded = false;
            kit.HuntAdvancedAt = Clock;
            kit.HuntNextAuraAt = Clock + LapplandScatterSeconds + 1;
            kit.Effect = new FormationGuardEffect
            {
                Kind = "hunt", Origin = target.Position, Radius = 1.5f,
                StartedAt = Clock, EndsAt = kit.ActiveUntil
            };
            kit.HuntWolves = new GuardHuntWolf[3];
            float facing = Mathf.Atan2(guard.Facing.y, guard.Facing.x);
            for (int index = 0; index < kit.HuntWolves.Length; index++)
            {
                float angle = facing + index * Mathf.PI * 2 / kit.HuntWolves.Length;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                kit.HuntWolves[index] = new GuardHuntWolf { Position = cast.From, Direction = direction, ScatterDirection = direction };
            }
        }

        void TickGuardHunt(FormationDefender guard, GuardKit kit)
        {
            float dt = (float)Math.Max(0, Clock - kit.HuntAdvancedAt);
            kit.HuntAdvancedAt = Clock;
            float age = (float)(Clock - kit.LastCast);
            var wolves = kit.HuntWolves;
            if (wolves == null) return;
            if (age <= LapplandScatterSeconds)
            {
                // Source dispersion starts at .1 cells/s, accelerates by 1.9 to a 2 cells/s cap.
                float distance = age <= 1 ? .1f * age + .95f * age * age : 1.05f + 2 * (age - 1);
                foreach (var wolf in wolves)
                    wolf.Position = guard.Position + wolf.ScatterDirection * distance;
                return;
            }
            for (int index = 0; index < wolves.Length; index++)
                AdvanceHuntWolf(guard, wolves[index], index, dt);
            if (Clock + 1e-7 < kit.HuntNextAuraAt) return;
            kit.HuntNextAuraAt += 1;
            // Preserve this board's existing aura size, damage and cap. Overlap never stacks it.
            var nearby = GuardTargets().Select(u => (Unit: u, Wolf: wolves.Where(wolf => wolf.Locked)
                    .OrderBy(wolf => (wolf.Position - u.Position).sqrMagnitude).FirstOrDefault()))
                .Where(pair => pair.Wolf != null && GuardInRange(pair.Wolf.Position, pair.Unit, kit.Effect.Radius))
                .OrderBy(pair => (pair.Wolf.Position - pair.Unit.Position).sqrMagnitude).ThenBy(pair => pair.Unit.Id).Take(2).ToArray();
            foreach (var pair in nearby)
                EmitLapplandHuntHit(guard, pair.Wolf, pair.Unit, .75f, Array.IndexOf(wolves, pair.Wolf));
        }

        void AdvanceHuntWolf(FormationDefender guard, GuardHuntWolf wolf, int index, float dt)
        {
            if (wolf.Target == null || !wolf.Target.Alive || !wolf.Target.Enabled || !HasEnteredBattleArea(wolf.Target))
            {
                wolf.Target = GuardTargets().OrderBy(u => (u.Position - wolf.Position).sqrMagnitude)
                    .ThenBy(u => (u.Position - guard.Position).sqrMagnitude).ThenBy(u => u.Id).FirstOrDefault();
                wolf.Locked = false;
                wolf.PursuitAge = 0;
                wolf.DamageScale = LapplandInitialMultiplier;
            }
            var target = wolf.Target;
            if (target == null) return;
            var offset = target.Position - wolf.Position;
            if (offset.sqrMagnitude > .0001f) wolf.Direction = offset.normalized;
            if (!wolf.Locked)
            {
                float speed = Mathf.Min(4, 2 + (float)wolf.PursuitAge);
                wolf.PursuitAge += dt;
                wolf.Position = Vector2.MoveTowards(wolf.Position, target.Position, speed * dt);
                if ((wolf.Position - target.Position).sqrMagnitude > .15f * .15f) return;
                wolf.Locked = true;
                wolf.NextAttackAt = Clock;
            }
            wolf.Position = target.Position;
            if (Clock + 1e-7 < wolf.NextAttackAt) return;
            wolf.NextAttackAt = Clock + LapplandAttackInterval / GuardAttackFrequency(guard);
            EmitLapplandHuntHit(guard, wolf, target, wolf.DamageScale, index);
            wolf.DamageScale = Mathf.Min(LapplandMultiplierCap, wolf.DamageScale + LapplandMultiplierStep);
        }

        void EmitLapplandHuntHit(FormationDefender guard, GuardHuntWolf wolf, FormationFighter target, float multiplier, int index)
        {
            var attack = new FormationPresentationAction
            {
                Sequence = ++presentationSequence, ActorId = guard.Id, TargetId = target.Id, From = wolf.Position, To = target.Position,
                At = Clock, LaunchedAt = Clock, ContactAt = Clock, EndAt = Clock,
                Rate = 2, Animation = LapplandHuntAttack, Skill = true, ProjectileOnly = true, ProjectileIndex = index
            };
            presentationActions.Add(attack);
            GuardContact(guard, attack, () => CombatDamage(guard, target, guard.Attack * multiplier, 1, true, false), trackedTarget: target);
        }

        void StartGuardSilverAshSkill(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            StartGuardCast(guard, kit, target, 2d / 30);
            kit.ActiveUntil = Clock + SilverAshSkillSeconds;
            kit.SignatureEnded = false;
            Vector2 facing = guard.Facing;
            facing = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y)
                ? new Vector2(facing.x < 0 ? -1 : 1, 0) : new Vector2(0, facing.y < 0 ? -1 : 1);
            kit.Effect = new FormationGuardEffect
            {
                Kind = "silverash", Origin = guard.Position, Direction = facing, StartedAt = Clock, EndsAt = kit.ActiveUntil
            };
            guard.Facing = facing;
            guard.NextAttackAt = guard.ActionUntil;
        }

        static bool SilverAshFieldContains(FormationGuardEffect field, Vector2 position)
        {
            // Source range 3-1: columns 0–3 on the center row, 0–2 on its adjacent rows.
            Vector2 offset = position - field.Origin;
            float col = Vector2.Dot(offset, field.Direction);
            float row = Mathf.Abs(Vector2.Dot(offset, new Vector2(-field.Direction.y, field.Direction.x)));
            return col >= -.5f && row <= 1.5f && col <= (row <= .5f ? 3.5f : 2.5f);
        }

        void FireGuardSilverAshWave(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            float haste = Buff(guard, "haste");
            float rate = (animationProfiles.TryGetValue(guard.ArtId, out var profile) ? profile.BaseRate : 2) / (haste > 0 ? haste : 1);
            if (Clock + SilverAshAttackRelease / rate > kit.ActiveUntil) return;
            var action = BeginGuardAction(guard, true, 1.2 / rate, target, motion: "skill_attack");
            action.Rate = guard.PresentationRate = rate;
            action.LaunchedAt = action.ContactAt = action.At + SilverAshAttackRelease / action.Rate;
            guard.NextAttackAt = Clock + SilverAshAttackInterval / GuardAttackFrequency(guard);
            Vector2 aim = (action.To - action.From).normalized;
            if (aim.sqrMagnitude < 1e-8f) aim = guard.Facing;
            var hit = new HashSet<int>();
            var field = kit.Effect;
            var context = CaptureActionContext(State(guard));
            context.Presentation = action;
            double end = action.LaunchedAt + SilverAshWaveSourceFlight;
            double previous = action.LaunchedAt;
            // The native projectile and effect are unscaled. Sweep the source 10-cells/s path each simulation tick.
            const int samples = 12;
            for (int sample = 0; sample <= samples; sample++)
            {
                double at = action.LaunchedAt + (end - action.LaunchedAt) * sample / samples;
                Vector2 from = action.From + aim * (float)((previous - action.LaunchedAt) * SilverAshWaveSpeed);
                Vector2 to = action.From + aim * (float)((at - action.LaunchedAt) * SilverAshWaveSpeed);
                Schedule(at, () =>
                {
                    if (hit.Count >= 3 || action.Cancelled) return;
                    if (!guard.Alive) { if (Clock <= action.LaunchedAt) action.Cancelled = true; return; }
                    var contacts = GuardTargets().Where(u => !hit.Contains(u.Id) && SilverAshFieldContains(field, u.Position) &&
                        Vector2.Dot(u.Position - action.From, aim) >= 0 && GuardWaveDistanceSquared(u.Position, from, to) <= SilverAshWaveRadius * SilverAshWaveRadius)
                        .OrderBy(u => (u.Position - from).sqrMagnitude).ThenBy(u => u.Id).Take(3 - hit.Count).ToArray();
                    foreach (var victim in contacts)
                    {
                        hit.Add(victim.Id);
                        action.ContactAt = Clock;
                        RunInPresentationContext(guard, context, () => CombatDamage(guard, victim, guard.Attack * 2.4f, 0, true, false));
                    }
                });
                previous = at;
            }
        }

        static float GuardWaveDistanceSquared(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 offset = to - from;
            float fraction = offset.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(point - from, offset) / offset.sqrMagnitude) : 0;
            return (point - (from + fraction * offset)).sqrMagnitude;
        }
    }
}
