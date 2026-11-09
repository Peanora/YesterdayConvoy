using System;

namespace YesterdayConvoy
{
    // Short-campaign adaptation of Currency Wars' income/interest/XP structure.
    // These starting funds and XP thresholds are our tuning values, not claimed
    // to be the original game's unverified opening or base-income table.
    public static class CampaignJourneyEconomy
    {
        public const int InitialGold=5;
        static readonly int[] LevelExperience={0,2,4,6,8,10,12,20,28,48,0};
        public static int RequiredXP(int level)
        {
            if(level<1||level>10)throw new ArgumentOutOfRangeException(nameof(level));
            return LevelExperience[level];
        }
        public static int StreakBonus(int wins)=>wins>=5?3:wins>=3?2:wins>=2?1:0;
    }
}
