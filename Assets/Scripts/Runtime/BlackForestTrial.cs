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
            if(Battle.WavesEnabled){DrawWaveRoutes();return;}
            DeploymentLane selected=null;
            if(shopDrag>=0&&shopMoving&&PointerGround(ScreenPosition(shopPointer),out var world))selected=Battle.FindEntryLane(world);
            if(selected==null)return;
            Color color=new Color(1,.77f,.34f);
            for(int i=1;i<selected.Points.Count;i++){
                Vector2 a=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[i-1]+Vector3.up*.04f)),b=GUIPosition(Camera.main.WorldToScreenPoint(selected.Points[i]+Vector3.up*.04f));
                ShopLine(a,b,color,2);Vector2 mid=Vector2.Lerp(a,b,.55f),direction=(b-a).normalized,wing=new Vector2(-direction.y,direction.x)*4;
                ShopLine(mid-direction*7+wing,mid,color);ShopLine(mid-direction*7-wing,mid,color);
            }
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
