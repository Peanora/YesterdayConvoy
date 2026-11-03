using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool loopMenuOpen;
        bool loopTestSettlementOpen,loopWasTestSettled;
        static Rect LoopTestPanelRect=>new Rect(958,62,302,104);
        static readonly Color LoopInk=new Color(.082f,.094f,.11f,.97f);
        static readonly Color LoopPanel=new Color(.125f,.145f,.173f,.97f);
        static readonly Color LoopMuted=new Color(.71f,.74f,.77f);
        Rect ActiveShopPanelRect=>Loop==null?ShopPanelRect:new Rect(0,56,1280,576);
        Rect CurrentShopBattlefieldRect=>Loop==null?ShopBattlefieldRect:new Rect(0,56,1280,496);
        static Rect LoopRosterRect(int index)=>new Rect(22+index*121,581,112,124);
        static Rect LoopMenuRect=>new Rect(1034,62,226,186);
        public Rect ShopPieceRect(int bench)
        {
            if(Loop==null||Loop.CanShop)return ShopBenchRect(bench);
            if(bench<0||bench>=shop.Bench.Count)return Rect.zero;
            var piece=shop.Bench[bench];int index=shopLineup.IndexOf(piece.Id);
            var best=shop.Bench.Where(p=>p.Id==piece.Id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First();
            return index<0||piece.Instance!=best.Instance?Rect.zero:LoopRosterRect(index);
        }
        GUIStyle LoopText(int size=15,TextAnchor alignment=TextAnchor.MiddleLeft,Color? color=null)=>new GUIStyle(textStyle){fontSize=size,fontStyle=FontStyle.Normal,alignment=alignment,normal={textColor=color??Color.white}};
        bool LoopButton(Rect rect,string text,bool primary=false)
        {
            var style=new GUIStyle(buttonStyle){fontSize=15,fontStyle=FontStyle.Normal};
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused})state.textColor=primary?new Color(.09f,.11f,.13f):Color.white;
            var old=GUI.backgroundColor;GUI.backgroundColor=primary?new Color(.93f,.94f,.95f):new Color(.23f,.26f,.30f);
            bool clicked=GUI.Button(rect,text,style);GUI.backgroundColor=old;return clicked;
        }
        void DrawLoopInterface()
        {
            EnsureStyles();var e=Event.current;
            Vector2 point=TacticalViewport.Current.FromTopLeft(e.mousePosition);
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Space)e.Use();
            if(ShopPointerEvent(e.type,point,e.button))e.Use();
            Matrix4x4 previous=GUI.matrix;Color old=GUI.color,background=GUI.backgroundColor;
            GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                DrawShopDefenderBars();
                foreach(var unit in Battle.Units)
                {
                    if(!unit.Active||Battle.StrongholdInTransit(unit.EntityId))continue;
                    Vector2 p=GUIPosition(Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*UnitVisualHeight(unit)));
                    if(!CurrentShopBattlefieldRect.Contains(p))continue;
                    Fill(new Rect(p.x-26,p.y,52,5),LoopInk);Fill(new Rect(p.x-25,p.y+1,50*unit.Health/unit.MaxHealth,3),new Color(.4f,.9f,.55f));
                    if(Battle.UnitCombat(unit).HasSkill)DrawSkillResourceBar(p.x,p.y+7,unit);
                }
                Fill(new Rect(0,0,1280,56),LoopInk);
                GUI.Label(new Rect(20,10,350,36),shopMapCopy.mapTitle+"  /  "+(Loop.LateGame?"后期预置演示":"基础演示"),LoopText(13,TextAnchor.MiddleLeft,LoopMuted));
                GUI.Label(new Rect(486,10,90,36),"剩余干员",LoopText(13,TextAnchor.MiddleRight,LoopMuted));
                GUI.Label(new Rect(589,7,45,40),Battle.Defenders.Count(d=>d.Alive).ToString(),LoopText(27));
                GUI.Label(new Rect(643,10,82,36),"守方防御",LoopText(13,TextAnchor.MiddleLeft,LoopMuted));
                GUI.Label(new Rect(737,7,60,40),Battle.Defense.ToString(),LoopText(27,TextAnchor.MiddleLeft,new Color(.5f,.78f,.9f)));
                if(Debug.isDebugBuild&&Loop.CanDeploy&&LoopButton(new Rect(958,11,122,34),loopTestSettlementOpen?"测试结算 · 开":"测试结算 · 关"))
                {loopTestSettlementOpen=!loopTestSettlementOpen;loopMenuOpen=false;CancelShopDrag();CloseDefenderInspector();CloseSkillMenu();}
                if(LoopButton(new Rect(1090,11,48,34),"菜单")){loopMenuOpen=!loopMenuOpen;loopTestSettlementOpen=false;}
                if(LoopButton(new Rect(1144,11,48,34),SkillMenuOpen?"慢速":SimulationSpeed+"×"))ToggleSpeed();
                GUI.enabled=Loop.CanDeploy;
                if(LoopButton(new Rect(1198,11,62,34),Battle.IsPaused?"继续":"暂停"))Battle.SetPaused(!Battle.IsPaused);
                GUI.enabled=true;
                if(Loop.CanShop||ShopPanelTransitioning)DrawLoopShop();else DrawLoopRoster(point);
                if(Loop.CanDeploy&&!shopVisible&&!ShopPanelTransitioning)
                {
                    DrawShopRouteLabels(LoopText(12));DrawInteractionOverlay();DrawDefenderInspector(LoopText(),buttonStyle);
                }
                if(shopDrag>=0&&shopMoving)
                {
                    var piece=shop.Bench.FirstOrDefault(p=>p.Instance==shopDrag);
                    if(piece!=null&&Loop.CanDeploy)DrawTestPortrait(new Rect(point.x-38,point.y-100,76,94),OriginalTalulahArt.CardPortrait(shopDefinitions[piece.Id].Card));
                    if(Loop.CanShop)
                    {
                        var q=shop.Quote(shopDrag);Fill(ShopSellRect,LoopPanel);
                        GUI.Label(ShopSellRect,$"松开出售   {q.Message}   +{q.Value} 金币",LoopText(14,TextAnchor.MiddleCenter));
                    }
                }
                if(Time.unscaledTime<shopMessageUntil&&!string.IsNullOrEmpty(shopMessage))
                {
                    Fill(new Rect(350,514,580,30),LoopInk);GUI.Label(new Rect(365,514,550,30),shopMessage,LoopText(13,TextAnchor.MiddleCenter));
                }
                if(!Loop.CanDeploy&&!Loop.CanShop)DrawLoopSettlement();
                if(loopMenuOpen)DrawLoopMenu();
                if(Debug.isDebugBuild&&Loop.CanDeploy&&loopTestSettlementOpen)DrawLoopTestSettlement();
            }
            finally{GUI.enabled=true;GUI.matrix=previous;GUI.color=old;GUI.backgroundColor=background;}
        }
        void DrawLoopRoster(Vector2 point)
        {
            Fill(new Rect(0,552,1280,168),LoopInk);Fill(new Rect(0,552,1280,1),new Color(.36f,.40f,.44f));
            GUI.Label(new Rect(22,556,140,23),"编队",LoopText(12,TextAnchor.MiddleLeft,LoopMuted));
            for(int i=0;i<shopLineup.Count;i++)
            {
                string id=shopLineup[i];var state=Battle.FindCard(id);if(state==null)continue;
                var card=shopDefinitions[id].Card;Rect r=LoopRosterRect(i);DrawLoopCostBackground(r,card.RecruitCost);
                DrawTestPortrait(new Rect(r.x+2,r.y+2,r.width-4,96),OriginalTalulahArt.CardPortrait(card));
                Fill(new Rect(r.x,r.y,33,30),LoopInk);GUI.Label(new Rect(r.x+6,r.y-1,32,31),state.Settings.DeploymentCost.ToString(),LoopText(23));
                Fill(new Rect(r.x,r.yMax-26,r.width,26),LoopInk);GUI.Label(new Rect(r.x+6,r.yMax-26,r.width-12,26),card.DisplayName,LoopText(card.DisplayName.Length>6?11:12));
                GUI.Label(new Rect(r.x+58,r.y+3,48,22),state.RemainingUses+"次",LoopText(11,TextAnchor.MiddleRight));
                double cd=Battle.CardCooldown(id);
                if(cd>0||state.RemainingUses==0||Battle.DP<state.Settings.DeploymentCost)
                {
                    Fill(new Rect(r.x,r.y,r.width,98),new Color(0,0,0,.66f));
                    GUI.Label(new Rect(r.x,r.y+36,r.width,30),cd>0?$"{cd:0.0}s":state.RemainingUses==0?"次数用尽":"费用不足",LoopText(12,TextAnchor.MiddleCenter));
                }
                Fill(new Rect(r.x,r.yMax-2,r.width,2),ShopTier(card.RecruitCost));
            }
            Fill(new Rect(1108,584,1,106),new Color(.33f,.37f,.40f));
            GUI.Label(new Rect(1130,575,130,29),"战斗费用",LoopText(12,TextAnchor.MiddleLeft,LoopMuted));
            GUI.Label(new Rect(1128,602,140,62),Battle.DP.ToString(),LoopText(48));
            GUI.Label(new Rect(1130,668,130,25),$"编队 {shopLineup.Count} / {shop.Level}",LoopText(12,TextAnchor.MiddleLeft,LoopMuted));
        }
        void DrawLoopCostBackground(Rect rect,int recruitCost)
        {
            // Reuse the original shop's five-cost palette; deployment DP and merge stars stay separate.
            Color tier=ShopTier(recruitCost);Fill(rect,Color.Lerp(LoopPanel,tier,.68f));
            if(shopFade!=null)
            {
                Color previous=GUI.color;GUI.color=previous*tier;
                GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),shopFade);GUI.color=previous;
            }
            ShopBorder(rect,Color.Lerp(tier,Color.white,.15f),2);
        }
        void DrawLoopShop()
        {
            float reveal=ShopPanelVisibility;Fill(new Rect(0,56,1280,576),new Color(.065f,.08f,.10f,.94f*reveal));
            bool trading=Loop.CanShop&&shopVisible&&!ShopPanelTransitioning;
            GUI.enabled=trading;
            GUI.Label(new Rect(24,75,300,40),"战后整备",LoopText(26));
            GUI.Label(new Rect(582,80,150,30),$"等级 {shop.Level}   {shop.XP}/{ShopTrialModel.Experience[shop.Level]}",LoopText(13,TextAnchor.MiddleRight,LoopMuted));
            GUI.Label(new Rect(755,76,160,38),"金币 "+shop.Gold,LoopText(24,TextAnchor.MiddleLeft,new Color(.94f,.83f,.61f)));
            if(LoopButton(new Rect(919,78,112,34),"购买经验  4")){var result=shop.Upgrade();if(result.Success)SyncShopDeck();else ShopNotice(result.Message);}
            if(LoopButton(new Rect(1040,78,100,34),"刷新  2")){var result=shop.Refresh();if(result.Success)shopRefreshAt=Time.unscaledTime;else ShopNotice(result.Message);}
            if(LoopButton(new Rect(1149,78,107,34),"锁店  "+(shop.Locked?"开":"关")))shop.Locked=!shop.Locked;
            for(int i=0;i<5;i++)
            {
                Rect r=new Rect(24+i*249,136,232,335);Fill(r,LoopPanel);string id=shop.Offers[i];
                if(id==null){GUI.Label(r,"已购 / 暂无库存",LoopText(14,TextAnchor.MiddleCenter,LoopMuted));continue;}
                var role=shop.Definition(id);var preview=shop.PreviewPurchase(i);
                DrawLoopCostBackground(r,role.Cost);
                DrawShopRankPortrait(new Rect(r.x+6,r.y+4,r.width-12,232),OriginalTalulahArt.CardPortrait(shopDefinitions[id].Card),preview.Success?preview.FinalStar:1);
                GUI.Label(new Rect(r.x+12,r.y+235,r.width-24,29),role.Name,LoopText(16));
                GUI.Label(new Rect(r.x+12,r.y+266,r.width-24,22),string.Join(" / ",ShopTags(id)),LoopText(11,TextAnchor.MiddleLeft,LoopMuted));
                GUI.enabled=trading&&shop.Gold>=role.Cost;
                if(LoopButton(new Rect(r.x,r.yMax-38,r.width,38),"招募                         "+role.Cost,true))PurchaseShopCard(i);
                GUI.enabled=trading;
            }
            GUI.Label(new Rect(24,490,650,30),$"编队 {shopLineup.Count}/{shop.Level}   备战 {shop.Bench.Count}/10",LoopText(14,TextAnchor.MiddleLeft,LoopMuted));
            GUI.Label(new Rect(24,526,820,28),"轻点备战棋子调整编队，拖到下沿出售",LoopText(12,TextAnchor.MiddleLeft,LoopMuted));
            if(LoopButton(new Rect(1036,513,220,46),"开始下一战",true)&&!StartLoopNextBattle())ShopNotice("请至少编队一张卡");
            GUI.enabled=true;
            DrawShopBench(LoopText(12,TextAnchor.MiddleCenter));
        }
        void DrawLoopSettlement()
        {
            Fill(new Rect(0,56,1280,664),new Color(.05f,.07f,.09f,.91f));
            var reward=Loop.Reward;bool failed=Loop.Phase==BattleShopPhase.Failed;
            Fill(new Rect(195,164,4,364),failed?new Color(.85f,.35f,.28f):Color.white);
            GUI.Label(new Rect(225,168,330,30),"第"+Loop.Stage+"关结束"+(loopWasTestSettled?" · 测试结算":""),LoopText(14,TextAnchor.MiddleLeft,LoopMuted));
            GUI.Label(new Rect(224,210,345,88),failed?"作战失败":Loop.Phase==BattleShopPhase.Complete?"演示通关":"作战胜利",LoopText(54));
            GUI.Label(new Rect(225,312,320,32),shopMapCopy.mapTitle,LoopText(19));
            GUI.Label(new Rect(225,356,320,48),failed?"本次挑战结束":Loop.Phase==BattleShopPhase.Complete?"两道防线均已突破":"补充阵容后迎战下一道防线",LoopText(13,TextAnchor.UpperLeft,LoopMuted));
            string[] names={"基础奖励","利息","连胜奖励"};int[] values={reward.Base,reward.Interest,reward.Streak};
            GUI.Label(new Rect(590,167,500,30),"本场收入",LoopText(14,TextAnchor.MiddleLeft,LoopMuted));
            for(int i=0;i<3;i++){float y=218+i*46;GUI.Label(new Rect(590,y,270,35),names[i],LoopText(18));GUI.Label(new Rect(960,y,126,35),"+"+values[i],LoopText(18,TextAnchor.MiddleRight));Fill(new Rect(590,y+39,496,1),new Color(.26f,.30f,.34f));}
            GUI.Label(new Rect(590,370,200,42),"金币合计",LoopText(18));GUI.Label(new Rect(870,364,216,54),$"{shop.Gold-reward.Total} → {shop.Gold}",LoopText(31,TextAnchor.MiddleRight,new Color(.94f,.83f,.61f)));
            GUI.Label(new Rect(590,429,490,30),$"经验 +{reward.Experience}    等级 {shop.Level}    {shop.XP}/{ShopTrialModel.Experience[shop.Level]}",LoopText(13,TextAnchor.MiddleLeft,LoopMuted));
            if(LoopButton(new Rect(590,480,496,48),Loop.Phase==BattleShopPhase.Settlement?"前往商店":"开始新挑战",true))
            {if(Loop.Phase==BattleShopPhase.Settlement)OpenLoopShop();else StartBattleShopLoop(0,Loop.LateGame);}
        }
        void DrawLoopMenu()
        {
            Fill(LoopMenuRect,LoopPanel);
            if(LoopButton(new Rect(1046,74,202,34),Muted?"开启音效":"关闭音效"))Muted=!Muted;
            if(LoopButton(new Rect(1046,116,202,34),"重新开始本次挑战")){int seed=Loop.Seed;bool late=Loop.LateGame;StartBattleShopLoop(seed,late);}
            if(LoopButton(new Rect(1046,158,202,34),"返回测试选择"))LeaveShopTrial();
            if(LoopButton(new Rect(1046,200,202,34),"关闭菜单"))loopMenuOpen=false;
        }
        void DrawLoopTestSettlement()
        {
            Fill(LoopTestPanelRect,LoopPanel);
            GUI.Label(new Rect(970,70,278,26),"仅供测试 · 会正常结算奖励",LoopText(12,TextAnchor.MiddleLeft,LoopMuted));
            if(LoopButton(new Rect(970,108,132,42),"判胜利",true))TryTestSettlement(true);
            if(LoopButton(new Rect(1116,108,132,42),"判失败"))TryTestSettlement(false);
        }
    }
}
