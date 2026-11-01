using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    // Reproducible real combat simulation; no damage/health shortcuts.
    public static class StrongholdChallengePlan
    {
        [Serializable]public sealed class Order {public float time;public string card;public int lane;}
        [Serializable]public sealed class Plan {public Order[] orders;}
        public static ManualBattle Create(string[] cards)
        {
            var map=StrongholdChallengeLayout.Create(Resources.Load<DefenderTrialDefinition>("MapTrials/city"));
            var data=new[]{"M08"}.Concat(cards).Distinct().Select(id=>Resources.Load<ManualBattleDefinition>("MonsterTests/"+id)).ToArray();
            var first=data[0];var b=new ManualBattle(StrongholdChallengeLayout.Settings(first.Snapshot()),map.route,first.Engagement.DefenderPosition,first.Engagement.Snapshot(),first.Engagement.ExtraDefenders,map.Snapshot(),map.LaneSnapshot());
            b.ConfigureCardLoadout(data.Skip(1).Select(d=>new BasicRosterProfile(StrongholdChallengeLayout.Settings(d.Snapshot()),d.Engagement.Snapshot())));
            b.ConfigureStrongholdMap(true);foreach(var d in b.Defenders)b.ConfigureDefenderHighGround(d.EntityId,d.Position.y>.5f);
            UnityEngine.Object.DestroyImmediate(map);return b;
        }
        public static ManualBattle Run(Plan plan,float maxTime=240)
        {
            var b=Create(plan.orders.Select(o=>o.card.Replace("CARD_","")).Distinct().ToArray());b.Begin();int index=0;long request=0;
            for(int step=0;step<maxTime*10&&b.Phase==BattlePhase.Running;step++)
            {
                while(index<plan.orders.Length&&plan.orders[index].time<=b.Clock+.001)
                {
                    var order=plan.orders[index++];b.TryDeployRole(b.RunId,++request,b.Lanes[order.lane].Entry,order.card);
                }
                b.Advance(.1f);
                if(index==plan.orders.Length&&b.ActiveCount==0)break;
            }
            return b;
        }
    }
}
