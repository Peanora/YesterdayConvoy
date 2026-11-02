using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public BattleShopRun Loop {get;private set;}
        public bool BattleShopLoopActive=>Loop!=null;
        public void StartBattleShopLoop(int seed=0)
        {
            if(seed==0)seed=Guid.NewGuid().GetHashCode();
            EnterShopTrial(0,StrongholdChallengeLayout.Id);
            shop=new ShopTrialModel(shopDefinitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),seed);
            var low=shop.Roles.Where(r=>r.Cost==1).OrderBy(r=>r.Id).ToArray();
            if(low.Length==0)throw new InvalidOperationException("Low-cost starter required");
            string starter=low[new System.Random(seed).Next(low.Length)].Id;
            shop.Preset(new[]{new ShopTrialModel.Piece{Id=starter,Star=1}},Array.Empty<string>(),1,0,10);
            Loop=new BattleShopRun(shop,seed);shopLineup.Clear();shopLineup.Add(starter);ConfigureLoopStage();
        }
        void ConfigureLoopStage()
        {
            CloseDefenderInspector();CancelShopDrag();SetShopVisible(false);ResetShopPresentation();
            shopMapCopy.defenders=BattleShopStages.Defenders(Loop.Stage);shopMapCopy.mapTitle="基础防线 · 第"+Loop.Stage+"关";
            SyncShopDeck(false,true);terminalPresentationElapsed=readyIdleElapsed=0;requestSequence=0;
        }
        public bool OpenLoopShop()
        {
            if(Loop==null||!Loop.OpenShop())return false;
            ConfigureLoopStage();SetShopVisible(true);return true;
        }
        public bool StartLoopNextBattle()
        {
            if(Loop==null||!Loop.CanShop||shopLineup.Count==0||shop.Bench.Count==0)return false;
            SyncShopDeck(false);if(!Loop.PrepareBattle())return false;SetShopVisible(false);CancelShopDrag();return true;
        }
        void StepBattleShopLoop()
        {
            if(Loop!=null&&Loop.Observe(Battle.Phase)){SetShopVisible(false);CancelShopDrag();CloseDefenderInspector();}
        }
        void DrawBattleShopLoop(GUIStyle label,GUIStyle button)
        {
            Fill(new Rect(12,602,1260,28),new Color(.03f,.06f,.08f,.98f));
            GUI.Label(new Rect(20,603,650,26),$"基础防线 {Loop.Stage}/2 · {new[]{"准备出兵","战斗中","战后结算","商店整备","已通关","挑战失败"}[(int)Loop.Phase]} · DP {Battle.DP} · 守方 {Battle.Defense}",label);
            if(Loop.CanShop){if(GUI.Button(new Rect(760,603,210,25),"开始第2关",button)&&!StartLoopNextBattle())ShopNotice("请至少编队一张卡");}
            else if(Loop.CanDeploy&&GUI.Button(new Rect(760,603,210,25),Battle.IsPaused?"继续战斗":"暂停战斗",button)&&Battle.Phase==BattlePhase.Running)Battle.SetPaused(!Battle.IsPaused);
            if(GUI.Button(new Rect(980,603,140,25),"重开本次挑战",button)){int seed=Loop.Seed;StartBattleShopLoop(seed);}
            if(GUI.Button(new Rect(1130,603,130,25),"退出",button))LeaveShopTrial();
            if(Loop==null||Loop.CanDeploy||Loop.CanShop)return;
            Fill(new Rect(300,220,680,330),new Color(.025f,.05f,.07f,.98f));
            var reward=Loop.Reward;
            GUI.Label(new Rect(330,240,600,40),Loop.Phase==BattleShopPhase.Failed?"挑战失败":Loop.Phase==BattleShopPhase.Complete?"两关通关！":"第1关胜利",new GUIStyle(label){fontSize=28});
            GUI.Label(new Rect(330,300,600,90),$"基础奖励 +{reward.Base}    利息 +{reward.Interest}    连胜 +{reward.Streak}\n合计 +{reward.Total} 金币    通关经验 +{reward.Experience}\n当前金币 {shop.Gold}    等级 {shop.Level}",label);
            if(Loop.Phase==BattleShopPhase.Settlement){if(GUI.Button(new Rect(400,435,480,55),"进入商店 · 购买并准备第2关",button))OpenLoopShop();}
            else if(GUI.Button(new Rect(400,435,480,55),"开始新挑战",button))StartBattleShopLoop();
        }
    }
}
