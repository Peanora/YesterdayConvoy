using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const string SupernovaBossClipPrefix = "skill_supernova_";
        internal const string SupernovaBossEffectPrefix = "Formation Supernova / ";
        static readonly string[] SupernovaBossBodyClips =
        {
            "skill_supernova_s2_begin", "skill_supernova_s2_idle", "skill_supernova_s2_volley",
            "skill_supernova_s2_overload_begin", "skill_supernova_s2_overload_idle", "skill_supernova_s2_overload",
            "skill_supernova_transition", "skill_supernova_s3_begin", "skill_supernova_s3_end"
        };
        static readonly string[] SupernovaBossEffects =
        {
            "02_buff", "02_buff_02", "02_start", "02_overload_start", "02_hit", "02_hit_02",
            "03_buff_f", "03_start", "03_trail", "03_hit", "03_hit_02", "03_hit_03"
        };

        bool UsesSupernovaBossArt(string artId) => (artId == "D31" || artId == "D31B") &&
            Model != null && Campaign?.CurrentNode.Id == FormationCampaignRules.SupernovaBossNodeId;

        static bool IsSupernovaBossClip(string key) => key.StartsWith(SupernovaBossClipPrefix, StringComparison.Ordinal);

        static bool SupernovaRearView(FormationActor actor) => actor.Facing.y > Mathf.Abs(actor.Facing.x);

        void SelectSupernovaBossPose(FormationDefender guard, ArtCatalog source, double now,
            ref string key, ref double age, ref bool oneShot)
        {
            if (guard.PresentationAction?.StartsWith("supernova_", StringComparison.Ordinal) == true)
            {
                string motion = guard.PresentationAction == "supernova_s3_shell" ? "attack" : "skill_" + guard.PresentationAction;
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
            key = guard.SupernovaBossPhase == 2 ? "idle" : mode.Animation == "supernova_s2_overload_begin"
                ? "skill_supernova_s2_overload_idle" : "skill_supernova_s2_idle";
            age = Math.Max(0, now - Math.Max(0, mode.StartedAt)) * FormationVisualRate;
        }

        static BossRosterFxArt.Clip SupernovaBossEffect(string suffix) =>
            BossRosterFxArt.Get(BossRosterFxArt.Key("B20", "wisdel_skill_" + suffix + "_sale#14"));

        static IEnumerable<ArtSequence> FormationSupernovaBossEffectSequences()
        {
            foreach (string suffix in SupernovaBossEffects) yield return SupernovaBossEffect(suffix).Sequence;
        }

        void PresentSupernovaBossState(FormationDefender guard, double now)
        {
            var mode = Model.SupernovaBossMode(guard);
            string effect = mode.Animation switch
            {
                "supernova_s2_begin" => "02_buff",
                "supernova_s2_overload_begin" => "02_buff_02",
                "supernova_s3_begin" when !SupernovaRearView(guard) => "03_buff_f",
                _ => null
            };
            if (effect == null || !CombatViewActive || Model.SupernovaBossTransitionActive(guard)) return;
            ShowSupernovaBossEffect(effect, guard.SupernovaBossPhase == 1 ? "S2 state" : "S3 state",
                (now - mode.StartedAt) * FormationVisualRate, guard.Position, FormationDisplayPoint(guard), 10,
                flip: guard.Facing.x < 0, loop: true);
        }

        void PresentSupernovaBossAction(FormationDefender guard, FormationPresentationAction action, double now)
        {
            bool overload = action.Animation == "supernova_s2_overload";
            if (overload || action.Animation == "supernova_s2_volley")
            {
                if (overload || !action.ProjectileOnly)
                    ShowSupernovaBossEffect(overload ? "02_overload_start" : "02_start", overload ? "S2 muzzle overload" : "S2 muzzle",
                        (now - action.LaunchedAt) * action.Rate, action.From, SupernovaBossMuzzle(guard, action, false), 20,
                        flip: SupernovaFacesLeft(guard, SupernovaShotDirection(action)));
                return;
            }

            if (action.Animation != "supernova_s3_shell") return;
            var target = FormationFeedbackActor(action.TargetId);
            Vector3 muzzle = SupernovaBossMuzzle(guard, action, true);
            ShowSupernovaBossEffect("03_start", "S3 muzzle", (now - action.LaunchedAt) * action.Rate, action.From, muzzle, 20,
                flip: SupernovaFacesLeft(guard, SupernovaShotDirection(action)));
            if (now < action.ContactAt)
            {
                Vector2 destination = target?.Alive == true ? target.Position : action.To;
                var contact = FormationFeedbackContactPoint(target, destination, .52f);
                ShowSupernovaBossEffect("03_hit_03", "S3 aim", (now - action.At) * action.Rate, destination, contact, 21);
                if (now < action.LaunchedAt) return;
                float progress = Mathf.Clamp01((float)((now - action.LaunchedAt) / (action.ContactAt - action.LaunchedAt)));
                ShowSupernovaBossEffect("03_trail", "S3 trail", (now - action.LaunchedAt) * action.Rate,
                    Vector2.Lerp(action.From, destination, progress), Vector3.Lerp(muzzle, contact, progress), 26,
                    loop: true, direction: contact - muzzle);
                return;
            }

            // The ground effect is emitted once when the real shell stops, even for a shot that hits no surviving unit.
            ShowSupernovaBossEffect("03_hit_02", "S3 impact ground", (now - action.ContactAt) * action.Rate - .15,
                action.To, ActorWorldPoint(action.To) + Vector3.up * .025f, -4, ground: true);
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
            if (ShowSupernovaBossEffect(effect, label, age, hit.To, point, 24)) formationImpactOwners.Add(hit);
            if (s2) ShowSupernovaBossEffect("02_hit_02", "S2 impact ground", age - 1, hit.To,
                ActorWorldPoint(hit.To) + Vector3.up * .025f, -4, ground: true);
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
            bool flip = false, bool loop = false, bool ground = false, Vector3? direction = null)
        {
            if (age < 0 || !FormationPointVisibleInView(cell)) return false;
            var clip = SupernovaBossEffect(suffix);
            if (!clip.Sequence.Ready) return false;
            float roll = direction.HasValue ? Mathf.Atan2(Vector3.Dot(direction.Value, battleCamera.transform.up),
                Vector3.Dot(direction.Value, battleCamera.transform.right)) * Mathf.Rad2Deg : 0;
            var renderer = LegacyDraw(clip.At(age, loop), legacyHdrMaterial, position, cell, 1f, order,
                flip: flip, hdr: clip.HdrRange, flat: ground, roll: roll);
            if (renderer == null) return false;
            renderer.name = SupernovaBossEffectPrefix + label;
            return true;
        }
    }
}
