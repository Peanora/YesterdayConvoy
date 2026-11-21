using System;
using System.Globalization;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        OriginalNativeFxLayer formationSixStarFx;

        static bool UsesFormationSixStarNativeFx(string artId) => artId is
            "D21" or "D22" or "D23" or "D24" or "D25" or "D26" or "D28" or "D29" or "D30" or "D32" or "D33" or "D34" or "D35";

        static string FxTime(double time) => time.ToString("R", CultureInfo.InvariantCulture);

        bool ShowFormationGuardEffect(FormationDefender guard, string clip, string instance, double age,
            Vector2 cell, int order, bool loop = false, bool flat = false, Vector3? point = null,
            Vector3? direction = null, bool flip = false, FormationActor recipient = null)
        {
            if (age < 0 || !FormationPointVisibleInView(cell)) return false;
            string key = FormationGuardFxKey(guard.ArtId, clip);
            if (!UsesFormationSixStarNativeFx(guard.ArtId) || !OriginalNativeFxCatalog.Supports(key))
                return LegacyOriginal(key, age, cell, order, loop, flat, explicitPosition: point, direction: direction, flip: flip) != null;
            if (formationSixStarFx == null || !OriginalNativeFxCatalog.TryPrepared(key, out var definition, out _) ||
                !OriginalNativeFxCatalog.TryGetPlacement(key, out var placement)) return false;

            Vector3 anchor = point ?? ActorWorldPoint(cell) + Vector3.up * .025f;
            Vector3 aim = direction ?? new Vector3(guard.Facing.x, 0, guard.Facing.y);
            bool ground = flat || placement.UseBodyRotation || placement.HasProjectileDriver ||
                placement.SpawnLocation is 5 or 7 or 17 or 26;
            Quaternion rotation = ground ? Quaternion.Euler(90, 0, 0) : battleCamera.transform.rotation;
            Vector2 scale = Vector2.one;
            bodies.TryGetValue(guard.Id, out var body);
            if (DefenderNativeMounts.RequiresMount(guard.ArtId, clip))
            {
                if (!DefenderNativeMounts.TryResolve(guard.ArtId, clip, body, out anchor, out float roll, out scale))
                    return false;
                rotation = battleCamera.transform.rotation * Quaternion.AngleAxis(roll, Vector3.forward);
                flip = body.flipX;
            }
            else if (placement.HasFollowMountPoint)
            {
                string mount = placement.MountPoint switch
                {
                    1 => "Hit", 2 => "Muzzle", 3 => "Head", 5 => "SpecialPoint0", 6 => "SpecialPoint1",
                    >= 8 and <= 21 => "SpecialPoint" + (placement.MountPoint - 6), _ => null
                };
                if (mount == null && body != null && placement.MountPoint is 0 or 7)
                    anchor = body.transform.position;
                else if (!DefenderNativeMounts.TryResolvePoint(guard.ArtId, mount, body, out anchor, out float roll))
                    return false;
                else if (!placement.KeepOriginRotation)
                    rotation = battleCamera.transform.rotation * Quaternion.AngleAxis(roll, Vector3.forward);
                flip = body.flipX;
            }
            else if (placement.HasParticleDriver)
            {
                if (placement.SpawnLocation == 2 && recipient is FormationDefender other &&
                    bodies.TryGetValue(other.Id, out var targetBody) &&
                    DefenderNativeMounts.TryResolvePoint(other.ArtId, "Hit", targetBody, out var hitAnchor, out _))
                    anchor = hitAnchor;
                if (point == null && placement.SpawnLocation is 3 or 4 or 6 &&
                    DefenderNativeMounts.TryResolvePoint(guard.ArtId, placement.SpawnLocation == 4 ? "Head" : "Muzzle",
                        body, out var nativeAnchor, out float roll))
                {
                    anchor = nativeAnchor;
                    rotation = battleCamera.transform.rotation * Quaternion.AngleAxis(placement.SpawnLocation == 6 ? 0 : roll, Vector3.forward);
                }
                if (placement.HoldByOwner && placement.RotateType == 1 && body != null) flip = body.flipX;
            }

            var pose = new OriginalNativeFxPose(anchor, rotation, aim, flip, new Vector3(scale.x, scale.y, 1))
            {
                FormationSortingOrder = LegacyOrder(cell, order)
            };
            bool repeats = loop && definition.loop;
            return formationSixStarFx.TryShow(key, guard.Id + "/" + instance + "/" + key, age, pose, repeats,
                !repeats && definition.duration > 0 ? definition.duration : double.PositiveInfinity);
        }

        void PresentFormationSixStarFields(FormationDefender guard)
        {
            if (!UsesFormationSixStarNativeFx(guard.ArtId)) return;
            if (FormationBattleModel.IsArchivedSignatureGuard(guard.ArtId))
            {
                PresentArchivedGuardFields(guard);
                return;
            }
            if (guard.ArtId == "D21")
            {
                PresentFormationLapplandWolves(guard);
                return;
            }
            if (guard.ArtId == "D32")
            {
                PresentFormationChenFields(guard);
                return;
            }
            double startedAt = Model.GuardSignatureStart(guard);
            if (double.IsNegativeInfinity(startedAt)) return;
            double age = (Model.Clock - startedAt) * FormationVisualRate;
            string instance = "signature/" + FxTime(startedAt);
            var position = guard.Position;
            if (guard.ArtId == "D23" && Model.GuardTransformed(guard))
            {
                ShowFormationGuardEffect(guard, "monstr_skill_03_begin_01", instance, age, Model.GuardHomePosition(guard), 11, flat: true);
                ShowFormationGuardEffect(guard, "monstr_skill_03_begin_02", instance, age, position, 24);
                ShowFormationGuardEffect(guard, "monstr_skill_03_begin_03", instance, age, position, 12, true, true);
                for (int i = 1; i <= 5; i++)
                    ShowFormationGuardEffect(guard, "monstr_skill_03_buff_0" + i, instance, age, position, 12 + i,
                        loop: true);
            }
            else if (guard.ArtId == "D24" && Model.GuardSkillActive(guard))
                ShowFormationGuardEffect(guard, "angel2_skill_3_buff_01", instance, age, position, 12, true, true);
            else if (guard.ArtId == "D25" && guard.Shield > 0)
            {
                ShowFormationGuardEffect(guard, "hsgma2_skill_03_range_01", instance, age, position, 11, true, true);
                ShowFormationGuardEffect(guard, "hsgma2_skill_03_buff_01", instance, age, position, 12, true, true);
                ShowFormationGuardEffect(guard, "hsgma2_skill_03_buff_02", instance, age, position, 13, true, true);
            }
            else if (guard.ArtId == "D26" && Model.GuardEffect(guard) is { Kind: "silverash" } field)
            {
                // The source aura, weapon and terrain range ignore animation playback speed.
                double fieldAge = Model.Clock - startedAt;
                var facing = new Vector3(field.Direction.x, 0, field.Direction.y);
                ShowFormationGuardEffect(guard, "svash2_skill_03_buff_01", instance, fieldAge, position, 18,
                    loop: true, flat: true, direction: facing);
                ShowFormationGuardEffect(guard, "svash2_skill_03_buff_weapon_01", instance, fieldAge, position, 22, loop: true);
                ShowFormationGuardEffect(guard, "svash2_skill_03_range_01", instance, fieldAge, position, 11,
                    loop: true, flat: true, point: WorldPoint(position) + Vector3.up * .025f, direction: facing);
            }
            else if (guard.ArtId == "D29" && guard.Shield > 0)
                ShowFormationGuardEffect(guard, "demkni_skill_03_self", instance, age, position, 12, true, true);
        }

        void PresentFormationLapplandWolves(FormationDefender guard)
        {
            if (Model.Phase is not (FormationPhase.Preparation or FormationPhase.Fighting)) return;
            double sinceCast = Model.Clock - Model.GuardSignatureStart(guard);
            if (!Model.GuardSkillActive(guard))
            {
                double begun = Model.GuardSignatureEnd(guard);
                // Normal-mode SpecialPoint0 remains attached as in the source. Each separate wolf hit has its own contact cue.
                ShowFormationGuardEffect(guard, "whitw2_token_01", "ordinary-token/" + FxTime(begun),
                    (Model.Clock - begun) * FormationVisualRate, guard.Position, 32, loop: true);
                return;
            }

            var wolves = Model.GuardHuntWolves(guard);
            string instance = "hunt/" + FxTime(Model.GuardSignatureStart(guard));
            double age = sinceCast;
            ShowFormationGuardEffect(guard, "whitw2_skill_03_buff_start", instance, age, guard.Position, 12);
            for (int i = 0; i < wolves.Count; i++)
            {
                var wolf = wolves[i];
                Vector3 origin = wolf.Locked ? ActorWorldPoint(wolf.Position) : WorldPoint(wolf.Position);
                Vector3 aim = new Vector3(wolf.Direction.x, 0, wolf.Direction.y);
                ShowFormationGuardEffect(guard, "whitw2_skill_03_token_trail_01", instance + "/wolf/" + i,
                    age, wolf.Position, 33, loop: true, point: origin, direction: aim);
            }
        }

        void PresentFormationYuFirewall(FormationDefender guard, FormationBattleModel.FormationGuardEffect wall,
            string instance, double age)
        {
            // The archived wall renders one native emitter per tile plus a native cap on each board edge.
            // The wall's settlement radius remains the actual model value.
            bool facingRight = wall.Direction.x >= 0;
            string clip = facingRight ? "yu_skill3_range_01_r" : "yu_skill3_range_01_l";
            float column = wall.Origin.x;
            for (int row = 0; row < FormationBattleModel.Height; row++)
                ShowFormationGuardEffect(guard, clip, instance + "/" + row, age, new Vector2(column, row),
                    11, loop: true, flat: true);
            ShowFormationGuardEffect(guard, clip + (facingRight ? "_d_end" : "_u_end"), instance,
                age, new Vector2(column, -.5f), 11, loop: true, flat: true);
            ShowFormationGuardEffect(guard, clip + (facingRight ? "_u_end" : "_d_end"), instance,
                age, new Vector2(column, FormationBattleModel.Height - .5f), 11, loop: true, flat: true);
            ShowFormationGuardEffect(guard, "yu_skill3_buff_01", instance, age, guard.Position, 18,
                loop: true, flat: true);
        }

        void PresentFormationSixStarSignatureAction(FormationDefender guard, FormationPresentationAction action, double now)
        {
            if (!UsesFormationSixStarNativeFx(guard.ArtId)) return;
            if (FormationBattleModel.IsArchivedSignatureGuard(guard.ArtId))
            {
                PresentArchivedGuardAction(guard, action, now);
                return;
            }
            string instance = "cast/" + action.Sequence;
            double age = (now - action.At) * action.Rate;
            var direction = ActorWorldPoint(action.To) - ActorWorldPoint(action.From);
            if (guard.ArtId == "D32" && action.Animation == "skill_attack")
                PresentFormationChenSlash(guard, action, instance, age);
            else if (guard.ArtId == "D23" && action.Animation == "skill_end")
            {
                ShowFormationGuardEffect(guard, "monstr_skill_03_end_01", instance, age, action.From, 24);
                ShowFormationGuardEffect(guard, "monstr_skill_03_end_02", instance, age,
                    guard.SkillCell.HasValue ? (Vector2)guard.SkillCell.Value : action.From, 11, flat: true);
            }
            else if (guard.ArtId == "D26" && action.Animation == "skill_attack")
            {
                ShowFormationGuardEffect(guard, "svash2_skill_03_combat_start_01", instance, age, action.From, 23,
                    direction: direction);
                double travelAge = now - action.LaunchedAt;
                if (travelAge < 0 || !FormationPointVisibleInView(action.From)) return;
                direction.y = 0;
                if (direction.sqrMagnitude < 1e-8f) direction = new Vector3(guard.Facing.x, 0, guard.Facing.y);
                // The prefab's animated tip advances along its authored +Y, projected to world +Z by the flat pose.
                // Rotate that authored root toward the actual wave without also advancing the renderer's root.
                var rotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, direction, Vector3.up), Vector3.up) *
                    Quaternion.Euler(90, 0, 0);
                var pose = new OriginalNativeFxPose(ActorWorldPoint(action.From) + Vector3.up * .025f, rotation, direction)
                {
                    FormationSortingOrder = LegacyOrder(action.From, 30)
                };
                formationSixStarFx.TryShow("defender/D26/svash2_skill_03_trail_02", guard.Id + "/wave/" + action.Sequence,
                    travelAge, pose, loop: true, stopAge: .3);
            }
            else if (guard.ArtId == "D30" && action.Animation == "skill_attack")
            {
                ShowFormationGuardEffect(guard, "mlynar_skill_03_buff_01", instance, age, action.From, 20,
                    direction: direction);
                ShowFormationGuardEffect(guard, "mlynar_skill_03_range_01", instance, (now - action.LaunchedAt) * action.Rate,
                    action.From, 17, flat: true, direction: direction);
            }
        }

        void PresentFormationWangBoard(FormationDefender guard,string instance,double age)
        {
            var cell=guard.Position;
            ShowFormationGuardEffect(guard,"wang_skill_03_range_start",instance,age,cell,11,flat:true);
            ShowFormationGuardEffect(guard,"wang_skill_03_range",instance,age,cell,10,true,true);
            ShowFormationGuardEffect(guard,"wang_skill_03_buff_02",instance,age,cell,26,true,true);
        }
    }
}
