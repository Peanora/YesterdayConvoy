using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class CampaignTools
    {
        public const string CampaignScene="Assets/Scenes/Campaign.unity",SandboxScene="Assets/Scenes/SynergyTrial.unity";
        [MenuItem("Yesterday Convoy/Scenes/Open Synergy Trial")]
        public static void OpenSandbox()=>EditorSceneManager.OpenScene(SandboxScene);
        [MenuItem("Yesterday Convoy/Scenes/Open Three Act Campaign")]
        public static void OpenCampaign()=>EditorSceneManager.OpenScene(CampaignScene);
        // Explicit authoring step. Both scenes retain serialized terrain/portals and materials.
        public static void CreateScenes()
        {
            foreach(bool sandbox in new[]{false,true})
            {
                var scene=EditorSceneManager.OpenScene(ShopMapTools.ScenePath);
                foreach(var old in Object.FindObjectsByType<ShopMapBootstrap>(FindObjectsSortMode.None))Object.DestroyImmediate(old);
                var go=new GameObject(sandbox?"Synergy free trial":"Three act challenge");go.AddComponent<CampaignBootstrap>().sandbox=sandbox;
                EditorSceneManager.SaveScene(scene,sandbox?SandboxScene:CampaignScene);
            }
            GameSceneCatalog.UpdateBuildSettings();
            AssetDatabase.SaveAssets();Debug.Log("CAMPAIGN_SCENES_PASS: serialized terrain, four build scenes");
        }
    }
}
