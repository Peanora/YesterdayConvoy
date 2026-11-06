using System.Linq;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        GuerrillaOrderView guerrillaOrderView;

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
        }
    }
}
