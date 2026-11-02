using System;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public BattleShopRun Loop {get;private set;}
        public bool BattleShopLoopActive=>Loop!=null;
        public void StartLateBattleShopLoop(int seed=0)=>StartBattleShopLoop(seed,true);
        public void StartBattleShopLoop(int seed=0,bool lateGame=false)
        {
            if(seed==0)seed=Guid.NewGuid().GetHashCode();
            EnterShopTrial(0,StrongholdChallengeLayout.Id);
            shop=new ShopTrialModel(shopDefinitions.Values.Select(d=>new ShopTrialModel.Role{Id=d.Card.CardId,Name=d.Card.DisplayName,Cost=d.Card.RecruitCost}),seed);
            var low=shop.Roles.Where(r=>r.Cost==1).OrderBy(r=>r.Id).ToArray();
            if(low.Length==0)throw new InvalidOperationException("Low-cost starter required");
            string starter=low[new System.Random(seed).Next(low.Length)].Id;
            string[] roster=lateGame?new[]{"CARD_M08","CARD_M02","CARD_M17","CARD_M18","CARD_M05"}:new[]{starter};
            shop.Preset(roster.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1}),Array.Empty<string>(),lateGame?7:1,0,lateGame?30:10);
            Loop=new BattleShopRun(shop,seed,lateGame);shopLineup.Clear();shopLineup.AddRange(roster);ConfigureLoopStage();
        }
        void ConfigureLoopStage()
        {
            CloseDefenderInspector();CancelShopDrag();SetShopVisible(false);ResetShopPresentation();
            shopMapCopy.defenders=BattleShopStages.Defenders(Loop.Stage,Loop.LateGame);shopMapCopy.mapTitle=(Loop.LateGame?"盟约防线":"基础防线")+" · 第"+Loop.Stage+"关";
            shopMessage=null;loopMenuOpen=false;
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
    }
}
