using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    public static class SynergyTrialSceneTools
    {
        [MenuItem("Yesterday Convoy/Synergy Trials/Create Missing Authored Scenes")]
        public static void CreateMissingScenes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring scenes.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or close dirty scenes before authoring trials.");
            if(!File.Exists(CampaignTools.SandboxScene))throw new InvalidOperationException("Restore the authored SynergyTrial scene from Git.");
            var source=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/Data/Resources/Maps/BattleShop.asset");
            if(source==null)throw new InvalidOperationException("The authored map encounter configuration is missing.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureFolder("Assets/Scenes/SynergyTrials");EnsureFolder("Assets/Data/SynergyTrials/Encounters");
                foreach(var entry in SynergyTrialCatalog.AllEntries)
                {
                    var encounter=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(entry.EncounterPath);
                    if(encounter==null)
                    {
                        // Opening a different scene unloads unreferenced source assets; reacquire per iteration.
                        encounter=Object.Instantiate(AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>("Assets/Data/Resources/Maps/BattleShop.asset"));encounter.name=entry.Id+"-defenders";
                        encounter.mapTitle=entry.Title+" · 专项验证";encounter.defenders=DefaultDefenders(entry.Id);
                        AssetDatabase.CreateAsset(encounter,entry.EncounterPath);
                    }
                    var definition=AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(entry.DefinitionPath);
                    if(definition==null)
                    {
                        definition=ScriptableObject.CreateInstance<SynergyTrialDefinition>();definition.name=entry.Id;
                        definition.trialId=entry.Id;definition.displayTitle=entry.Title+" · 专项验证";definition.initialLevel=8;definition.encounter=encounter;
                        definition.initialMembers=entry.Members.Select(id=>new SynergyTrialStartingCard{cardId=id,star=1}).ToArray();
                        if(entry.Id=="infection")definition.infectionPrimary=InfectionForm.Burst;
                        if(entry.Id=="sarkaz")definition.contractCardId="CARD_M37";
                        AssetDatabase.CreateAsset(definition,entry.DefinitionPath);
                    }
                    definition.Validate();
                    if(File.Exists(entry.ScenePath))continue; // Never regenerate an existing authored map or overwrite Inspector edits.
                    if(!AssetDatabase.CopyAsset(CampaignTools.SandboxScene,entry.ScenePath))throw new InvalidOperationException("Scene copy failed: "+entry.ScenePath);
                    var scene=EditorSceneManager.OpenScene(entry.ScenePath,OpenSceneMode.Single);
                    var bootstrap=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignBootstrap>(true)).Single();
                    bootstrap.sandbox=true;bootstrap.trialDefinition=definition;
                    EditorUtility.SetDirty(bootstrap);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    if(AssetDatabase.AssetPathToGUID(entry.ScenePath)==AssetDatabase.AssetPathToGUID(CampaignTools.SandboxScene))
                        throw new InvalidOperationException("Copied scene reused the source GUID: "+entry.ScenePath);
                }
                AssetDatabase.SaveAssets();GameSceneCatalog.UpdateBuildSettings();ValidateExistingScenes();
            Debug.Log("DEDICATED_SYNERGY_SCENES_PREPARED: nine faction and six combat-style independently saved scenes; no runtime terrain generation.");
            }
            finally {if(setup.Any(s=>s.isLoaded&&s.isActive&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [MenuItem("Yesterday Convoy/Synergy Trials/Validate Authored Scenes")]
        public static void ValidateExistingScenes()
        {
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or close dirty scenes before validation.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var seen=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach(var entry in SynergyTrialCatalog.AllEntries)
                {
                    if(!File.Exists(entry.ScenePath))throw new InvalidOperationException("Missing authored trial: "+entry.ScenePath);
                    string guid=AssetDatabase.AssetPathToGUID(entry.ScenePath);
                    if(string.IsNullOrEmpty(guid)||!seen.Add(guid))throw new InvalidOperationException("Missing or duplicate scene GUID: "+entry.ScenePath);
                    var scene=EditorSceneManager.OpenScene(entry.ScenePath,OpenSceneMode.Single);
                    var roots=scene.GetRootGameObjects();var bootstrap=roots.SelectMany(g=>g.GetComponentsInChildren<CampaignBootstrap>(true)).Single();
                    if(!bootstrap.sandbox||bootstrap.trialDefinition==null||bootstrap.trialDefinition.trialId!=entry.Id)
                        throw new InvalidOperationException("Wrong dedicated bootstrap binding: "+entry.ScenePath);
                    bootstrap.trialDefinition.Validate();
                    if(roots.SelectMany(g=>g.GetComponentsInChildren<ShopMapBootstrap>(true)).Any())throw new InvalidOperationException("Two competing startup bootstraps: "+entry.ScenePath);
                    if(!roots.SelectMany(g=>g.GetComponentsInChildren<AuthoredBattlefield>(true)).Any())throw new InvalidOperationException("Authored terrain is missing: "+entry.ScenePath);
                    if(roots.Sum(g=>g.GetComponentsInChildren<Renderer>(true).Length)==0)
                        throw new InvalidOperationException("Authored renderers are missing: "+entry.ScenePath);
                }
            }
            finally {if(setup.Any(s=>s.isLoaded&&s.isActive&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(path))))throw new InvalidOperationException("Cannot create asset folder: "+path);
        }
        static DefenderPlacement Guard(string id,string art,string title,Vector3 position,float hp,float attack,float armor,float interval=1.8f)
        {
            return new DefenderPlacement{id=id,artId=art,displayName=title,position=position,health=hp,attack=attack,armor=armor,
                range=1.05f,interval=interval,windup=.3f,actionDuration=1.2f,blocks=2,role=DefenderRole.Fighter,damageType=CombatDamageType.Physical};
        }
        static DefenderPlacement[] DefaultDefenders(string trial)
        {
            // Baseline values only. The existing sandbox still applies HP x12 and attack x8 at runtime.
            float hp=200,attack=8,armor=4,interval=1.8f;
            if(trial=="snow"){hp=400;attack=6;interval=2.4f;}
            if(trial=="infection"){hp=120;attack=18;interval=1.2f;}
            if(trial=="guerrilla"){hp=250;attack=10;interval=1.6f;}
            if(trial=="mudrock"){hp=200;attack=12;}
            if(trial=="herd"){hp=100;attack=20;interval=1.2f;}
            if(trial=="deep-pool"){hp=100;attack=10;interval=2;}
            if(trial=="marine"){hp=100;attack=10;armor=8;}
            if(trial=="sarkaz"){hp=350;attack=16;interval=1.5f;}
            if(SynergyTrialCatalog.IsCombatStyle(trial)){hp=300;attack=10;armor=8;interval=1.8f;}
            if(trial=="style-assault"){armor=40;attack=8;}
            if(trial=="style-frenzy"){hp=180;attack=18;interval=1.2f;}
            var result=new System.Collections.Generic.List<DefenderPlacement>{
                Guard(trial+"-north-a","D01","米格鲁 · 北线",new Vector3(0,.04f,2.4f),hp,attack,armor,interval),
                Guard(trial+"-north-b","D04","玫兰莎 · 北线",new Vector3(.8f,.04f,2.1f),hp,attack,armor,interval),
                Guard(trial+"-south-a","D01","米格鲁 · 南线",new Vector3(2,.04f,-1),hp,attack,armor,interval),
                Guard(trial+"-south-b","D04","玫兰莎 · 南线",new Vector3(2,.04f,0),hp,attack,armor,interval),
            };
            // Direct melee attacks normally stop outside the block radius. The
            // assault trial intentionally presents actual entry interception.
            if(trial=="style-assault")
            {result[0].position=new Vector3(0,.04f,3.5f);result[2].position=new Vector3(0,.04f,-3.5f);}
            if(trial=="snow"||trial=="deep-pool"||trial=="style-mystic")
            {
                result.Add(Guard(trial+"-north-c","D04","玫兰莎 · 群体目标",new Vector3(-.6f,.04f,2.1f),hp,attack,armor,interval));
                result.Add(Guard(trial+"-south-c","D04","玫兰莎 · 群体目标",new Vector3(2.8f,.04f,-.5f),hp,attack,armor,interval));
            }
            if(trial=="marine")
            {
                // Actual high-ground positions on the authored board. Ranged contact is required to observe Barrier.
                result[1]=Guard("marine-north-ranged","D07","克洛丝 · 北高台",StrongholdMapLayout.Cell(7,6,.76f),70,10,2,1.5f);
                result[3]=Guard("marine-south-ranged","D07","克洛丝 · 南高台",StrongholdMapLayout.Cell(13,0,.76f),70,10,2,1.5f);
                foreach(var ranged in new[]{result[1],result[3]}){ranged.range=3.4f;ranged.blocks=0;ranged.windup=.4333333f;ranged.actionDuration=.9333333f;}
            }
            return result.ToArray();
        }
    }
}
