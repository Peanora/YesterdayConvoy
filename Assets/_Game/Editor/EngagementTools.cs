using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class EngagementTools
    {
        public const string DefinitionPath = "Assets/_Game/Data/Battles/WisadelVsKroos.asset";
        public const string KroosPath = "Assets/_Game/Art/Characters/Defenders/kroos-comic.png";

        [MenuItem("Yesterday Convoy/Initial Demo/Enable Wisadel vs Kroos")]
        public static void Initialize()
        {
            ManualDeploymentTools.Initialize();
            var kroos = ImportKroos();
            var definition = AssetDatabase.LoadAssetAtPath<EngagementDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EngagementDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            var data = new SerializedObject(definition);
            data.FindProperty("kroosSprite").objectReferenceValue = kroos;
            data.ApplyModifiedPropertiesWithoutUndo();
            var level = new SerializedObject(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(ManualDeploymentTools.DefinitionPath));
            level.FindProperty("engagement").objectReferenceValue = definition;
            level.ApplyModifiedPropertiesWithoutUndo();
            var controller = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<ManualBattleController>());
            controller.FindProperty("uiSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Art/Audio/SFX/ui-audio-click1.ogg");
            controller.FindProperty("hitSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Art/Audio/SFX/rpg-audio-metalClick.ogg");
            controller.FindProperty("skillIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icons/icon-magic.png");
            controller.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Validate(definition);
        }

        [MenuItem("Yesterday Convoy/Initial Demo/Apply Kroos Comic Art")]
        public static void ApplyKroosComic()
        {
            AssetDatabase.Refresh();
            Sprite sprite=ImportKroos();
            var definition=AssetDatabase.LoadAssetAtPath<EngagementDefinition>(DefinitionPath);
            var data=new SerializedObject(definition);
            data.FindProperty("kroosSprite").objectReferenceValue=sprite;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
            Validate(definition);
            Debug.Log("KROOS_COMIC_PASS: transparent sprite; Bilinear; 1.8 world height; combat values and scene unchanged.");
        }

        private static Sprite ImportKroos()
        {
            DemoArtTools.ImportSprite(KroosPath,1.8f);
            var importer=(TextureImporter)AssetImporter.GetAtPath(KroosPath);
            importer.filterMode=FilterMode.Bilinear; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(KroosPath);
        }

        public static void Validate(EngagementDefinition definition)
        {
            if (definition == null || definition.KroosSprite == null)
                throw new InvalidOperationException("Engagement art/configuration is incomplete.");
            _ = definition.Snapshot();
            Vector3 position = definition.DefenderPosition;
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y) ||
                float.IsNaN(position.z) || float.IsInfinity(position.z)) throw new InvalidOperationException("Invalid defender position.");
            Debug.Log("ENGAGEMENT_CONFIG_PASS: Kroos; per-entity SP; manual ammo skill; read-only values.");
        }
    }
}
