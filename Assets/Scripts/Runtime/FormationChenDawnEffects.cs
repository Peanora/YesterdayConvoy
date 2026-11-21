using System;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float ChenWaveSpeed = 1.5f;
        const float ChenWaveClearance = .25f;
        const string ChenWaveKey = "defender/D32/chen3_skill_03_trail_01_dir_right";
        readonly List<OriginalNativeFxPathPoint> formationChenPath = new List<OriginalNativeFxPathPoint>();
        FormationPresentationAction formationChenPathOwner;

        static Vector2 ChenCardinal(Vector2 direction) => Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? new Vector2(direction.x <= 0 ? -1 : 1, 0)
            : new Vector2(0, direction.y < 0 ? -1 : 1);

        static Vector3 ChenAim(Vector2 direction) => new Vector3(direction.x, 0, direction.y);

        static string ChenDirectionSuffix(Vector2 direction) => direction.x < 0 ? "_dir_left" : direction.x > 0
            ? "_dir_right" : direction.y > 0 ? "_dir_up" : "_dir_down";

        void PresentFormationChenFields(FormationDefender guard)
        {
            // D32 is preserved in the defender library. The opt-in review runs the
            // original talent and skill visual layers on the saved formation board.
            if (showcaseGuardArt != "D32" || Model.Phase != FormationPhase.Fighting) return;
            double now = Model.Clock;
            double passiveAge = now * FormationVisualRate;
            ShowFormationGuardEffect(guard, "chen3_shield_buff_01", "dawn-talent-shield", passiveAge, guard.Position, 26, loop: true);
            ShowFormationGuardEffect(guard, "chen3_buff_start_01", "dawn-talent-start", passiveAge, guard.Position, 24);
            ShowFormationGuardEffect(guard, "chen3_shield_buff_02", "dawn-talent-heal", passiveAge, guard.Position, 25);
            var action = Model.ShowcaseChenLaunch;
            if (action == null || action.ActorId != guard.Id || now > action.EndAt + 2) return;
            Vector2 facing = ChenCardinal(action.To - action.From);
            Vector3 aim = ChenAim(facing);
            string instance = "dawn-skill/" + action.Sequence;
            if (now <= action.EndAt)
            {
                double age = (now - action.At) * action.Rate;
                bool left = Vector3.Dot(aim, battleCamera.transform.right) < 0;
                ShowFormationGuardEffect(guard, left ? "chen3_skill_03_buff_l_01" : "chen3_skill_03_buff_r_01",
                    instance, age, guard.Position, 25, loop: true, direction: aim);
                ShowFormationGuardEffect(guard, "chen3_skill_03_weapon_01", instance, age, guard.Position, 26, loop: true, direction: aim);
                ShowFormationGuardEffect(guard, "chen3_skill_03_range_01" + ChenDirectionSuffix(facing), instance,
                    age, guard.Position, 11, loop: true, flat: true, direction: aim);
            }
            if (now < action.LaunchedAt) return;
            if (formationChenPathOwner != action)
            {
                BuildFormationChenPath(action, facing);
                formationChenPathOwner = action;
            }
            var sample = FormationChenWavePoint(now);
            var pose = new OriginalNativeFxPose(sample.Position, Quaternion.Euler(90, 0, 0), sample.Direction)
            {
                FormationSortingOrder = LegacyOrder(new Vector2(sample.Position.x + (FormationBattleModel.Width - 1) * .5f,
                    sample.Position.z + (FormationBattleModel.Height - 1) * .5f), 33)
            };
            formationSixStarFx.TryShowPath(ChenWaveKey, instance, now - action.LaunchedAt, formationChenPath,
                pose, true, action.EndAt - action.LaunchedAt);
        }

        void PresentFormationChenSlash(FormationDefender guard, FormationPresentationAction action, double now,
            string instance, double age)
        {
            Vector2 facing = ChenCardinal(action.To - action.From);
            Vector3 aim = ChenAim(facing);
            ShowFormationGuardEffect(guard, "chen3_skill_03_start_01", instance, age, action.From, 23, direction: aim);
            ShowFormationGuardEffect(guard, "chen3_skill_03_start_02" + ChenDirectionSuffix(facing), instance,
                age, action.From, 24, direction: aim);
            if (Model.ShowcaseGuardVisualAction != action) return;
            var target = FormationFeedbackActor(action.TargetId);
            var contact = FormationFeedbackContactPoint(target, action.To, .52f);
            // Source S3 emits the three separate hit events at frames 16, 18 and 20 (30fps).
            for (int hit = 0; hit < 3; hit++)
                ShowFormationGuardEffect(guard, "chen3_skill_03_hit_0" + (hit + 1), instance,
                    (now - action.ContactAt) * action.Rate - hit * (2d / 30), action.To, 35,
                    point: contact, direction: aim);
        }

        void BuildFormationChenPath(FormationPresentationAction action, Vector2 direction)
        {
            formationChenPath.Clear();
            var position = action.From;
            double at = action.LaunchedAt;
            formationChenPath.Add(new OriginalNativeFxPathPoint(WorldPoint(position), at, ChenAim(direction)));
            // The archived controller sampled at .03s and turned clockwise at
            // the edge of a highland footprint. Keep both snapshots at a corner.
            while (at < action.EndAt - 1e-7)
            {
                double next = Math.Min(action.EndAt, at + .03);
                float distance = (float)(next - at) * ChenWaveSpeed;
                float beforeTurn = ChenTravelBeforeTurn(position, direction);
                position += direction * Mathf.Min(distance, beforeTurn);
                if (beforeTurn < distance - 1e-6f)
                {
                    Vector2 clockwise = new Vector2(direction.y, -direction.x);
                    if (ChenTravelBeforeTurn(position, clockwise) > 1e-6f ||
                        ChenTravelBeforeTurn(position, -direction) > 1e-6f ||
                        ChenTravelBeforeTurn(position, -clockwise) > 1e-6f)
                    {
                        double turnedAt = at + beforeTurn / ChenWaveSpeed;
                        formationChenPath.Add(new OriginalNativeFxPathPoint(WorldPoint(position), turnedAt, ChenAim(direction)));
                        direction = clockwise;
                        formationChenPath.Add(new OriginalNativeFxPathPoint(WorldPoint(position), turnedAt, ChenAim(direction), true));
                        position += direction * Mathf.Min(distance - beforeTurn, ChenTravelBeforeTurn(position, direction));
                    }
                }
                at = next;
                formationChenPath.Add(new OriginalNativeFxPathPoint(WorldPoint(position), at, ChenAim(direction)));
            }
        }

        float ChenTravelBeforeTurn(Vector2 position, Vector2 direction)
        {
            bool horizontal = direction.x != 0;
            float coordinate = horizontal ? position.x : position.y;
            float side = horizontal ? position.y : position.x;
            bool positive = (horizontal ? direction.x : direction.y) > 0;
            float low = horizontal ? FormationBattleModel.BattleAreaStart : -.5f;
            float high = (horizontal ? FormationBattleModel.Width : FormationBattleModel.Height) - .5f;
            float travel = positive ? high - coordinate - ChenWaveClearance : coordinate - low - ChenWaveClearance;
            if (Model.Terrain != null)
                foreach (var region in Model.Terrain.Regions)
                {
                    RectInt cells = region.cells;
                    int sideMin = horizontal ? cells.yMin : cells.xMin;
                    int sideMax = horizontal ? cells.yMax : cells.xMax;
                    if (side <= sideMin - .5f || side >= sideMax - .5f) continue;
                    low = (horizontal ? cells.xMin : cells.yMin) - .5f;
                    high = (horizontal ? cells.xMax : cells.yMax) - .5f;
                    if (positive ? coordinate >= high : coordinate <= low) continue;
                    travel = Mathf.Min(travel, positive ? low - coordinate - ChenWaveClearance : coordinate - high - ChenWaveClearance);
                }
            return Mathf.Max(0, travel);
        }

        OriginalNativeFxPathPoint FormationChenWavePoint(double now)
        {
            int lo = 0, hi = formationChenPath.Count;
            while (lo + 1 < hi)
            {
                int mid = (lo + hi) / 2;
                if (formationChenPath[mid].At <= now) lo = mid;
                else hi = mid;
            }
            var a = formationChenPath[lo];
            if (hi == formationChenPath.Count) return a;
            var b = formationChenPath[hi];
            return new OriginalNativeFxPathPoint(Vector3.Lerp(a.Position, b.Position, (float)((now - a.At) / (b.At - a.At))),
                now, a.Direction);
        }
    }
}
