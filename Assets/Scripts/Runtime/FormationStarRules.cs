using System;
namespace YesterdayConvoy
{
    public static partial class FormationCampaignRules
    {
        internal static bool IsThreeStarFiveCost(UnitDefinition unit, int star, int talulahCost) =>
            unit != null && star == 3 && (unit.Id == "TAL" ? talulahCost == 5 : unit.Cost == 5);
        internal static float AttackStarMultiplier(UnitDefinition unit, int star, int talulahCost) =>
            IsThreeStarFiveCost(unit,star,talulahCost) ? 10 : AttackStarMultiplier(star);

        internal static float AttackStarMultiplier(int star) => star switch
        {
            2 => 1.8f,
            3 => 3.24f,
            4 => 5.832f,
            _ => 1f,
        };

        internal static float BaseHealthAtStar(UnitDefinition unit, int star, int talulahCost)
        {
            float health = unit.Id == "TAL" ? talulahCost switch
            {
                4 => 1010f,
                5 => 2800f,
                _ => unit.Health,
            } : unit.Health;
            if (IsThreeStarFiveCost(unit, star, talulahCost)) return health * 18;
            return health * (star switch
            {
                2 => 1.8f,
                3 => 3.4f,
                4 => 4.8f,
                _ => 1f,
            });
        }

    }
}
