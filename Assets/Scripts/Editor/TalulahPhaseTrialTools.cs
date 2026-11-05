using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    public static class TalulahPhaseTrialTools
    {
        public const string ScenePath="Assets/Scenes/TalulahPhaseTrial.unity";
        public const string EncounterPath="Assets/Data/Trials/TalulahPhaseDefenders.asset";
        [MenuItem("Yesterday Convoy/Talulah Phase Trial/Create Missing Authored Scene")]
        public static void CreateMissingScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before scene authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save or close dirty scenes first.");
            if(!File.Exists(InitialDemoTools.ScenePath))throw new InvalidOperationException("Restore the authored MonsterTrial scene first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var source=EditorSceneManager.OpenScene(InitialDemoTools.ScenePath,OpenSceneMode.Single);
                var demo=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<InitialDemo>(true)).Single();
                var talulah=AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>("Assets/Data/Battles/TalulahTrial.asset");
                if(talulah==null)throw new InvalidOperationException("The original Talulah model/configuration is missing.");
                if(!AssetDatabase.IsValidFolder("Assets/Data/Trials"))AssetDatabase.CreateFolder("Assets/Data","Trials");
                var encounter=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(EncounterPath);
                if(encounter==null)
                {
                    encounter=ScriptableObject.CreateInstance<DefenderTrialDefinition>();encounter.name="TalulahPhaseDefenders";
                    encounter.battle=talulah;encounter.route=demo.Waypoints.ToArray();encounter.mapId="talulah-phase-trial";encounter.mapTitle="三星塔露拉 · 自然承伤双阶段";
                    var positions=new[]{talulah.Engagement.DefenderPosition}.Concat(talulah.Engagement.ExtraDefenders).ToArray();
                    if(positions.Length!=3)throw new InvalidOperationException("Expected three saved Talulah encounter positions.");
                    encounter.defenders=positions.Select((position,i)=>new DefenderPlacement{
                        id="talulah-phase-kroos-"+i,artId="D07",displayName="克洛丝 · 双阶段测试",position=position,
                        health=50000,attack=140,armor=40,range=9,interval=1,windup=.4333333f,actionDuration=.9333333f,
                        blocks=0,role=DefenderRole.Fighter,damageType=CombatDamageType.Physical}).ToArray();
                    AssetDatabase.CreateAsset(encounter,EncounterPath);
                }
                _=encounter.Snapshot();
                if(!File.Exists(ScenePath))
                {
                    if(!AssetDatabase.CopyAsset(InitialDemoTools.ScenePath,ScenePath))throw new InvalidOperationException("Cannot copy the authored monster scene.");
                    var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var roots=scene.GetRootGameObjects();
                    if(roots.SelectMany(g=>g.GetComponentsInChildren<CampaignBootstrap>(true)).Any()||roots.SelectMany(g=>g.GetComponentsInChildren<ShopMapBootstrap>(true)).Any())
                        throw new InvalidOperationException("Unexpected competing startup components in source scene.");
                    var bootstrap=new GameObject("Talulah phase trial configuration").AddComponent<TalulahPhaseTrialBootstrap>();bootstrap.encounter=encounter;
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    if(AssetDatabase.AssetPathToGUID(ScenePath)==AssetDatabase.AssetPathToGUID(InitialDemoTools.ScenePath))throw new InvalidOperationException("The new scene reused the source GUID.");
                }
                AssetDatabase.SaveAssets();GameSceneCatalog.UpdateBuildSettings();
                Debug.Log("TALULAH_PHASE_SCENE_PREPARED: saved original terrain; A4250/336/12; three original Kroos models at 50000/140/40, interval1.");
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
