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
    public static class BossArenaTools
    {
        public const string ScenePath="Assets/Scenes/WisadelBossArena.unity";
        public const string DataPath="Assets/Data/Resources/BossArena";
        public const string DefinitionPath=DataPath+"/WisadelArena.asset";
        static readonly string[] Initial={"CARD_M07","CARD_M08","CARD_M10","CARD_M11","CARD_M12","CARD_M15","CARD_M18","CARD_TALULAH_TRIAL"};
        static TacticalLane[] ArenaLanes()=>Enumerable.Range(0,8).Select(i=>new TacticalLane{id="arena-entry-"+i,exitId="arena-center",points=new[]{
            new Vector3(-2.8f+(i/4)*1.34f,.04f,(i%4-1.5f)*1.32f),new Vector3(-.3f,.04f,(i%4-1.5f)*.6f),new Vector3(1.4f,.04f,0)}}).ToArray();
        public static ManualBattleDefinition Card(string id)=>AssetDatabase.FindAssets("t:ManualBattleDefinition")
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ManualBattleDefinition>).First(d=>d.Card.CardId==id);
        static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
        [MenuItem("Yesterday Convoy/Boss Arena/Author Missing Scene")]
        public static void Author()
        {
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes before authoring.");
            Folder(DataPath);Folder("Assets/Art/Environment/BossArena");
            var encounter=AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(DataPath+"/Encounter.asset");
            if(encounter==null)
            {
                encounter=ScriptableObject.CreateInstance<DefenderTrialDefinition>();encounter.battle=Card(Initial[0]);
                encounter.mapId=StrongholdChallengeLayout.Id;encounter.mapTitle="维什戴尔 · 超新星攻擂";
                encounter.lanes=ArenaLanes();
                encounter.route=encounter.lanes[0].points;
                encounter.defenders=new[]{new DefenderPlacement{id=BossArenaRules.BossId,displayName="维什戴尔 · 超新星",artId="D31",position=new Vector3(1.4f,.04f,0),
                    health=120000,attack=140,armor=10,range=7,interval=5,blocks=30,windup=.5333333f,actionDuration=5,splashRadius=.85f}};
                AssetDatabase.CreateAsset(encounter,DataPath+"/Encounter.asset");
            }
            var prep=AssetDatabase.LoadAssetAtPath<SynergyTrialDefinition>(DataPath+"/Preparation.asset");
            if(prep==null)
            {
                prep=ScriptableObject.CreateInstance<SynergyTrialDefinition>();prep.trialId=BossArenaRules.Id;prep.displayTitle="军团攻擂 · 维什戴尔";
                prep.initialLevel=8;prep.encounter=encounter;prep.initialMembers=Initial.Select(id=>new SynergyTrialStartingCard{cardId=id,star=2}).ToArray();
                AssetDatabase.CreateAsset(prep,DataPath+"/Preparation.asset");
            }
            var definition=AssetDatabase.LoadAssetAtPath<BossArenaDefinition>(DefinitionPath);
            if(definition==null){definition=ScriptableObject.CreateInstance<BossArenaDefinition>();definition.preparation=prep;definition.bossVisualScale=2.6f;AssetDatabase.CreateAsset(definition,DefinitionPath);}
            if(!File.Exists(ScenePath))
            {
                if(!AssetDatabase.CopyAsset(CampaignTools.SandboxScene,ScenePath))throw new InvalidOperationException("Could not copy the scene bindings.");
                var authored=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                foreach(var bootstrap in Object.FindObjectsByType<CampaignBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(bootstrap);
                foreach(var anchor in Object.FindObjectsByType<GramophoneAnchor>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(anchor.gameObject);
                foreach(var field in Object.FindObjectsByType<AuthoredBattlefield>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(field.gameObject);
                var root=new GameObject("Authored compact arena / 9 x 7");root.AddComponent<AuthoredBattlefield>();
                var floor=Material("Floor",new Color(.23f,.29f,.31f));var edge=Material("Edge",new Color(.10f,.14f,.17f));
                var pale=Material("Pale",new Color(.37f,.42f,.43f));var red=Material("Entry",new Color(.68f,.19f,.18f));
                Cube(root.transform,"Arena foundation",new Vector3(0,-.27f,0),new Vector3(7.55f,.42f,5.85f),edge);
                for(int row=-3;row<=3;row++)for(int col=-4;col<=4;col++)
                    Cube(root.transform,$"Tile {col+4},{row+3}",new Vector3(col*.8f,-.055f,row*.8f),new Vector3(.775f,.1f,.775f),col==4||row==-3||row==3?pale:floor);
                for(int row=-2;row<=2;row++)Cube(root.transform,"East parapet "+row,new Vector3(3.7f,.12f,row*.8f),new Vector3(.22f,.4f,.76f),edge);
                for(int i=0;i<encounter.lanes.Length;i++)Cube(root.transform,"Red assembly pad "+i,encounter.lanes[i].points[0]-Vector3.up*.025f,new Vector3(.65f,.035f,.65f),red);
                Cube(root.transform,"Boss plinth",new Vector3(1.4f,.015f,0),new Vector3(1.6f,.035f,1.6f),pale);
                var controller=Object.FindAnyObjectByType<ManualBattleController>();var boot=controller.gameObject.AddComponent<BossArenaBootstrap>();boot.definition=definition;
                Camera.main.transform.rotation=Quaternion.Euler(60,0,0);Camera.main.fieldOfView=24;Camera.main.transform.position=new Vector3(0,1.9f,.2f)-Camera.main.transform.forward*17;
                EditorSceneManager.MarkSceneDirty(authored);EditorSceneManager.SaveScene(authored);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();GameSceneCatalog.UpdateBuildSettings();Validate();
            Debug.Log("BOSS_ARENA_AUTHORED: independent compact saved scene; no runtime terrain generation.");
        }
        // Explicit refinement of this task's prototype only; normal Author never overwrites saved layout edits.
        public static void RefineTaskLayout()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            if(Object.FindAnyObjectByType<BossArenaBootstrap>()==null)throw new InvalidOperationException("Not the owned arena scene.");
            var data=AssetDatabase.LoadAssetAtPath<BossArenaDefinition>(DefinitionPath);var encounter=data.preparation.encounter;
            encounter.lanes=ArenaLanes();encounter.route=encounter.lanes[0].points;EditorUtility.SetDirty(encounter);
            foreach(var anchor in Object.FindObjectsByType<GramophoneAnchor>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(anchor.gameObject);
            var field=Object.FindAnyObjectByType<AuthoredBattlefield>();
            foreach(var t in field.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Red assembly pad ")).ToArray())Object.DestroyImmediate(t.gameObject);
            var red=Material("Entry",new Color(.68f,.19f,.18f));
            for(int i=0;i<encounter.lanes.Length;i++)Cube(field.transform,"Red assembly pad "+i,encounter.lanes[i].points[0]-Vector3.up*.025f,new Vector3(.65f,.035f,.65f),red);
            Camera.main.transform.rotation=Quaternion.Euler(60,0,0);Camera.main.fieldOfView=24;Camera.main.transform.position=new Vector3(0,1.9f,.2f)-Camera.main.transform.forward*17;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();
            Debug.Log("BOSS_ARENA_LAYOUT_REFINED: eight nonoverlapping assembly slots; no inherited gramophones.");
        }
        static Material Material(string name,Color color)
        {
            string path="Assets/Art/Environment/BossArena/"+name+".mat";var found=AssetDatabase.LoadAssetAtPath<Material>(path);if(found!=null)return found;
            found=new Material(Shader.Find("Standard")){name="Arena "+name,color=color};found.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(found,path);return found;
        }
        static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name=name;item.transform.SetParent(parent,false);item.transform.position=position;item.transform.localScale=scale;
            item.GetComponent<Renderer>().sharedMaterial=material;item.isStatic=true;
        }
        public static void Validate()
        {
            var data=AssetDatabase.LoadAssetAtPath<BossArenaDefinition>(DefinitionPath);if(data==null||!File.Exists(ScenePath))throw new InvalidOperationException("Missing authored arena.");
            data.Validate();if(!GameSceneCatalog.BuildPaths().Contains(ScenePath))throw new InvalidOperationException("Arena absent from build catalog.");
        }
        public static void BuildWindows()
        {Validate();Evaluate();BaselineTools.BuildWindows();}
        [MenuItem("Yesterday Convoy/Boss Arena/Evaluate Current Balance")]
        public static void Evaluate()
        {
            Validate();var data=AssetDatabase.LoadAssetAtPath<BossArenaDefinition>(DefinitionPath);var rows=new System.Collections.Generic.List<object>();
            foreach(int star in new[]{1,2,3})
            {
                var profiles=Initial.Select(id=>CampaignV2Balance.Build(Card(id),star,0,1)).ToArray();var encounter=data.preparation.encounter;
                var b=new ManualBattle(profiles[0].Unit,encounter.route,Vector3.zero,profiles[0].Combat,roster:encounter.Snapshot(),deploymentLanes:encounter.LaneSnapshot());
                b.ConfigureCardLoadout(profiles.Skip(1));b.ConfigureCampaignV2();
                b.ConfigureSynergies(Initial.Select(id=>new ShopTrialModel.Piece{Id=id,Star=star}));
                b.ConfigureBossArena(new BossArenaRules(data.redeploymentSeconds,data.enragedAttackSpeed));b.Begin();long skillRequest=1000000;
                for(int step=0;step<7200&&b.Phase==BattlePhase.Running;step++)
                {
                    b.DeployReadyArenaSquad();foreach(var u in b.Units.ToArray())if(u.Active&&b.IsSkillReady(u.EntityId))b.TryActivateSkill(b.RunId,++skillRequest,u.EntityId);
                    b.Advance(.1f);
                }
                var row=new ArenaResult{star=star,seconds=b.Clock,phase=b.Phase.ToString(),bossHealth=b.ArenaBoss.Health,bossMaxHealth=b.ArenaBoss.MaxHealth,deployments=b.ArenaDeployments,halfAt=b.ArenaEnragedAt,casts=b.SkillCasts};rows.Add(row);Debug.Log("ARENA_SIM "+JsonUtility.ToJson(row));
            }
            Directory.CreateDirectory("artifacts/boss-arena");File.WriteAllText("artifacts/boss-arena/balance.json","["+string.Join(",",rows.Select(row=>JsonUtility.ToJson(row)))+"]");
        }
        [Serializable]sealed class ArenaResult{public int star,deployments,casts;public double seconds,halfAt;public string phase;public float bossHealth,bossMaxHealth;}
    }
}
