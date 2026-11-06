using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        GuerrillaOrderView guerrillaOrderView;
        // Combat may run at 2x, but this cinematic and its impact retain the original pace.
        // Keep the existing skill-menu slow motion and explicit pause semantics.
        internal float GuerrillaPresentationRate=>Mathf.Min(1,EffectiveSimulationSpeed);

        void InitializeGuerrillaOrderPresentation()
        {
            guerrillaOrderView=gameObject.AddComponent<GuerrillaOrderView>();
            guerrillaOrderView.Initialize(this,scene.InterfaceFont);
        }

        void ShowGuerrillaOrder()
        {
            // The model owns the same locked membership used for tier calculation, including transfers.
            var members=Battle.GuerrillaMemberIds.ToArray();
            var portraits=new Dictionary<string,Sprite>();
            foreach(string id in members)
                if(!CampaignBalance.Guerrilla(id)&&shopDefinitions.TryGetValue(id,out var card))
                    portraits[id]=card.Card.Portrait;
            var assignments=GuerrillaTransferIds;
            guerrillaOrderView.Play(members,portraits,assignments.Count>0?assignments:null);
            guerrillaOrderView.BindVolley(Battle.LatestGuerrillaOrder);
        }
    }
}
