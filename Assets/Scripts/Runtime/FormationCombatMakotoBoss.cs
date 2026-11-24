using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        internal const float MakotoSecondHealth = 160000;
        internal const float MakotoThirdHealth = 150000;
        const float MakotoTrialHealthScale = 1.5f;
        const float MakotoThanatosAttackFrequency = 2.2f;
        internal const float MakotoRecoveryReduction = .12f;
        internal const float MakotoHumanRange = 1.6f;
        internal const float MakotoRecoveryHeal = 800;
        internal const int MakotoRecoveryPulses = 8;
        static float MakotoTotalHealth => FormationCampaignRules.MakotoBoss.Health + MakotoSecondHealth + MakotoThirdHealth;
        const double HumanAttackSourceSeconds = 1.2;
        const double HumanAttackSourceContact = 16d / 30;
        const double ThanatosAttackSourceSeconds = 1.8;
        const double ThanatosAttackSourceContact = .8;

        sealed class MakotoKit
        {
            internal double FormStartedAt;
            internal double NextHealAt;
            internal int HealPulses, AttackCount;
        }

        public bool MakotoBossTransitionActive(FormationDefender guard) =>
            Phase == FormationPhase.Fighting && guard.IsMakotoBoss && guard.MakotoBossTransitionUntil > Clock;

        internal bool MakotoBossRecovering(FormationDefender guard) =>
            guard.IsMakotoBoss && guard.Alive && guard.MakotoBossPhase == 3;

        internal (string Animation, double StartedAt) MakotoBossMode(FormationDefender guard)
        {
            if (!guard.IsMakotoBoss || !guard.Alive || !guardKits.TryGetValue(guard.Id, out var kit) || kit.Makoto == null)
                return (null, -1);
            return (guard.MakotoBossPhase switch
            {
                1 => "makoto_s3_human_idle",
                2 => "makoto_s3_idle",
                _ => "makoto_s3_recovery"
            }, kit.Makoto.FormStartedAt);
        }

        void ResetMakotoBoss(FormationDefender guard, GuardKit kit)
        {
            guard.MakotoBossPhase = 1;
            guard.MakotoBossTransitionUntil = 0;
            guard.BossHealthRefillAt = double.NegativeInfinity;
            guard.PhaseStage = 0;
            guard.MaxHealth = guard.Health = State(guard).LowestHealth = MakotoPhaseHealth(guard, 1);
            guard.Range = MakotoHumanRange;
            kit.Makoto = new MakotoKit();
        }

        bool BeginMakotoBossTransition(FormationDefender guard)
        {
            if (!guard.IsMakotoBoss || guard.MakotoBossPhase >= 3 || guard.MakotoBossTransitionUntil > 0) return false;

            CancelMakotoContacts(guard);
            if (guard.MakotoBossPhase == 1)
                BeginArchivedGuardMotion(guard, true, false, "makoto_s3_human_end", 1, 0);
            else
                BeginArchivedGuardMotion(guard, true, false, "makoto_s3_change_begin", .5, 0);
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
            var guard = Defenders.Find(defender => defender.IsMakotoBoss);
            if (guard == null || !guard.Alive || guard.MakotoBossTransitionUntil == 0 ||
                Clock + 1e-8 < guard.MakotoBossTransitionUntil) return;

            guard.MakotoBossTransitionUntil = 0;
            guard.MakotoBossPhase++;
            guard.PhaseStage = guard.MakotoBossPhase - 1;
            guard.MaxHealth = MakotoPhaseHealth(guard, guard.MakotoBossPhase);
            guard.Health = State(guard).LowestHealth = guard.MaxHealth;
            guard.BossHealthRefillAt = Clock;
            guard.Range = State(guard).BaseRange;
            var kit = Kit(guard).Makoto;
            kit.AttackCount = 0;
            if (guard.MakotoBossPhase == 2)
            {
                BeginArchivedGuardMotion(guard, true, false, "makoto_s3_begin", 1, 0);
                BattleEvents.Add($"{Clock:0.00}:结城理:开辟明日的剑刃·塔纳托斯");
            }
            else
            {
                BeginArchivedGuardMotion(guard, true, false, "makoto_s3_change_end", 1, .8);
                kit.NextHealAt = guard.ActionUntil;
                BattleEvents.Add($"{Clock:0.00}:结城理:开辟明日的剑刃·俄耳甫斯守备");
            }
            kit.FormStartedAt = guard.NextAttackAt = guard.ActionUntil;
        }

        static float MakotoPhaseHealth(FormationDefender guard, int phase)
        {
            float second = Mathf.Round(MakotoSecondHealth * guard.MakotoHealthScale);
            float third = Mathf.Round(MakotoThirdHealth * guard.MakotoHealthScale);
            return phase switch
            {
                1 => Mathf.Round(MakotoTotalHealth * guard.MakotoHealthScale) - second - third,
                2 => second,
                _ => third
            };
        }

        float MakotoFutureHealth(FormationDefender guard) => guard.MakotoBossPhase switch
        {
            1 => MakotoPhaseHealth(guard, 2) + MakotoPhaseHealth(guard, 3),
            2 => MakotoPhaseHealth(guard, 3),
            _ => 0,
        };

        void CancelMakotoContacts(FormationDefender guard)
        {
            foreach (var action in presentationActions)
                if (action.ActorId == guard.Id && action.ContactAt > Clock) action.Cancelled = true;
        }

        void TickMakotoBoss(FormationDefender guard, MakotoKit kit)
        {
            if (MakotoBossTransitionActive(guard)) return;
            if (guard.MakotoBossPhase == 3)
            {
                TickMakotoRecovery(guard, kit);
                return;
            }

            if (Clock + 1e-8 < guard.ActionUntil || Clock + 1e-8 < guard.NextAttackAt ||
                Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0) return;
            var targets = GuardVictims(guard.Position, guard.Range, guard.MakotoBossPhase == 1 ? 1 : 4);
            if (targets.Length == 0) return;
            var target = targets[0];
            guard.TargetId = target.Id;
            Face(guard, target.Position);
            float frequency = GuardAttackFrequency(guard);
            if (guard.MakotoBossPhase == 1)
                TickMakotoHumanSword(guard, kit, target, frequency);
            else
                TickMakotoThanatos(guard, kit, targets, frequency * MakotoThanatosAttackFrequency);
        }

        void TickMakotoHumanSword(FormationDefender guard, MakotoKit kit, FormationFighter target, float frequency)
        {
            string motion = kit.AttackCount++ % 2 == 0 ? "makoto_s3_human_attack_a" : "makoto_s3_human_attack_b";
            var action = BeginArchivedGuardMotion(guard, false, false, motion, HumanAttackSourceSeconds,
                HumanAttackSourceContact, target, frequency);
            guard.NextAttackAt = Clock + guard.Interval / frequency;
            GuardContact(guard, action, () =>
            {
                if (target.Alive && GuardInRange(guard.Position, target, guard.Range))
                    CombatDamage(guard, target, guard.Attack, 0, false, false);
            }, trackedTarget: target);
        }

        void TickMakotoThanatos(FormationDefender guard, MakotoKit kit, FormationFighter[] targets, float frequency)
        {
            string motion = kit.AttackCount++ % 2 == 0 ? "makoto_s3_attack_a" : "makoto_s3_attack_b";
            var action = BeginArchivedGuardMotion(guard, true, false, motion, ThanatosAttackSourceSeconds,
                ThanatosAttackSourceContact, targets[0], frequency);
            guard.NextAttackAt = Clock + guard.Interval / frequency;
            GuardContact(guard, action, () =>
            {
                foreach (var target in targets)
                    if (target.Alive) CombatDamage(guard, target, guard.Attack * 2.3f, MakotoWeakPointDamage, true, false);
            }, trackedTarget: targets[0]);
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
            float heal = MakotoRecoveryHeal * (IsBossDefender(guard) ? 1f : guard.MakotoHealthScale);
            GuardContact(guard, action, () => HealGuard(guard, guard, heal, true), trackedTarget: guard);
        }

    }
}
