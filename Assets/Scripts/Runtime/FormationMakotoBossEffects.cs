using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        internal const string MakotoBossEffectPrefix = "Formation Makoto / ";
        static readonly string[] MakotoEffectSuffixes =
        {
            "change_01", "change_01_l",
            "attack_a_start_01", "attack_a_hit_01", "attack_b_start_01", "attack_b_start_01_back", "attack_b_hit_01",
            "skill_03_p1_buff_01_l", "skill_03_p1_buff_01_r", "skill_03_p1_range_01_start", "skill_03_p1_range_01",
            "skill_03_p1_start_a_01", "skill_03_p1_start_b_01", "skill_03_p1_start_a_01_back", "skill_03_p1_start_b_01_back",
            "skill_03_p1_hit_01", "skill_03_p1_hit_02",
            "skill_03_p1_weak_01", "skill_03_p2_buff_01", "skill_03_p2_buff_01_l", "skill_03_p2_range_01",
            "skill_03_p2_range_02_start", "skill_03_p2_range_02", "skill_03_p2_heal_01",
        };

        static string MakotoEffectKey(string suffix) => BossRosterFxArt.Key(BossRosterFxArt.MakotoTask, "makoto_" + suffix + "_p3r#1");

        void PresentMakotoBossState(FormationDefender guard, double now)
        {
            var mode = Model.MakotoBossMode(guard);
            if (!CombatViewActive || mode.Animation == null || Model.MakotoBossTransitionActive(guard) || now < mode.StartedAt) return;
            if (guard.PresentationAction == "makoto_s3_change_begin" && now < guard.PresentationStartedAt + guard.PresentationDuration) return;

            double age = (now - mode.StartedAt) * FormationVisualRate;
            Vector2 cell = guard.Position;
            Vector3 foot = FormationDisplayPoint(guard);
            bool left = guard.Facing.x < 0;
            string side = left ? "_l" : "";
            Vector3 ground = ActorWorldPoint(cell) + Vector3.up * .065f;
            float scale = FormationGuardBodyScale(guard, null) / MakotoBossBodyScale;
            // Source doll glows wrap the body. Keep the sustained billboard behind its visible skin.
            const int bodyAuraOrder = -8;
            switch (mode.Animation)
            {
                case "makoto_s3_idle":
                    ShowMakotoEffect(left ? "skill_03_p1_buff_01_l" : "skill_03_p1_buff_01_r", age, cell, foot, bodyAuraOrder, loop: true, scale: scale);
                    ShowMakotoEffect("skill_03_p1_range_01_start", age, cell, ground, -80, ground: true);
                    ShowMakotoEffect("skill_03_p1_range_01", age, cell, ground, -80, loop: true, ground: true);
                    break;
                case "makoto_s3_recovery":
                    ShowMakotoEffect("skill_03_p2_buff_01" + side, age, cell, foot, bodyAuraOrder, loop: true, scale: scale);
                    ShowMakotoEffect("skill_03_p2_range_01", age, cell, ground, -80, loop: true, ground: true);
                    ShowMakotoEffect("skill_03_p2_range_02_start", age, cell, ground, -79, ground: true);
                    ShowMakotoEffect("skill_03_p2_range_02", age, cell, ground, -79, loop: true, ground: true);
                    break;
            }
        }

        void PresentMakotoBossAction(FormationDefender guard, FormationPresentationAction action, double now)
        {
            if (!CombatViewActive) return;
            bool left = action.To.x == action.From.x ? guard.Facing.x < 0 : action.To.x < action.From.x;
            Vector2 direction = action.To - action.From;
            bool rear = direction.sqrMagnitude == 0 ? MakotoRearView(guard) : direction.y > Mathf.Abs(direction.x);
            string view = rear ? "_back" : "";
            Vector3 foot = LegacyOwnerPoint(guard, action.From);
            double age = (now - action.At) * action.Rate;
            float visualRatio = FormationGuardBodyScale(guard, null) / MakotoBossBodyScale;
            switch (action.Animation)
            {
                case "makoto_s3_begin":
                case "makoto_s3_change_end":
                    ShowMakotoEffect(left ? "change_01_l" : "change_01", (now - action.LaunchedAt) * action.Rate,
                        action.From, foot, 22, scale: visualRatio);
                    break;
                case "makoto_s3_human_attack_a":
                case "makoto_s3_human_attack_b":
                    // The source A swing shares a cast layer in both facings. B has a dedicated rear layer.
                    ShowMakotoEffect(action.Animation == "makoto_s3_human_attack_a" ? "attack_a_start_01" : "attack_b_start_01" + view,
                        age, action.From, foot, 20, flip: left, scale: visualRatio);
                    break;
                case "makoto_s3_attack_a":
                case "makoto_s3_attack_b":
                    ShowMakotoEffect((action.Animation == "makoto_s3_attack_a" ? "skill_03_p1_start_a_01" : "skill_03_p1_start_b_01") + view,
                        age, action.From, foot, 20, flip: left, scale: visualRatio);
                    break;
                case "makoto_s3_heal":
                    var target = FormationFeedbackActor(action.TargetId);
                    if (target?.Alive == true)
                        ShowMakotoEffect("skill_03_p2_heal_01", age, target.Position, FormationDisplayPoint(target), 35, scale: visualRatio);
                    break;
            }
        }

        void PresentMakotoBossHit(FormationHit hit, double now)
        {
            var action = hit.Action;
            if (action == null) return;
            string suffix = action.Animation switch
            {
                "makoto_s3_human_attack_a" => "attack_a_hit_01",
                "makoto_s3_human_attack_b" => "attack_b_hit_01",
                "makoto_s3_attack_a" or "makoto_s3_attack_b" => hit.DamageType == 0 ? "skill_03_p1_hit_02" : "skill_03_p1_hit_01",
                _ => null,
            };
            if (suffix == null) return;
            var stamp = (hit.FromId, hit.ToId, hit.At, suffix);
            if (!guardHitSprites.Add(stamp)) return;
            double age = (now - hit.At) * FormationVisualRate;
            var target = FormationFeedbackActor(hit.ToId);
            Vector3 contact = FormationFeedbackContactPoint(target, hit.To, .52f);
            Vector3 targetRoot = target == null ? ActorWorldPoint(hit.To) : LegacyOwnerPoint(target, hit.To);
            bool human = suffix == "attack_a_hit_01" || suffix == "attack_b_hit_01";
            bool onBody = human || suffix == "skill_03_p1_hit_02";
            if (ShowMakotoEffect(suffix, age, hit.To, onBody ? contact : targetRoot, 54, flip: human && hit.To.x < hit.From.x))
                formationImpactOwners.Add(hit);
            if (suffix == "skill_03_p1_hit_02")
                ShowMakotoEffect("skill_03_p1_weak_01", age, hit.To, contact, 56);
        }

        bool ShowMakotoEffect(string suffix, double age, Vector2 cell, Vector3 point, int order,
            bool flip = false, bool loop = false, bool ground = false, float scale = 1)
        {
            if (age < 0 || !FormationPointVisibleInView(cell)) return false;
            if (!BossRosterFxArt.TryGet(MakotoEffectKey(suffix), out var clip) || !clip.Sequence.Ready) return false;
            // Keep the source fade-in once. Later repeats use only the sustained part of the capture.
            if (loop && clip.Loop && age >= clip.Duration && clip.Duration > .55)
                age = .55 + (age - .55) % (clip.Duration - .55);
            var renderer = LegacyDraw(clip.At(age), legacyHdrMaterial, point, cell, scale, order, flip: flip,
                hdr: clip.HdrRange, flat: ground, gammaOutput: true);
            if (renderer == null) return false;
            renderer.name = MakotoBossEffectPrefix + suffix;
            return true;
        }
    }
}
