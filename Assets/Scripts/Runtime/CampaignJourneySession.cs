using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // One in-memory challenge survives authored scene loads. No visual object is retained.
    public sealed class CampaignJourneySession
    {
        public static CampaignJourneySession Current {get;private set;}
        public int Seed {get;}
        public ShopTrialModel Shop {get;internal set;}
        public BattleShopRun Loop {get;internal set;}
        public int Stage=>Loop?.Stage??1;
        public bool Ended=>Loop!=null&&(Loop.Phase==BattleShopPhase.Complete||Loop.Phase==BattleShopPhase.Failed);
        public bool Won=>Loop!=null&&Loop.Phase==BattleShopPhase.Complete;
        [Serializable]public sealed class RoundSummary
        {public int stage,level,deployments;public bool won,supply;public double seconds;public float damage;}
        internal readonly List<RoundSummary> Results=new List<RoundSummary>();
        public IReadOnlyList<RoundSummary> CompletedRounds=>Results.AsReadOnly();
        internal readonly List<string> Lineup=new List<string>();
        internal ShopSeatLayout Seats;
        internal string Contract,Commission;
        internal InfectionForm InfectionPrimary=InfectionForm.Swarm,InfectionSecondary=InfectionForm.Shell;
        internal MarineAdaptation MarinePrimary=MarineAdaptation.Barrier,MarineSecondary=MarineAdaptation.Hunt;
        internal bool ReunionRanged;
        internal int InfectionChoiceTier;
        CampaignJourneySession(int seed){Seed=seed==0?Guid.NewGuid().GetHashCode():seed;}
        public static CampaignJourneySession Begin(int seed=0)=>Current=new CampaignJourneySession(seed);
        public static void Clear()=>Current=null;
        public ApprovedMapDefinition Map
        {
            get
            {
                for(int i=Stage;i<=CampaignPlan.Count;i++)
                {var stage=CampaignJourneyCatalog.Stage(i);if(stage.map!=null)return stage.map;}
                throw new InvalidOperationException("Journey has no following authored battle map.");
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()=>Clear();
    }
}
