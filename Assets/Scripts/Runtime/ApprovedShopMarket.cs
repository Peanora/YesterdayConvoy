using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool ShopTradingAvailable=>ShopTrialActive&&Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop)&&shopVisible&&!ShopPanelTransitioning;
        int ShopHeldCopies(string id)=>shop.Bench.Where(p=>p.Id==id).Sum(p=>ShopTrialModel.Copies(p.Star));
        float ShopRecruitPulse=>.15f+.85f*(.5f-.5f*Mathf.Cos(Time.unscaledTime*Mathf.PI*2/1.5f));

        void DrawApprovedShopMarket(bool prep)
        {
            if(shop==null)return;
            if(!shopVisible&&!ShopPanelTransitioning)
            {
                bool enabled=GUI.enabled;GUI.enabled=enabled&&(Loop==null||Loop.CanShop);
                if(LoopButton(new Rect(1144,69,118,30),"打开商店 Tab"))SetShopVisible(true);
                GUI.enabled=enabled;return;
            }
            bool originalEnabled=GUI.enabled;
            float reveal=ShopPanelVisibility,offset=-(1-reveal)*ShopPanelRect.height;
            Rect panel=ShopPanelRect;
            GUI.BeginGroup(panel);
            try
            {
                Fill(new Rect(0,offset,panel.width,30),ApprovedUi.Paper);
                GUI.Label(new Rect(9,offset+3,102,24),"招募商店",LoopText(15,TextAnchor.MiddleLeft,null,700));
                GUI.Label(new Rect(105,offset+4,220,22),"五格商品 / 点击购买",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                GUI.Label(new Rect(680,offset+4,32,22),"等级",LoopText(11,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                GUI.Label(new Rect(714,offset+2,25,25),shop.Level.ToString(),LoopText(19,TextAnchor.MiddleCenter,null,600));
                Fill(new Rect(745,offset+9,82,3),new Color(.68f,.74f,.79f));
                Fill(new Rect(745,offset+9,82*(shop.Level==10?1:Mathf.Clamp01((float)shop.XP/shop.NextLevelXP)),3),new Color(.35f,.50f,.62f));
                GUI.Label(new Rect(745,offset+15,85,13),shop.Level==10?"等级上限":$"{shop.XP}/{shop.NextLevelXP}经验",LoopText(8,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                GUI.enabled=originalEnabled&&prep&&ShopTradingAvailable;
                if(LoopButton(new Rect(840,offset+4,111,22),"买经验 4金币"))
                {
                    var result=shop.Upgrade();if(result.Success){shopUpgradeAt=Time.unscaledTime;SyncShopDeck();}ShopNotice(result.Message);
                }
                GUI.enabled=originalEnabled;
                if(LoopButton(new Rect(980,offset+4,86,22),"收起商店"))SetShopVisible(false);

                Rect refresh=new Rect(956,offset+36,118,32),lockRect=new Rect(956,offset+74,118,32);
                GUI.enabled=originalEnabled&&prep&&ShopTradingAvailable;
                if(LoopButton(refresh,"刷新 2金币",true))
                {var result=shop.Refresh();if(result.Success)shopRefreshAt=Time.unscaledTime;ShopNotice(result.Message);}
                if(LoopButton(lockRect,shop.Locked?"已锁定 · 解锁":"锁定商店"))shop.Locked=!shop.Locked;
                GUI.enabled=originalEnabled;
                for(int i=0;i<5;i++)
                {
                    Rect global=ShopOfferRect(i);global.y+=offset;
                    var local=global;local.position-=panel.position;
                    DrawApprovedShopOffer(i,local,global,originalEnabled&&prep&&ShopTradingAvailable);
                }
            }
            finally{GUI.enabled=originalEnabled;GUI.EndGroup();}
            if(Battle.Phase==BattlePhase.Ready&&(Loop==null||Loop.CanShop))DrawApprovedRecruitList(originalEnabled&&prep&&ShopTradingAvailable,reveal);
        }
        void DrawApprovedShopOffer(int slot,Rect rect,Rect global,bool trading)
        {
            string id=shop.Offers[slot];
            if(id==null)
            {
                Fill(rect,new Color(.52f,.60f,.65f,.50f));ShopBorder(rect,new Color(.78f,.84f,.88f,.55f),1);
                GUI.Label(rect,"暂无商品",LoopText(12,TextAnchor.MiddleCenter,new Color(.83f,.89f,.93f)));return;
            }
            var role=shop.Definition(id);int copies=ShopHeldCopies(id);
            var preview=shop.PreviewPurchase(slot);int upgrade=preview.Success&&preview.FinalStar>1?preview.FinalStar:0;
            DrawLoopCostBackground(rect,role.Cost);
            DrawTestPortrait(new Rect(rect.x+rect.width*.34f,rect.y+5,rect.width*.64f,108),OriginalTalulahArt.CardPortrait(shopDefinitions[id].Card));
            Fill(new Rect(rect.x,rect.y+73,rect.width,47),new Color(.09f,.15f,.20f,.91f));
            if(copies>0)
            {
                Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),new Color(.88f,.95f,1,.055f));
                ShopBorder(rect,new Color(.87f,.94f,1,.55f),1.5f);
            }
            if(upgrade>0)
            {
                float pulse=ShopRecruitPulse;
                Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),new Color(1,.93f,.64f,.08f*pulse));
                ShopBorder(rect,new Color(1,.91f,.59f,.94f*pulse),2);
            }
            if(shopCoin!=null)GUI.DrawTexture(new Rect(rect.x+11,rect.y+12,13,13),shopCoin,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(rect.x+29,rect.y+5,36,26),role.Cost.ToString(),LoopText(23,TextAnchor.MiddleLeft,Color.white,700));
            GUI.Label(new Rect(rect.x+10,rect.y+75,rect.width-20,22),role.Name,LoopText(role.Name.Length>10?12:14,TextAnchor.MiddleLeft,Color.white,600));
            var tags=LoopText(9,TextAnchor.UpperLeft,new Color(.89f,.93f,.96f));tags.wordWrap=true;
            GUI.Label(new Rect(rect.x+10,rect.y+98,rect.width-20,22),string.Join(" / ",ShopTags(id)),tags);
            if(upgrade>0)DrawApprovedUpgradeCue(new Rect(rect.xMax-65,rect.y+6,59,17),upgrade);
            else if(copies>0)
            {
                var owned=new Rect(rect.xMax-45,rect.y+6,39,17);Fill(owned,new Color(.90f,.95f,.98f,.93f));
                GUI.Label(owned,"持有 "+copies,LoopText(9,TextAnchor.MiddleCenter,ApprovedUi.Ink,500));
            }
            bool enabled=GUI.enabled;GUI.enabled=enabled&&trading&&shop.CanAfford(role.Cost);
            if(GUI.Button(rect,new GUIContent(string.Empty,role.Name+" · "+role.Cost+"金币 · 持有"+copies+"/9"),GUIStyle.none))
            {shopPurchaseOrigin=global.center;PurchaseShopCard(slot);}
            GUI.enabled=enabled;
        }
        void DrawApprovedUpgradeCue(Rect rect,int star)
        {
            Fill(rect,new Color(.94f,.83f,.50f,.97f));
            GUI.Label(new Rect(rect.x+4,rect.y,29,rect.height),"可合成",LoopText(9,TextAnchor.MiddleCenter,new Color(.36f,.28f,.14f),600));
            DrawShopStars(new Rect(rect.x+33,rect.y+2,rect.width-36,rect.height-4),star,new Color(.45f,.32f,.11f));
        }
        void DrawApprovedRecruitList(bool trading,float reveal)
        {
            Rect panel=ShopRecruitListRect;float offset=-(1-reveal)*panel.height;bool enabled=GUI.enabled;
            GUI.BeginGroup(panel);
            try
            {
                Fill(new Rect(0,offset,panel.width,panel.height),ApprovedUi.Paper);
                GUI.Label(new Rect(10,offset+5,112,25),"招募清单",LoopText(14,TextAnchor.MiddleLeft,null,700));
                GUI.Label(new Rect(124,offset+7,118,21),"与上方五张商品同步",LoopText(8,TextAnchor.MiddleRight,ApprovedUi.Muted));
                for(int i=0;i<5;i++)
                {
                    Rect global=ShopRecruitRowRect(i);global.y+=offset;Rect row=global;row.position-=panel.position;
                    string id=shop.Offers[i];Fill(row,new Color(.94f,.96f,.97f));ShopBorder(row,new Color(.71f,.78f,.83f),1);
                    if(id==null){GUI.Label(row,"暂无商品",LoopText(10,TextAnchor.MiddleCenter,ApprovedUi.Muted));continue;}
                    var role=shop.Definition(id);int copies=ShopHeldCopies(id);var preview=shop.PreviewPurchase(i);
                    int upgrade=preview.Success&&preview.FinalStar>1?preview.FinalStar:0;
                    if(copies>0)Fill(row,new Color(.90f,.95f,.98f,.45f));
                    Fill(new Rect(row.x,row.y,3,row.height),Color.Lerp(ApprovedUi.Tier(role.Cost),Color.white,.25f));
                    DrawTestPortrait(new Rect(row.x+5,row.y+1,31,32),OriginalTalulahArt.CardPortrait(shopDefinitions[id].Card));
                    GUI.Label(new Rect(row.x+42,row.y+2,row.width-76,17),role.Name,LoopText(12,TextAnchor.MiddleLeft,null,600));
                    string tags=(copies>0?"持有 "+copies+"/9 · ":"")+string.Join(" / ",ShopTags(id));
                    GUI.Label(new Rect(row.x+42,row.y+20,row.width-79,12),tags,LoopText(8,TextAnchor.MiddleLeft,upgrade>0?new Color(.52f,.39f,.16f):ApprovedUi.Muted));
                    if(shopCoin!=null)GUI.DrawTexture(new Rect(row.xMax-29,row.y+11,11,11),shopCoin,ScaleMode.ScaleToFit,true);
                    GUI.Label(new Rect(row.xMax-16,row.y+4,13,26),role.Cost.ToString(),LoopText(16,TextAnchor.MiddleCenter,null,600));
                    if(upgrade>0)ShopBorder(row,new Color(.81f,.66f,.29f,.15f+.85f*ShopRecruitPulse),2);
                    GUI.enabled=enabled&&trading&&shop.CanAfford(role.Cost);
                    if(GUI.Button(row,new GUIContent(string.Empty,role.Name+" · "+string.Join(" / ",ShopTags(id))),GUIStyle.none))
                    {shopPurchaseOrigin=global.center;PurchaseShopCard(i);}
                    GUI.enabled=enabled;
                }
                ShopBorder(new Rect(0,offset,panel.width,panel.height),ApprovedUi.Rule,1);
            }
            finally{GUI.enabled=enabled;GUI.EndGroup();}
        }
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
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&loopMenuOpen){loopMenuOpen=false;CancelShopDrag();e.Use();}
            if(!loopMenuOpen&&ShopPointerEvent(e.type,point,e.button))e.Use();
            EventType originalEvent=e.type;bool suppressed=loopMenuOpen&&originalEvent!=EventType.Repaint&&originalEvent!=EventType.Layout;
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
                if(loopMenuOpen){if(suppressed)e.type=originalEvent;DrawLoopMenu();}
            }
            finally{if(e.type==EventType.Ignore)e.type=originalEvent;GUI.enabled=enabled;GUI.matrix=previous;GUI.color=old;GUI.backgroundColor=background;}
        }
    }
}
