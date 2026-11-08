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
        public int GolemExperience {get;private set;}
        public int LastGolemGrowth {get;private set;}
        public int LastGolemExperienceBefore {get;private set;}
        public int LastGolemCapacity {get;private set;}=1;
        public int SandboxRound {get;private set;}=1;
        // Legacy read surface for old trial assets/callers. Marine growth retired in favour of per-battle terrain.
        public int MarineExperience=>0;
        public int LastMarineGrowth=>0;
        public readonly int Seed;
        public readonly bool LateGame;
        public readonly bool Campaign,Sandbox,JourneyV2;
        public bool RewardStage=>Campaign&&!Sandbox&&CampaignPlan.Supply(Stage);
        public int TotalStages=>Campaign?Sandbox?1:CampaignPlan.Count:2;
        public ShopTrialModel.RoundReward Reward {get;private set;}
        public bool CanShop=>Phase==BattleShopPhase.Shop;
        public bool CanDeploy=>Phase==BattleShopPhase.Preparation||Phase==BattleShopPhase.Fighting;
        public BattleShopRun(ShopTrialModel economy,int seed,bool lateGame=false,bool campaign=false,bool sandbox=false,bool journeyV2=false){Campaign=campaign;Sandbox=sandbox;JourneyV2=journeyV2;Economy=economy??throw new ArgumentNullException(nameof(economy));Seed=seed;LateGame=lateGame;}
        public bool Observe(BattlePhase battle,int golemGrowth=0,int marineGrowth=0,int golemCapacity=5)
        {
            if(!CanDeploy)return false;
            if(battle==BattlePhase.Running)Phase=BattleShopPhase.Fighting;
            if(battle!=BattlePhase.Won&&battle!=BattlePhase.Lost)return false;
            if(marineGrowth<0||marineGrowth>3)throw new ArgumentOutOfRangeException(nameof(marineGrowth));
            if(golemGrowth<0||golemGrowth>15)throw new ArgumentOutOfRangeException(nameof(golemGrowth));
            GolemGrowth.Threshold(golemCapacity);
            bool won=battle==BattlePhase.Won;
            LastGolemExperienceBefore=GolemExperience;LastGolemCapacity=golemCapacity;
            LastGolemGrowth=Campaign&&!RewardStage?GolemGrowth.Award(GolemExperience,golemCapacity,golemGrowth):0;
            GolemExperience+=LastGolemGrowth;
            string receipt=Sandbox?"sandbox-round-"+SandboxRound:"stage-"+Stage;
            Reward=Economy.SettleRound(receipt,won,JourneyV2?CampaignJourneyCatalog.Stage(Stage).baseGold:Campaign?CampaignPlan.BaseGold[Stage-1]:5,JourneyV2?CampaignJourneyCatalog.Stage(Stage).naturalXP:Campaign?CampaignPlan.NaturalXP[Stage-1]:2);
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
        internal void SandboxPreparation()
        {
            if(!Sandbox)throw new InvalidOperationException();
            if(Phase==BattleShopPhase.Complete||Phase==BattleShopPhase.Failed)SandboxRound++;
            Phase=BattleShopPhase.Shop;
        }
        internal void PresetGolemGrowth(int experience)
        {
            if(!Sandbox||!CanShop||experience<0)throw new InvalidOperationException();
            GolemExperience=experience;
        }
        public bool PrepareBattle(){if(!CanShop)return false;Phase=BattleShopPhase.Preparation;return true;}
    }
}
