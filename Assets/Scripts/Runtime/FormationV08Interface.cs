using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        int v08SuiRow=-1;
        static readonly string[] IberiaNames={"赎罪","狂信","审判"};
        static readonly string[] IberiaEffects={"全队每秒回复2%自身最大生命","全队攻速+45%","本体直接伤害后处决：普通12%，精英/首领5%，严格低于阈值"};
        DecisionRequest BuildV08IberiaDecision(FormationCampaignRun run,int count)
        {
            var pairs=Enumerable.Range(0,3).SelectMany(first=>count>=4?Enumerable.Range(0,3).Where(second=>second!=first).Select(second=>(first,second)):new[]{(first,(first+1)%3)}).ToArray();
            var options=pairs.Select(pair=>new DecisionOption{Name=IberiaNames[pair.Item1]+(count>=4?" + "+IberiaNames[pair.Item2]:""),Description=IberiaEffects[pair.Item1]+(count>=4?"；"+IberiaEffects[pair.Item2]:""),Icon="pact",Current=run.IberiaFirst==pair.Item1&&(count<4||run.IberiaSecond==pair.Item2)}).ToArray();
            return TacticalOptionRequest("v08-iberia","伊比利亚 · 选择本场誓约","开战锁定，每场整备可重选。",options,index=>run.SelectIberia(pairs[index].Item1,pairs[index].Item2));
        }
        DecisionRequest BuildV08TradeDecision(FormationCampaignRun run)
        {
            var names=new[]{"基础件自选1件","8 XP","4次免费刷新"};
            var options=names.Select(name=>new DecisionOption{Name=name,Outcome="支付8金币",Disabled=!run.MilitaryTradeAvailable||!Shop.UnlimitedGold&&Shop.Gold<8,Icon="mark"}).ToArray();
            return TacticalOptionRequest("v08-trade","行商军需 · 本位面一次","出售重购或重试均不能恢复次数。",options,index=>run.MilitaryTrade(index));
        }
        DecisionRequest BuildV08SuiDecision(FormationCampaignRun run)
        {
            if(v08SuiRow<0)
            {
                var rows=Enumerable.Range(0,FormationBattleModel.Height).Select(row=>new DecisionOption{Name=$"第{row+1}行",Outcome="选择后指定列",Icon="mark"}).ToArray();
                return new DecisionRequest{Key="tactical:v08-sui-row",Title="画境中心 · 选行",Subtitle="我方部署区；后排画境可保护支援队伍。",Navigation=true,Options=rows,Back=true,Confirm=index=>{v08SuiRow=index;return true;},Close=()=>tacticalDecisionPage=null};
            }
            int rowIndex=v08SuiRow;
            var cells=Enumerable.Range(0,FormationBattleModel.DeploymentColumns).Select(col=>new Vector2Int(col,rowIndex)).ToArray();
            var options=cells.Select(cell=>new DecisionOption{Name=$"{cell.x+1}列 / {cell.y+1}行",Icon="mark",Current=run.SuiCentre==cell,Disabled=Model.Terrain!=null&&!Model.Terrain.IsWalkable(cell)}).ToArray();
            return new DecisionRequest{Key="tactical:v08-sui-cell:"+rowIndex,Title="画境中心 · 选列",Options=options,Back=true,Confirm=index=>{if((uint)index>=(uint)cells.Length||options[index].Disabled||!run.SelectSuiCentre(cells[index]))return false;v08SuiRow=-1;tacticalDecisionPage=null;RefreshCampaignPreparation();return true;},Close=()=>v08SuiRow=-1};
        }
        DecisionRequest BuildV08PopulationDecision(FormationCampaignRun run) => TacticalOptionRequest("v08-population","人口来源","人口上限14；商店等级仍最高10。",new[]{new DecisionOption{Name=$"等级 {Shop.Level}",Description=$"战备 +{(run.HasAugment("CP1")?1:0)}；冠冕装备 +{run.CrownPopulation}；实际人口 {Shop.PopulationCap}",Icon="mark"}},index=>{});
    }
}
