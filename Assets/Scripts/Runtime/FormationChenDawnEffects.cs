using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const string ChenWaveKey = "defender/D32/chen3_skill_03_trail_01_dir_right";
        readonly Dictionary<int, (FormationBattleModel.ChenDawnCast Cast, List<OriginalNativeFxPathPoint> Path)> formationChenPaths =
            new Dictionary<int, (FormationBattleModel.ChenDawnCast, List<OriginalNativeFxPathPoint>)>();

        static Vector3 ChenAim(Vector2 direction) => new Vector3(direction.x, 0, direction.y);

        static string ChenDirectionSuffix(Vector2 direction) => direction.x < 0 ? "_dir_left" : direction.x > 0
            ? "_dir_right" : direction.y > 0 ? "_dir_up" : "_dir_down";

        void PresentFormationChenFields(FormationDefender guard)
        {
            var cast = Model.GuardChenDawnCast(guard);
            if (cast == null) return;
            var action = cast.Action;
            double now = Model.Clock;
            if (now > cast.Until + 2) return;
            Vector2 facing = cast.Path[0].Direction;
            Vector3 aim = ChenAim(facing);
            string instance = "dawn-skill/" + action.Sequence;
            if (now < cast.Until)
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
            if (!formationChenPaths.TryGetValue(guard.Id, out var cached) || cached.Cast != cast)
            {
                var path = new List<OriginalNativeFxPathPoint>(cast.Path.Count);
                foreach (var point in cast.Path)
                {
                    bool turned = path.Count > 0 && path[path.Count - 1].Direction != ChenAim(point.Direction);
                    path.Add(new OriginalNativeFxPathPoint(WorldPoint(point.Position), point.At, ChenAim(point.Direction), turned));
                }
                cached = (cast, path);
                formationChenPaths[guard.Id] = cached;
            }
            var sample = cast.WaveAt(now);
            var pose = new OriginalNativeFxPose(WorldPoint(sample.Position), Quaternion.Euler(90, 0, 0), ChenAim(sample.Direction))
            {
                FormationSortingOrder = LegacyOrder(sample.Position, 33)
            };
            formationSixStarFx.TryShowPath(ChenWaveKey, instance, now - action.LaunchedAt, cached.Path,
                pose, true, cast.Until - action.LaunchedAt);
        }

        void PresentFormationChenSlash(FormationDefender guard, FormationPresentationAction action, string instance, double age)
        {
            Vector2 facing = Model.GuardChenDawnCast(guard)?.Path[0].Direction ?? guard.Facing;
            Vector3 aim = ChenAim(facing);
            ShowFormationGuardEffect(guard, "chen3_skill_03_start_01", instance, age, action.From, 23, direction: aim);
            ShowFormationGuardEffect(guard, "chen3_skill_03_start_02" + ChenDirectionSuffix(facing), instance,
                age, action.From, 24, direction: aim);
        }
    }
}
