using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool infectionChoiceRequested;
        Vector2 synergyRulesScroll,synergyMembersScroll;
        public string SynergyDetailsName=>inspectedSynergy;
        public static Rect UnifiedSynergyPanelRect=>new Rect(210,78,850,538);
        public bool OpenSynergyDetails(string name)
        {
            if(!CampaignActive||!CurrentSynergies.Any(s=>s.Name==name))return false;
            CloseShopUiOverlays();inspectedSynergy=name;campaignConstructOpen=false;
            synergyRulesScroll=synergyMembersScroll=Vector2.zero;CancelShopDrag();return true;
        }
        public bool OpenInfectionChoiceDetails()
        {
            if(!OpenSynergyDetails("感染生物"))return false;
            infectionChoiceRequested=true;return true;
        }
        public SynergyDetailUiModel CurrentSynergyDetail
        {
            get
            {
                var summary=CurrentSynergies.FirstOrDefault(s=>s.Name==inspectedSynergy);
                return summary==null?null:SynergyDetailUiModel.Build(summary,Battle?.CampaignV2Enabled==true,shop.Roles.Select(r=>r.Id));
            }
        }
        void DrawUnifiedSynergyDetails(SynergySummary summary)
        {
            var r=UnifiedSynergyPanelRect;var model=CurrentSynergyDetail;
            var paper=ApprovedUi.Paper;paper.a=1;Fill(r,paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+20,r.y+12,430,29),summary.Name,CampaignText(22,weight:700));
            string current=summary.Tier<=0?"尚未激活":summary.Name=="泥岩小队"?$"人数{summary.Tier}档已达":summary.Name=="感染生物"?$"当前{summary.Tier}档可选":$"当前{summary.Tier}档生效";
            GUI.Label(new Rect(r.x+20,r.y+44,425,19),$"{summary.Count}种已编队  ·  "+current+"  ·  "+model.source,CampaignText(11,color:ApprovedUi.Muted));
            if(summary.Name=="泥岩小队")GUI.Label(new Rect(r.x+444,r.y+44,386,19),$"总星 {Battle.MudrockTotalStars}  ·  培养上限 {GolemLevelName(Battle.GolemCapacity)}  ·  当前巨像 {GolemLevelName(Battle.GolemStage)}",CampaignText(11,color:ApprovedUi.Muted));
            else if(summary.Name=="感染生物")GUI.Label(new Rect(r.x+444,r.y+44,386,19),"主形态 "+FormName(VisibleInfectionMain)+"  ·  "+(summary.Tier>=6?"副形态 "+FormName(VisibleInfectionSecond):"副形态未解锁"),CampaignText(11,color:ApprovedUi.Muted));
            if(LoopButton(new Rect(r.xMax-51,r.y+15,31,28),"×")){CloseCampaignOverlay();return;}
            Fill(new Rect(r.x+20,r.y+74,r.width-40,1),ApprovedUi.Rule);
            var rules=new Rect(r.x+20,r.y+87,404,391);var members=new Rect(r.x+444,r.y+87,386,391);
            var body=CampaignText(12,TextAnchor.UpperLeft);body.wordWrap=true;
            float inner=rules.width-19,commonHeight=body.CalcHeight(new GUIContent(model.common),inner-12)+17;
            var heights=model.tiers.Select(t=>body.CalcHeight(new GUIContent(t.text),inner-28)+39).ToArray();
            float total=commonHeight+30+heights.Sum()+model.tiers.Length*8;
            synergyRulesScroll=GUI.BeginScrollView(rules,synergyRulesScroll,new Rect(0,0,inner,Mathf.Max(rules.height,total)),false,false);
            try
            {
                GUI.Label(new Rect(1,0,inner,19),"共通规则",CampaignText(11,color:ApprovedUi.Muted,weight:600));
                GUI.Label(new Rect(1,25,inner-12,commonHeight),model.common,body);float y=commonHeight+30;
                for(int i=0;i<model.tiers.Length;i++)
                {
                    var tier=model.tiers[i];float h=heights[i];var ink=tier.active?ApprovedUi.Ink:new Color(.47f,.53f,.57f);
                    Fill(new Rect(0,y,inner,h),tier.active?new Color(.77f,.84f,.86f):new Color(.85f,.88f,.89f));
                    if(tier.active)Fill(new Rect(0,y,3,h),ApprovedUi.Gold);
                    string activeLabel=summary.Name=="泥岩小队"?"人数档已达":summary.Name=="感染生物"?"当前档可选效果":"正在生效";
                    GUI.Label(new Rect(12,y+6,inner-24,21),tier.tier+"档"+(tier.active?"  "+activeLabel:tier.tier<summary.Tier?"  已被高档覆盖":"  未达到"),CampaignText(13,color:ink,weight:tier.active?700:400));
                    var rowBody=CampaignText(12,TextAnchor.UpperLeft,ink);rowBody.wordWrap=true;
                    GUI.Label(new Rect(12,y+33,inner-28,h-37),tier.text,rowBody);y+=h+8;
                }
            }
            finally{GUI.EndScrollView();}
            Fill(new Rect(r.x+434,r.y+88,1,389),ApprovedUi.Rule);
            const int columns=4;const float cardWidth=85,cardHeight=94,gap=7;
            int rows=(model.members.Length+columns-1)/columns;float rosterHeight=34+rows*(cardHeight+gap);
            synergyMembersScroll=GUI.BeginScrollView(members,synergyMembersScroll,new Rect(0,0,members.width-18,Mathf.Max(members.height,rosterHeight)),false,false);
            try
            {
                GUI.Label(new Rect(0,0,348,23),$"全部成员  {model.members.Count(m=>m.selected)}/{model.members.Length} 已编队",CampaignText(13,weight:600));
                for(int i=0;i<model.members.Length;i++)
                {
                    var member=model.members[i];var card=new Rect((i%columns)*(cardWidth+gap),32+(i/columns)*(cardHeight+gap),cardWidth,cardHeight);
                    Fill(card,member.selected?new Color(.64f,.74f,.79f):new Color(.75f,.79f,.81f));
                    Color old=GUI.color;if(!member.selected)GUI.color=new Color(.42f,.45f,.48f,.42f);
                    if(shopDefinitions.TryGetValue(member.id,out var definition))DrawTestPortrait(new Rect(card.x+2,card.y+2,card.width-4,66),OriginalTalulahArt.CardPortrait(definition.Card));
                    GUI.color=old;
                    if(!member.selected)Fill(new Rect(card.x+2,card.y+2,card.width-4,66),new Color(.48f,.52f,.55f,.38f));
                    Fill(new Rect(card.x,card.y+68,card.width,26),member.selected?new Color(.23f,.36f,.43f):new Color(.64f,.69f,.72f));
                    ApprovedUi.Border(card,member.selected?new Color(.30f,.48f,.58f):new Color(.67f,.72f,.74f),member.selected?1.5f:1);
                    string name=shop.Definition(member.id).Name;
                    var nameStyle=CampaignText(10,TextAnchor.MiddleCenter,member.selected?Color.white:new Color(.37f,.43f,.47f),member.selected?600:400);nameStyle.wordWrap=false;
                    while(nameStyle.fontSize>8&&nameStyle.CalcSize(new GUIContent(name)).x>card.width-6)nameStyle.fontSize--;
                    GUI.Label(new Rect(card.x+3,card.y+70,card.width-6,16),name,nameStyle);
                    GUI.Label(new Rect(card.x+3,card.y+84,card.width-6,10),member.transfer?"转职编队":member.selected?"已编队":"未编队",CampaignText(8,TextAnchor.MiddleCenter,member.selected?new Color(.91f,.95f,.96f):new Color(.43f,.49f,.53f)));
                }
            }
            finally{GUI.EndScrollView();}
            GUI.Label(new Rect(r.x+444,r.yMax-47,386,30),"按编队中的不同角色计数；未部署或死亡不会变灰。",CampaignText(10,color:ApprovedUi.Muted));
            if(summary.Name=="感染生物")
            {if(LoopButton(new Rect(r.x+20,r.yMax-43,190,27),InfectionChoiceEditable?"选择主 / 副形态":"查看锁定形态"))OpenInfectionChoiceDetails();}
            else if(summary.Name=="泥岩小队")
            {if(LoopButton(new Rect(r.x+20,r.yMax-43,190,27),"查看巨像培养"))OpenGolemTrainingPanel();}
            else if(summary.Name=="整合运动"&&summary.Tier==4)
            {
                bool enabled=GUI.enabled;GUI.enabled=enabled&&Loop.CanShop;
                if(LoopButton(new Rect(r.x+20,r.yMax-43,190,27),reunionRanged?"四档援军：弩手":"四档援军：士兵")){reunionRanged=!reunionRanged;SyncShopDeck(false);}
                GUI.enabled=enabled;
            }
            else GUI.Label(new Rect(r.x+20,r.yMax-43,404,27),"仅当前达到的最高档效果生效",CampaignText(11,color:ApprovedUi.Muted));
        }
    }
}
