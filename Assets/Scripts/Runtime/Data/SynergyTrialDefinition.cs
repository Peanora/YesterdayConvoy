using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    [Serializable]
    public sealed class SynergyTrialStartingCard
    {
        public string cardId;
        [Range(1,3)] public int star=1;
    }

    [CreateAssetMenu(menuName="Yesterday Convoy/Synergy Trial")]
    public sealed class SynergyTrialDefinition:ScriptableObject
    {
        [Tooltip("Stable ID from SynergyTrialCatalog. Do not use the display title as an ID.")]
        public string trialId;
        public string displayTitle;
        [Range(1,10)] public int initialLevel=8;
        public SynergyTrialStartingCard[] initialMembers=Array.Empty<SynergyTrialStartingCard>();
        [Tooltip("Independent baseline configuration. Sandbox multipliers are applied to a fresh runtime copy.")]
        public DefenderTrialDefinition encounter;
        public InfectionForm infectionPrimary=InfectionForm.Swarm;
        public InfectionForm infectionSecondary=InfectionForm.Shell;
        public MarineAdaptation marinePrimary=MarineAdaptation.Barrier;
        public MarineAdaptation marineSecondary=MarineAdaptation.Hunt;
        [Tooltip("Optional starting contract owner. Empty uses the first starting member.")]
        public string contractCardId;

        public void Validate()
        {
            if(SynergyTrialCatalog.Find(trialId)==null)throw new InvalidOperationException("Unknown dedicated trial ID: "+trialId);
            if(string.IsNullOrWhiteSpace(displayTitle)||initialLevel<1||initialLevel>10||encounter==null)
                throw new InvalidOperationException("Trial title, level or encounter is invalid: "+name);
            if(initialMembers==null||initialMembers.Length<1||initialMembers.Length>initialLevel)
                throw new InvalidOperationException("Starting formation must fit the starting level: "+name);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var member in initialMembers)
            {
                if(member==null||!CampaignBalance.Supports(member.cardId)||member.star<1||member.star>3||!ids.Add(member.cardId))
                    throw new InvalidOperationException("Invalid or duplicate starting card: "+name);
                // A starting 1/2/3-star card reserves 1/3/9 copies through Shop.Preset, never an infinite stock.
            }
            if(!string.IsNullOrEmpty(contractCardId)&&!ids.Contains(contractCardId))
                throw new InvalidOperationException("Contract owner must be a starting member: "+name);
            if(!Enum.IsDefined(typeof(InfectionForm),infectionPrimary)||!Enum.IsDefined(typeof(InfectionForm),infectionSecondary)||
                !Enum.IsDefined(typeof(MarineAdaptation),marinePrimary)||!Enum.IsDefined(typeof(MarineAdaptation),marineSecondary))
                throw new InvalidOperationException("Invalid starting faction choice: "+name);
            _=encounter.Snapshot();
        }

        public ShopTrialModel.Piece[] StartingPieces()
        {
            Validate();return initialMembers.Select(m=>new ShopTrialModel.Piece{Id=m.cardId,Star=m.star}).ToArray();
        }
        public DefenderPlacement[] CopyDefenders()
        {
            Validate();return encounter.defenders.Select(d=>JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(d))).ToArray();
        }
    }
}
