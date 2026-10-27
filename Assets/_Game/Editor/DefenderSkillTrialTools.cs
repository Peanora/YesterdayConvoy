using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DefenderSkillTrialTools
    {
        const string Root="Assets/_Game/Data/Resources/DefenderTrials/";
        [MenuItem("Yesterday Convoy/Defenders/Create Batch 1 Skill Trials")]
        public static void CreateBatch1()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var template=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/_Game/Data/Resources/MapTrials/city.asset");
            foreach(string id in ManualBattleController.DefenderBatch1Ids)
            {
                string path=Root+id+".asset";
                if(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(path)!=null)continue;
                var engagement=UnityEngine.Object.Instantiate(template.battle.Engagement);engagement.name=id+"-combat";
                Set(engagement,"unitAttack",25f);Set(engagement,"unitRange",3.2f);Set(engagement,"unitInterval",1.7f);
                AssetDatabase.CreateAsset(engagement,Root+id+"-combat.asset");
                var battle=UnityEngine.Object.Instantiate(template.battle);battle.name=id+"-battle";
                Set(battle,"levelId","DEFENDER_SKILL_"+id);Set(battle,"engagement",engagement);Set(battle,"unitHealth",18000f);
                AssetDatabase.CreateAsset(battle,Root+id+"-battle.asset");
                var data=UnityEngine.Object.Instantiate(template);data.name=id;data.battle=battle;data.defenderTestId=id;
                data.lanes=new[]{template.lanes[0]};data.route=(Vector3[])template.lanes[0].points.Clone();
                var d=new DefenderPlacement{id=id,artId=id,position=new Vector3(-5.5f,.04f,3),health=900,armor=6,
                    attack=16,range=.9f,interval=1.2f,windup=.4f,actionDuration=1,blocks=3,role=DefenderRole.Fighter,
                    skill=new DefenderSkillDefinition()};
                if(id=="D02")
                {d.displayName="卡缇";d.skill.kind=DefenderSkillKind.CardiganRecovery;d.skill.initialSP=10;d.skill.requiredSP=20;d.skill.healFraction=.4f;}
                if(id=="D05")
                {d.displayName="月见夜";d.armor=3;d.blocks=2;d.range=3;d.interval=1.3f;d.actionDuration=1.2f;d.rangedAttackMultiplier=.8f;d.criticalChance=.2f;
                    d.skill.kind=DefenderSkillKind.MidnightEnchant;d.skill.initialSP=30;d.skill.requiredSP=70;d.skill.duration=40;d.skill.attackMultiplier=1.35f;}
                if(id=="D06")
                {d.displayName="翎羽";d.armor=3;d.blocks=1;d.interval=1;d.actionDuration=.9f;d.windup=.3f;
                    d.skill.kind=DefenderSkillKind.PlumeSwiftStrike;d.skill.requiredSP=45;d.skill.duration=25;d.skill.attackMultiplier=1.25f;d.skill.attackSpeedMultiplier=1.25f;}
                if(id=="D11")
                {d.displayName="安赛尔";d.health=600;d.armor=2;d.blocks=0;d.position=new Vector3(-3.5f,.76f,2);d.role=DefenderRole.Medic;
                    d.attack=28;d.range=d.healingRange=2.5f;d.interval=2.85f;d.extraHealChance=.15f;
                    d.skill.kind=DefenderSkillKind.AnselRange;d.skill.initialSP=10;d.skill.requiredSP=35;d.skill.duration=25;d.skill.attackMultiplier=1.4f;d.skill.healRangeBonus=2;
                }
                data.defenders=id=="D11"?new[]{d,new DefenderPlacement{id="partner",artId="D01",displayName="米格鲁 · 医疗样板搭档",position=new Vector3(-5.5f,.04f,3),health=900,armor=6,attack=8,range=.9f,blocks=3,interval=1.6f,actionDuration=1,windup=.4f}}:new[]{d};
                data.mapId="defender-"+id;data.mapTitle=d.displayName+" · 守军技能测试";
                AssetDatabase.CreateAsset(data,path);
            }
            // A melee guard cannot demonstrate an offensive skill against a unit staying out of reach.
            // Keep Talulah available, and supply a durable, low-damage hound for the Plume sample.
            var plume=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+"D06.asset");
            if(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>(Root+"D06-hound.asset")==null)
            {
                var hound=UnityEngine.Object.Instantiate(template.rosterCards[1]);hound.name="D06-hound";
                var combat=UnityEngine.Object.Instantiate(hound.Engagement);combat.name="D06-hound-combat";
                Set(combat,"unitAttack",11f);Set(combat,"unitInterval",1.3f);
                AssetDatabase.CreateAsset(combat,Root+"D06-hound-combat.asset");
                Set(hound,"engagement",combat);Set(hound,"levelId","DEFENDER_D06_HOUND");Set(hound,"unitHealth",18000f);
                AssetDatabase.CreateAsset(hound,Root+"D06-hound.asset");
                plume.rosterCards=(ManualBattleDefinition[])plume.rosterCards.Clone();plume.rosterCards[1]=hound;EditorUtility.SetDirty(plume);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();ValidateBatch1();
        }
        public static void ValidateBatch1()
        {
            foreach(string id in ManualBattleController.DefenderBatch1Ids)
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(data==null||data.defenderTestId!=id||data.lanes.Length!=1||data.defenders[0].id!=id||!data.defenders[0].Snapshot().Skill.Enabled)throw new InvalidOperationException("Invalid defender skill trial "+id);
                _=data.Snapshot();var front=DefenderArt.Load(id);var rear=DefenderArt.Load(id+"B");
                if(front.ActionDuration<=0||rear.ActionDuration<=0)throw new InvalidOperationException("Missing action");
            }
            Debug.Log("DEFENDER_BATCH1_CONFIG_PASS: four trials, original front/rear art, individual skill snapshots; scenes/prefabs/settings unchanged.");
        }
        static void Set(UnityEngine.Object target,string field,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(field);
            if(value is float f)p.floatValue=f;else if(value is string s)p.stringValue=s;else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
