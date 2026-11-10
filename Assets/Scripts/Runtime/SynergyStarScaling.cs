using System;

namespace YesterdayConvoy
{
    // Every selected kind contributes all its stars. The active threshold, not the
    // current member count, is the denominator, so adding a one-star member cannot dilute a tier.
    public static class SynergyStarScaling
    {
        public static float Reunion(int totalStars,int tier)=>Scale(totalStars,tier,.5);
        public static float Snow(int totalStars,int tier)=>Scale(totalStars,tier,.4);
        public static float Deep(int totalStars,int tier)=>Scale(totalStars,tier,1d/3);

        static float Scale(int totalStars,int tier,double exponent)
        {
            if(totalStars<0)throw new ArgumentOutOfRangeException(nameof(totalStars));
            if(tier<0)throw new ArgumentOutOfRangeException(nameof(tier));
            if(tier==0)return 1;
            if(totalStars<tier)throw new ArgumentOutOfRangeException(nameof(totalStars));
            return (float)Math.Pow((double)totalStars/tier,exponent);
        }
    }
}
