using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DefenderTrialTools
    {
        private const string Root="Assets/_Game/Data/Resources/DefenderTrial.asset";
        [MenuItem("Yesterday Convoy/Defender Trial/Prepare and Validate")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root);
            if(data==null)
            {
                data=ScriptableObject.CreateInstance<DefenderTrialDefinition>();
                AssetDatabase.CreateAsset(data,Root);
                var original=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(TalulahTools.LevelPath);
                var level=UnityEngine.Object.Instantiate(original);
                var combat=UnityEngine.Object.Instantiate(original.Engagement);
                AssetDatabase.CreateAsset(combat,"Assets/_Game/Data/Battles/EightDefendersCombat.asset");
                AssetDatabase.CreateAsset(level,"Assets/_Game/Data/Battles/EightDefendersBattle.asset");
                Set(level,"levelId","EIGHT_DEFENDERS_WATERWORKS"); Set(level,"engagement",combat);
                Set(level,"unitHealth",900f); Set(level,"deploymentLimit",4); Set(level,"initialDP",20);
                Set(level,"initialDefense",1); Set(level,"deploymentCooldown",2f);
                Set(combat,"unitAttack",55f); Set(combat,"unitRange",1.6f); Set(combat,"skillAttack",60f);
                data.battle=level;
                data.route=new[]{P(-6,-2),P(-3,-2),P(-3,2),P(1,2),P(1,-2),P(6,-2)};
                data.defenders=new[]{
                    D("D01","米格鲁",-3,0,false,260,10,12,.85f,1.6f,3,windup:.5f),
                    D("D03","斑点",1,0,false,220,18,8,.85f,2.2f,3,DefenderRole.Guardian,windup:.5f,healRange:2.5f),
                    D("D04","玫兰莎",2,-2,false,180,24,2,.9f,2.5f,1,duration:2,windup:.9666667f),
                    D("D07","克洛丝",-5,-1,true,140,12,2,2.6f,1.5f,0,duration:.9333333f,windup:.4333333f),
                    D("D08","史都华德",-1,1,true,140,16,2,2.6f,1.8f,0,damage:CombatDamageType.Arts,duration:.8666667f,windup:.5f),
                    D("D09","炎熔",3,1,true,140,15,2,2.8f,2.8f,0,damage:CombatDamageType.Arts,duration:1.5666667f,windup:.8333333f,splash:.65f),
                    D("D10","芙蓉",-1,-1,true,140,28,2,3.4f,2.2f,0,DefenderRole.Medic,windup:.4666667f),
                    D("D12","梓兰",4,-1,true,140,12,2,2.8f,1.8f,0,damage:CombatDamageType.Arts,windup:.5666667f)
                };
                string dir="Assets/_Game/Art/Materials/Industrial/";
                string[] names={"Route Concrete","Deck Limestone","Deck Sidewall","Platform Surface","Canal Water","Pale Edges","Safety Amber"};
                data.surfaces=new Material[names.Length];
                for(int i=0;i<names.Length;i++) data.surfaces[i]=AssetDatabase.LoadAssetAtPath<Material>(dir+names[i]+".mat");
                EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
            }
            Validate();
        }
        private static Vector3 P(float x,float z) => new Vector3(x,.04f,z);
        private static DefenderPlacement D(string id,string name,float x,float z,bool high,float hp,float attack,float armor,float range,float interval,int blocks,
            DefenderRole role=DefenderRole.Fighter,CombatDamageType damage=CombatDamageType.Physical,float duration=1,float windup=.4f,float splash=0,float healRange=0)
            => new DefenderPlacement { id=id,displayName=name,artId=id,position=new Vector3(x,high ? .46f : .04f,z),health=hp,
                attack=attack,armor=armor,range=range,interval=interval,blocks=blocks,role=role,damageType=damage,
                actionDuration=duration,windup=windup,splashRadius=splash,healingRange=healRange };
        public static void Validate()
        {
            var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root);
            if(data==null) throw new InvalidOperationException("Missing defender trial.");
            var roster=data.Snapshot(); foreach(var d in roster) _=DefenderArt.Load(d.ArtId);
            if(data.surfaces?.Length!=7 || Array.Exists(data.surfaces,m=>m==null)) throw new InvalidOperationException("Missing industrial material.");
            var b=new ManualBattle(data.battle.Snapshot(),data.route,Vector3.zero,data.battle.Engagement.Snapshot(),roster:roster);
            if(b.Defenders.Count!=8) throw new InvalidOperationException("Invalid defender count.");
            Debug.Log("DEFENDER_TRIAL_CONFIGURATION_PASS: eight original front models, independent stats, fixed route, industrial materials.");
        }
        private static void Set(UnityEngine.Object asset,string name,object value)
        {
            var so=new SerializedObject(asset); var p=so.FindProperty(name);
            if(p==null) throw new InvalidOperationException(name);
            if(value is UnityEngine.Object obj) p.objectReferenceValue=obj;
            else if(value is float f) p.floatValue=f;
            else if(value is int n) p.intValue=n;
            else if(value is string s) p.stringValue=s;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
