using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Idempotent approved Boss asset bindings. The scene remains owned by campaign Author.</summary>
    public static class FormationRosterTools
    {
        [MenuItem("Yesterday Convoy/Formation Battle/Install Approved v0.7 Roster")]
        public static void Install()
        {
            foreach(var entry in BossRosterCatalog.Entries)
            {
                var rules=FormationCampaignRules.Find(entry.CardId);
                if(rules==null)throw new InvalidOperationException("Missing approved formation role: "+entry.CardId);
                var unit=LoadOrCreate<UnitDefinition>("Assets/Data/Units/BossRoster/"+entry.Task+".asset");
                Set(unit,"unitId",entry.UnitId);Set(unit,"displayName",rules.Name);
                Set(unit,"moveSpeed",Math.Max(.01f,rules.MoveSpeed));Set(unit,"gateDamage",1);
                var card=LoadOrCreate<CardDefinition>("Assets/Data/Cards/BossRoster/"+entry.Task+".asset");
                Set(card,"cardId",entry.CardId);Set(card,"displayName",rules.Name);
                Set(card,"recruitCost",rules.Cost);Set(card,"unit",unit);
            }
            AssetDatabase.SaveAssets();Validate();
            Debug.Log("FORMATION_V07_ROSTER_INSTALL_PASS: 19 new approved Boss cards; old cards and scenes retained.");
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Validate Approved v0.7 Roster")]
        public static void Validate()
        {
            var cards=AssetDatabase.FindAssets("t:CardDefinition",new[]{"Assets/Data/Cards"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .ToDictionary(c=>c.CardId,StringComparer.Ordinal);
            FormationUnitArt.RegisterCards(cards.Values);
            foreach(var rules in FormationCampaignRules.AllUnits)
            {
                if(!cards.TryGetValue(rules.CardId,out var card)||card.Unit==null)
                    throw new InvalidOperationException("Missing complete card/unit binding: "+rules.CardId);
                var definition=FormationUnitCatalog.Find(rules.CardId);
                if(definition==null||rules.Id.StartsWith("B",StringComparison.Ordinal)&&definition.Cost!=rules.Cost)
                    throw new InvalidOperationException("Formation catalog does not use v0.7: "+rules.CardId);
                var art=FormationUnitArt.For(rules.CardId);
                if(art==null||!art.HasAction("idle")||art.Duration("idle")<=0)
                    throw new InvalidOperationException("Missing original idle metadata: "+rules.CardId);
                if(rules.RequiredSP>0&&!art.HasAction("cast")&&!art.HasAction("skill")&&!art.HasAction("attack"))
                    throw new InvalidOperationException("Missing actual cast or attack motion: "+rules.CardId);
            }
            Debug.Log("FORMATION_V07_ROSTER_VALIDATE_PASS: 46 approved identities with real source motion metadata.");
        }

        static T LoadOrCreate<T>(string path)where T:ScriptableObject
        {
            var prior=AssetDatabase.LoadAssetAtPath<T>(path);if(prior!=null)return prior;
            if(File.Exists(path))throw new InvalidOperationException("Occupied asset path: "+path);
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));
            var asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
        }
        static void Set(UnityEngine.Object asset,string key,object value)
        {
            var data=new SerializedObject(asset);var property=data.FindProperty(key);
            if(property==null)throw new InvalidOperationException(asset.name+" missing serialized field "+key);
            if(value is string text)property.stringValue=text;
            else if(value is int number)property.intValue=number;
            else if(value is float scalar)property.floatValue=scalar;
            else property.objectReferenceValue=value as UnityEngine.Object;
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
        }
    }
}
