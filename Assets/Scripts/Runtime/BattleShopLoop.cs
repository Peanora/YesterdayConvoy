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
            CancelArtPreparation();
            if(seed==0)seed=Guid.NewGuid().GetHashCode();
            EnterShopTrial(0,StrongholdChallengeLayout.Id);
            if(lateGame)
            {
                shopDefinitions[talulahDefinition.Card.CardId]=talulahDefinition;
                var frost=monsterTests.Single(o=>o.Id=="CARD_M14"&&o.Playable).Definition;
                shopDefinitions[frost.Card.CardId]=frost;
                var mandra=monsterTests.Single(o=>o.Id=="CARD_M32"&&o.Playable).Definition;
                shopDefinitions[mandra.Card.CardId]=mandra;
                var mephi=monsterTests.Single(o=>o.Id=="CARD_M25"&&o.Playable).Definition;
                shopDefinitions[mephi.Card.CardId]=mephi;
                var patriot=monsterTests.Single(o=>o.Id=="CARD_M20"&&o.Playable).Definition;
                shopDefinitions[patriot.Card.CardId]=patriot;
                var wolf=monsterTests.Single(o=>o.Id==SilverWolfCombat.CardId && o.Playable).Definition;
                shopDefinitions[wolf.Card.CardId]=wolf;
            }
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
            var dedicated=SynergySandbox?activeSynergyTrial:null;
            // The explicit IV benchmark keeps its existing original-stat six-star lineup.
            // Every normal restart re-copies the dedicated asset before applying sandbox multipliers.
            if(JourneyActive)ConfigureJourneyStage();
            else
            {
                shopMapCopy.defenders=ApprovedMapActive?ApprovedMap.CopyDefenders():BlackForestChallengeActive?BlackForestChallengePlan.CopyDefenders():dedicated!=null&&!golemBenchmark?dedicated.CopyDefenders():CampaignActive?(SynergySandbox&&golemBenchmark?BattleShopStages.Defenders(2,true):CampaignPlan.Guards(Loop.Stage,SynergySandbox)):BattleShopStages.Defenders(Loop.Stage,Loop.LateGame);
                shopMapCopy.mapTitle=ApprovedMapActive?ApprovedMap.title:BlackForestChallengeActive?BlackForestChallengePlan.Title:dedicated!=null?dedicated.displayTitle+(golemBenchmark?" · 完全体原数值基准":""):CampaignActive?(SynergySandbox?"羁绊自由试验":CampaignPlan.Title(Loop.Stage)):(Loop.LateGame?"盟约防线":"基础防线")+" · 第"+Loop.Stage+"关";
            }
            if(BoostSandboxStats)foreach(var guard in shopMapCopy.defenders){guard.health*=SandboxDefenderHealth;guard.attack*=SandboxDefenderAttack;}
            shopMessage=null;loopMenuOpen=false;loopTestSettlementOpen=loopWasTestSettled=false;
            SyncShopDeck(false,true);terminalPresentationElapsed=readyIdleElapsed=0;requestSequence=0;
        }
        public bool OpenLoopShop()
        {
            if(JourneyActive)return AdvanceJourney();
            if(Loop==null)return false;
            if(Loop.LateGame&&Loop.Stage==1&&Loop.Phase==BattleShopPhase.Settlement)shop.GuaranteedRefreshOffer=talulahDefinition.Card.CardId;
            if(!Loop.OpenShop())return false;
            ConfigureLoopStage();SetShopVisible(true);return true;
        }
        public bool StartLoopNextBattle()
        {
            if(Loop==null||!Loop.CanShop||shopLineup.Count==0||shop.Bench.Count==0)return false;
            CompleteGolemExperienceFeedback();
            CancelArtPreparation();
            CompactShopLineupSeats();SyncShopDeck(false);if(!Loop.PrepareBattle())return false;SetShopVisible(false);CancelShopDrag();
            if(Battle.WavesEnabled)Battle.Begin();
            return true;
        }
        void StepBattleShopLoop()
        {
            int before=Loop?.GolemExperience??0;
            if(Loop!=null&&Loop.Observe(Battle.Phase,Battle.GolemGrowthEarned,golemCapacity:Battle.GolemCapacity))
            {RecordJourneyRound();BeginGolemExperienceFeedback(before);SetShopVisible(false);CancelShopDrag();CloseDefenderInspector();}
        }
        bool TryTestSettlement(bool victory)
        {
            if(!Debug.isDebugBuild||!loopTestSettlementOpen||Loop==null||!Loop.CanDeploy||!Battle.FinishForTesting(Battle.RunId,victory))return false;
            loopTestSettlementOpen=loopMenuOpen=false;loopWasTestSettled=true;
            CancelInteraction();CancelShopDrag();CloseDefenderInspector();CloseSkillMenu();
            StepBattleShopLoop();
            Debug.Log("TEST_SETTLEMENT: stage="+Loop.Stage+"; victory="+victory+"; run="+Battle.RunId);
            return true;
        }
    }
}
