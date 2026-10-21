using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Only creates this monster's independent assets. Never opens or saves a scene.</summary>
    public static class OriginiumSlugTools
    {
        public const string LevelPath="Assets/_Game/Data/Resources/MonsterTests/OriginiumSlug.asset";
        [MenuItem("Yesterday Convoy/Configuration/Prepare Originium Slug Test")]
        public static void Prepare()
        {
            if(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(LevelPath)!=null) { Validate();return; }
            var unit=Create<UnitDefinition>("Assets/_Game/Data/Units/OriginiumSlug.asset");
            Set(unit,"unitId","ORIGINIUM_SLUG");Set(unit,"displayName","源石虫");Set(unit,"moveSpeed",.75f);Set(unit,"gateDamage",1);
            var card=Create<CardDefinition>("Assets/_Game/Data/Cards/OriginiumSlug.asset");
            Set(card,"cardId","CARD_ORIGINIUM_SLUG");Set(card,"displayName","源石虫");Set(card,"recruitCost",1);Set(card,"unit",unit);
            // Portrait and battle frames are resolved from an approved, optional local package.
            // Do not copy another monster's frames or invent a skill to satisfy validation.
            var combat=Create<EngagementDefinition>("Assets/_Game/Data/Battles/OriginiumSlug.asset");
            Set(combat,"kroosSprite",AssetDatabase.LoadAssetAtPath<Sprite>(EngagementTools.KroosPath));
            Set(combat,"defenderPosition",new Vector3(0,.35f,0));Set(combat,"unitAttack",28f);Set(combat,"unitArmor",0f);
            Set(combat,"unitRange",.7f);Set(combat,"unitInterval",1.7f);Set(combat,"defenderHealth",100f);
            Set(combat,"defenderAttack",18f);Set(combat,"defenderArmor",3f);Set(combat,"defenderRange",3.2f);
            Set(combat,"defenderInterval",1f);Set(combat,"blockCount",0);Set(combat,"hasSkill",false);Set(combat,"melee",true);
            foreach(string field in new[]{"initialSP","requiredSP","spPerSecond","skillAttack","skillInterval","skillRange"}) Set(combat,field,0f);
            Set(combat,"skillAmmo",0);Set(combat,"projectileTime",1f/3);
            var level=Create<ManualBattleDefinition>(LevelPath);
            Set(level,"levelId","ORIGINIUM_SLUG_TEST");Set(level,"card",card);Set(level,"engagement",combat);
            Set(level,"initialDP",12);Set(level,"initialDefense",1);Set(level,"deploymentCost",1);
            Set(level,"deploymentLimit",3);Set(level,"deploymentCooldown",1f);Set(level,"unitHealth",100f);
            AssetDatabase.SaveAssets();Validate();
        }
        public static void Validate()
        {
            var level=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(LevelPath);
            if(level==null) throw new InvalidOperationException("Missing slug configuration.");
            var combat=level.Engagement.Snapshot();
            if(combat.HasSkill || !combat.Melee || combat.BlockCount!=0 || level.Card.RecruitCost!=1)
                throw new InvalidOperationException("Invalid slug test role.");
            // A public clone may legitimately lack original media. Still validate numerical data.
            bool local=level.Card.HasFrameAnimation;
            if(local) _=level.Snapshot();
            UnitConfigurationTools.ValidateProject();
            Debug.Log("SLUG_CONFIGURATION_PASS: local art="+local+"; no active skill; independent test, no scene edits.");
        }
        private static T Create<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset!=null) return asset;
            if(File.Exists(path)) throw new InvalidOperationException("Asset path is occupied: "+path);
            Folder(Path.GetDirectoryName(path).Replace('\\','/'));
            asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            int index=path.LastIndexOf('/');Folder(path.Substring(0,index));AssetDatabase.CreateFolder(path.Substring(0,index),path.Substring(index+1));
        }
        private static void Set(UnityEngine.Object asset,string field,object value)
        {
            var data=new SerializedObject(asset);var prop=data.FindProperty(field);
            if(prop==null) throw new InvalidOperationException("Unknown field "+field);
            if(value is UnityEngine.Object obj) prop.objectReferenceValue=obj;
            else if(value is string text) prop.stringValue=text;
            else if(value is int integer) prop.intValue=integer;
            else if(value is float number) prop.floatValue=number;
            else if(value is bool flag) prop.boolValue=flag;
            else if(value is Vector3 vector) prop.vector3Value=vector;
            else throw new ArgumentException(field);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
