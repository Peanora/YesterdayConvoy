using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const string SupernovaBossClipPrefix = "skill_supernova_";
        const string SupernovaBossDeathClip = "skill_supernova_death";
        const string SupernovaBlastLettering = "03_hit_02_name";
        const string SupernovaBlastFloor = "03_hit_02_floor";
        const string SupernovaBlastBurst = "03_hit_02_burst";
        internal const string SupernovaBossEffectPrefix = "Formation Supernova / ";
        static readonly string[] SupernovaBossBodyClips =
        {
            "skill_supernova_s2_begin", "skill_supernova_s2_idle", "skill_supernova_s2_volley",
            "skill_supernova_s2_overload_begin", "skill_supernova_s2_overload_idle", "skill_supernova_s2_overload",
            "skill_supernova_transition", "skill_supernova_s3_begin", "skill_supernova_s3_idle",
            "skill_supernova_s3_shell", "skill_supernova_s3_end"
        };
        static readonly string[] SupernovaBossFrontClips = SupernovaBossBodyClips.Append(SupernovaBossDeathClip).ToArray();
        static readonly string[] SupernovaBossEffects =
        {
            "02_buff", "02_buff_02", "02_start", "02_overload_start", "02_hit", "02_hit_02",
            "03_buff_f", "03_buff_b", "03_buff_02_f", "03_buff_02_b", "03_buff_03",
            "03_start", "03_up_start", "03_down_start", "03_trail", "03_hit", "03_hit_03",
            SupernovaBlastFloor, SupernovaBlastBurst, SupernovaBlastLettering
        };

        bool UsesSupernovaBossArt(string artId) => (artId == "D31" || artId == "D31B") &&
            Model != null && Model.Defenders.Any(guard => guard.IsSupernovaBoss);

        static bool IsSupernovaBossClip(string key) => key.StartsWith(SupernovaBossClipPrefix, StringComparison.Ordinal);
        static string[] SupernovaBossBodyKeys(string artId) => artId == "D31" ? SupernovaBossFrontClips : SupernovaBossBodyClips;

        static bool SupernovaRearView(FormationActor actor) => actor.Facing.y > Mathf.Abs(actor.Facing.x);

        void SelectSupernovaBossPose(FormationDefender guard, ArtCatalog source, double now,
            ref string key, ref double age, ref bool oneShot)
        {
            if (guard.PresentationAction?.StartsWith("supernova_", StringComparison.Ordinal) == true)
            {
                string motion = "skill_" + guard.PresentationAction;
                bool hold = guard.PresentationAction == "supernova_s3_end" || Model.SupernovaBossTransitionActive(guard);
                if ((hold || now < guard.PresentationStartedAt + guard.PresentationDuration) && source.Clips.ContainsKey(motion))
                {
                    key = motion;
                    oneShot = true;
                    age = Math.Max(0, now - guard.PresentationStartedAt) * guard.PresentationRate;
                    return;
                }
            }

            var mode = Model.SupernovaBossMode(guard);
            key = guard.SupernovaBossPhase == 2 ? "skill_supernova_s3_idle" : mode.Animation == "supernova_s2_overload_begin"
                ? "skill_supernova_s2_overload_idle" : "skill_supernova_s2_idle";
            age = (Model.Phase == FormationPhase.Preparation ? Time.unscaledTimeAsDouble :
                Math.Max(0, now - Math.Max(0, mode.StartedAt))) * FormationVisualRate;
        }

        static string SupernovaBossEffectKey(string suffix) => BossRosterFxArt.Key("B20", "wisdel_skill_" + suffix + "_sale#14");

        static IEnumerable<ArtSequence> FormationSupernovaBossEffectSequences()
        {
            foreach (string suffix in SupernovaBossEffects) yield return BossRosterFxArt.Get(SupernovaBossEffectKey(suffix)).Sequence;
        }

        void PresentSupernovaBossState(FormationDefender guard, double now)
        {
            var mode = Model.SupernovaBossMode(guard);
            if (mode.Animation == null || !CombatViewActive) return;
            double age = (now - mode.StartedAt) * FormationVisualRate;
            Vector3 foot = FormationDisplayPoint(guard);
            bool flip = guard.Facing.x < 0;
            if (guard.SupernovaBossPhase == 1)
            {
                string effect = mode.Animation == "supernova_s2_overload_begin" ? "02_buff_02" : "02_buff";
                ShowSupernovaBossEffect(effect, "S2 state", age, guard.Position, foot, 10, flip: flip, loop: true);
                return;
            }

            bool rear = SupernovaRearView(guard);
            ShowSupernovaBossEffect(rear ? "03_buff_b" : "03_buff_f", "S3 state", age, guard.Position, foot, 10,
                flip: flip, loop: true);
            float scale = FormationGuardBodyScale(guard, null);
            // SPECIAL_0 follows the original chest bone in both views. Idle/Loop stays near (-3, 192).
            Vector3 shoulder = foot + (battleCamera.transform.up * 192 + battleCamera.transform.right * (flip ? 3 : -3))
                * (scale / 320);
            // The native buff is held on the caster. Play its buildup once, then sample its measured steady second.
            double wingAge = age < 2 ? age : 1 + (age - 1) % 1;
            ShowSupernovaBossEffect(rear ? "03_buff_02_b" : "03_buff_02_f", "S3 wings", wingAge, guard.Position,
                shoulder - battleCamera.transform.forward * .12f, 56, flip: flip, scale: scale * 1.2f);
            ShowSupernovaBossEffect("03_buff_03", "S3 activation", age, guard.Position, shoulder, 12, flip: flip, scale: scale);
        }

        void PresentSupernovaBossAction(FormationDefender guard, FormationPresentationAction action, double now)
        {
            bool overload = action.Animation == "supernova_s2_overload";
            double sinceRelease = (now - action.LaunchedAt) * action.Rate;
            if (overload || action.Animation == "supernova_s2_volley")
            {
                if (sinceRelease >= 0 && (overload || !action.ProjectileOnly))
                {
                    // The baked muzzle includes the transparent pre-release part of the source animation.
                    ShowSupernovaBossEffect(overload ? "02_overload_start" : "02_start", overload ? "S2 muzzle overload" : "S2 muzzle",
                        sinceRelease + 7d / 30, action.From, SupernovaBossMuzzle(guard, action, false), 66,
                        flip: SupernovaFacesLeft(guard, SupernovaShotDirection(action)), scale: 1.25f);
                }
                return;
            }

            if (action.Animation != "supernova_s3_shell") return;
            var target = FormationFeedbackActor(action.TargetId);
            Vector3 muzzle = SupernovaBossMuzzle(guard, action, true);
            Vector2 shotDirection = SupernovaShotDirection(action);
            string muzzleEffect = Mathf.Abs(shotDirection.y) > Mathf.Abs(shotDirection.x)
                ? (shotDirection.y > 0 ? "03_up_start" : "03_down_start") : "03_start";
            if (sinceRelease >= 0)
                ShowSupernovaBossEffect(muzzleEffect, "S3 muzzle", sinceRelease + 16d / 30, action.From, muzzle, 66,
                    flip: SupernovaFacesLeft(guard, shotDirection));
            if (now < action.ContactAt)
            {
                Vector2 destination = target?.Alive == true ? target.Position : action.To;
                var contact = FormationFeedbackContactPoint(target, destination, .52f);
                ShowSupernovaBossEffect("03_hit_03", "S3 aim", (now - action.At) * action.Rate, destination, contact, 52, scale: 1.2f);
                if (now < action.LaunchedAt) return;
                float progress = Mathf.Clamp01((float)((now - action.LaunchedAt) / (action.ContactAt - action.LaunchedAt)));
                Vector2 sortingCell = new Vector2(Mathf.Lerp(action.From.x, destination.x, progress), Mathf.Min(action.From.y, destination.y));
                ShowSupernovaBossEffect("03_trail", "S3 trail", sinceRelease,
                    sortingCell, Vector3.Lerp(muzzle, contact, progress), 68,
                    loop: true, direction: contact - muzzle, scale: 1.4f);
                return;
            }

            // The native stop emits a floor spread, an upright burst and raised lettering at the landing cell.
            double blastAge = (now - action.ContactAt) * action.Rate - .15;
            Vector3 blastRoot = ActorWorldPoint(action.To) + Vector3.up * .045f;
            ShowSupernovaBossEffect(SupernovaBlastFloor, "S3 impact ground", blastAge, action.To, blastRoot, -150,
                ground: true, scale: 1.27f);
            ShowSupernovaBossEffect(SupernovaBlastBurst, "S3 impact burst", blastAge, action.To,
                blastRoot + battleCamera.transform.up * .25f, 68, scale: 1.24f, minimumHeight: blastRoot.y);
            ShowSupernovaBossEffect(SupernovaBlastLettering, "S3 blast lettering", blastAge, action.To,
                blastRoot + battleCamera.transform.up * .77f, 82, scale: 1.06f, minimumHeight: blastRoot.y);
        }

        void PresentSupernovaBossHit(FormationHit hit, double now)
        {
            var action = hit.Action;
            bool s2 = action?.Animation == "supernova_s2_volley" || action?.Animation == "supernova_s2_overload";
            if (!s2 && action?.Animation != "supernova_s3_shell") return;
            string effect = s2 ? "02_hit" : "03_hit";
            string label = s2 ? "S2 impact" : "S3 impact";
            var stamp = (hit.FromId, hit.ToId, hit.At, effect);
            if (!guardHitSprites.Add(stamp))
            {
                formationImpactOwners.Add(hit);
                return;
            }
            double age = (now - hit.At) * action.Rate;
            var point = FormationFeedbackContactPoint(FormationFeedbackActor(hit.ToId), hit.To, .52f);
            if (ShowSupernovaBossEffect(effect, label, age, hit.To, point, 55, scale: s2 ? 1.3f : 1.5f)) formationImpactOwners.Add(hit);
            if (s2) ShowSupernovaBossEffect("02_hit_02", "S2 impact ground", age - 1, hit.To,
                ActorWorldPoint(hit.To) + Vector3.up * .045f, -65, ground: true);
        }

        static bool SupernovaFacesLeft(FormationDefender guard, Vector2 toward) =>
            toward.x < 0 || toward.x == 0 && guard.Facing.x < 0;

        Vector2 SupernovaShotDirection(FormationPresentationAction action)
        {
            if (action.ProjectileOnly)
            {
                // The source fires later overload bullets from the same body pose and mount.
                var parent = Model.PresentationActions.FirstOrDefault(item => item.Sequence == action.ParentSequence);
                if (parent != null) return parent.To - parent.From;
            }
            return action.To - action.From;
        }

        Vector3 SupernovaBossMuzzle(FormationDefender guard, FormationPresentationAction action, bool s3)
        {
            Vector2 toward = SupernovaShotDirection(action);
            bool rear = toward.y > Mathf.Abs(toward.x);
            Vector2 muzzle = s3 ? (rear ? new Vector2(303, 520) : new Vector2(424, 265)) :
                rear ? new Vector2(318, 130) : new Vector2(268, 87);
            // Original Spine F_Muzzle/B_Muzzle positions sampled at OnAttack and expressed in D31's body units.
            float worldPerSourcePixel = .5f / 160f * FormationGuardBodyScale(guard, null);
            if (SupernovaFacesLeft(guard, toward)) muzzle.x = -muzzle.x;
            return LegacyOwnerPoint(guard, action.From) + (battleCamera.transform.right * muzzle.x + battleCamera.transform.up * muzzle.y) * worldPerSourcePixel;
        }

        bool ShowSupernovaBossEffect(string suffix, string label, double age, Vector2 cell, Vector3 position, int order,
            bool flip = false, bool loop = false, bool ground = false, Vector3? direction = null, float scale = 1,
            float? minimumHeight = null)
        {
            if (age < 0 || !FormationPointVisibleInView(cell)) return false;
            if (!BossRosterFxArt.TryGet(SupernovaBossEffectKey(suffix), out var clip) || !clip.Sequence.Ready) return false;
            var frame = clip.At(age, loop);
            if (frame == null) return false;
            float roll = direction.HasValue ? Mathf.Atan2(Vector3.Dot(direction.Value, battleCamera.transform.up),
                Vector3.Dot(direction.Value, battleCamera.transform.right)) * Mathf.Rad2Deg : 0;
            if (minimumHeight.HasValue)
            {
                float bottom = position.y + frame.bounds.min.y * scale * battleCamera.transform.up.y;
                if (bottom < minimumHeight.Value && battleCamera.transform.position.y > minimumHeight.Value)
                {
                    // Bring only this billboard's depth above the landing tile. Scaling about the eye
                    // keeps the screen location and size, while still testing depth against actual terrain.
                    float ratio = (battleCamera.transform.position.y - minimumHeight.Value) /
                        (battleCamera.transform.position.y - bottom);
                    position = Vector3.LerpUnclamped(battleCamera.transform.position, position, ratio);
                    scale *= ratio;
                }
            }
            var renderer = LegacyDraw(frame, legacyHdrMaterial, position, cell, scale, order,
                flip: flip, hdr: clip.HdrRange, flat: ground, roll: roll, gammaOutput: true);
            if (renderer == null) return false;
            renderer.name = SupernovaBossEffectPrefix + label;
            return true;
        }
    }
}
