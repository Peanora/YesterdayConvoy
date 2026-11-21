using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        internal const float MakotoSecondHealth = 160000;
        internal const float MakotoThirdHealth = 150000;
        internal const float MakotoRecoveryReduction = .12f;
        internal const float MakotoRecoveryThreshold = .45f;
        internal const float MakotoRecoveryHeal = 800;
        internal const int MakotoRecoveryPulses = 8;
        const double MakotoSwitchSourceSeconds = 1;
        const double MakotoAttackSourceSeconds = 1.8;
        const double MakotoAttackSourceContact = .8;
        const double MakotoFearSeconds = 1.5;
        const string MakotoFear = "makoto_fear";

        sealed class MakotoKit
        {
            internal FormationRandom FearRandom;
            internal double FormStartedAt = -1;
            internal double RecoveryAt = -1;
            internal double NextHealAt;
            internal double NextFearCheckAt;
            internal int HealPulses, HarpHeals, AttackCount;
        }

        public bool MakotoBossTransitionActive(FormationDefender guard) =>
            Phase == FormationPhase.Fighting && guard.IsMakotoBoss && guard.MakotoBossTransitionUntil > Clock;

        internal bool MakotoBossRecovering(FormationDefender guard) =>
            guard.IsMakotoBoss && guard.Alive && guard.MakotoBossPhase == 3 && Kit(guard).Makoto.RecoveryAt >= 0;

        internal (string Animation, double StartedAt) MakotoBossMode(FormationDefender guard)
        {
            if (!guard.IsMakotoBoss || !guard.Alive || !guardKits.TryGetValue(guard.Id, out var kit) || kit.Makoto == null)
                return (null, -1);
            var makoto = kit.Makoto;
            if (makoto.FormStartedAt < 0) return (null, -1);
            return (guard.MakotoBossPhase switch
            {
                1 => "makoto_s1_idle",
                2 => "makoto_s2_idle",
                _ => makoto.RecoveryAt >= 0 ? "makoto_s3_recovery" : "makoto_s3_idle"
            }, makoto.FormStartedAt);
        }

        void ResetMakotoBoss(FormationDefender guard, GuardKit kit)
        {
            guard.MakotoBossPhase = 1;
            guard.MakotoBossTransitionUntil = 0;
            guard.BossHealthRefillAt = double.NegativeInfinity;
            guard.PhaseStage = 0;
            guard.MaxHealth = guard.Health = State(guard).LowestHealth = FormationCampaignRules.MakotoBoss.Health;
            kit.Makoto = new MakotoKit { FearRandom = new FormationRandom(unchecked((uint)Seed ^ 0x6c8e9cf5u)) };
        }

        bool BeginMakotoBossTransition(FormationDefender guard)
        {
            if (!guard.IsMakotoBoss || guard.MakotoBossPhase >= 3 || guard.MakotoBossTransitionUntil > 0) return false;

            CancelMakotoContacts(guard);
            BeginArchivedGuardMotion(guard, true, false, $"makoto_s{guard.MakotoBossPhase}_end", MakotoSwitchSourceSeconds, 0);
            guard.MakotoBossTransitionUntil = guard.ActionUntil;
            guard.NextAttackAt = guard.ActionUntil;
            guard.TargetId = -1;
            var state = State(guard);
            state.Buffs.Clear();
            state.Shields.Clear();
            guard.Shield = 0;
            BattleEvents.Add($"{Clock:0.00}:结城理:第{guard.MakotoBossPhase}阶段结束");
            return true;
        }

        void CompleteMakotoBossTransition()
        {
            if (!Campaign.IsMakotoBossTrial) return;
            var guard = Defenders.Find(defender => defender.IsMakotoBoss);
            if (guard == null || !guard.Alive || guard.MakotoBossTransitionUntil == 0 ||
                Clock + 1e-8 < guard.MakotoBossTransitionUntil) return;

            guard.MakotoBossTransitionUntil = 0;
            guard.MakotoBossPhase++;
            guard.PhaseStage = guard.MakotoBossPhase - 1;
            guard.MaxHealth = guard.MakotoBossPhase == 2 ? MakotoSecondHealth : MakotoThirdHealth;
            guard.Health = State(guard).LowestHealth = guard.MaxHealth;
            guard.BossHealthRefillAt = Clock;
            StartMakotoForm(guard, Kit(guard).Makoto);
        }

        float MakotoFutureHealth(FormationDefender guard) => guard.MakotoBossPhase switch
        {
            1 => MakotoSecondHealth + MakotoThirdHealth,
            2 => MakotoThirdHealth,
            _ => 0,
        };

        void StartMakotoForm(FormationDefender guard, MakotoKit kit)
        {
            BeginArchivedGuardMotion(guard, true, false, $"makoto_s{guard.MakotoBossPhase}_begin", MakotoSwitchSourceSeconds, 0);
            kit.FormStartedAt = guard.ActionUntil;
            guard.NextAttackAt = guard.ActionUntil;
            string name = guard.MakotoBossPhase switch
            {
                1 => "俄耳甫斯的竖琴",
                2 => "塔纳托斯的囚锁",
                _ => "开辟明日的剑刃",
            };
            BattleEvents.Add($"{Clock:0.00}:结城理:{name}");
        }

        void CancelMakotoContacts(FormationDefender guard)
        {
            foreach (var action in presentationActions)
                if (action.ActorId == guard.Id && action.ContactAt > Clock) action.Cancelled = true;
        }

        void TickMakotoBoss(FormationDefender guard, MakotoKit kit)
        {
            if (MakotoBossTransitionActive(guard)) return;
            if (kit.FormStartedAt < 0)
            {
                StartMakotoForm(guard, kit);
                return;
            }

            if (guard.MakotoBossPhase == 3 && kit.RecoveryAt < 0 && guard.Health <= guard.MaxHealth * MakotoRecoveryThreshold)
                BeginMakotoRecovery(guard, kit);
            if (kit.RecoveryAt >= 0)
            {
                TickMakotoRecovery(guard, kit);
                return;
            }

            if (guard.MakotoBossPhase == 2) TickMakotoFearExecute(guard, kit);

            if (Clock + 1e-8 < guard.ActionUntil || Clock + 1e-8 < guard.NextAttackAt ||
                Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0) return;
            var targets = GuardVictims(guard.Position, guard.Range, guard.MakotoBossPhase == 1 ? 1 : 4);
            if (targets.Length == 0) return;
            var target = targets[0];
            guard.TargetId = target.Id;
            Face(guard, target.Position);
            float frequency = GuardAttackFrequency(guard);
            switch (guard.MakotoBossPhase)
            {
                case 1:
                    TickMakotoHarp(guard, kit, target, frequency);
                    break;
                case 2:
                    TickMakotoChains(guard, kit, targets, frequency);
                    break;
                case 3:
                    TickMakotoSword(guard, kit, targets, frequency * 1.6f);
                    break;
            }
        }

        void TickMakotoHarp(FormationDefender guard, MakotoKit kit, FormationFighter target, float frequency)
        {
            bool heal = kit.HarpHeals < 6 && guard.Health <= guard.MaxHealth * .5f;
            var action = BeginArchivedGuardMotion(guard, true, heal, "makoto_s1_attack",
                MakotoAttackSourceSeconds, MakotoAttackSourceContact, heal ? guard : (FormationActor)target, frequency);
            if (!heal) SetGuardProjectileFlight(action, 10);
            guard.NextAttackAt = Clock + guard.Interval / frequency;
            GuardContact(guard, action, () =>
            {
                if (heal)
                {
                    if (HealGuard(guard, guard, 600, true) > 0) kit.HarpHeals++;
                }
                else if (target.Alive)
                    CombatDamage(guard, target, guard.Attack * 2.8f, 1, true, false);
            }, trackedTarget: heal ? guard : target);
        }

        void TickMakotoChains(FormationDefender guard, MakotoKit kit, FormationFighter[] targets, float frequency)
        {
            // The original continuous S2 body loop has no OnAttack event.
            var action = BeginArchivedGuardMotion(guard, true, false, "makoto_s2_attack", 2, .8, targets[0], frequency);
            guard.NextAttackAt = Clock + 1.75 / frequency;
            GuardContact(guard, action, () =>
            {
                foreach (var target in targets)
                {
                    if (!target.Alive) continue;
                    CombatDamage(guard, target, guard.Attack * 2.5f, 1, true, false);
                    if (!target.Alive) continue;
                    if (kit.FearRandom.Next(100) < 35)
                        GiveBuff(target, MakotoFear, guard.Id, MakotoFearSeconds);
                }
            }, trackedTarget: targets[0]);
        }

        void TickMakotoFearExecute(FormationDefender guard, MakotoKit kit)
        {
            if (Clock + 1e-8 < kit.NextFearCheckAt) return;
            kit.NextFearCheckAt = Clock + .1;
            foreach (var target in GuardTargets())
            {
                if (Buff(target, MakotoFear) == 0 || target.Health > guard.Attack * 2.8f ||
                    !GuardInRange(guard.Position, target, guard.Range)) continue;

                var context = CaptureActionContext(State(guard));
                context.Presentation = new FormationPresentationAction
                {
                    ActorId = guard.Id, TargetId = target.Id, From = guard.Position, To = target.Position,
                    At = Clock, LaunchedAt = Clock, ContactAt = Clock, Skill = true, Animation = "makoto_s2_execute"
                };
                RunInPresentationContext(guard, context, () =>
                    CombatDamage(guard, target, (target.Health + target.Shield) * 10, 2, true, false));
            }
        }

        void TickMakotoSword(FormationDefender guard, MakotoKit kit, FormationFighter[] targets, float frequency)
        {
            string motion = kit.AttackCount++ % 2 == 0 ? "makoto_s3_attack_a" : "makoto_s3_attack_b";
            var action = BeginArchivedGuardMotion(guard, true, false, motion, MakotoAttackSourceSeconds,
                MakotoAttackSourceContact, targets[0], frequency);
            guard.NextAttackAt = Clock + guard.Interval / frequency;
            GuardContact(guard, action, () =>
            {
                foreach (var target in targets)
                    if (target.Alive) CombatDamage(guard, target, guard.Attack * 2.3f, MakotoWeakPointDamage, true, false);
            }, trackedTarget: targets[0]);
        }

        void BeginMakotoRecovery(FormationDefender guard, MakotoKit kit)
        {
            CancelMakotoContacts(guard);
            kit.RecoveryAt = Clock;
            var change = BeginArchivedGuardMotion(guard, true, false, "makoto_s3_change_begin", .5, 0);
            Schedule(change.EndAt, () =>
            {
                if (!guard.Alive) return;
                BeginArchivedGuardMotion(guard, true, false, "makoto_s3_change_end", 1, .8);
                kit.FormStartedAt = guard.ActionUntil;
                kit.NextHealAt = kit.FormStartedAt;
            });
            kit.NextHealAt = double.PositiveInfinity;
            guard.TargetId = -1;
            BattleEvents.Add($"{Clock:0.00}:结城理:俄耳甫斯守备");
        }

        void TickMakotoRecovery(FormationDefender guard, MakotoKit kit)
        {
            if (kit.HealPulses >= MakotoRecoveryPulses || Clock + 1e-8 < kit.NextHealAt) return;
            kit.HealPulses++;
            kit.NextHealAt += 1;
            var action = new FormationPresentationAction
            {
                Sequence = ++presentationSequence,
                ActorId = guard.Id, TargetId = guard.Id, From = guard.Position, To = guard.Position,
                At = Clock, LaunchedAt = Clock, ContactAt = Clock + .5, EndAt = Clock + .5,
                Rate = FormationBattleController.FormationVisualRate,
                Skill = true, Heal = true, Animation = "makoto_s3_heal"
            };
            presentationActions.Add(action);
            GuardContact(guard, action, () => HealGuard(guard, guard, MakotoRecoveryHeal, true), trackedTarget: guard);
        }

        bool MoveMakotoFear(FormationFighter unit, float dt)
        {
            int sourceId = (int)Buff(unit, MakotoFear);
            if (sourceId == 0) return false;
            var source = Defenders.Find(guard => guard.Id == sourceId && guard.Alive && guard.MakotoBossPhase == 2 &&
                !MakotoBossTransitionActive(guard));
            if (source == null) return false;

            unit.TargetId = -1;
            if (State(unit).RidingCarrier != 0) return true;
            Vector2 away = unit.Position - source.Position;
            if (away.sqrMagnitude < 1e-6f) away = Vector2.left;
            Vector2 next = unit.Position + away.normalized * (unit.MoveSpeed * dt);
            if (next.x < BattleAreaStart || next.x >= Width - 1 || next.y < 0 || next.y > Height - 1 ||
                Terrain != null && !Terrain.CanTravel(unit.Position, next)) return true;
            unit.Position = next;
            Face(unit, next + away);
            unit.IsMoving = true;
            return true;
        }
    }
}
