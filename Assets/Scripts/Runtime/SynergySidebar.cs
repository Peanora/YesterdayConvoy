using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class SynergySummary
    {
        public string Name {get;internal set;}
        public int Count {get;internal set;}
        public int Tier {get;internal set;}
        public int[] Thresholds {get;internal set;}
        public bool Implemented=>Thresholds.Length>0;
        public string Effect {get;internal set;}
        public string[] Members {get;internal set;}
        static string GuerrillaEffect()
        {
            var lines=new List<string>{"普通实体首次有效直接攻击/受击+1军令，每种卡最多2点；支援/分体不产点。所有档整令不限次数。"};
            foreach(int tier in new[]{2,4,6,8,9})
            {
                var rule=GuerrillaRules.ForTier(tier);
                string people=rule.ParticipantLimit==int.MaxValue?"全部合格普通实体":"最多"+rule.ParticipantLimit+"人";
                lines.Add($"{tier}档：整令{rule.Cost}点 / {people} / {Mathf.RoundToInt(rule.AttackMultiplier*100)}%攻击"+(rule.InitialOrders>0?$"，开战+{rule.InitialOrders}军令。":"。"));
            }
            lines.Add("0.9演出秒，对每名存活守军各结算一份参与者预算，逐名过护甲/抗性；不递归产点。");
            lines.Add("2/4/6需普攻收手且有射程内目标；8/9可在移动/普攻中参加，无需射程目标。技能、复起、控制锁照旧。");
            lines.Add("4档参与近战护盾10%生命3秒；6/8/9档15%生命5秒。");
            lines.Add("仅6档可拆两次3点半令，后半排除前半参与者；半令未完成不能插入整令。暂停或无合格单位/存活守军不扣费。");
            return string.Join("\n",lines);
        }
        public static SynergySummary[] From(IEnumerable<string> lineup,IEnumerable<string> extraMembers=null,IEnumerable<string> extraMudrockMembers=null)
        {
            var ids=lineup.Distinct().ToArray();
            var transfers=new HashSet<string>((extraMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.Guerrilla(id)));
            var mudrockTransfers=new HashSet<string>((extraMudrockMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.MudrockCrew(id)));
            string[] names={"整合运动","雪怪小队","感染生物","游击队","泥岩小队","牧群","深池","海嗣","萨卡兹"};
            int[][] levels={new[]{2,4,6},new[]{2,5},new[]{2,4,6},new[]{2,4,6,8,9},new[]{2,4,5,6},new[]{2,4},new[]{2,4},new[]{2,4},new[]{2,4,6}};
            string[] effects={
                "2档：部署2种不同卡触发1名援军，本场1次。\n4档：部署3种触发2名所选援军，本场2次。\n6档：部署3种触发士兵与弩手各1，本场3次；援军获15%生命护盾4秒。援军不扣蓝门防御。",
                "冻结守军在2.5秒内受到两种不同原卡直接命中时碎冰。\n2档：50法伤，半径0.9/最多2人；消耗冻结，目标冷却4秒。\n5档：120法伤，半径1.5/最多5人；保留冻结，目标冷却3秒。\n仅5档：3次碎冰或霜星手动冰封发动8秒暴风雪，开启清零寒意；每次碎冰只计1点，天气期间不积累、不叠加、不续时。\n0/2/4/6秒全场寒潮，每次(60+目标最大生命2%)法伤、过法抗；碎冰/寒潮按雪怪平均星级每升1星+35%。\n寒冷：攻速70%；再次受寒冷冻结1.2秒。天气冻结结束后保护3秒，只防控制、不免寒潮伤害。",
                "战前选主形态，6档再选另一种2档副形态。\n集群2/4/6：2只×55%、2只×70%、3只×55%生命/攻击；一次扣费，额外分体不扣蓝门。\n甲壳：18/30/35%生命护盾6秒，护甲+0/2/3。\n爆裂：原体死亡造成35/65/85法伤，半径1/1.2/1.3，最多2/3/3人；每升一星+35%。原有死亡爆炸独立保留并按自身范围/伤害类型结算；额外爆裂只算一次，不重复寒冷。",
                GuerrillaEffect(),
                "2/4/5/6档：按战前编队的不同种类与总星级S确定培养上限。\n2种可养至II；4种且S≥7至III；5种且S≥10至IV；6种且S≥12至V。每种只算最高星，备战不计；可免费转职一种非原生成员。\nI/II/III/IV/V累计经验门槛0/6/16/30/48。每场实际战斗结束自动获得：档位＋⌊S/2⌋经验，胜负都结算；补给与中途退出不发。达到当前上限停止获得，不预存未来经验，历史经验不扣。\n生命360/550/1800/6000/7500，攻击60/84/240/1600/2000，DP4/5/8/14/14。每场一次、不扣门血；IV/V至多3目标、每第三击全场80%攻击法伤、每次命中回8%生命一次、免疫控制。",
                "牧群：全队非五费正常实体最终死亡后生成短时转化体。2档前3个、35%生命/攻击、4甲、10秒；4档前5个、45%生命/攻击、6甲、12秒，可保留远程基础攻击（2.5射程、攻击再乘85%）。\n转化沿原位置/路线，穿门0，不复制主动、爆炸、羁绊资源或再次转化。原动作较长时攻击间隔保留完整动作。",
                "深池：被灼烧守军遭正常伤害击杀留下火种。2档最多4枚，传播半径1.2、最多2人、15法伤/秒持续3秒；4档最多6枚、半径1.6、3人、25/秒。\n4档可消耗2火种手动引爆，半径1.8、最多4人、120法伤，每场2次。伤害每成员平均星级增加25%。衍生击杀不再产火种。",
                "海嗣 · 溟痕蔓延：每种海嗣本场首次有效输出或外来承伤，在脚下落种一次；重复部署、无敌、自损和支援不重复落种。\n2/4档：每6/4秒向相邻通行地面扩散一圈，全场覆盖至多8/16格；为尚未接敌的每种海嗣预留1格，高台不铺。\n海嗣站在溟痕上，原生神经损伤×1.5/2；地面守军踩溟痕每秒积累3点神经损伤，该地面积累不再乘倍率。壳海负责前锋落种，不新增神经普攻。\n神经积累100时爆发：100真实伤害、10秒眩晕，其间不再积累。无存活海嗣时溟痕休眠，后续部署恢复；战后清空。\n战斗中点击「查看溟痕范围」突出边界；守军神经条和海嗣×倍率标识显示真实状态。",
                "萨卡兹：战前指定承契卡，其余普通友军首次由直接伤害跨入40%生命以下贡献1点，每种卡最多2点。4点手动选择存活承契实体爆发，不扣DP。\n2/4/6档：攻击+20/30/40%、15/25/30%生命盾，持续6/8/10秒，本场1/2/2次。6档其他普通友军各获承契者10%生命盾、最多200。\n自损、转化体、巨像和援军不贡献；同一实体只贡献一次。"};
            return names.Select((name,i)=>new SynergySummary{Name=name,Members=ids.Where(id=>CampaignBalance.Tags(id).Contains(name)||name=="游击队"&&transfers.Contains(id)||name=="泥岩小队"&&mudrockTransfers.Contains(id)).ToArray(),Thresholds=levels[i],Effect=effects[i]})
                .Select(s=>{s.Count=s.Members.Length;s.Tier=s.Name=="游击队"?GuerrillaRules.TierForCount(s.Count):s.Thresholds.Where(n=>s.Count>=n).DefaultIfEmpty(0).Max();return s;})
                .Where(s=>s.Count>0).OrderByDescending(s=>s.Tier>0).ThenByDescending(s=>s.Count).ToArray();
        }
    }
    public sealed partial class ManualBattleController
    {
        string inspectedSynergy;
        Vector2 guerrillaDetailScroll;
        Rect SynergyListRect=>new Rect(16,69,157,28+Mathf.Min(350,CurrentSynergies.Length*46));
        Rect SynergyDetailRect=>inspectedSynergy=="感染生物"?InfectionFormPanelRect:new Rect(252,90,620,415);
        SynergySummary[] CurrentSynergies=>SynergySummary.From(shopLineup,PreparedGuerrillaTransferIds,PreparedMudrockTransferIds);
        bool SynergyBlocksMap(Vector2 point)=>CampaignActive&&(SynergyListRect.Contains(point)||inspectedSynergy!=null);
        void DrawSynergySidebar()
        {
            if(!CampaignActive)return;
            DrawSnowstormFeedback();DrawMarineFieldFeedback();
            var summaries=CurrentSynergies;var bounds=SynergyListRect;
            Fill(new Rect(bounds.x,bounds.y,bounds.width,25),ApprovedUi.Paper);
            GUI.Label(new Rect(bounds.x+8,bounds.y,55,25),"羁绊",CampaignText(12,weight:600));
            GUI.Label(new Rect(bounds.x+58,bounds.y,91,25),summaries.Count(s=>s.Tier>0)+"条已激活",CampaignText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
            if(summaries.Length==0){inspectedSynergy=null;return;}
            Rect viewport=new Rect(bounds.x,bounds.y+28,bounds.width,bounds.height-28);
            float contentWidth=summaries.Length*46>viewport.height?139:157;
            campaignSynergyScroll=GUI.BeginScrollView(viewport,campaignSynergyScroll,new Rect(0,0,contentWidth,Mathf.Max(1,summaries.Length*46)),false,false);
            try
            {
                for(int i=0;i<summaries.Length;i++)
                {
                    var s=summaries[i];Rect row=new Rect(0,i*46,contentWidth,41);
                    if(GUI.Button(row,GUIContent.none,GUIStyle.none))
                    {
                        if(s.Name=="泥岩小队")OpenGolemTrainingPanel();
                        else {inspectedSynergy=inspectedSynergy==s.Name?null:s.Name;campaignConstructOpen=false;CancelShopDrag();}
                    }
                    bool active=s.Tier>0,snowstorm=s.Name=="雪怪小队"&&SnowstormHudActive;
                    Color ink=snowstorm?SnowstormInk:active?ApprovedUi.Ink:ApprovedUi.Muted;
                    Fill(row,snowstorm?SnowstormPaper:active?ApprovedUi.Paper:new Color(.85f,.89f,.91f,.89f));
                    Fill(new Rect(row.x,row.y,3,row.height),snowstorm?SnowstormBlue:active?(s.Name=="感染生物"?InfectionOrange:new Color(.72f,.59f,.30f)):ApprovedUi.Rule);
                    GUI.Label(new Rect(9,row.y+3,contentWidth-43,21),s.Name+(s.Name=="泥岩小队"||s.Name=="感染生物"&&InfectionChoiceEditable?" ›":""),CampaignText(13,color:ink,weight:600));
                    GUI.Label(new Rect(contentWidth-31,row.y+1,23,23),s.Count.ToString(),CampaignText(15,TextAnchor.MiddleRight,ink,600));
                    if(s.Name=="感染生物")DrawInfectionFormRow(row,s.Tier);
                    else if(s.Name=="泥岩小队")DrawGolemSidebarProgress(row);
                    else if(s.Name=="海嗣"&&s.Tier>0)DrawMarineRow(row);
                    else if(snowstorm)DrawSnowstormRow(row);
                    else if(s.Name=="雪怪小队"&&SnowstormChargeHudVisible)DrawSnowstormChargeRow(row);
                    else
                    {
                        float pitch=s.Thresholds.Length>3?12:22,width=s.Thresholds.Length>3?10:19;
                        for(int t=0;t<s.Thresholds.Length;t++)Fill(new Rect(10+t*pitch,row.y+31,width,2),s.Count>=s.Thresholds[t]?new Color(.66f,.53f,.23f):new Color(.68f,.75f,.80f));
                        string target=s.Tier>0?s.Tier+"档":s.Implemented?s.Count+"/"+s.Thresholds[0]:"待实现";
                        GUI.Label(new Rect(contentWidth-62,row.y+22,54,16),target,CampaignText(10,TextAnchor.MiddleRight,ApprovedUi.Muted));
                    }
                }
            }
            finally {GUI.EndScrollView();}
            var detail=summaries.FirstOrDefault(s=>s.Name==inspectedSynergy);if(detail==null){inspectedSynergy=null;return;}
            if(detail.Name=="感染生物"){DrawInfectionFormPanel(detail);return;}
            Fill(SynergyDetailRect,ApprovedUi.Paper);ApprovedUi.Border(SynergyDetailRect,ApprovedUi.Rule);
            GUI.Label(new Rect(268,102,510,28),$"{detail.Name} · {detail.Count}种 · "+(detail.Tier>0?detail.Tier+"档已激活":detail.Implemented?"未达门槛":"待实现"),CampaignText(17,weight:700));
            if(LoopButton(new Rect(803,102,53,28),"关闭")){inspectedSynergy=null;return;}
            var body=CampaignText(14,TextAnchor.UpperLeft);body.wordWrap=true;
            if(detail.Name=="游击队")
            {
                var detailViewport=new Rect(268,149,588,269);float content=body.CalcHeight(new GUIContent(detail.Effect),detailViewport.width-18)+8;
                guerrillaDetailScroll=GUI.BeginScrollView(detailViewport,guerrillaDetailScroll,new Rect(0,0,detailViewport.width-18,Mathf.Max(detailViewport.height,content)),false,false);
                try{GUI.Label(new Rect(0,0,detailViewport.width-18,content),detail.Effect,body);}
                finally{GUI.EndScrollView();}
            }
            else GUI.Label(new Rect(268,149,588,269),detail.Effect,body);
            string members=string.Join("、",detail.Members.Select(id=>shop.Definition(id).Name+((detail.Name=="游击队"&&IsGuerrillaTransfer(id)||detail.Name=="泥岩小队"&&IsMudrockTransfer(id))?"（转职）":"")));
            Fill(new Rect(268,430,588,1),ApprovedUi.Rule);
            var memberStyle=CampaignText(12,TextAnchor.UpperLeft,ApprovedUi.Muted);memberStyle.wordWrap=true;
            GUI.Label(new Rect(268,441,588,54),"编队成员："+members+"\n按战前编队锁定；未部署或死亡不改变档位。",memberStyle);
        }
    }
}
