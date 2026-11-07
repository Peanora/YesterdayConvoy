using System;
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
        static readonly Color LoopPanel=ApprovedUi.Paper;
        static readonly Color LoopMuted=ApprovedUi.Muted;
        Rect ActiveShopPanelRect=>ShopPanelRect;
        Rect CurrentShopBattlefieldRect=>new Rect(0,0,1280,720);
        Rect LoopRosterRect(int index)=>new Rect(18+index*106,581,100,124);
        static Rect LoopMenuRect=>ApprovedUi.Menu;
        public Rect ShopPieceRect(int bench)
        {
            if(shop==null||bench<0||bench>=shop.Bench.Count)return Rect.zero;
            if(Loop==null?Battle.Phase==BattlePhase.Ready:Loop.CanShop)return ShopPreparationPieceRect(bench);
            var piece=shop.Bench[bench];int index=shopLineup.IndexOf(piece.Id);
            var best=shop.Bench.Where(p=>p.Id==piece.Id).OrderByDescending(p=>p.Star).ThenBy(p=>p.Instance).First();
            return index<0||piece.Instance!=best.Instance?Rect.zero:LoopRosterRect(index);
        }
        GUIStyle LoopText(int size=15,TextAnchor alignment=TextAnchor.MiddleLeft,Color? color=null,int weight=400)
            =>ApprovedUi.Text(scene.InterfaceFont,size,alignment,color??ApprovedUi.Ink,weight);
        bool LoopButton(Rect rect,string text,bool primary=false)
        {
            var style=LoopText(14,TextAnchor.MiddleCenter,primary?new Color(.26f,.23f,.16f):ApprovedUi.Ink,600);
            style.padding=new RectOffset(5,5,0,0);style.border=new RectOffset();
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused})
            {state.textColor=primary?new Color(.26f,.23f,.16f):ApprovedUi.Ink;state.background=Texture2D.whiteTexture;}
            Color old=GUI.backgroundColor;GUI.backgroundColor=primary?ApprovedUi.Gold:new Color(.82f,.87f,.90f);
            bool clicked=GUI.Button(rect,text,style);GUI.backgroundColor=old;
            ApprovedUi.Border(rect,new Color(.51f,.61f,.69f,GUI.enabled?.55f:.2f));
            return clicked;
        }
        void DrawLoopHeader()
        {
            bool preparing=Loop==null?Battle.Phase==BattlePhase.Ready:Loop.CanShop;
            string mode=Loop==null?(TacticalMapId==BlackForestLayout.Id?"地形试走 · 暂无守军":"商店试验"):CampaignActive?(SynergySandbox?"羁绊验证":"三阶段挑战"):Loop.LateGame?"后期演示":"基础演示";
            string phase=preparing?"整备阶段":Battle.IsPaused?"已暂停":Battle.Phase==BattlePhase.Running?"战斗进行中":"本关结束";
            Fill(new Rect(16,8,185,46),ApprovedUi.Paper);Fill(new Rect(16,8,3,46),new Color(.29f,.44f,.55f));
            GUI.Label(new Rect(28,11,165,24),shopMapCopy!=null?shopMapCopy.mapTitle:"商店试验",LoopText(17,TextAnchor.MiddleLeft,null,700));
            GUI.Label(new Rect(28,36,165,15),mode+"  /  "+phase,LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            Fill(new Rect(578,8,151,46),ApprovedUi.Paper);DrawLoopShield(new Vector2(589,20),new Color(.40f,.56f,.67f));
            GUI.Label(new Rect(613,14,76,21),"守方防御",LoopText(12));
            GUI.Label(new Rect(613,34,87,14),"存活干员 "+Battle.Defenders.Count(d=>d.Alive),LoopText(9,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(690,12,32,33),Battle.Defense.ToString(),LoopText(26,TextAnchor.MiddleCenter,null,700));
            Fill(new Rect(1018,8,100,42),ApprovedUi.Paper);
            if(shopCoin!=null)GUI.DrawTexture(new Rect(1028,20,15,15),shopCoin,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(1048,17,27,24),"金币",LoopText(10,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            string gold=shop.UnlimitedGold?"∞":shop.Gold.ToString();int goldSize=gold.Length>=5?14:gold.Length==4?18:gold.Length==3?22:25;
            GUI.Label(new Rect(1070,11,42,33),gold,LoopText(goldSize,TextAnchor.MiddleRight,new Color(.44f,.33f,.16f),600));
            if(LoopButton(new Rect(1126,8,42,42),SkillMenuOpen?"慢速":SimulationSpeed+"×"))ToggleSpeed();
            bool enabled=GUI.enabled;GUI.enabled=enabled&&!preparing&&(Loop==null?Battle.Phase==BattlePhase.Running:Loop.CanDeploy);
            if(LoopButton(new Rect(1174,8,42,42),Battle.IsPaused?"继续":"暂停"))Battle.SetPaused(!Battle.IsPaused);
            GUI.enabled=enabled;
            if(LoopButton(new Rect(1222,8,42,42),"菜单"))
            {loopMenuOpen=!loopMenuOpen;loopTestSettlementOpen=false;CancelShopDrag();CloseDefenderInspector();CloseSkillMenu();}
        }
        static void DrawLoopShield(Vector2 origin,Color color)
        {
            Vector2[] vertices={new Vector2(0,0),new Vector2(15,0),new Vector2(15,12),new Vector2(7.5f,19),new Vector2(0,12),new Vector2(0,0)};
            for(int i=0;i<vertices.Length-1;i++)ShopLine(origin+vertices[i],origin+vertices[i+1],color,1.5f);
            ShopLine(origin+new Vector2(5,4),origin+new Vector2(10,4),color,1.5f);
            ShopLine(origin+new Vector2(7.5f,4),origin+new Vector2(7.5f,12),color,1.5f);
        }
        void DrawLoopInterface()
        {
            EnsureStyles();var e=Event.current;
            Vector2 point=TacticalViewport.Current.FromTopLeft(e.mousePosition);
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&(loopMenuOpen||loopTestSettlementOpen))
            {loopMenuOpen=loopTestSettlementOpen=false;CancelShopDrag();e.Use();}
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Space)e.Use();
            bool campaignModal=CampaignUiModalOpen;
            bool modal=campaignModal||loopMenuOpen||loopTestSettlementOpen;
            if(!modal&&ShopPointerEvent(e.type,point,e.button))e.Use();
            EventType originalEvent=e.type;
            bool suppressed=modal&&originalEvent!=EventType.Repaint&&originalEvent!=EventType.Layout;
            Matrix4x4 previous=GUI.matrix;Color old=GUI.color,background=GUI.backgroundColor;
            GUI.matrix=TacticalViewport.Current.Matrix;
            try
            {
                // Covered controls still receive IDs but never receive the modal's pointer events.
                if(suppressed)e.type=EventType.Ignore;
                DrawShopDefenderBars();
                foreach(var unit in Battle.Units)
                {
                    if(!unit.Active||Battle.StrongholdInTransit(unit.EntityId))continue;
                    var spirit=Battle.HerdSpiritFor(unit.EntityId);
                    if(spirit!=null&&Battle.Clock<spirit.StartedAt+HerdSpiritState.FallDuration)continue;
                    Vector2 p=GUIPosition(Camera.main.WorldToScreenPoint(unit.Motion.Position+Camera.main.transform.up*UnitVisualHeight(unit)));
                    if(!CurrentShopBattlefieldRect.Contains(p))continue;
                    Fill(new Rect(p.x-26,p.y,52,5),LoopInk);Fill(new Rect(p.x-25,p.y+1,50*unit.Health/unit.MaxHealth,3),new Color(.4f,.9f,.55f));
                    if(unit.SynergyShield>0&&unit.SynergyShieldUntil>Battle.Clock)Fill(new Rect(p.x-25,p.y-3,50*Mathf.Clamp01(unit.SynergyShield/unit.MaxHealth),2),new Color(.35f,.8f,1));
                    float soulShield=Battle.ContractShieldRemaining(unit);
                    if(soulShield>0)Fill(new Rect(p.x-25,p.y-6,50*Mathf.Clamp01(soulShield/unit.MaxHealth),2),new Color(.81f,.65f,1));
                    if(spirit!=null)GUI.Label(new Rect(p.x-40,p.y+7,80,20),unit.IsReviving?$"凝聚 {Math.Max(0,spirit.ReadyAt-Battle.Clock):0.0}s":$"灵体 {Battle.HerdRemaining(unit.EntityId):0.0}s",LoopText(11,TextAnchor.MiddleCenter,new Color(.55f,.88f,1)));
                    if(Battle.SarkazTier>0&&Battle.IsContractBeneficiary(unit))
                    {
                        string contract=unit.CardId==Battle.ContractCard?"承契者":Battle.ContractAttackBonus(unit)>0?"众魂共鸣":null;
                        if(contract!=null)GUI.Label(new Rect(p.x-48,p.y-24,96,20),contract,LoopText(11,TextAnchor.MiddleCenter,Color.white));
                    }
                    if(Battle.UnitCombat(unit).HasSkill)DrawSkillResourceBar(p.x,p.y+7,unit);
                }
                DrawLoopHeader();
                if(Loop.CanShop||ShopPanelTransitioning)DrawLoopShop();else DrawLoopRoster(point);
                DrawShopFlows();
                DrawSynergyActivationFeedback();
                if(Loop.CanDeploy&&!shopVisible&&!ShopPanelTransitioning)
                {
                    DrawShopRouteLabels(LoopText(12,TextAnchor.MiddleLeft,Color.white));DrawInteractionOverlay();DrawDefenderInspector(LoopText(),buttonStyle);
                }
                if(Time.unscaledTime<shopMessageUntil&&!string.IsNullOrEmpty(shopMessage))
                {
                    Fill(new Rect(282,333,690,28),ApprovedUi.Paper);GUI.Label(new Rect(294,333,666,28),shopMessage,LoopText(12,TextAnchor.MiddleCenter));
                }
                if(campaignModal&&suppressed)e.type=originalEvent;
                DrawCampaignControls(point);
                DrawSarkazContractBadges();
                if(Loop==null)return;
                if(!Loop.CanDeploy&&!Loop.CanShop&&!DeathPresentationPending)
                {if(suppressed)e.type=originalEvent;DrawLoopSettlement();}
                if(shopDrag>=0&&shopMoving)DrawShopSaleTargets(point);
                if(loopMenuOpen){if(suppressed)e.type=originalEvent;DrawLoopMenu();}
                if(Debug.isDebugBuild&&Loop!=null&&Loop.CanDeploy&&loopTestSettlementOpen)
                {if(suppressed)e.type=originalEvent;DrawLoopTestSettlement();}
            }
            finally{if(e.type==EventType.Ignore)e.type=originalEvent;GUI.enabled=true;GUI.matrix=previous;GUI.color=old;GUI.backgroundColor=background;}
        }
        void DrawLoopRoster(Vector2 point)
        {
            Fill(new Rect(0,552,1280,168),new Color(.12f,.20f,.26f,.35f));
            GUI.Label(new Rect(18,553,160,24),"部署栏",LoopText(14,TextAnchor.MiddleLeft,Color.white,600));
            GUI.Label(new Rect(150,553,520,24),"拖至红门部署 · 暂停时仍可放置",LoopText(11,TextAnchor.MiddleLeft,new Color(.86f,.92f,.96f)));
            for(int i=0;i<shopLineup.Count;i++)
            {
                string id=shopLineup[i];var state=Battle.FindCard(id);if(state==null)continue;
                var card=shopDefinitions[id].Card;Rect r=LoopRosterRect(i);DrawLoopCostBackground(r,card.RecruitCost);
                DrawTestPortrait(new Rect(r.x+2,r.y+2,r.width-4,96),OriginalTalulahArt.CardPortrait(card));
                Fill(new Rect(r.x+3,r.y+3,31,29),new Color(.13f,.23f,.32f,.88f));
                GUI.Label(new Rect(r.x+6,r.y+1,26,29),state.Settings.DeploymentCost.ToString(),LoopText(22,TextAnchor.MiddleLeft,Color.white,700));
                Fill(new Rect(r.x,r.yMax-27,r.width,27),Color.Lerp(ApprovedUi.Tier(card.RecruitCost),Color.black,.34f));
                GUI.Label(new Rect(r.x+5,r.yMax-27,r.width-10,17),card.DisplayName,LoopText(card.DisplayName.Length>6?10:11,TextAnchor.MiddleLeft,Color.white,600));
                GUI.Label(new Rect(r.x+5,r.yMax-12,r.width-10,12),state.RemainingUses+"次",LoopText(9,TextAnchor.MiddleLeft,new Color(.88f,.93f,.96f)));
                int star=shop.Bench.Where(p=>p.Id==id).Select(p=>p.Star).DefaultIfEmpty(1).Max();
                GUI.Label(new Rect(r.x+37,r.y+4,r.width-41,21),new string('★',star),LoopText(12,TextAnchor.MiddleRight,new Color(1,.94f,.73f),600));
                double cd=Battle.CardCooldown(id);
                if(cd>0||state.RemainingUses==0||Battle.DP<state.Settings.DeploymentCost)
                {
                    Fill(new Rect(r.x,r.y,r.width,97),new Color(.08f,.14f,.20f,.68f));
                    GUI.Label(new Rect(r.x,r.y+36,r.width,30),cd>0?$"{cd:0.0}s":state.RemainingUses==0?"次数用尽":"费用不足",LoopText(12,TextAnchor.MiddleCenter,Color.white));
                }
            }
            Fill(new Rect(1125,582,137,117),ApprovedUi.Paper);Fill(new Rect(1125,582,137,2),new Color(.35f,.49f,.60f));
            GUI.Label(new Rect(1138,590,113,23),"部署点 DP",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(1136,612,113,54),Battle.DP.ToString(),LoopText(42,TextAnchor.MiddleLeft,null,700));
            GUI.Label(new Rect(1138,671,113,19),$"编队 {shopLineup.Count} / {shop.Level}",LoopText(11,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            if(Battle.IsPaused)
            {
                Fill(new Rect(508,278,264,61),new Color(.90f,.93f,.95f,.94f));
                GUI.Label(new Rect(522,283,236,29),"暂停中",LoopText(24,TextAnchor.MiddleCenter,null,600));
                GUI.Label(new Rect(522,312,236,18),"可拖动部署 · 空格继续",LoopText(11,TextAnchor.MiddleCenter,ApprovedUi.Muted));
            }
        }
        void DrawLoopCostBackground(Rect rect,int recruitCost)
        {
            Color tier=ApprovedUi.Tier(recruitCost),top=Color.Lerp(tier,Color.white,.30f);
            for(int row=0;row<16;row++)Fill(new Rect(rect.x,rect.y+row*rect.height/16,rect.width,rect.height/16+1),Color.Lerp(top,tier,row/15f));
            Fill(new Rect(rect.x,rect.yMax-5,rect.width,5),Color.Lerp(tier,Color.black,.28f));
            ShopBorder(rect,Color.Lerp(tier,Color.white,.45f),1);
        }
        void DrawLoopShop()
        {
            DrawShopMarket(Loop.CanShop);
            DrawShopBench(LoopText(12,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(1142,624,120,18),"开战后锁定本场编队",LoopText(10,TextAnchor.MiddleCenter,Color.white));
            GUI.enabled=Loop.CanShop&&!ShopPanelTransitioning;
            if(LoopButton(new Rect(1142,643,120,50),"开始下一战  ▶",true))
            {if(shopLineup.Count==0)ShopNotice("请至少编队一张卡");else PrepareNextLoopBattle();}
            GUI.enabled=true;
        }
        void DrawLoopSettlement()
        {
            Fill(new Rect(0,0,1280,720),new Color(.10f,.16f,.21f,.38f));
            Fill(new Rect(176,152,952,416),ApprovedUi.Paper);ShopBorder(new Rect(176,152,952,416),ApprovedUi.Rule,1);
            var reward=Loop.Reward;bool failed=Loop.Phase==BattleShopPhase.Failed;
            Color accent=failed?new Color(.65f,.31f,.25f):new Color(.28f,.47f,.61f);
            Fill(new Rect(176,152,5,416),accent);
            GUI.Label(new Rect(206,173,340,29),(SynergySandbox?"第"+Loop.SandboxRound+"轮验证结束":"第"+Loop.Stage+"关结束")+(loopWasTestSettled?" · 测试结算":""),LoopText(13,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            GUI.Label(new Rect(204,219,346,70),failed?"作战失败":SynergySandbox?"作战胜利":Loop.Phase==BattleShopPhase.Complete?"演示通关":"作战胜利",LoopText(43,TextAnchor.MiddleLeft,accent,700));
            GUI.Label(new Rect(206,306,330,32),shopMapCopy.mapTitle,LoopText(18,TextAnchor.MiddleLeft,null,600));
            GUI.Label(new Rect(206,350,330,45),SynergySandbox?"保留阵容和培养，返回整备继续下一轮":failed?"本次挑战结束":Loop.Phase==BattleShopPhase.Complete?CampaignActive?"三阶段挑战完成":"两道防线均已突破":"补充阵容后迎战下一道防线",LoopText(12,TextAnchor.UpperLeft,ApprovedUi.Muted));
            DrawGolemSettlementFeedback();
            string[] names={"基础奖励","利息","连胜奖励"};int[] values={reward.Base,reward.Interest,reward.Streak};
            GUI.Label(new Rect(590,174,496,28),"本场收入",LoopText(13,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            for(int i=0;i<3;i++)
            {
                float y=217+i*44;GUI.Label(new Rect(590,y,270,32),names[i],LoopText(17));
                GUI.Label(new Rect(960,y,126,32),"+"+values[i],LoopText(18,TextAnchor.MiddleRight,null,600));
                Fill(new Rect(590,y+37,496,1),new Color(.69f,.76f,.80f,.6f));
            }
            GUI.Label(new Rect(590,368,180,40),"金币合计",LoopText(17));
            GUI.Label(new Rect(813,364,273,44),shop.UnlimitedGold?"∞":$"{reward.GoldBefore} → {shop.Gold}",LoopText(29,TextAnchor.MiddleRight,new Color(.49f,.36f,.16f),600));
            GUI.Label(new Rect(590,427,496,28),$"经验 +{reward.Experience}    等级 {shop.Level}    {shop.XP}/{shop.NextLevelXP}",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            if(LoopButton(new Rect(590,488,496,48),Loop.Phase==BattleShopPhase.Settlement?"前往商店":SynergySandbox?"返回整备继续验证":"开始新挑战",true))
            {if(Loop.Phase==BattleShopPhase.Settlement)OpenLoopShop();else if(SynergySandbox)ReplaySandbox();else RestartCurrentRun();}
        }
        void DrawLoopMenu()
        {
            Fill(LoopMenuRect,LoopPanel);ShopBorder(LoopMenuRect,ApprovedUi.Rule,1);
            float x=LoopMenuRect.x+12,y=LoopMenuRect.y+44;float width=LoopMenuRect.width-24;
            GUI.Label(new Rect(x,LoopMenuRect.y+9,width-30,27),"系统菜单",LoopText(19,TextAnchor.MiddleLeft,null,700));
            if(LoopButton(new Rect(LoopMenuRect.xMax-36,LoopMenuRect.y+9,24,24),"×"))loopMenuOpen=false;
            if(LoopButton(new Rect(x,y,width,30),Muted?"开启音效":"关闭音效"))Muted=!Muted;y+=37;
            if(LoopButton(new Rect(x,y,width,30),"重新开始本次挑战"))
            {if(Loop==null)EnterShopTrial(0,TacticalMapId);else RestartCurrentRun();return;}y+=37;
            if(Loop==null)
            {
                if(LoopButton(new Rect(x,y,width,30),"满席两星样板")){EnterShopTrial(1,TacticalMapId);loopMenuOpen=false;return;}y+=37;
                if(LoopButton(new Rect(x,y,width,30),"满席三星样板")){EnterShopTrial(2,TacticalMapId);loopMenuOpen=false;return;}y+=37;
            }
            if(LoopButton(new Rect(x,y,width,30),"返回测试选择")){ReturnToTrialScene();return;}y+=37;
            if(CampaignActive&&Loop.CanDeploy)
            {if(LoopButton(new Rect(x,y,width,30),"重开本关（保留运营）")){RestartCampaignEncounter();return;}y+=37;}
            if(Debug.isDebugBuild&&Loop!=null&&Loop.CanDeploy)
            {
                if(LoopButton(new Rect(x,y,width,30),"验证工具 · 直接结算"))
                {loopTestSettlementOpen=true;loopMenuOpen=false;CancelShopDrag();CloseDefenderInspector();CloseSkillMenu();}y+=37;
            }
            if(LoopButton(new Rect(x,y,width,30),"关闭菜单"))loopMenuOpen=false;
        }
        void ReturnToTrialScene(){LeaveShopTrial();UnityEngine.SceneManagement.SceneManager.LoadScene("MonsterTrial");}
        void DrawLoopTestSettlement()
        {
            Fill(LoopTestPanelRect,LoopPanel);ShopBorder(LoopTestPanelRect,ApprovedUi.Rule,1);
            GUI.Label(new Rect(970,70,248,26),"仅供测试 · 会正常结算奖励",LoopText(12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            if(LoopButton(new Rect(1227,71,23,23),"×")){loopTestSettlementOpen=false;return;}
            if(LoopButton(new Rect(970,108,132,42),"判胜利",true))TryTestSettlement(true);
            if(LoopButton(new Rect(1116,108,132,42),"判失败"))TryTestSettlement(false);
        }
    }
}
