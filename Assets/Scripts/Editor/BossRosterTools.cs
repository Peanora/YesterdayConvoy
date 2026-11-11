using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public static class BossRosterTools
    {
        [MenuItem("Yesterday Convoy/Boss Roster/Install Approved Cards")]
        public static void InstallAll()
        {
            foreach(var e in BossRosterCatalog.Entries)
            {
                var art=BossRosterArt.For(e.CardId);if(art==null)throw new InvalidOperationException("Approved source model missing: "+e.Task);
                var unit=Create<UnitDefinition>("Assets/Data/Units/BossRoster/"+e.Task+".asset");
                Set(unit,"unitId",e.UnitId);Set(unit,"displayName",e.Name);Set(unit,"moveSpeed",e.Speed);Set(unit,"gateDamage",e.Cost>=4?2:1);
                var card=Create<CardDefinition>("Assets/Data/Cards/BossRoster/"+e.Task+".asset");
                Set(card,"cardId",e.CardId);Set(card,"displayName",e.Name);Set(card,"recruitCost",e.Cost);Set(card,"unit",unit);
                var combat=Create<EngagementDefinition>("Assets/Data/Battles/BossRoster/"+e.Task+".asset");
                Set(combat,"kroosSprite",AssetDatabase.LoadAssetAtPath<Sprite>(EngagementTools.KroosPath));
                Set(combat,"defenderPosition",new Vector3(0,.35f,0));
                Set(combat,"unitAttack",e.Attack);Set(combat,"unitArmor",e.Armor);Set(combat,"unitRange",e.Range);
                Set(combat,"unitInterval",Math.Max(e.Interval,art.AttackDuration+.1f));Set(combat,"attackDuration",art.AttackDuration);Set(combat,"attackWindup",art.AttackContact);
                Set(combat,"noOrdinaryAttack",!art.HasAttack);Set(combat,"melee",e.Melee);Set(combat,"damageType",(int)e.DamageType);
                Set(combat,"defenderHealth",1200f);Set(combat,"defenderAttack",15f);Set(combat,"defenderArmor",3f);Set(combat,"defenderRange",3.2f);Set(combat,"defenderInterval",1.5f);Set(combat,"blockCount",1);
                Set(combat,"hasSkill",true);Set(combat,"initialSP",12f);Set(combat,"requiredSP",12f);Set(combat,"spPerSecond",1f);Set(combat,"skillAmmo",1);
                Set(combat,"skillAttack",e.Attack*2);Set(combat,"skillInterval",4f);Set(combat,"skillRange",3.5f);Set(combat,"projectileTime",.1f);
                var level=Create<ManualBattleDefinition>("Assets/Data/Resources/MonsterTests/"+e.Task+".asset");
                Set(level,"levelId",e.Task+"_BOSS_TRIAL");Set(level,"card",card);Set(level,"engagement",combat);
                Set(level,"initialDP",36);Set(level,"initialDefense",1);Set(level,"deploymentCost",e.DeploymentCost);Set(level,"deploymentLimit",e.Task=="B10"?1:3);Set(level,"deploymentCooldown",3f);Set(level,"unitHealth",e.Health);
                _=level.Snapshot();_=combat.Snapshot();
            }
            AssetDatabase.SaveAssets();Debug.Log("BOSS_ROSTER_INSTALL_PASS cards="+BossRosterCatalog.Entries.Count);
        }
        static T Create<T>(string path)where T:ScriptableObject
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old!=null)return old;
            if(File.Exists(path))throw new InvalidOperationException("Occupied asset path: "+path);
            string folder=Path.GetDirectoryName(path).Replace('\\','/');Folder(folder);
            var value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);return value;
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static void Set(UnityEngine.Object asset,string field,object value)
        {
            var so=new SerializedObject(asset);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(asset.name+" missing "+field);
            if(value is string s)p.stringValue=s;else if(value is int i)p.intValue=i;else if(value is float f)p.floatValue=f;else if(value is bool b)p.boolValue=b;else if(value is Vector3 v)p.vector3Value=v;else p.objectReferenceValue=value as UnityEngine.Object;
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
        }
    }
}
