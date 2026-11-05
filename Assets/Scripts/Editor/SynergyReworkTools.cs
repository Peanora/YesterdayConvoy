using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class SynergyReworkTools
    {
        // Only add missing authored devices. Re-running this does not regenerate terrain or overwrite positions.
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/VFX/Mudrock/TowerEditorSprite.asset");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Mudrock/Resources/MudrockVfx/OriginalEffect.mat");
            if(sprite==null||material==null)throw new InvalidOperationException("Original gramophone art missing");
            foreach(string path in new[]{CampaignTools.CampaignScene,CampaignTools.SandboxScene})
            {
                var scene=EditorSceneManager.OpenScene(path);
                var anchors=UnityEngine.Object.FindObjectsByType<GramophoneAnchor>(FindObjectsSortMode.None);
                if(anchors.Length>0){if(anchors.Length!=3)throw new InvalidOperationException("Review existing scene devices: "+path);continue;}
                var root=new GameObject("Authored contested gramophones");
                Vector3[] positions={new Vector3(-5,.06f,0),new Vector3(5,.06f,0),new Vector3(-1,.06f,1)};
                for(int i=0;i<positions.Length;i++)
                {
                    var go=new GameObject("L-44 crew "+i);go.transform.SetParent(root.transform);go.transform.position=positions[i];go.transform.rotation=Camera.main.transform.rotation;
                    var anchor=go.AddComponent<GramophoneAnchor>();anchor.deviceId="crew-device-"+i;anchor.initialCharge=i==1?100:50;
                    var body=go.AddComponent<SpriteRenderer>();body.sprite=sprite;body.sharedMaterial=material;
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            foreach(string id in new[]{"M42","M43"})MonsterCombatTools.ValidateTask(id);
            UnitConfigurationTools.ValidateProject();AssetDatabase.SaveAssets();
            Debug.Log("SYNERGY_REWORK_PREPARE_PASS: 39-card pool, two original leaders and authored contested devices.");
        }
    }
}
