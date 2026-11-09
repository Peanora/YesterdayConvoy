using System;
using System.Collections.Generic;
using System.Linq;

namespace YesterdayConvoy
{
    // Short-campaign adaptation of Currency Wars' income/interest/XP structure.
    // These starting funds and XP thresholds are our tuning values, not claimed
    // to be the original game's unverified opening or base-income table.
    public static class CampaignJourneyEconomy
    {
        public const int InitialGold=5;
        // Journey base rewards are already doubled in the authored stage data.
        // Apply this only to settlement interest and combat streak gold.
        public const int RewardGoldMultiplier=2;
        public static string[] StartingCards(IEnumerable<ShopTrialModel.Role> roles,int seed)
        {
            var candidates=roles.Where(r=>r.Cost>=1&&r.Cost<=2)
                .Select(r=>r.Id).Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            if(candidates.Length<3)throw new InvalidOperationException("Three distinct low-cost starting roles are required.");
            var random=new Random(seed);
            for(int i=0;i<3;i++){int next=random.Next(i,candidates.Length);string id=candidates[i];candidates[i]=candidates[next];candidates[next]=id;}
            return candidates.Take(3).ToArray();
        }
        static readonly int[] LevelExperience={0,2,4,6,8,10,12,20,28,48,0};
        public static int RequiredXP(int level)
        {
            if(level<1||level>10)throw new ArgumentOutOfRangeException(nameof(level));
            return LevelExperience[level];
        }
        public static int StreakBonus(int wins)=>wins>=5?3:wins>=3?2:wins>=2?1:0;
    }
}
