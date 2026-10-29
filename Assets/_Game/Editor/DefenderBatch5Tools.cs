using System;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DefenderBatch5Tools
    {
        const string Root="Assets/_Game/Data/Resources/DefenderTrials/";
        [MenuItem("Yesterday Convoy/Defenders/Create Six Star Batch Five")]
        public static void Apply()
        {
            var template=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+"D06.asset");
            var battle=CopyBattle(template.battle,"B5-battle",18000,12,32,2);
            var engagement=AssetDatabase.LoadAssetAtPath<EngagementDefinition>(Root+"B5-combat.asset");
            if(engagement==null)
            {engagement=UnityEngine.Object.Instantiate(template.battle.Engagement);Set(engagement,"skillAttack",700f);AssetDatabase.CreateAsset(engagement,Root+"B5-combat.asset");Set(battle,"engagement",engagement);}
            var cards=(ManualBattleDefinition[])template.rosterCards.Clone();
            for(int i=0;i<cards.Length;i++)cards[i]=CopyBattle(cards[i],"B5-creature-"+i,18000,12,32,1);
            foreach(string id in ManualBattleController.DefenderBatch5Ids)
            {
                if(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset")!=null)continue;
                var data=UnityEngine.Object.Instantiate(template);data.name=id;data.battle=battle;data.rosterCards=cards;data.defenderTestId=id;data.mapId="defender-"+id;
                var d=Make(id);data.mapTitle=d.displayName+" · 六星技能测试";
                var helper=Helper("helper","D01","米格鲁 · 测试搭档",new Vector3(-5.5f,.04f,1),false);
                helper.health=id=="D23"?6000:1800;
                if(id=="D24"){helper.reserve=true;helper.position=new Vector3(-5.5f,.04f,1);helper.skill=new DefenderSkillDefinition{kind=DefenderSkillKind.BeagleGuard,initialSP=0,requiredSP=40,spPerSecond=1,duration=10,defenseMultiplier=1.5f};}
                data.defenders=new[]{d,helper};
                AssetDatabase.CreateAsset(data,Root+id+".asset");
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static DefenderPlacement Make(string id)
        {
            var d=new DefenderPlacement{id=id,artId=id,health=1400,attack=20,armor=12,range=3.5f,interval=1.6f,windup=.35f,actionDuration=1.333333f,blocks=3,
                position=new Vector3(-4.5f,.76f,2),relayPosition=new Vector3(-5.5f,.04f,2),skill=new DefenderSkillDefinition()};var s=d.skill;
            switch(id)
            {
                case "D23":d.displayName="Mon3tr";d.role=DefenderRole.Medic;d.noOrdinaryAttack=true;d.interval=d.actionDuration=2.833333f;d.healingRange=4;
                    s.kind=DefenderSkillKind.Mon3trMeltdown;s.initialSP=7;s.requiredSP=15;s.duration=25;s.attackMultiplier=3.8f;s.effectRadius=.95f;break;
                case "D24":d.displayName="新约能天使";d.position=new Vector3(-4.5f,.04f,3);d.blocks=1;d.interval=d.actionDuration=1.333333f;
                    s.kind=DefenderSkillKind.ExusiaiDelivery;s.initialSP=25;s.requiredSP=35;s.duration=99999;s.attackMultiplier=1.2f;s.effectRadius=3.5f;break;
                case "D25":d.displayName="斩业星熊";d.position=new Vector3(-5.5f,.04f,3);d.interval=d.actionDuration=1.6f;d.range=.9f;
                    s.kind=DefenderSkillKind.HoshigumaInferno;s.initialSP=31;s.requiredSP=50;s.duration=32;s.attackMultiplier=2.8f;s.effectRadius=2.5f;break;
            }
            return d;
        }
        static DefenderPlacement Helper(string id,string art,string name,Vector3 position,bool caster)=>new DefenderPlacement
        {id=id,artId=art,displayName=name,position=position,health=900,attack=caster?18:2,armor=12,range=caster?5:.9f,
            interval=2,windup=.4f,actionDuration=1.2f,blocks=caster?0:3,damageType=caster?CombatDamageType.Arts:CombatDamageType.Physical};
        static ManualBattleDefinition CopyBattle(ManualBattleDefinition source,string name,float health,int uses,int dp,int cost)
        {
            var data=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(Root+name+".asset");if(data!=null)return data;
            data=UnityEngine.Object.Instantiate(source);data.name=name;Set(data,"levelId","DEFENDER_"+name);Set(data,"unitHealth",health);Set(data,"deploymentLimit",uses);Set(data,"initialDP",dp);Set(data,"deploymentCost",cost);Set(data,"deploymentCooldown",.7f);
            AssetDatabase.CreateAsset(data,Root+name+".asset");return data;
        }
        static void Set(UnityEngine.Object obj,string field,object value)
        {
            var so=new SerializedObject(obj);var p=so.FindProperty(field);
            if(value is int i)p.intValue=i;else if(value is float f)p.floatValue=f;else if(value is string s)p.stringValue=s;else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void Validate()
        {
            foreach(string id in ManualBattleController.DefenderBatch5Ids)
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(data==null||data.defenderTestId!=id||data.defenders[0].id!=id||!data.defenders[0].Snapshot().Skill.Enabled)throw new InvalidOperationException("Missing batch-four trial "+id);
                _=data.Snapshot();_=DefenderArt.Load(id);_=DefenderArt.Load(id+"B");
            }
            Debug.Log("DEFENDER_BATCH5_CONFIG_PASS: four original six-star S3 trials; prior twelve and mixed maps untouched.");
        }
    }
}
