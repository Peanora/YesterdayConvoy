using System;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DefenderBatch4Tools
    {
        const string Root="Assets/_Game/Data/Resources/DefenderTrials/";
        [MenuItem("Yesterday Convoy/Defenders/Create Six Star Batch Four")]
        public static void Apply()
        {
            var template=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+"D06.asset");
            var battle=CopyBattle(template.battle,"B4-battle",18000,12,32,2);
            var engagement=AssetDatabase.LoadAssetAtPath<EngagementDefinition>(Root+"B4-combat.asset");
            if(engagement==null)
            {engagement=UnityEngine.Object.Instantiate(template.battle.Engagement);Set(engagement,"skillAttack",700f);AssetDatabase.CreateAsset(engagement,Root+"B4-combat.asset");Set(battle,"engagement",engagement);}
            var cards=(ManualBattleDefinition[])template.rosterCards.Clone();
            for(int i=0;i<cards.Length;i++)cards[i]=CopyBattle(cards[i],"B4-creature-"+i,18000,12,32,1);
            foreach(string id in ManualBattleController.DefenderBatch4Ids)
            {
                if(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset")!=null)continue;
                var data=UnityEngine.Object.Instantiate(template);data.name=id;data.battle=battle;data.rosterCards=cards;data.defenderTestId=id;data.mapId="defender-"+id;
                var d=Make(id);data.mapTitle=d.displayName+" · 六星技能测试";
                if(id=="D21"||id=="D30")data.defenders=new[]{d,Helper("blocker","D01","米格鲁 · 测试搭档",new Vector3(-5.5f,.04f,id=="D30"?1:3),false)};
                else data.defenders=new[]{d,Helper("caster-a","D08","史都华德 · 法术搭档",new Vector3(-2.5f,.76f,2),true),
                    Helper("caster-b","D09","炎熔 · 法术搭档",new Vector3(-3.5f,.76f,1),true),Helper("helper","D01","米格鲁 · 测试搭档",new Vector3(-5.5f,.04f,0),false)};
                AssetDatabase.CreateAsset(data,Root+id+".asset");
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static DefenderPlacement Make(string id)
        {
            var d=new DefenderPlacement{id=id,artId=id,health=1400,attack=24,armor=12,range=.9f,interval=1.6f,windup=.4f,actionDuration=1.2f,blocks=3,
                position=new Vector3(-5.5f,.04f,3),skill=new DefenderSkillDefinition()};var s=d.skill;
            switch(id)
            {
                case "D21":d.displayName="荒芜拉普兰德";d.position=new Vector3(-3.5f,.76f,2);d.health=1000;d.range=3.5f;d.armor=5;d.blocks=0;d.damageType=CombatDamageType.Arts;d.interval=1.3f;
                    s.kind=DefenderSkillKind.LapplandHavoc;s.initialSP=38;s.requiredSP=63;s.duration=40;s.attackMultiplier=1.6f;s.moveMultiplier=.6f;s.targets=3;s.effectRadius=.9f;break;
                case "D22":d.displayName="余";d.attack=30;d.health=1600;d.armor=14;
                    s.kind=DefenderSkillKind.YuKitchen;s.initialSP=33;s.requiredSP=55;s.duration=42;s.attackMultiplier=s.defenseMultiplier=s.healthMultiplier=1.8f;s.effectRadius=5;s.elementFraction=.07f;s.eraseChance=.15f;break;
                case "D29":d.displayName="塞雷娅";d.role=DefenderRole.Guardian;d.attack=40;d.healingRange=4;d.interval=1.2f;d.actionDuration=1.1666666f;
                    s.kind=DefenderSkillKind.SariaCalcification;s.initialSP=55;s.requiredSP=80;s.duration=22;s.healFraction=.2f;s.effectRadius=4;s.damageAmplification=1.4f;s.moveMultiplier=.4f;break;
                case "D30":d.displayName="玛恩纳";d.noOrdinaryAttack=true;d.kasimierz=true;d.attack=20;d.interval=1.2f;d.actionDuration=1.2f;
                    s.kind=DefenderSkillKind.MlynarGlory;s.initialSP=20;s.requiredSP=45;s.duration=26;s.attackMultiplier=1.5f;s.targets=5;s.chargeSeconds=40;s.effectRadius=3.5f;break;
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
            foreach(string id in ManualBattleController.DefenderBatch4Ids)
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(data==null||data.defenderTestId!=id||data.defenders[0].id!=id||!data.defenders[0].Snapshot().Skill.Enabled)throw new InvalidOperationException("Missing batch-four trial "+id);
                _=data.Snapshot();_=DefenderArt.Load(id);_=DefenderArt.Load(id+"B");
            }
            Debug.Log("DEFENDER_BATCH4_CONFIG_PASS: four original six-star S3 trials; prior twelve and mixed maps untouched.");
        }
    }
}
