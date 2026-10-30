using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class OriginalEightSkillTools
    {
        const string Root="Assets/_Game/Data/Resources/DefenderTrials/";
        [MenuItem("Yesterday Convoy/Defenders/Apply Original Eight Skills")]
        public static void Apply()
        {
            var eight=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/_Game/Data/Resources/DefenderTrial.asset");
            var city=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/_Game/Data/Resources/MapTrials/city.asset");
            var ruins=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/_Game/Data/Resources/MapTrials/ruins.asset");
            foreach(var data in new[]{eight,city,ruins})
            {
                foreach(var d in data.defenders)
                    if(Array.IndexOf(ManualBattleController.OriginalEightIds,d.id)>=0&&(d.skill==null||d.skill.kind==DefenderSkillKind.None))Assign(d);
                EditorUtility.SetDirty(data);
            }
            var template=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+"D06.asset");
            foreach(string id in ManualBattleController.OriginalEightIds)
            {
                if(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset")!=null)continue;
                var d=JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(eight.defenders.First(v=>v.id==id)));
                d.health=d.role==DefenderRole.Medic?600:900;
                bool high=id=="D07"||id=="D08"||id=="D09"||id=="D10"||id=="D12";
                d.position=high?new Vector3(-3.5f,.76f,2):new Vector3(-5.5f,.04f,3);
                if(high){d.range=id=="D12"?4.5f:3.5f;d.healingRange=3.5f;}
                var data=UnityEngine.Object.Instantiate(template);data.name=id;data.defenderTestId=id;
                data.mapId="defender-"+id;data.mapTitle=d.displayName+" · 守军技能测试";
                data.defenders=high?new[]{d,new DefenderPlacement{id="support",artId="D01",displayName="米格鲁 · 测试搭档",position=new Vector3(-5.5f,.04f,3),
                    health=900,armor=12,attack=2,range=.9f,blocks=3,interval=1.6f,actionDuration=1,windup=.4f}}:new[]{d};
                AssetDatabase.CreateAsset(data,Root+id+".asset");
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static void Assign(DefenderPlacement d)
        {
            var s=new DefenderSkillDefinition();d.skill=s;
            switch(d.id)
            {
                case "D01":s.kind=DefenderSkillKind.BeagleGuard;s.requiredSP=40;s.duration=30;s.defenseMultiplier=1.5f;break;
                case "D03":s.kind=DefenderSkillKind.SpotHealingMode;s.initialSP=25;s.requiredSP=40;s.duration=25;s.attackMultiplier=1.45f;s.healingInterval=1.3f;break;
                case "D04":s.kind=DefenderSkillKind.MelanthaStrength;s.requiredSP=40;s.duration=20;s.attackMultiplier=1.5f;break;
                case "D07":s.kind=DefenderSkillKind.KroosDoubleShot;s.requiredSP=4;s.spPerSecond=0;s.attackMultiplier=1.4f;d.criticalChance=.2f;d.criticalMultiplier=1.5f;break;
                case "D08":s.kind=DefenderSkillKind.StewardStrike;s.requiredSP=4;s.spPerSecond=0;s.attackMultiplier=1.9f;break;
                case "D09":s.kind=DefenderSkillKind.LavaChant;s.requiredSP=40;s.duration=20;s.attackSpeedMultiplier=1.5f;if(d.splashRadius<=0)d.splashRadius=1.1f;break;
                case "D10":s.kind=DefenderSkillKind.HibiscusHealing;s.requiredSP=30;s.duration=20;s.attackMultiplier=1.5f;break;
                case "D12":s.kind=DefenderSkillKind.OrchidSwiftStrike;s.requiredSP=45;s.duration=25;s.attackMultiplier=s.attackSpeedMultiplier=1.25f;break;
            }
        }
        public static void Validate()
        {
            foreach(string id in ManualBattleController.OriginalEightIds)
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Root+id+".asset");
                if(data==null||data.defenderTestId!=id||!data.defenders[0].Snapshot().Skill.Enabled)throw new InvalidOperationException("Missing original-eight skill "+id);
                _=data.Snapshot();_=DefenderArt.Load(id);_=DefenderArt.Load(id+"B");
            }
            foreach(string path in new[]{"Assets/_Game/Data/Resources/DefenderTrial.asset","Assets/_Game/Data/Resources/MapTrials/city.asset","Assets/_Game/Data/Resources/MapTrials/ruins.asset"})
            {
                var data=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(path);
                if(data.defenders.Count(d=>d.Snapshot().Skill.Enabled)!=8)throw new InvalidOperationException("Mixed defense skills missing: "+path);
                _=data.Snapshot();
            }
            Debug.Log("ORIGINAL_EIGHT_SKILLS_CONFIG_PASS: eight individual entries and all three mixed rosters; original GUIDs preserved.");
        }
    }
}
