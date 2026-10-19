using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class EngagementTools
    {
        public const string DefinitionPath = "Assets/_Game/Data/Battles/WisadelVsKroos.asset";
        public const string KroosPath = "Assets/_Game/Art/Characters/Defenders/kroos-battle.png";
        public const string AttackPath = "Assets/_Game/Art/Characters/Monsters/Boss/wisadel-attack.png";

        [MenuItem("Yesterday Convoy/Initial Demo/Enable Wisadel vs Kroos")]
        public static void Initialize()
        {
            ManualDeploymentTools.Initialize();
            var kroos = DemoArtTools.ImportSprite(KroosPath, 1.8f);
            var attack = DemoArtTools.ImportSprite(AttackPath, 1.65f);
            var definition = AssetDatabase.LoadAssetAtPath<EngagementDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EngagementDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            var data = new SerializedObject(definition);
            data.FindProperty("kroosSprite").objectReferenceValue = kroos;
            data.FindProperty("wisadelAttackSprite").objectReferenceValue = attack;
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

        public static void Validate(EngagementDefinition definition)
        {
            if (definition == null || definition.KroosSprite == null || definition.WisadelAttackSprite == null)
                throw new InvalidOperationException("Engagement art/configuration is incomplete.");
            _ = definition.Snapshot();
            Vector3 position = definition.DefenderPosition;
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y) ||
                float.IsNaN(position.z) || float.IsInfinity(position.z)) throw new InvalidOperationException("Invalid defender position.");
            Debug.Log("ENGAGEMENT_CONFIG_PASS: Kroos; per-entity SP; manual ammo skill; read-only values.");
        }
    }
}
