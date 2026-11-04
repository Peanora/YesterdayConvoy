using System;
using UnityEngine;

namespace YesterdayConvoy
{
    // Thirteen encounters, including five guaranteed supply stops. No wall-clock income.
    public static class CampaignPlan
    {
        public const int Count=13;
        public static readonly int[] BaseGold={10,10,12,14,16,22,16,18,18,24,20,22,28};
        public static readonly int[] NaturalXP={2,2,2,3,3,4,3,4,4,5,5,6,0};
        public static readonly int[] XP={0,2,4,6,10,14,20,28,36,48,0};
        public static readonly int[] BattleDP={12,12,16,20,22,24,28,32,34,38,42,46,50};
        public static bool Supply(int stage)=>stage==1||stage==2||stage==5||stage==9||stage==12;
        public static bool Boss(int stage)=>stage==6||stage==10||stage==13;
        public static int Act(int stage)=>stage<=6?1:stage<=10?2:3;
        public static string Title(int stage)=>$"阶段{Act(stage)} · {stage}/{Count} · "+(Supply(stage)?"补给奖励关":Boss(stage)?"Boss防线":"突围战");
        public static int Uses(int cost)=>new[]{0,8,6,4,3,2}[cost];
        public static int DP(int cost,int baseDP,int star)=>baseDP+(star-1)*(cost==5?2:1);
        public static DefenderPlacement[] Guards(int stage,bool sandbox)
        {
            // Authored positions on the existing board; only encounter configuration changes.
            if(!sandbox&&Boss(stage))
            {
                var map=Resources.Load<DefenderTrialDefinition>("Maps/BattleShop");
                int[] picks=stage==6?new[]{1}:stage==10?new[]{1,2}:new[]{1,4,5};
                var bosses=new DefenderPlacement[picks.Length];
                for(int i=0;i<picks.Length;i++)
                {
                    var g=JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(map.defenders[picks[i]]));
                    g.health=stage==6?450:stage==10?700:950;g.attack=stage==6?6:stage==10?9:11;
                    bosses[i]=g;
                }
                return bosses;
            }
            var source=BattleShopStages.Defenders(1,false);
            int count=sandbox?2:Boss(stage)?2:1;
            var result=new DefenderPlacement[count];
            for(int i=0;i<count;i++)
            {
                var g=source[i];
                g.health=sandbox?950:90+stage*32+(Boss(stage)?180:0);
                g.attack=sandbox?5:7+stage*1.2f;g.armor=sandbox?8:Mathf.Min(18,stage);
                g.blocks=2;g.range=1.1f;g.interval=2;result[i]=g;
                if(sandbox)g.position=i==0?new Vector3(0,.04f,2.4f):new Vector3(.8f,.04f,2.1f);
            }
            return result;
        }
    }
}
