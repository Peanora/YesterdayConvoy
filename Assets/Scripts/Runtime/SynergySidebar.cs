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
            var lines=new List<string>{"普通实体首次有效直接攻击/受击+1军令，每种卡最多2点；支援/分体不产点。达到本档点数且有合格出手者时自动整令，不限次数；演出结束后才能再次触发。"};
            foreach(int tier in new[]{2,4,6,8,9})
            {
                var rule=GuerrillaRules.ForTier(tier);
                string people=rule.ParticipantLimit==int.MaxValue?"全部合格普通实体":"最多"+rule.ParticipantLimit+"人";
                lines.Add($"{tier}档：整令{rule.Cost}点 / {people} / {Mathf.RoundToInt(rule.AttackMultiplier*100)}%攻击"+(rule.InitialOrders>0?$"，开战+{rule.InitialOrders}军令。":"。"));
            }
            lines.Add("0.9/1.2/1.5/1.8/2.8演出秒全场五段命中；前四段各15%，蓄力末段40%。原完整预算先过护甲/抗性再分段，不递归产点。1×/2×演出等速。");
            lines.Add("2/4/6需普攻收手且有射程内目标；8/9可在移动/普攻中参加，无需射程目标。技能、复起、控制锁照旧。");
            lines.Add("4档参与近战护盾10%生命3秒；6/8/9档15%生命5秒。");
            lines.Add("仅6档保留手动两次3点半令，后半排除前半参与者；半令未完成时自动整令等待。暂停、演出中或无合格单位/存活守军不扣费。");
            return string.Join("\n",lines);
        }
        public static SynergySummary[] From(IEnumerable<string> lineup,IEnumerable<string> extraMembers=null,IEnumerable<string> extraMudrockMembers=null)
        {
            var ids=lineup.Distinct().ToArray();
            var transfers=new HashSet<string>((extraMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.Guerrilla(id)));
            var mudrockTransfers=new HashSet<string>((extraMudrockMembers??Array.Empty<string>()).Where(id=>ids.Contains(id)&&CampaignBalance.Supports(id)&&!CampaignBalance.MudrockCrew(id)));
            string[] names={"整合运动","雪怪小队","感染生物","游击队","泥岩小队","牧群","深池","海嗣","萨卡兹"};
            int[][] levels={new[]{2,4,6},new[]{2,5},new[]{2,4,6},new[]{2,4,6,8,9},new[]{2,4,5,6},new[]{2,4},new[]{2,4},new[]{2,4},new[]{2,4,6,8,9}};
            string[] effects={
                "2档：部署2种不同卡触发1名援军，本场1次。\n4档：部署3种触发2名所选援军，本场2次。\n6档：部署3种触发士兵与弩手各1，本场3次；援军获15%生命护盾4秒。援军不扣蓝门防御。",
                "冻结守军在2.5秒内受到两种不同原卡直接命中时碎冰。\n2档：50法伤，半径0.9/最多2人；消耗冻结，目标冷却4秒。\n5档：120法伤，半径1.5/最多5人；保留冻结，目标冷却3秒。\n仅5档：3次碎冰或霜星手动冰封发动8秒暴风雪，开启清零寒意；每次碎冰只计1点，天气期间不积累、不叠加、不续时。\n0/2/4/6秒全场寒潮，每次(60+目标最大生命2%)法伤、过法抗；碎冰/寒潮按雪怪平均星级每升1星+35%。\n寒冷：攻速70%；再次受寒冷冻结1.2秒。天气冻结结束后保护3秒，只防控制、不免寒潮伤害。",
                "战前选主形态，6档再选另一种2档副形态。\n集群2/4/6：2只×55%、2只×70%、3只×55%生命/攻击；一次扣费，额外分体不扣蓝门。\n甲壳：18/30/35%生命护盾6秒，护甲+0/2/3。\n爆裂：原体死亡造成35/65/85法伤，半径1/1.2/1.3，最多2/3/3人；每升一星+35%。原有死亡爆炸独立保留并按自身范围/伤害类型结算；额外爆裂只算一次，不重复寒冷。",
                GuerrillaEffect(),
                "2/4/5/6档：按战前编队的不同种类与总星级S确定培养上限。\n2种可养至II；4种且S≥7至III；5种且S≥10至IV；6种且S≥12至V。每种只算最高星，备战不计；可免费转职一种非原生成员。\nI/II/III/IV/V累计经验门槛0/6/16/30/48。每场实际战斗结束自动获得：档位＋⌊S/2⌋经验，胜负都结算；补给与中途退出不发。达到当前上限停止获得，不预存未来经验，历史经验不扣。\n生命360/550/1800/6000/7500，攻击60/84/240/1600/2000，DP4/5/8/14/14。每场一次、不扣门血；IV/V至多3目标、每第三击全场80%攻击法伤、每次命中回8%生命一次、免疫控制。",
                "牧群：全队非五费正常实体最终死亡后生成短时转化体。2档前3个、35%生命/攻击、4甲、10秒；4档前5个、45%生命/攻击、6甲、12秒，可保留远程基础攻击（2.5射程、攻击再乘85%）。\n转化沿原位置/路线，穿门0，不复制主动、爆炸、羁绊资源或再次转化。原动作较长时攻击间隔保留完整动作。",
                "深池：成员普攻/原生技能有效命中即燎烧，持续4秒，同名取高刷新。2档15法伤/秒，4档25/秒；独立于原生灼燃，不自带减法抗。\n燎烧或原生灼燃目标被正常伤害/深池燎烧消灭后留火种，首击击杀也有效。2档本场累计最多4枚、传播半径1.8/最多2人；4档6枚、2.2/3人，已消耗仍计上限。\n4档消耗1火种引爆，优先有活目标的火种，半径1.8/最多4人、180法伤，每场2次。引爆不返火种；伤害每成员平均星级增加25%。",
                "海嗣 · 溟痕蔓延：每种海嗣本场首次有效输出或外来承伤，在脚下落种一次；重复部署、无敌、自损和支援不重复落种。\n2/4档：每6/4秒向相邻通行地面扩散一圈，全场覆盖至多8/16格；为尚未接敌的每种海嗣预留1格，高台不铺。\n海嗣站在溟痕上，原生神经损伤×1.5/2；地面守军踩溟痕每秒积累3点神经损伤，该地面积累不再乘倍率。壳海负责前锋落种，不新增神经普攻。\n神经积累100时爆发：100真实伤害、10秒眩晕，其间不再积累。无存活海嗣时溟痕休眠，后续部署恢复；战后清空。\n战斗中点击「查看溟痕范围」突出边界；守军神经条和海嗣×倍率标识显示真实状态。",
                "众魂之契：战前把侧栏旁的萨卡兹徽章拖到萨卡兹编队卡，指定该卡为承契者；该卡所有正常部署实体统一受益，开战后锁定。羁绊外角色不能成为承契者。\n各档部署时自动触发，持续本场，无需手动释放、低血或阵亡充能，不消耗DP/技力。\n2/4/6/8/9档，全体萨卡兹最大生命+10/20/30/40/60%。\n2档：承契者攻击+35%、一次20%生命护盾。\n4档：攻击+60%、一次30%护盾。\n6档：攻击+90%、一次40%护盾；其他萨卡兹获一半攻击加成与入场盾。\n8档：攻击+120%、一次50%护盾；其他萨卡兹共享完整效果。\n9档「众魂归乡」：全体萨卡兹攻击永久+150%，部署时获得一次自身最大生命60%护盾。\n护盾按各自最终最大生命计算，同名取高、不累计；没有时限，但消耗后不定时补发。复起不重新触发；巨像、援军、转化体与分体不参与。"};
            return names.Select((name,i)=>new SynergySummary{Name=name,Members=ids.Where(id=>CampaignBalance.Tags(id).Contains(name)||name=="游击队"&&transfers.Contains(id)||name=="泥岩小队"&&mudrockTransfers.Contains(id)).ToArray(),Thresholds=levels[i],Effect=effects[i]})
                .Select(s=>{s.Count=s.Members.Length;s.Tier=s.Name=="游击队"?GuerrillaRules.TierForCount(s.Count):s.Thresholds.Where(n=>s.Count>=n).DefaultIfEmpty(0).Max();return s;})
                .Where(s=>s.Count>0).OrderByDescending(s=>s.Tier>0).ThenByDescending(s=>s.Count).ToArray();
        }
    }
    public sealed partial class ManualBattleController
    {
        string inspectedSynergy;
        Vector2 guerrillaDetailScroll;
        Vector2 sarkazDetailScroll;
        const float SynergyRowPitch=48;
        const float SynergyHeaderHeight=26;
        Rect SynergyBoundsFor(int count,bool preparation)
        {
            int rows=Mathf.Min(count,preparation?9:4);
            // Nine complete preparation rows still leave the construction button above y552.
            float top=preparation&&rows==9?60:65;
            return new Rect(12,top,174,SynergyHeaderHeight+rows*SynergyRowPitch);
        }
        Rect SynergyViewportFor(Rect bounds)=>new Rect(bounds.x,bounds.y+SynergyHeaderHeight,bounds.width,bounds.height-SynergyHeaderHeight);
        float SynergyWidthFor(int count,Rect viewport)=>count*SynergyRowPitch>viewport.height?156:174;
        Rect SynergyRowAtWidth(int index,float width)=>new Rect(0,index*SynergyRowPitch,width,SynergyRowPitch);
        Rect SynergyListRect=>SynergyBoundsFor(CurrentSynergies.Length,Loop!=null&&Loop.CanShop);
        Rect SynergyRowsViewportRect=>SynergyViewportFor(SynergyListRect);
        float SynergyContentWidth=>SynergyWidthFor(CurrentSynergies.Length,SynergyRowsViewportRect);
        Rect SynergyRowContentRect(int index)=>SynergyRowAtWidth(index,SynergyContentWidth);
        Rect SynergyRowScreenRect(int index)
        {
            var row=SynergyRowContentRect(index);var viewport=SynergyRowsViewportRect;
            return new Rect(viewport.x+row.x-campaignSynergyScroll.x,viewport.y+row.y-campaignSynergyScroll.y,row.width,row.height);
        }
        Rect SynergyBadgeRect(Rect row)=>new Rect(row.x+2,row.y+1,44,44);
        Rect SynergyGlyphRect(Rect row)=>new Rect(row.x+10,row.y+9,28,28);
        Rect SynergyCountRect(Rect row)=>new Rect(row.x+31.44f,row.y+30.44f,16.8f,16.8f);
        Rect SynergyNameRect(Rect row)=>new Rect(row.x+58,row.y,row.width-64,20);
        Rect SynergyThresholdRect(Rect row)=>new Rect(row.x+56,row.y+20,row.width-62,15);
        Rect SynergySupplementRect(Rect row)=>new Rect(row.x+58,row.y+35,row.width-64,12);
        Rect SynergyProgressRect(Rect row)=>new Rect(row.x+58,row.y+47,row.width-64,1);
        Rect SynergyDetailRect=>inspectedSynergy=="感染生物"?InfectionFormPanelRect:new Rect(252,90,620,415);
        SynergySummary[] CurrentSynergies=>SynergySummary.From(shopLineup,PreparedGuerrillaTransferIds,PreparedMudrockTransferIds);
        bool SynergyBlocksMap(Vector2 point)=>CampaignActive&&(SynergyListRect.Contains(point)||inspectedSynergy!=null);
        void DrawSynergySidebar()
        {
            if(!CampaignActive)return;
            DrawSnowstormFeedback();DrawMarineFieldFeedback();
            var summaries=CurrentSynergies;var bounds=SynergyBoundsFor(summaries.Length,Loop.CanShop);
            Fill(new Rect(0,bounds.y,bounds.xMax,bounds.height),new Color(.065f,.11f,.165f,.95f));
            GUI.Label(new Rect(bounds.x+1,bounds.y,55,22),"羁绊",CampaignText(12,color:new Color(.92f,.95f,.98f),weight:600));
            GUI.Label(new Rect(bounds.x+65,bounds.y,bounds.width-70,22),summaries.Count(s=>s.Tier>0)+"条已激活",CampaignText(10,TextAnchor.MiddleRight,new Color(.61f,.70f,.80f)));
            Fill(new Rect(bounds.x,bounds.y+22,bounds.width,1),new Color(.54f,.65f,.77f,.17f));
            if(summaries.Length==0){inspectedSynergy=null;return;}
            Rect viewport=SynergyViewportFor(bounds);float contentWidth=SynergyWidthFor(summaries.Length,viewport);
            campaignSynergyScroll=GUI.BeginScrollView(viewport,campaignSynergyScroll,new Rect(0,0,contentWidth,Mathf.Max(1,summaries.Length*SynergyRowPitch)),false,false);
            try
            {
                for(int i=0;i<summaries.Length;i++)
                {
                    var s=summaries[i];Rect row=SynergyRowAtWidth(i,contentWidth);
                    if(GUI.Button(row,GUIContent.none,GUIStyle.none))
                    {
                        if(s.Name=="泥岩小队")OpenGolemTrainingPanel();
                        else {inspectedSynergy=inspectedSynergy==s.Name?null:s.Name;campaignConstructOpen=false;CancelShopDrag();}
                    }
                    bool active=s.Tier>0,snowstorm=s.Name=="雪怪小队"&&SnowstormHudActive;
                    Color ink=active?new Color(.95f,.965f,.98f):new Color(.65f,.70f,.77f);
                    SynergyBadgePresentation.DrawBadge(SynergyBadgeRect(row),s.Name,s.Tier,s.Count,scene.InterfaceFont);
                    GUI.Label(SynergyNameRect(row),s.Name+(s.Name=="泥岩小队"||s.Name=="感染生物"&&InfectionChoiceEditable?" ›":""),CampaignText(14,color:ink,weight:600));
                    SynergyBadgePresentation.DrawThresholds(SynergyThresholdRect(row),s.Thresholds,s.Tier,scene.InterfaceFont);
                    if(s.Name=="感染生物")DrawInfectionFormRow(row,s.Tier);
                    else if(s.Name=="泥岩小队")DrawGolemSidebarProgress(row);
                    else if(s.Name=="海嗣"&&s.Tier>0)DrawMarineRow(row);
                    else if(snowstorm)DrawSnowstormRow(row);
                    else if(s.Name=="雪怪小队"&&SnowstormChargeHudVisible)DrawSnowstormChargeRow(row);
                }
            }
            finally {GUI.EndScrollView();}
            var detail=summaries.FirstOrDefault(s=>s.Name==inspectedSynergy);if(detail==null){inspectedSynergy=null;return;}
            if(detail.Name=="感染生物"){DrawInfectionFormPanel(detail);return;}
            Fill(SynergyDetailRect,ApprovedUi.Paper);ApprovedUi.Border(SynergyDetailRect,ApprovedUi.Rule);
            GUI.Label(new Rect(268,102,510,28),$"{detail.Name} · {detail.Count}种 · "+(detail.Tier>0?detail.Tier+"档已激活":detail.Implemented?"未达门槛":"待实现"),CampaignText(17,weight:700));
            if(LoopButton(new Rect(803,102,53,28),"关闭")){inspectedSynergy=null;return;}
            var body=CampaignText(14,TextAnchor.UpperLeft);body.wordWrap=true;
            if(detail.Name=="游击队"||detail.Name=="萨卡兹")
            {
                var detailViewport=new Rect(268,149,588,269);float content=body.CalcHeight(new GUIContent(detail.Effect),detailViewport.width-18)+8;
                var scroll=detail.Name=="萨卡兹"?sarkazDetailScroll:guerrillaDetailScroll;
                scroll=GUI.BeginScrollView(detailViewport,scroll,new Rect(0,0,detailViewport.width-18,Mathf.Max(detailViewport.height,content)),false,false);
                if(detail.Name=="萨卡兹")sarkazDetailScroll=scroll;else guerrillaDetailScroll=scroll;
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
