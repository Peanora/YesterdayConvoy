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
        public ShopTrialModel.RoundReward Reward {get;private set;}
        public bool CanShop=>Phase==BattleShopPhase.Shop;
        public bool CanDeploy=>Phase==BattleShopPhase.Preparation||Phase==BattleShopPhase.Fighting;
        public BattleShopRun(ShopTrialModel economy,int seed,bool lateGame=false){Economy=economy??throw new ArgumentNullException(nameof(economy));Seed=seed;LateGame=lateGame;}
        public bool Observe(BattlePhase battle)
        {
            if(!CanDeploy)return false;
            if(battle==BattlePhase.Running)Phase=BattleShopPhase.Fighting;
            if(battle!=BattlePhase.Won&&battle!=BattlePhase.Lost)return false;
            bool won=battle==BattlePhase.Won;Reward=Economy.SettleRound("stage-"+Stage,won);
            Phase=!won?BattleShopPhase.Failed:Stage==2?BattleShopPhase.Complete:BattleShopPhase.Settlement;return true;
        }
        public bool OpenShop()
        {
            if(Phase!=BattleShopPhase.Settlement)return false;
            Stage++;Phase=BattleShopPhase.Shop;
            if(!Economy.Locked)Economy.Refresh(true);
            return true;
        }
        public bool PrepareBattle(){if(!CanShop)return false;Phase=BattleShopPhase.Preparation;return true;}
    }
}
