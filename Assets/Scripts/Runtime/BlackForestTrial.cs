using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        void DrawBlackForestRouteLabels(GUIStyle label)
        {
            DrawWaveRoutes();
        }
        public IEnumerator PrepareBlackForest(DefenderTrialDefinition encounter)
        {
            if(encounter==null||encounter.mapId!=BlackForestLayout.Id)throw new InvalidOperationException("Missing authored forest encounter");
            var ids=BlackForestChallengePlan.StartingIds.Concat(new[]{"CARD_M07","CARD_M08"}).Distinct().ToArray();
            if(ids.Except(monsterTests.Where(o=>o.Playable).Select(o=>o.Id)).Any())throw new InvalidOperationException("A forest starter is missing playable art.");
            var cards=monsterTests.Where(o=>o.Playable&&ids.Contains(o.Id)).Select(o=>o.Definition.Card);
            return Prepare(Plan(encounter.defenders,cards,true),()=>{
                StartCampaign(true,encounterOverride:encounter);
                ShopNotice("波次进攻：整备时查看三波入口与预算；开始战斗后点击卡牌部署，次数与冷却跨波保留");
            },"准备树海六星防线");
        }
    }
}
