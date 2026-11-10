using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class ApprovedMapDeliveryTools
    {
        public static void Prepare()
        {
            ApprovedMapsSceneTools.RebuildTaskScenes();
            GameSceneCatalog.UpdateBuildSettings();
            ApprovedMapsSceneTools.Validate();
            ApprovedMapsSceneTools.RenderPreviews();
            Debug.Log("APPROVED_MAPS_PREPARE_PASS");
        }
    }
}
