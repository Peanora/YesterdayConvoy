using System.Collections;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class ApprovedMapBootstrap : MonoBehaviour
    {
        public ApprovedMapDefinition map;
        ManualBattleController controller;
        ApprovedHeatMarker[] heatMarkers;
        IEnumerator Start()
        {
            yield return null;
            if(map==null)throw new System.InvalidOperationException("Missing saved map profile");
            controller=FindAnyObjectByType<ManualBattleController>();
            heatMarkers=FindObjectsByType<ApprovedHeatMarker>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if(CampaignJourneySession.Current!=null)yield return controller.PrepareJourneyStage();
            else yield return controller.PrepareApprovedMap(map);
        }
        void Update()
        {
            if(controller==null||controller.ArtLoading||!controller.ShopTrialActive)return;
            if(Input.GetKeyDown(KeyCode.Tab))controller.SetShopVisible(!controller.ShopVisible);
            if(Input.GetKeyDown(KeyCode.Escape)){controller.CancelShopDrag();controller.SetShopVisible(false);controller.CloseDefenderInspector();}
            if(controller.Battle!=null&&heatMarkers!=null)foreach(var tile in heatMarkers)if(tile!=null)tile.Apply(controller.Battle);
        }
    }
}
