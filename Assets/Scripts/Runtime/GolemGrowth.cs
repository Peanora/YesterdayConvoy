using System;
namespace YesterdayConvoy
{
    // Shared by preparation, combat snapshots and settlement. Historical XP never decreases.
    public static class GolemGrowth
    {
        static readonly int[] thresholds={0,0,6,16,30,48};
        public static int TierForCount(int crew)
        {
            if(crew<0)throw new ArgumentOutOfRangeException(nameof(crew));
            return crew>=6?6:crew>=5?5:crew>=4?4:crew>=2?2:0;
        }
        public static int Capacity(int crew,int totalStars)
        {
            if(crew<0||totalStars<0)throw new ArgumentOutOfRangeException();
            if(crew>=6&&totalStars>=12)return 5;
            if(crew>=5&&totalStars>=10)return 4;
            if(crew>=4&&totalStars>=7)return 3;
            if(crew>=2)return 2;
            return 1;
        }
        public static int Threshold(int stage)
        {
            if(stage<1||stage>5)throw new ArgumentOutOfRangeException(nameof(stage));
            return thresholds[stage];
        }
        public static int Stage(int experience,int capacity)
        {
            if(experience<0)throw new ArgumentOutOfRangeException(nameof(experience));
            Threshold(capacity);
            for(int stage=capacity;stage>1;stage--)if(experience>=Threshold(stage))return stage;
            return 1;
        }
        public static int BattleXP(int tier,int totalStars)
        {
            if(totalStars<0||tier!=0&&tier!=2&&tier!=4&&tier!=5&&tier!=6)throw new ArgumentOutOfRangeException();
            return tier==0?0:tier+totalStars/2;
        }
        public static int Award(int currentXP,int capacity,int rawXP)
        {
            if(currentXP<0||rawXP<0)throw new ArgumentOutOfRangeException();
            int remaining=Math.Max(0,Threshold(capacity)-currentXP);
            return Math.Min(rawXP,remaining);
        }
        public static int Cost(int stage){Threshold(stage);return new[]{0,4,5,8,14,14}[stage];}
        public static string Requirements(int stage)=>stage==1?"II：经验6 · 至少2名成员":stage==2?"III：经验16 · 至少4名成员、总星级7":stage==3?"IV：经验30 · 至少5名成员、总星级10":stage==4?"V：经验48 · 至少6名成员、总星级12":"V 终极巨像已解锁";
    }
    public sealed partial class ManualBattle
    {
        public bool UnitControlImmune(DeployedUnitState u)=>PatriotControlImmune(u)||u!=null&&u.Golem&&GolemStage>=4;
    }
}
