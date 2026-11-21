using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    public static class FormationDeploymentTools
    {
        [MenuItem("Yesterday Convoy/Formation Battle/Configure Full Roster Deployment")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes first.");
            var scene=EditorSceneManager.OpenScene(FormationBattleTools.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            var cards=AssetDatabase.FindAssets("t:CardDefinition",new[]{"Assets/Data/Cards"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c=>c!=null&&FormationUnitCatalog.Find(c.CardId)!=null).ToArray();
            controller.formationCards=FormationUnitCatalog.All.Select(definition=>cards.Single(c=>c.CardId==definition.CardId)).ToArray();
            ClearForegroundOcclusion();
            EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("FORMATION_DEPLOYMENT_AUTHOR_PASS: "+FormationUnitCatalog.All.Count+" saved card references, v0.7 formal roster selected by campaign rules, expanded saved terrain and source camera retained");
        }
        [MenuItem("Yesterday Convoy/Formation Battle/Rebuild Expanded Preparation Board")]
        public static void RebuildExpandedBoard()
        {
            FormationCanyonTools.Apply();
            Configure();
        }

        static void ClearForegroundOcclusion()
        {
            var native=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name==FormationNativeMapTools.RootName).transform;
            const string groupName="Foreground moved outward for deployment visibility";
            if(native.Find(groupName)!=null)return; // Preserve the authored adjustment on repeat runs.
            var source=native.Find("Original LS-5 perimeter parts");
            if(source==null)return;
            var obstructing=source.GetComponentsInChildren<MeshRenderer>().Where(r=>r.bounds.center.z< -4f&&r.bounds.max.y>1.5f).ToArray();
            var group=new GameObject(groupName).transform;group.SetParent(native,false);
            foreach(var renderer in obstructing)
            {renderer.transform.SetParent(group,true);renderer.transform.position+=Vector3.back*1.8f;}
            Debug.Log("FORMATION_FOREGROUND_CLEARANCE: moved "+obstructing.Length+" tall source props outward; all deployment cells and terrain remain unchanged");
        }
        [MenuItem("Yesterday Convoy/Formation Battle/Validate Full Roster Deployment")]
        public static void Validate()
        {
            FormationCanyonTools.Validate();
            FormationMapTools.Validate();
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            if(controller.formationCards==null||controller.formationCards.Length!=FormationUnitCatalog.All.Count||
                controller.formationCards.Any(c=>c==null)||controller.formationCards.Select(c=>c.CardId).Distinct().Count()!=FormationUnitCatalog.All.Count)
                throw new InvalidOperationException("Saved formation card references must cover the full authored catalog.");
            foreach(var definition in FormationUnitCatalog.All)
                if(!controller.formationCards.Any(c=>c.CardId==definition.CardId)||definition.Footprint.x<1||definition.Footprint.y<1||definition.VisualHeight<=0)
                    throw new InvalidOperationException("Invalid deployment presentation: "+definition.CardId);
            Debug.Log("FORMATION_DEPLOYMENT_VALIDATE_PASS: saved source card bindings and complete footprint/presentation metadata");
        }
    }
}
