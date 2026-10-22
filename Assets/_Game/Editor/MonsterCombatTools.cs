using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class MonsterCombatTools
    {
        public const string ArcherPath="Assets/_Game/Data/Resources/MonsterTests/M08.asset";
        [MenuItem("Yesterday Convoy/Configuration/Prepare M08 Archer Combat")]
        public static void PrepareArcher()
        {
            if(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(ArcherPath)!=null) { ValidateArcher(); return; }
            var unit=Create<UnitDefinition>("Assets/_Game/Data/Units/M08.asset");
            Set(unit,"unitId","M08_ARCHER");Set(unit,"displayName","弩手");Set(unit,"moveSpeed",.75f);Set(unit,"gateDamage",1);
            var card=Create<CardDefinition>("Assets/_Game/Data/Cards/M08.asset");
            Set(card,"cardId","CARD_M08");Set(card,"displayName","弩手");Set(card,"recruitCost",1);Set(card,"unit",unit);
            var combat=Create<EngagementDefinition>("Assets/_Game/Data/Battles/M08.asset");
            Set(combat,"kroosSprite",AssetDatabase.LoadAssetAtPath<Sprite>(EngagementTools.KroosPath));
            Set(combat,"defenderPosition",new Vector3(0,.35f,0));Set(combat,"unitAttack",22f);Set(combat,"unitArmor",2f);
            Set(combat,"unitRange",3.1f);Set(combat,"unitInterval",1.8f);Set(combat,"attackDuration",1f);Set(combat,"attackWindup",32f/60);
            Set(combat,"defenderHealth",140f);Set(combat,"defenderAttack",18f);Set(combat,"defenderArmor",3f);
            Set(combat,"defenderRange",3.2f);Set(combat,"defenderInterval",1f);Set(combat,"blockCount",0);
            Set(combat,"hasSkill",false);Set(combat,"melee",false);
            foreach(string field in new[]{"initialSP","requiredSP","spPerSecond","skillAttack","skillInterval","skillRange"}) Set(combat,field,0f);
            Set(combat,"skillAmmo",0);Set(combat,"projectileTime",.18f);
            var level=Create<ManualBattleDefinition>(ArcherPath);
            Set(level,"levelId","M08_ARCHER_TEST");Set(level,"card",card);Set(level,"engagement",combat);
            Set(level,"initialDP",12);Set(level,"initialDefense",1);Set(level,"deploymentCost",1);
            Set(level,"deploymentLimit",3);Set(level,"deploymentCooldown",1f);Set(level,"unitHealth",110f);
            AssetDatabase.SaveAssets();ValidateArcher();
        }
        public static void ValidateArcher()
        {
            var level=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(ArcherPath);
            if(level==null) throw new InvalidOperationException("M08 configuration missing.");
            var c=level.Engagement.Snapshot();
            if(c.HasSkill || c.Melee || level.Card.CardId!="CARD_M08" || !level.Card.HasFrameAnimation)
                throw new InvalidOperationException("M08 configuration or approved local media missing.");
            _=level.Snapshot(); UnitConfigurationTools.ValidateProject();
            Debug.Log("M08_CONFIGURATION_PASS: approved local media, ranged physical attack, no active skill.");
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
