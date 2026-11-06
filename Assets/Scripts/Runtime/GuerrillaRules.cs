using System;

namespace YesterdayConvoy
{
    /// <summary>Shared military-order values. Uses remain limited by earned orders, never a per-battle cast cap.</summary>
    public static class GuerrillaRules
    {
        public readonly struct Rule
        {
            public readonly int Tier,Cost,InitialOrders,ParticipantLimit;
            public readonly float AttackMultiplier;
            public readonly bool AllowsHalf,ExpandedEligibility;
            internal Rule(int tier,int cost,int initial,int participants,float attack,bool half=false,bool expanded=false)
            {Tier=tier;Cost=cost;InitialOrders=initial;ParticipantLimit=participants;AttackMultiplier=attack;AllowsHalf=half;ExpandedEligibility=expanded;}
        }
        public static int TierForCount(int distinctMembers)
        {
            if(distinctMembers<0)throw new ArgumentOutOfRangeException(nameof(distinctMembers));
            return distinctMembers>=9?9:distinctMembers>=8?8:distinctMembers>=6?6:distinctMembers>=4?4:distinctMembers>=2?2:0;
        }
        public static Rule ForTier(int tier)
        {
            switch(tier)
            {
                case 0:return default;
                case 2:return new Rule(2,4,0,6,.6f);
                case 4:return new Rule(4,5,0,8,.8f);
                case 6:return new Rule(6,6,0,12,.9f,true);
                case 8:return new Rule(8,3,2,int.MaxValue,1.8f,expanded:true);
                case 9:return new Rule(9,2,2,int.MaxValue,3,expanded:true);
                default:throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }
    }
}
