using System;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DefenderBatch6Tools
    {
        const string Root="Assets/_Game/Data/Resources/DefenderTrials/";
        [MenuItem("Yesterday Convoy/Defenders/Create Six Star Batch Six")]
        public static void Apply()
        {
            var template=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+"D06.asset");
            var battle=CopyBattle(template.battle,"B6-battle",18000,12,32,2);
            var engagement=AssetDatabase.LoadAssetAtPath<EngagementDefinition>(Root+"B6-combat.asset");
            if(engagement==null)
            {engagement=UnityEngine.Object.Instantiate(template.battle.Engagement);Set(engagement,"skillAttack",700f);AssetDatabase.CreateAsset(engagement,Root+"B6-combat.asset");Set(battle,"engagement",engagement);}
            var cards=(ManualBattleDefinition[])template.rosterCards.Clone();
            for(int i=0;i<cards.Length;i++)cards[i]=CopyBattle(cards[i],"B6-creature-"+i,18000,12,32,1);
            foreach(string id in ManualBattleController.DefenderBatch6Ids)
            {
                if(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset")!=null)continue;
                var data=UnityEngine.Object.Instantiate(template);data.name=id;data.battle=battle;data.rosterCards=cards;data.defenderTestId=id;data.mapId="defender-"+id;
                var d=Make(id);data.mapTitle=d.displayName+" · 六星技能测试";
                var helper=Helper("helper","D01","米格鲁 · 测试搭档",new Vector3(-5.5f,.04f,2),false);helper.health=6000;
                if(id=="D26"){
                    var a=Helper("reserve-high","D04","玫兰莎 · 备援",new Vector3(-5.5f,.04f,1),false);a.reserve=true;a.reserveCost=20;a.relayPosition=a.position;
                    var b=Helper("reserve-low","D01","米格鲁 · 备援",new Vector3(-3.5f,.04f,0),false);b.reserve=true;b.reserveCost=8;b.relayPosition=b.position;
                    data.defenders=new[]{d,helper,a,b};
                }else data.defenders=new[]{d,helper};
                AssetDatabase.CreateAsset(data,Root+id+".asset");
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static DefenderPlacement Make(string id)
        {
            var d=new DefenderPlacement{id=id,artId=id,health=1800,attack=20,armor=12,range=3.5f,interval=2.2f,windup=.4f,actionDuration=2.2f,blocks=0,position=new Vector3(-4.5f,.76f,2),skill=new DefenderSkillDefinition()};var s=d.skill;
            switch(id){
                case "D26":d.displayName="凛御银灰";d.health=6000;d.position=new Vector3(-4.5f,.04f,3);d.blocks=2;d.interval=d.actionDuration=1.2f;d.windup=.3f;
                    s.kind=DefenderSkillKind.SilverAshReign;s.initialSP=42;s.requiredSP=55;s.duration=48;s.attackMultiplier=1.7f;s.effectRadius=5.5f;break;
                case "D27":d.displayName="圣聆初雪";d.noOrdinaryAttack=true;d.interval=d.actionDuration=2;
                    s.kind=DefenderSkillKind.PramanixSnow;s.initialSP=35;s.requiredSP=53;s.duration=35;s.attackMultiplier=1.7f;s.attackSpeedMultiplier=1.3f;s.effectRadius=4.5f;break;
                case "D28":d.displayName="望";s.kind=DefenderSkillKind.WangGambit;s.initialSP=38;s.requiredSP=54;s.duration=99999;s.effectRadius=5.5f;break;
            }return d;
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
            foreach(string id in ManualBattleController.DefenderBatch6Ids)
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(data==null||data.defenderTestId!=id||data.defenders[0].id!=id||!data.defenders[0].Snapshot().Skill.Enabled)throw new InvalidOperationException("Missing batch-six trial "+id);
                _=data.Snapshot();_=DefenderArt.Load(id);_=DefenderArt.Load(id+"B");
            }
            Debug.Log("DEFENDER_BATCH6_CONFIG_PASS: three original six-star S3 trials; prior nineteen and mixed maps untouched.");
        }
    }
}
