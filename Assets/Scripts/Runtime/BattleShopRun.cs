using System;
namespace YesterdayConvoy
{
    public enum BattleShopPhase { Preparation,Fighting,Settlement,Shop,Complete,Failed }
    // One run owns one economy. Combat instances are discarded between stages.
    public sealed class BattleShopRun
    {
        public ShopTrialModel Economy {get;}
        public BattleShopPhase Phase {get;private set;}=BattleShopPhase.Preparation;
        public int Stage {get;private set;}=1;
        public readonly int Seed;
        public readonly bool LateGame;
        public readonly bool Campaign,Sandbox;
        public bool RewardStage=>Campaign&&!Sandbox&&CampaignPlan.Supply(Stage);
        public int TotalStages=>Campaign?Sandbox?1:CampaignPlan.Count:2;
        public ShopTrialModel.RoundReward Reward {get;private set;}
        public bool CanShop=>Phase==BattleShopPhase.Shop;
        public bool CanDeploy=>Phase==BattleShopPhase.Preparation||Phase==BattleShopPhase.Fighting;
        public BattleShopRun(ShopTrialModel economy,int seed,bool lateGame=false,bool campaign=false,bool sandbox=false){Campaign=campaign;Sandbox=sandbox;Economy=economy??throw new ArgumentNullException(nameof(economy));Seed=seed;LateGame=lateGame;}
        public bool Observe(BattlePhase battle)
        {
            if(!CanDeploy)return false;
            if(battle==BattlePhase.Running)Phase=BattleShopPhase.Fighting;
            if(battle!=BattlePhase.Won&&battle!=BattlePhase.Lost)return false;
            bool won=battle==BattlePhase.Won;Reward=Economy.SettleRound("stage-"+Stage,won,Campaign?CampaignPlan.BaseGold[Stage-1]:5,Campaign?CampaignPlan.NaturalXP[Stage-1]:2);
            Phase=!won?BattleShopPhase.Failed:Stage==TotalStages?BattleShopPhase.Complete:BattleShopPhase.Settlement;return true;
        }
        public bool OpenShop()
        {
            if(Phase!=BattleShopPhase.Settlement)return false;
            Stage++;Phase=BattleShopPhase.Shop;Economy.BeginPreparation();
            if(!Economy.Locked)Economy.Refresh(true);
            return true;
        }
        public bool RestartEncounter(){if(!Campaign||!CanDeploy)return false;Phase=BattleShopPhase.Preparation;return true;}
        internal void SandboxPreparation(){if(!Sandbox)throw new InvalidOperationException();Phase=BattleShopPhase.Shop;}
        public bool PrepareBattle(){if(!CanShop)return false;Phase=BattleShopPhase.Preparation;return true;}
    }
}
