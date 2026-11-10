using System;

namespace YesterdayConvoy
{
    public static class WaveDeploymentCadence
    {
        public static float ForCard(string cardId,int recruitCost)
        {
            if(recruitCost<1||recruitCost>5)throw new ArgumentOutOfRangeException(nameof(recruitCost));
            switch(cardId)
            {
                case "CARD_M04":return 14;
                case "CARD_M06":return 16;
                case "CARD_M17":return 20;
                case "CARD_M38":return 24;
                default:return new[]{0,6,10,16,24,45}[recruitCost];
            }
        }
    }
}
