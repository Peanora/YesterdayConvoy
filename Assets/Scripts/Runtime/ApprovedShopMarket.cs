using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool ShopTradingAvailable=>ShopTrialActive&&Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop)&&shopVisible&&!ShopPanelTransitioning;
        int ShopHeldCopies(string id)=>shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star));
        float ShopRecruitPulse=>.15f+.85f*(.5f-.5f*Mathf.Cos(Time.unscaledTime*Mathf.PI*2/1.5f));

        void DrawApprovedShopMarket(bool prep)=>OriginalShopPresentation.DrawApprovedShopMarket(prep);
        void DrawApprovedShopOffer(int slot,Rect rect,Rect global,bool trading)=>OriginalShopPresentation.DrawApprovedShopOffer(slot,rect,global,trading);
        void DrawApprovedUpgradeCue(Rect rect,int star)=>OriginalShopPresentation.DrawApprovedUpgradeCue(rect,star);
        void DrawShopSaleTargets(Vector2 point)
        {
            if(shopDrag<0||!shopMoving||shop==null)return;
            var piece=shop.Bench.FirstOrDefault(p=>p.Instance==shopDrag);if(piece==null)return;
            bool prep=Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop);
            if(!prep)
            {
                if(Loop==null||Loop.CanDeploy)DrawTestPortrait(new Rect(point.x-25,point.y-66,50,62),OriginalTalulahArt.CardPortrait(shopDefinitions[piece.Id].Card));
                return;
            }
            var quote=shop.Quote(shopDrag);if(!quote.Success)return;
            foreach(Rect rect in new[]{ShopSellRect,ShopSellRightRect})
            {
                bool hit=rect.Contains(point);Color ink=hit?new Color(.46f,.32f,.21f):new Color(.28f,.38f,.46f);
                Fill(rect,hit?new Color(.92f,.86f,.79f,.97f):new Color(.85f,.89f,.91f,.95f));
                ShopBorder(rect,hit?new Color(.71f,.54f,.34f):ApprovedUi.Rule,hit?2:1);
                float y=rect.center.y-88;
                GUI.Label(new Rect(rect.x+7,y,rect.width-14,30),"出售",LoopText(20,TextAnchor.MiddleCenter,ink,600));
                GUI.Label(new Rect(rect.x+7,y+39,rect.width-14,24),"松开出售",LoopText(14,TextAnchor.MiddleCenter,ink,600));
                GUI.Label(new Rect(rect.x+7,y+68,rect.width-14,22),quote.Message,LoopText(quote.Message.Length>6?9:11,TextAnchor.MiddleCenter,ink,500));
                GUI.Label(new Rect(rect.x+7,y+94,rect.width-14,18),quote.FinalStar+"星",LoopText(10,TextAnchor.MiddleCenter,ApprovedUi.Muted));
                GUI.Label(new Rect(rect.x+7,y+119,rect.width-14,40),"+"+quote.Value,LoopText(28,TextAnchor.MiddleCenter,new Color(.56f,.43f,.23f),700));
                GUI.Label(new Rect(rect.x+7,y+159,rect.width-14,18),"金币",LoopText(10,TextAnchor.MiddleCenter,new Color(.56f,.43f,.23f)));
                GUI.Label(new Rect(rect.x+7,y+194,rect.width-14,18),"拖回或Esc取消",LoopText(9,TextAnchor.MiddleCenter,ApprovedUi.Muted));
            }
        }
        void DrawApprovedStandaloneShop()
        {
            EnsureStyles();var e=Event.current;Vector2 point=TacticalViewport.Current.FromTopLeft(e.mousePosition);
            TrackRouteHover(point);
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&(loopMenuOpen||shopOddsOpen)){loopMenuOpen=shopOddsOpen=false;CancelShopDrag();e.Use();}
            if(!loopMenuOpen&&!shopOddsOpen&&ShopPointerEvent(e.type,point,e.button))e.Use();
            EventType originalEvent=e.type;bool suppressed=(loopMenuOpen||shopOddsOpen)&&originalEvent!=EventType.Repaint&&originalEvent!=EventType.Layout;
            Matrix4x4 previous=GUI.matrix;Color old=GUI.color,background=GUI.backgroundColor;bool enabled=GUI.enabled;
            GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                if(suppressed)e.type=EventType.Ignore;
                DrawShopDefenderBars();
                foreach(var unit in Battle.Units)
                {
                    if(!unit.Active||Battle.StrongholdInTransit(unit.EntityId))continue;
                    Vector2 p=GUIPosition(Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*UnitVisualHeight(unit)));
                    Fill(new Rect(p.x-26,p.y,52,5),LoopInk);Fill(new Rect(p.x-25,p.y+1,50*unit.Health/unit.MaxHealth,3),new Color(.4f,.9f,.55f));
                    if(Battle.UnitCombat(unit).HasSkill)DrawSkillResourceBar(p.x,p.y+7,unit);
                }
                DrawLoopHeader();bool prep=Battle.Phase==BattlePhase.Ready;DrawShopMarket(prep);
                if(prep||ShopPanelTransitioning)DrawShopBench(LoopText(12,TextAnchor.MiddleCenter));else DrawLoopRoster(point);
                DrawShopResourceHud(prep);
                DrawShopFlows();
                if(StrongholdChallengeLayout.IsStronghold(TacticalMapId)&&ShopPanelVisibility<.01f)
                {
                    Fill(new Rect(188,69,625,23),ApprovedUi.Paper);
                    GUI.Label(new Rect(197,69,607,23),"活性源石：攻击与攻速提升，持续失血 · 青色传送口：等待3秒后返回中央",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                }
                if(prep)GUI.Label(new Rect(1142,638,120,51),"拖至红门部署\n开始测试",LoopText(12,TextAnchor.MiddleCenter,Color.white,600));
                if(Time.unscaledTime<shopMessageUntil&&!string.IsNullOrEmpty(shopMessage))
                {Fill(new Rect(282,333,690,28),ApprovedUi.Paper);GUI.Label(new Rect(294,333,666,28),shopMessage,LoopText(12,TextAnchor.MiddleCenter));}
                if(ShopPanelVisibility<.01f)DrawShopRouteLabels(LoopText(11,TextAnchor.MiddleLeft,Color.white));
                DrawDefenderInspector(LoopText(),buttonStyle);
                if(shopDrag>=0&&shopMoving)DrawShopSaleTargets(point);
                if(Battle.Phase==BattlePhase.Won||Battle.Phase==BattlePhase.Lost)
                {
                    var result=new Rect(420,322,440,148);Fill(result,ApprovedUi.Paper);ShopBorder(result,ApprovedUi.Rule,1);
                    GUI.Label(new Rect(438,336,404,48),Battle.Phase==BattlePhase.Won?"突破成功":"作战结束",LoopText(28,TextAnchor.MiddleCenter,null,700));
                    if(LoopButton(new Rect(438,402,195,43),"重新测试",true)){EnterShopTrial(0,TacticalMapId);return;}
                    if(LoopButton(new Rect(647,402,195,43),"返回测试选择")){ReturnToTrialScene();return;}
                }
                DrawDamageRanking();
                if(loopMenuOpen){if(suppressed)e.type=originalEvent;DrawLoopMenu();}
                if(shopOddsOpen){if(suppressed)e.type=originalEvent;DrawShopOdds(point);}
            }
            finally{if(e.type==EventType.Ignore)e.type=originalEvent;GUI.enabled=enabled;GUI.matrix=previous;GUI.color=old;GUI.backgroundColor=background;}
        }
    }
}
