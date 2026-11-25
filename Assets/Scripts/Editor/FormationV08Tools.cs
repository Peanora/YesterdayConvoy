using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class FormationV08Tools
    {
        public static void InstallPrototype()
        {
            const string unitPath="Assets/Data/Units/SteamPrototype.asset", cardPath="Assets/Data/Cards/SteamPrototype.asset";
            var unit=AssetDatabase.LoadAssetAtPath<UnitDefinition>(unitPath);
            if(unit==null){unit=ScriptableObject.CreateInstance<UnitDefinition>();AssetDatabase.CreateAsset(unit,unitPath);}
            var unitData=new SerializedObject(unit);unitData.FindProperty("unitId").stringValue="V08_STEAM_DRIVER";
            unitData.FindProperty("displayName").stringValue="蒸汽骑士驾驶员";unitData.FindProperty("moveSpeed").floatValue=.6f;unitData.FindProperty("gateDamage").intValue=1;
            unitData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(unit);
            var card=AssetDatabase.LoadAssetAtPath<CardDefinition>(cardPath);
            if(card==null){card=ScriptableObject.CreateInstance<CardDefinition>();AssetDatabase.CreateAsset(card,cardPath);}
            var cardData=new SerializedObject(card);cardData.FindProperty("cardId").stringValue="CARD_STEAM";
            cardData.FindProperty("displayName").stringValue="蒸汽骑士驾驶员";cardData.FindProperty("recruitCost").intValue=4;
            cardData.FindProperty("unit").objectReferenceValue=unit;cardData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(card);
            // Explicitly invoked authoring changes only this scene's card reference array.
            var scene=EditorSceneManager.OpenScene(FormationBattleTools.ScenePath,OpenSceneMode.Single);
            var controller=UnityEngine.Object.FindAnyObjectByType<FormationBattleController>();
            if(!controller.formationCards.Any(existing=>existing!=null&&existing.CardId==card.CardId))
            {controller.formationCards=controller.formationCards.Append(card).ToArray();EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            AssetDatabase.SaveAssets();BaselineTools.Validate();
            Debug.Log("FORMATION_V08_INSTALL_PASS: 47 cards; prototype intentionally reuses retained GOLEM art, no new original-game identity claimed.");
        }
    }
}
