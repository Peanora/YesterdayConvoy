using System.Linq;
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
            // A formation snapshot, independent of deployment, survival or order recipients.
            // Future profession changes should extend this membership query and supply their portraits.
            guerrillaOrderView.Play(shopLineup.Where(CampaignBalance.Guerrilla).Distinct().ToArray());
            guerrillaOrderView.BindVolley(Battle.LatestGuerrillaOrder);
        }
    }
}
