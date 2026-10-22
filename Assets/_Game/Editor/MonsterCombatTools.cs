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
        // Read the explicitly requested task; never overwrite an existing authored configuration.
        public static void PrepareTask()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-monsterTask");
            if(index<0 || index+1>=args.Length) throw new ArgumentException("-monsterTask required.");
            string task=args[index+1],name,id;float speed,health,attack,armor,interval,windup,defenseHealth=140;int cost=1;
            float range=.75f,duration=1,resistance=0,shred=0,blast=0,blastRadius=0,blastDelay=0,cold=0;bool melee=true;int damageType=0;
            switch(task)
            {
                case "M02":name="猎狗";id="HOUND";speed=1.1f;health=110;attack=30;armor=0;interval=1.4f;windup=.6f;break;
                case "M03":name="游击队猎犬";id="GUERRILLA_HOUND";speed=1.35f;health=130;attack=34;armor=2;interval=1.4f;windup=.6f;break;
                case "M07":name="士兵";id="SOLDIER";speed=.75f;health=150;attack=28;armor=5;interval=1.6f;windup=.4f;break;
                case "M10":name="重装防御者";id="HEAVY_DEFENDER";speed=.55f;health=340;attack=40;armor=14;interval=2.6f;windup=28f/60;cost=2;defenseHealth=200;break;
                case "M04":name="高能源石虫";id="EXPLOSIVE_SLUG";speed=.65f;health=130;attack=32;armor=0;interval=2.8f;windup=28f/60;duration=2.5f;cost=2;blast=90;blastRadius=1.65f;blastDelay=1;break;
                case "M05":name="酸液源石虫";id="ACID_SLUG";speed=.65f;health=130;attack=24;armor=0;interval=2.2f;windup=50f/60;duration=80f/60;cost=2;range=2.8f;melee=false;shred=2;break;
                case "M06":name="冰爆源石虫";id="ICE_SLUG";speed=.65f;health=90;defenseHealth=240;attack=30;armor=0;interval=2.8f;windup=28f/60;duration=2.5f;cost=2;blast=60;blastRadius=1.65f;blastDelay=1;cold=10;break;
                case "M09":name="术师";id="CASTER";speed=.65f;health=120;attack=26;armor=1;interval=2.4f;windup=38f/60;duration=1.2f;range=3;melee=false;damageType=1;resistance=20;break;
                default:throw new ArgumentException("Unsupported task: "+task);
            }
            string levelPath="Assets/_Game/Data/Resources/MonsterTests/"+task+".asset";
            if(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(levelPath)!=null) { ValidateTask(task);return; }
            var unit=Create<UnitDefinition>("Assets/_Game/Data/Units/"+task+".asset");
            Set(unit,"unitId",task+"_"+id);Set(unit,"displayName",name);Set(unit,"moveSpeed",speed);Set(unit,"gateDamage",1);
            var card=Create<CardDefinition>("Assets/_Game/Data/Cards/"+task+".asset");
            Set(card,"cardId","CARD_"+task);Set(card,"displayName",name);Set(card,"recruitCost",cost);Set(card,"unit",unit);
            var combat=Create<EngagementDefinition>("Assets/_Game/Data/Battles/"+task+".asset");
            Set(combat,"kroosSprite",AssetDatabase.LoadAssetAtPath<Sprite>(EngagementTools.KroosPath));
            Set(combat,"defenderPosition",new Vector3(0,.35f,0));Set(combat,"unitAttack",attack);Set(combat,"unitArmor",armor);
            Set(combat,"unitRange",range);Set(combat,"unitInterval",interval);Set(combat,"attackDuration",duration);Set(combat,"attackWindup",windup);
            Set(combat,"defenderHealth",defenseHealth);Set(combat,"defenderAttack",18f);Set(combat,"defenderArmor",3f);
            Set(combat,"defenderRange",3.2f);Set(combat,"defenderInterval",1f);Set(combat,"blockCount",0);
            Set(combat,"hasSkill",false);Set(combat,"melee",melee);
            Set(combat,"damageType",damageType);Set(combat,"defenderResistance",resistance);Set(combat,"armorShred",shred);
            Set(combat,"blastAttack",blast);Set(combat,"blastRadius",blastRadius);Set(combat,"blastDelay",blastDelay);Set(combat,"coldDuration",cold);
            if(task=="M05" || task=="M09") Set(combat,"defenderArmor",18f);
            foreach(string field in new[]{"initialSP","requiredSP","spPerSecond","skillAttack","skillInterval","skillRange"}) Set(combat,field,0f);
            Set(combat,"skillAmmo",0);Set(combat,"projectileTime",melee ? .02f : .25f);
            var level=Create<ManualBattleDefinition>(levelPath);
            Set(level,"levelId",task+"_TEST");Set(level,"card",card);Set(level,"engagement",combat);
            Set(level,"initialDP",12);Set(level,"initialDefense",1);Set(level,"deploymentCost",cost);
            Set(level,"deploymentLimit",3);Set(level,"deploymentCooldown",1f);Set(level,"unitHealth",health);
            AssetDatabase.SaveAssets();ValidateTask(task);
        }
        public static void ValidateTask(string task)
        {
            var level=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/_Game/Data/Resources/MonsterTests/"+task+".asset");
            if(level==null || !level.Card.HasFrameAnimation) throw new InvalidOperationException(task+" configuration/local art missing.");
            _=level.Snapshot();if(level.Engagement.Snapshot().HasSkill) throw new InvalidOperationException("Unexpected active skill.");
            UnitConfigurationTools.ValidateProject();Debug.Log("MONSTER_CONFIGURATION_PASS: "+task);
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
