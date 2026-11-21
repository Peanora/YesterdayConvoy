using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        const double SupernovaTransitionSeconds = 1.25;
        const double SupernovaOverloadAfter = 8;
        const int SupernovaShellsPerSalvo = 6;
        const float SupernovaProjectileSpeed = 10;
        const float SupernovaBlastRadius = 2.2f;

        public bool SupernovaBossTransitionActive(FormationDefender guard) =>
            Phase == FormationPhase.Fighting && guard.IsSupernovaBoss && guard.SupernovaBossTransitionUntil > Clock;

        internal (string Animation, double StartedAt) SupernovaBossMode(FormationDefender guard)
        {
            if (!guard.IsSupernovaBoss || !guard.Alive || SupernovaBossTransitionActive(guard) ||
                !guardKits.TryGetValue(guard.Id, out var kit) || kit.Guard != guard || kit.Casts == 0)
                return (null, -1);

            string animation = guard.SupernovaBossPhase == 2 ? "supernova_s3_begin" :
                kit.SupernovaOverload ? "supernova_s2_overload_begin" : "supernova_s2_begin";
            return (animation, kit.LastCast);
        }

        void ResetSupernovaBoss(FormationDefender guard, GuardKit kit)
        {
            guard.SupernovaBossPhase = 1;
            guard.SupernovaBossTransitionUntil = 0;
            guard.PhaseStage = 0;
            guard.Range = State(guard).BaseRange;
            kit.SupernovaTargetRandom = new FormationRandom(unchecked((uint)Seed ^ 0xeb09a593u));
        }

        bool BeginSupernovaBossTransition(FormationDefender guard)
        {
            if (!guard.IsSupernovaBoss || guard.SupernovaBossPhase != 1 || SupernovaBossTransitionActive(guard)) return false;
            guard.SupernovaBossTransitionUntil = Clock + SupernovaTransitionSeconds;
            foreach (var shot in presentationActions.Where(action => action.ActorId == guard.Id && action.ContactAt > Clock))
                shot.Cancelled = true;

            if (Kit(guard).SupernovaOverload)
                BeginArchivedGuardMotion(guard, true, false, "supernova_transition", 15d / 30, 0);
            else
            {
                guard.PresentationAction = null;
                guard.PresentationDuration = 0;
            }
            guard.TargetId = -1;
            guard.ActionUntil = guard.NextAttackAt = guard.SupernovaBossTransitionUntil;
            var state = State(guard);
            state.Buffs.Clear();
            state.Shields.Clear();
            guard.Shield = 0;
            BattleEvents.Add($"{Clock:0.00}:维什戴尔·超新星:转阶段");
            return true;
        }

        void CompleteSupernovaBossTransition()
        {
            if (Campaign.CurrentNode.Id != FormationCampaignRules.SupernovaBossNodeId) return;
            var guard = Defenders.FirstOrDefault(defender => defender.IsSupernovaBoss);
            if (guard == null || !guard.Alive || guard.SupernovaBossTransitionUntil == 0 ||
                Clock + 1e-8 < guard.SupernovaBossTransitionUntil) return;

            guard.SupernovaBossTransitionUntil = 0;
            guard.SupernovaBossPhase = 2;
            guard.PhaseStage = 1;
            var state = State(guard);
            guard.Health = state.LowestHealth = guard.MaxHealth;
            guard.Range = state.BaseRange + .75f;
            var kit = Kit(guard);
            kit.Casts = 0;
            kit.ArchivedAmmo = 0;
            BattleEvents.Add($"{Clock:0.00}:维什戴尔·超新星:爆裂黎明");
        }

        void TickSupernovaBoss(FormationDefender guard, GuardKit kit)
        {
            if (SupernovaBossTransitionActive(guard) || Clock + 1e-8 < guard.ActionUntil ||
                Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0) return;

            var targets = GuardVictims(guard.Position, guard.Range, int.MaxValue);
            var target = targets.FirstOrDefault();
            if (target != null)
            {
                guard.TargetId = target.Id;
                Face(guard, target.Position);
            }
            if (guard.SupernovaBossPhase == 1)
            {
                if (target != null) TickSupernovaS2(guard, kit, targets);
            }
            else
                TickSupernovaS3(guard, kit, target);
        }

        void TickSupernovaS2(FormationDefender guard, GuardKit kit, FormationFighter[] targets)
        {
            if (kit.Casts == 0)
            {
                kit.Casts = 1;
                kit.LastCast = Clock;
                BeginArchivedGuardMotion(guard, true, false, "supernova_s2_begin", 10d / 30, 0);
                BattleEvents.Add($"{Clock:0.00}:维什戴尔·超新星:饱和复仇");
                return;
            }
            if (!kit.SupernovaOverload && Clock >= kit.LastCast + SupernovaOverloadAfter)
            {
                kit.SupernovaOverload = true;
                kit.LastCast = Clock;
                BeginArchivedGuardMotion(guard, true, false, "supernova_s2_overload_begin", 5d / 30, 0);
                BattleEvents.Add($"{Clock:0.00}:维什戴尔·超新星:过载");
                return;
            }
            if (Clock + 1e-8 < guard.NextAttackAt) return;

            bool overload = kit.SupernovaOverload;
            var firstTarget = overload ? targets[kit.SupernovaTargetRandom.Next(targets.Length)] : targets[0];
            Face(guard, firstTarget.Position);
            guard.TargetId = firstTarget.Id;
            float frequency = GuardAttackFrequency(guard);
            var volley = BeginArchivedGuardMotion(guard, true, false,
                overload ? "supernova_s2_overload" : "supernova_s2_volley", 42d / 30, 8d / 30, firstTarget, frequency);
            guard.NextAttackAt = Clock + guard.Interval / frequency;
            int count = overload ? 4 : Math.Min(3, targets.Length);
            for (int i = 0; i < count; i++)
            {
                var target = i == 0 ? firstTarget : overload ? targets[kit.SupernovaTargetRandom.Next(targets.Length)] : targets[i];
                var shot = i == 0 ? volley : ArchivedExtraShot(volley, target);
                shot.ProjectileIndex = i;
                // Source AttachToTarget waits 0.1s before contact. Overload's extra shots are 0.1s apart.
                if (overload) shot.LaunchedAt += i * .1 / shot.Rate;
                shot.ContactAt = shot.LaunchedAt + .1 / shot.Rate;
                GuardContact(guard, shot, () =>
                {
                    if (target.Alive) CombatDamage(guard, target, guard.Attack * (overload ? .70f : 1.30f), 0, true, false);
                }, trackedTarget: target);
            }
        }

        void TickSupernovaS3(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            if (kit.Casts == 0)
            {
                if (Clock + 1e-8 < guard.NextAttackAt) return;
                kit.Casts = 1;
                kit.LastCast = Clock;
                kit.ArchivedAmmo = SupernovaShellsPerSalvo;
                BeginArchivedGuardMotion(guard, true, false, "supernova_s3_begin", 25d / 30, 0);
                return;
            }
            if (Clock + 1e-8 < guard.NextAttackAt) return;
            if (kit.ArchivedAmmo == 0)
            {
                kit.Casts = 0;
                BeginArchivedGuardMotion(guard, true, false, "supernova_s3_end", 15d / 30, 0);
                guard.NextAttackAt = Clock + 1;
                return;
            }
            if (target == null) return;

            kit.ArchivedAmmo--;
            float frequency = GuardAttackFrequency(guard);
            var shell = BeginArchivedGuardMotion(guard, true, false, "supernova_s3_shell", 150d / 30, 16d / 30, target, frequency);
            SetGuardProjectileFlight(shell, SupernovaProjectileSpeed);
            guard.NextAttackAt = Clock + 2.8 / frequency;
            GuardContact(guard, shell, () =>
            {
                foreach (var victim in GuardVictims(shell.To, SupernovaBlastRadius, int.MaxValue))
                    CombatDamage(guard, victim, guard.Attack * 3.8f, 0, true, false);
            }, trackedTarget: target);
        }
    }
}
