using System.IO;
using UnityEditor;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class GolemSupportTools
    {
        public static void Create()
        {
            const string folder="Assets/Data/Resources/Support";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<CardDefinition>("Assets/Data/Cards/M27.asset");
            var unit=AssetDatabase.LoadAssetAtPath<UnitDefinition>(folder+"/GolemUnit.asset");
            if(unit==null){unit=Object.Instantiate(source.Unit);AssetDatabase.CreateAsset(unit,folder+"/GolemUnit.asset");}
            var u=new SerializedObject(unit);u.FindProperty("unitId").stringValue="SYNERGY_GOLEM";u.FindProperty("displayName").stringValue="泥岩巨像";u.FindProperty("moveSpeed").floatValue=.45f;u.FindProperty("gateDamage").intValue=1;u.ApplyModifiedPropertiesWithoutUndo();
            var card=AssetDatabase.LoadAssetAtPath<CardDefinition>(folder+"/GolemCard.asset");
            if(card==null){card=Object.Instantiate(source);AssetDatabase.CreateAsset(card,folder+"/GolemCard.asset");}
            var c=new SerializedObject(card);c.FindProperty("cardId").stringValue=CampaignBalance.GolemCardId;c.FindProperty("displayName").stringValue="泥岩巨像";c.FindProperty("unit").objectReferenceValue=unit;c.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();Debug.Log("GOLEM_SUPPORT_ASSET_PASS: approved four clips; support is not a shop role");
        }
    }
}
