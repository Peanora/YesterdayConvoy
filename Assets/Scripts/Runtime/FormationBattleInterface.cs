using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color Dark=new Color(.045f,.075f,.105f,.96f),Panel=ApprovedUi.Paper;
        static readonly Color Cyan=new Color(.28f,.85f,.88f),Gold=new Color(1,.79f,.38f),White=new Color(.9f,.94f,.96f),Muted=new Color(.55f,.67f,.73f);
        Vector2 formationSynergyScroll,formationRankingScroll;
        GUIStyle formationRankingScrollbar;
        string formationInspectedTag;
        GUIStyle Text(int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)=>ApprovedUi.Text(null,size,align,color,size>=22?650:400);
        static void Fill(Rect rect,Color color)=>ApprovedUi.Fill(rect,color);
        void Label(Rect rect,string text,int size=16,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)=>GUI.Label(rect,text,Text(size,color??White,align));
        bool Button(Rect rect,string text,bool active=true,bool accent=false)
        {
            bool old=GUI.enabled;GUI.enabled=old&&active;
            bool clicked=approvedShop.LoopButton(rect,text,accent);GUI.enabled=old;return clicked;
        }
        public static Rect RosterRect(int slot)=>FormationLineupRect(slot);
        void OnGUI()
        {
            var input=Event.current;
            if(Ready&&input!=null&&input.button==0&&(input.type==EventType.MouseDown||input.type==EventType.MouseDrag||input.type==EventType.MouseUp))
                ProcessPointer(input.type,TacticalViewport.Current.FromTopLeft(input.mousePosition));
            var previous=GUI.matrix;bool enabled=GUI.enabled;GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                if(!Ready){Fill(new Rect(0,0,1280,720),Dark);Label(new Rect(150,250,980,80),loading,23,Cyan,TextAnchor.MiddleCenter);return;}
                if(CombatViewActive)
                {
                    DrawWorldLabels();
                    GUI.enabled=enabled&&!formationMenuOpen;
                    DrawFormationCombatHud();DrawFormationPauseOverlay();
                    GUI.enabled=enabled;
                    if(formationMenuOpen)DrawFormationMenu();
                    return;
                }
                GUI.enabled=enabled&&!shopOddsVisible&&!formationMenuOpen&&!CampaignInteractionBlocked&&shopInputBlockedFrame!=Time.frameCount;
                DrawWorldLabels();DrawFormationHeader();DrawFormationSidebars();
                DrawCampaignToolbar();
                bool preparing=Model.Phase==FormationPhase.Preparation;
                DrawFormationPreparation();
                DrawFormationResources(preparing);
                approvedShop.DrawShopFlows();
                if(preparing)DrawFormationMarket();
                if(pointerHeld&&shopDragInstance>0&&Vector2.Distance(formationShopPointer,pointerStart)>6)DrawFormationSaleTargets();
                if(Model.Phase==FormationPhase.Won||Model.Phase==FormationPhase.Lost)DrawSettlement();
                GUI.enabled=enabled;
                if(formationMenuOpen)DrawFormationMenu();
                if(shopOddsVisible)DrawFormationOdds();
                DrawCampaignOverlays();
                if(!CampaignInteractionBlocked&&!formationMenuOpen&&!shopOddsVisible)DrawCampaignTraitOverlay();
            }
            finally{GUI.enabled=enabled;GUI.matrix=previous;TacticalViewport.Current.DrawBars();}
        }
        void DrawFormationHeader()
        {
            bool prep=Model.Phase==FormationPhase.Preparation;
            string phase=prep?"整备阶段":Model.Paused?"已暂停":Model.Phase==FormationPhase.Fighting?"战斗进行中":Model.Phase==FormationPhase.Exiting?"前往蓝门":"本关结束";
            Fill(new Rect(16,8,185,46),ApprovedUi.Paper);Fill(new Rect(16,8,3,46),new Color(.29f,.44f,.55f));
            GUI.Label(new Rect(28,11,165,24),Campaign==null?$"{Model.Round}/5 · 矩形攻坚":$"{Campaign.CurrentNode.Id} · {Campaign.CurrentNode.Kind}",approvedShop.LoopText(17,TextAnchor.MiddleLeft,null,700));
            GUI.Label(new Rect(28,36,165,15),"军阵行动 / "+phase,approvedShop.LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            Fill(new Rect(578,8,151,46),ApprovedUi.Paper);ApprovedShopPresentation.DrawLoopShield(new Vector2(589,20),new Color(.40f,.56f,.67f));
            GUI.Label(new Rect(613,14,76,21),"守军防线",approvedShop.LoopText(12));
            GUI.Label(new Rect(613,34,87,14),"存活干员",approvedShop.LoopText(9,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(690,12,32,33),Model.RemainingDefenders.ToString(),approvedShop.LoopText(26,TextAnchor.MiddleCenter,null,700));
            Fill(new Rect(1008,8,110,46),ApprovedUi.Paper);
            GUI.Label(new Rect(1016,10,94,17),"剩余时间",approvedShop.LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(1016,27,94,23),double.IsInfinity(Model.TimeRemaining)?"不限时":Model.TimeRemaining.ToString("0.0")+" 秒",approvedShop.LoopText(20,TextAnchor.MiddleLeft,null,700));
            if(Button(new Rect(1174,8,42,42),Model.Paused?"继续":"暂停",Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting))Model.Paused=!Model.Paused;
            if(Button(new Rect(1222,8,42,42),"菜单")){formationMenuOpen=!formationMenuOpen;CancelFormationDrag();}
            if(prep&&Button(new Rect(790,8,208,42),ViewingDefenders?"返回我方布阵  [V]":"查看守军部署  [V]"))ToggleFormationView();
        }
        void DrawFormationSidebars()
        {
            if(Campaign!=null){DrawCampaignSynergies();DrawFormationRanking();return;}
            var ids=Session.Seats.Lineup(Shop.Bench).Select(piece=>piece.Id).ToArray();var summaries=SynergySummary.From(ids);
            ApprovedShopWidgets.DrawSynergies(summaries,Model.Phase==FormationPhase.Preparation,ApprovedUi.Font(null),ref formationSynergyScroll,
                s=>{formationInspectedTag=formationInspectedTag==s.Name?null:s.Name;CancelFormationDrag();},
                (r,s)=>GUI.Label(new Rect(r.x+58,r.y+35,r.width-64,12),"仅标签 · 未启用增益",approvedShop.LoopText(9,TextAnchor.MiddleLeft,new Color(.77f,.88f,.82f))),
                s=>s.Name,false);
            if(formationInspectedTag!=null)
            {
                var rect=new Rect(200,244,380,112);Fill(rect,ApprovedUi.Paper);ApprovedUi.Border(rect,ApprovedUi.Rule);
                GUI.Label(new Rect(rect.x+14,rect.y+10,350,26),formationInspectedTag+" · 编队标签",approvedShop.LoopText(17,TextAnchor.MiddleLeft,null,600));
                var style=approvedShop.LoopText(12,TextAnchor.UpperLeft,ApprovedUi.Muted);style.wordWrap=true;
                GUI.Label(new Rect(rect.x+14,rect.y+46,350,58),"现有角色已适配布阵与占地；战斗采用简化自动技能，原羁绊效果尚未接入。这里只显示真实编队的种类标签。",style);
            }
            DrawFormationRanking();
        }
        void DrawFormationRanking()
        {
            if(Model.Phase==FormationPhase.Preparation)return;
            var rows=Model.Units.OrderByDescending(u=>u.DamageDone).ThenBy(u=>u.ArtId,StringComparer.Ordinal).Select(u=>new ApprovedShopWidgets.RankingRow
            {CardId="CARD_"+u.ArtId,Name=u.Name,Damage=u.DamageDone,Portrait=FormationUnitArt.For(u.ArtId)?.Portrait}).ToArray();
            string state=Model.Paused?"已暂停":Model.Phase==FormationPhase.Fighting||Model.Phase==FormationPhase.Exiting?"实时统计":"本场结算";
            ApprovedShopWidgets.DrawRanking(rows,Model.Units.Sum(u=>u.DamageDone),state,ApprovedUi.Font(null),ref formationRankingScroll,ref formationRankingScrollbar);
        }
        void DrawFormationCombatRoster()
        {
            for(int i=0;i<Model.Units.Count;i++)
            {
                float step=928f/Mathf.Max(10,Model.Units.Count);
                var u=Model.Units[i];var r=new Rect(188+i*step,581,step-4,124);
                approvedShop.DrawLoopCostBackground(r,Shop.Definition("CARD_"+u.ArtId).Cost);
                DrawFormationPortrait(new Rect(r.x+2,r.y+2,r.width-4,96),"CARD_"+u.ArtId);
                approvedShop.DrawShopStars(new Rect(r.xMax-8-u.Star*12,r.y+2,u.Star*12,12),u.Star,new Color(1,.94f,.72f));
                Fill(new Rect(r.x+1,r.y+48,r.width-2,48),new Color(.06f,.11f,.16f,.82f));
                GUI.Label(new Rect(r.x+5,r.y+50,r.width-10,17),u.Name,approvedShop.LoopText(u.Name.Length>6?9:11,TextAnchor.MiddleLeft,Color.white,600));
                GUI.Label(new Rect(r.x+5,r.y+72,r.width-10,18),u.Alive?$"生命 {u.Health:0}":$"复活 {u.RespawnRemaining:0.0}s",approvedShop.LoopText(10,TextAnchor.MiddleLeft,Color.white));
                GUI.Label(new Rect(r.x+5,r.y+100,r.width-10,17),$"伤害 {u.DamageDone:0}",approvedShop.LoopText(10,TextAnchor.MiddleLeft,Color.white));
            }
            Fill(new Rect(1128,581,135,124),ApprovedUi.Paper);
            GUI.Label(new Rect(1138,590,113,23),"剩余时间",approvedShop.LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(1136,612,113,54),Model.TimeRemaining.ToString("0.0"),approvedShop.LoopText(34,TextAnchor.MiddleLeft,null,700));
            if(Campaign==null&&Button(new Rect(1138,673,113,25),"返回整备"))Retry();
        }
        void DrawFormationMenu()
        {
            var r=ApprovedUi.Menu;Fill(r,ApprovedUi.Paper);ApprovedUi.Border(r,ApprovedUi.Rule);
            GUI.Label(new Rect(r.x+12,r.y+9,r.width-60,27),"系统菜单",approvedShop.LoopText(19,TextAnchor.MiddleLeft,null,700));
            if(Button(new Rect(r.xMax-36,r.y+9,24,24),"×"))formationMenuOpen=false;
            if(Button(new Rect(r.x+12,r.y+44,r.width-24,30),soundEnabled?"关闭音效":"开启音效"))soundEnabled=!soundEnabled;
            if(Button(new Rect(r.x+12,r.y+81,r.width-24,30),"重新开始本次挑战")){formationMenuOpen=false;ResetDemo();}
            if(Button(new Rect(r.x+12,r.y+118,r.width-24,30),"返回原玩法"))CampaignBootstrap.OpenLegacyEntrance();
            if(Button(new Rect(r.x+12,r.y+155,r.width-24,30),"关闭菜单"))formationMenuOpen=false;
        }
        void DrawWorldLabels()
        {
            var labelFrame=CombatViewActive?new Rect(0,64,1280,656):BoardInputRect;
            foreach(var actor in Model.Units.Where(u=>u.Enabled).Cast<FormationActor>().Concat(Model.Defenders).Concat(Model.Summons))
            {
                if(!actor.Alive||!FormationActorVisibleInView(actor))continue;
                float top=formationLabelHeights.TryGetValue(actor.Id,out var labelTop)?labelTop:ActorPresentationHeight(actor);
                var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(FormationDisplayPoint(actor)+battleCamera.transform.up*(top+.12f)));
                if(p.y<labelFrame.yMin+5||p.y>labelFrame.yMax-3)continue;
                bool ally=actor is FormationFighter;
                Fill(new Rect(p.x-23,p.y,46,5),new Color(.015f,.025f,.03f,.9f));
                Fill(new Rect(p.x-22,p.y+1,44*Mathf.Clamp01(actor.Health/actor.MaxHealth),3),ally?Cyan:new Color(1,.42f,.32f));
                Label(new Rect(p.x-65,p.y-18,130,18),actor.Name,11,White,TextAnchor.MiddleCenter);
            }
            if(Model.Phase==FormationPhase.Preparation)
                foreach(var u in Model.Units.Where(u=>u.Enabled))
                {
                    if(!FormationActorVisibleInView(u))continue;
                    var p=CellScreen(u.SpawnCenter);
                    Label(new Rect(p.x-44,p.y-6,88,22),new string('★',u.Star),11,Gold,TextAnchor.MiddleCenter);
                }
            foreach(var h in Model.Hits)
            {
                double age=Model.Clock-h.At;if(age<0||age>.8)continue;
                if(ViewingDefenders!=(h.To.x>=FormationBattleModel.DeploymentColumns+.5f))continue;
                var p=TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(ActorWorldPoint(h.To)+Vector3.up*(1.35f+(float)age*.35f)));
                if(p.y<labelFrame.yMin+5||p.y>labelFrame.yMax-3)continue;
                Label(new Rect(p.x-48,p.y-10,96,24),(h.Heal?"+":"")+h.Amount.ToString("0"),h.Skill?20:15,h.Heal?Cyan:h.Skill?Gold:White,TextAnchor.MiddleCenter);
            }
        }
        void DrawSettlement()
        {
            if(Campaign!=null){if(!Campaign.Finished)DrawCampaignResult();return;}
            bool won=Model.Phase==FormationPhase.Won;Fill(new Rect(380,172,520,324),Dark);ApprovedUi.Border(new Rect(380,172,520,324),won?Cyan:Gold,2);
            Label(new Rect(405,191,470,45),won?"防线肃清 · 蓝门突破":"进攻未完成",29,won?Cyan:Gold,TextAnchor.MiddleCenter);
            Label(new Rect(410,245,460,32),won?$"剩余 {Model.TimeRemaining:0.0} 秒":$"仍有 {Model.Defenders.Count(d=>d.Alive)} 名守军，或未及时进门",16,White,TextAnchor.MiddleCenter);
            var top=Model.Units.OrderByDescending(u=>u.DamageDone).FirstOrDefault();
            if(top!=null)Label(new Rect(410,287,460,25),$"本关输出：{top.Name} {top.DamageDone:0}  ·  全队阵亡 {Model.Units.Sum(u=>u.Deaths)} 次",15,Muted,TextAnchor.MiddleCenter);
            Label(new Rect(410,327,460,43),won?"领取通关金币与经验，带着已有军阵继续整备。":"返回同关调整军阵；重试保留购买成果，不重复发奖。",14,Muted,TextAnchor.MiddleCenter);
            if(Button(new Rect(410,400,220,47),"同关整备重试"))Retry();
            if(Button(new Rect(650,400,220,47),Model.Round<5?"领取奖励 · 下一关":finalRewardClaimed?"开始新挑战":"领取奖励 · 完成挑战",won,true))
            {
                if(Model.Round<5)NextRound();else if(finalRewardClaimed)ResetDemo();
                else {var reward=Session.SettleWin(Model.Round);finalRewardClaimed=reward!=null;notice=reward!=null?$"最终奖励 +{reward.Total}金币、+{reward.Experience}经验；新挑战前保留购买记录。":"奖励尚未结算。";}
            }
        }
    }
}
