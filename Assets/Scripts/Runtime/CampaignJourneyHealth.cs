using System;

namespace YesterdayConvoy
{
    // Short-campaign playtest values. Breaching the enemy gate is progress;
    // only the unmet fraction of its original defense costs challenge health.
    public static class CampaignJourneyHealth
    {
        public const int InitialHealth=100;
        public static int DamageLimit(int stage)
        {
            switch(stage)
            {
                case 1:case 4:case 8:case 11:return 0;
                case 2:case 3:return 20;
                case 5:case 7:return 30;
                case 6:return 25;
                case 9:return 45;
                case 10:return 35;
                case 12:return 60;
                default:throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        public static int CalculateLoss(int remainingDefense,int initialDefense,int stage)
        {
            if(initialDefense<=0)throw new ArgumentOutOfRangeException(nameof(initialDefense));
            if(remainingDefense<0||remainingDefense>initialDefense)throw new ArgumentOutOfRangeException(nameof(remainingDefense));
            // Integer ceiling keeps partial progress deterministic, including exact thirds.
            return (int)(((long)DamageLimit(stage)*remainingDefense+initialDefense-1)/initialDefense);
        }
    }
}
