using System;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DefenderBatch7Tools
    {
        const string Root="Assets/Data/Resources/DefenderTrials/";
        [MenuItem("Yesterday Convoy/Defenders/Create Six Star Batch Seven")]
        public static void Apply()
        {
            var template=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+"D06.asset");
            if(template==null)throw new InvalidOperationException("Missing saved defender terrain template D06.");
            bool newBattle=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(Root+"B7-battle.asset")==null;
            var battle=CopyBattle(template.battle,"B7-battle",18000,12,40,1);
            var combat=AssetDatabase.LoadAssetAtPath<EngagementDefinition>(Root+"B7-combat.asset");
            if(combat==null){combat=UnityEngine.Object.Instantiate(template.battle.Engagement);Set(combat,"skillAttack",700f);AssetDatabase.CreateAsset(combat,Root+"B7-combat.asset");}
            if(newBattle)Set(battle,"engagement",combat);
            var cards=(ManualBattleDefinition[])template.rosterCards.Clone();
            for(int i=0;i<cards.Length;i++)cards[i]=CopyBattle(cards[i],"B7-creature-"+i,18000,12,40,1);
            foreach(string id in ManualBattleController.DefenderBatch7Ids)
            {
                // Existing authored tuning is retained on repeated execution.
                var existing=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(existing!=null)
                {
                    // Explicit correction of the misidentified D32; other authored trials
                    // and subsequent human tuning remain untouched on repeated execution.
                    if(id=="D32"&&existing.defenders[0].skill.kind==DefenderSkillKind.ChenHolungday)
                    {existing.defenders[0]=Make(id);existing.mapTitle="赤刃明霄陈 · 赤霄·天喟";EditorUtility.SetDirty(existing);}
                    continue;
                }
                var data=UnityEngine.Object.Instantiate(template);data.name=id;data.battle=battle;data.rosterCards=cards;data.defenderTestId=id;data.mapId="defender-"+id;
                var d=Make(id);data.mapTitle=d.displayName+" · 原技能7级适配试玩";
                var helper=new DefenderPlacement{id="helper",artId="D01",displayName="米格鲁 · 测试搭档",position=new Vector3(-5.5f,.04f,id=="D34"?1:3),health=6000,attack=2,armor=12,range=.9f,interval=2,windup=.4f,actionDuration=1.2f,blocks=3};
                data.defenders=new[]{d,helper};AssetDatabase.CreateAsset(data,Root+id+".asset");
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static DefenderPlacement Make(string id)
        {
            var d=new DefenderPlacement{id=id,artId=id,health=3600,attack=60,armor=12,range=3.5f,interval=2.2f,windup=.4f,actionDuration=1.6f,blocks=0,position=new Vector3(-4,.38f,1),skill=new DefenderSkillDefinition()};var s=d.skill;
            s.duration=99999;s.effectRadius=4.5f;
            switch(id)
            {
                case "D32":d.displayName="赤刃明霄陈";s.kind=DefenderSkillKind.ChenDawnstreak;s.initialSP=15;s.requiredSP=28;s.duration=20;s.attackMultiplier=1;s.effectRadius=3.5f;s.targets=3;d.position=new Vector3(-5.5f,.04f,2);d.blocks=1;d.damageType=CombatDamageType.Arts;d.range=1.5f;d.interval=1.25f;d.actionDuration=37f/30;d.windup=13f/30;break;
                case "D33":d.displayName="予愿安洁莉娜";s.kind=DefenderSkillKind.AngelinaWishes;s.initialSP=22;s.requiredSP=36;s.attackMultiplier=1.3f;d.position=new Vector3(-5.5f,.04f,2);d.blocks=1;d.interval=d.actionDuration=1.5f;break;
                case "D34":d.displayName="克莱门莎";s.kind=DefenderSkillKind.ClemensiaCorrosion;s.initialSP=28;s.requiredSP=45;d.attack=80;d.position=new Vector3(-5.5f,.04f,2);d.blocks=2;d.range=2.5f;d.interval=d.actionDuration=1.2f;d.windup=.3666667f;break;
                case "D35":d.displayName="凯尔希·思衡托";s.kind=DefenderSkillKind.KaltsitEsperanta;s.initialSP=20;s.requiredSP=35;s.attackMultiplier=2.25f;d.role=DefenderRole.Medic;d.noOrdinaryAttack=true;d.healingRange=3.5f;d.interval=d.actionDuration=3;d.windup=.5333333f;break;
                default:throw new ArgumentException("Unknown batch-seven defender "+id);
            }
            return d;
        }
        static ManualBattleDefinition CopyBattle(ManualBattleDefinition source,string name,float health,int uses,int dp,int cost)
        {
            var data=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(Root+name+".asset");if(data!=null)return data;
            data=UnityEngine.Object.Instantiate(source);data.name=name;Set(data,"levelId","DEFENDER_"+name);Set(data,"unitHealth",health);Set(data,"deploymentLimit",uses);Set(data,"initialDP",dp);Set(data,"deploymentCost",cost);Set(data,"deploymentCooldown",.7f);AssetDatabase.CreateAsset(data,Root+name+".asset");return data;
        }
        static void Set(UnityEngine.Object obj,string field,object value)
        {
            var so=new SerializedObject(obj);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException("Missing field "+field);
            if(value is int i)p.intValue=i;else if(value is float f)p.floatValue=f;else if(value is string s)p.stringValue=s;else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void Validate()
        {
            foreach(string id in ManualBattleController.DefenderBatch7Ids)
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(data==null||data.defenderTestId!=id||data.defenders[0].id!=id||!data.Snapshot()[0].Skill.Enabled)throw new InvalidOperationException("Missing batch-seven trial "+id);
                _=DefenderArt.Load(id);_=DefenderArt.Load(id+"B");
            }
            Debug.Log("DEFENDER_BATCH7_CONFIG_PASS: four original level-seven skill adaptations; existing authored terrain reused, formal stages unchanged.");
        }
    }
}
