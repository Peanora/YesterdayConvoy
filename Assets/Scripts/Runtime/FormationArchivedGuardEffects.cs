using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        // Outline of Angelina's nineteen archived attack cells in the saved facing basis.
        static readonly Vector2[] AngelinaPerimeter =
        {
            new Vector2(-.5f, -2.5f), new Vector2(2.5f, -2.5f), new Vector2(2.5f, -1.5f),
            new Vector2(3.5f, -1.5f), new Vector2(3.5f, -.5f), new Vector2(4.5f, -.5f),
            new Vector2(4.5f, .5f), new Vector2(3.5f, .5f), new Vector2(3.5f, 1.5f),
            new Vector2(2.5f, 1.5f), new Vector2(2.5f, 2.5f), new Vector2(-.5f, 2.5f),
            new Vector2(-.5f, -2.5f)
        };
        readonly Vector3[] angelinaBoundary = new Vector3[AngelinaPerimeter.Length];

        void PresentArchivedGuardFields(FormationDefender guard)
        {
            if (!Model.GuardSkillActive(guard)) return;
            double started = Model.GuardSignatureStart(guard);
            double age = (Model.Clock - started) * FormationVisualRate;
            string instance = "archive/" + FxTime(started);
            Vector2 facing = Model.ArchivedGuardFacing(guard);
            var direction = new Vector3(facing.x, 0, facing.y);
            bool left = Vector3.Dot(direction, battleCamera.transform.right) < 0;
            switch (guard.ArtId)
            {
                case "D33":
                    Field("aglna2_skill_03_range_01", 11);
                    Field("aglna2_skill_03_buff_weapon_01", 20);
                    Field(left ? "aglna2_skill_03_buff_left_01" : "aglna2_skill_03_buff_right_01", 22);
                    const string line = "defender/D33/aglna2_skill_03_range_line_01";
                    var anchor = ActorWorldPoint(guard.Position) + Vector3.up * .025f;
                    var side = new Vector3(-direction.z, 0, direction.x);
                    for (int i = 0; i < angelinaBoundary.Length; i++)
                    {
                        Vector2 vertex = AngelinaPerimeter[i];
                        angelinaBoundary[i] = anchor + direction * vertex.x + side * vertex.y;
                    }
                    var pose = new OriginalNativeFxPose(anchor, Quaternion.Euler(90, 0, 0), direction)
                        { FormationSortingOrder = LegacyOrder(guard.Position, 11) };
                    if (FormationPointVisibleInView(guard.Position))
                        formationSixStarFx.TrySetLine(line, guard.Id + "/" + instance + "/range_line", age, angelinaBoundary, pose, true);
                    break;
                case "D34":
                    Field(left ? "clemnt_skill_03_buff_01_l" : "clemnt_skill_03_buff_01_r", 18);
                    Field("clemnt_skill_03_buff_02", 22);
                    Field("clemnt_skill_03_range", 12);
                    break;
                case "D35":
                    Field("kalts2_skill_02_buff_01", 22);
                    break;
            }

            void Field(string clip, int order) => ShowFormationGuardEffect(guard, clip, instance, age, guard.Position,
                order, loop: true, direction: direction);
        }

        void PresentArchivedGuardAction(FormationDefender guard, FormationPresentationAction action, double now)
        {
            string instance = "archive_action/" + action.Sequence;
            double age = (now - action.At) * action.Rate;
            var direction = ActorWorldPoint(action.To) - ActorWorldPoint(action.From);
            bool left = Vector3.Dot(direction, battleCamera.transform.right) < 0;
            bool skill = action.Animation == "skill_attack";
            switch (guard.ArtId)
            {
                case "D33" when skill:
                    Cast("aglna2_skill_03_start_01");
                    Cast("aglna2_skill_03_start_02");
                    break;
                case "D33" when action.Animation == "attack":
                    Cast("aglna2_attack_start_01");
                    break;
                case "D34" when skill || action.Animation == "attack":
                    string suffix = Mathf.Abs(direction.z) > Mathf.Abs(direction.x)
                        ? direction.z < 0 ? "_down" : skill ? "_up" : "" : "";
                    Cast((skill ? "clemnt_skill_03_start_01" : "clemnt_attack_01_start_01") + suffix);
                    break;
                case "D34" when action.Animation == "skill_end":
                    Vector2 facing = Model.ArchivedGuardFacing(guard);
                    ShowFormationGuardEffect(guard, "clemnt_skill_03_range_end", instance, age, action.From, 12,
                        direction: new Vector3(facing.x, 0, facing.y));
                    break;
                case "D34" when action.Animation == "corrosion_burst" && now < action.EndAt:
                    ShowFormationGuardEffect(guard, "clemnt_skill_03_mark_all_01", instance, age, action.To, 16, loop: true);
                    break;
                case "D35" when skill || action.Animation == "medicine_detonation":
                    Cast("folnic_skill_02_start");
                    Cast(action.Heal ? "kalts2_skill_02_start_heal_01" : "kalts2_skill_02_start_damage_01");
                    if (action.Animation == "medicine_detonation")
                    {
                        string main = action.Heal ? "heal_01" : "damage_01";
                        string opposite = action.Heal ? "damage_01" : "heal_01";
                        Land("kalts2_skill_02_range_" + main, 14);
                        Land("kalts2_skill_02_indicator_" + main, 16);
                        Land("kalts2_skill_02_range_" + opposite + "_delay", 15);
                    }
                    break;
                case "D35" when action.Animation == "attack":
                    Cast("kalts2_skill_01_start_01");
                    break;
            }

            void Cast(string clip) => ShowFormationGuardEffect(guard, clip, instance, age, action.From, 23,
                point: LegacyOwnerPoint(guard, action.From), direction: direction, flip: left);
            void Land(string clip, int order) => ShowFormationGuardEffect(guard, clip, instance,
                (now - action.ContactAt) * action.Rate, action.To, order, flat: true);
        }
    }
}
