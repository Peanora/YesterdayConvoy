using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>One-way migration from the retired previews. Git retains their history.</summary>
    public static class LegacyAssetCleanup
    {
        private static readonly string[] RetiredImages = {
            "Characters/Monsters/Boss/wisadel-april-battle.png", "Characters/Monsters/Boss/wisadel-attack.png",
            "Characters/Monsters/Boss/wisadel-rear.png", "Characters/Monsters/Boss/wisadel-side.png",
            "Characters/Monsters/Boss/wisadel-rear-quarter.png", "Characters/Monsters/Boss/wisadel-walk-left.png",
            "Characters/Monsters/Boss/wisadel-walk-right.png", "Characters/Defenders/grumpy-cannon.png",
            "Characters/Defenders/kroos-battle.png", "Cards/Portraits/wisadel-april-approved.png",
            "Environment/Portal/rune-gate-red.png", "Environment/Portal/rune-gate-blue.png",
            "Environment/Tilesets/slate-floor.png"
        };

        public static void Apply()
        {
            // Remove old camera-space meshes while preserving the prefab's root GUID and card/font.
            const string cardPrefab="Assets/_Game/Prefabs/FiveCostCard.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(cardPrefab);
            try
            {
                while(prefab.transform.childCount>0) UnityEngine.Object.DestroyImmediate(prefab.transform.GetChild(0).gameObject);
                PrefabUtility.SaveAsPrefabAsset(prefab,cardPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            var scene=EditorSceneManager.OpenScene(InitialDemoTools.ScenePath,OpenSceneMode.Single);
            var source=UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var controller=UnityEngine.Object.FindAnyObjectByType<ManualBattleController>();
            var card=source.Card;
            var combat=controller.Definition.Engagement;
            foreach(string name in new[]{"Defense Tower","Tower Range","Tower Shot","Raised Tactical Board"})
            {
                var retired=scene.GetRootGameObjects().FirstOrDefault(go=>go.name==name);
                if(retired!=null) UnityEngine.Object.DestroyImmediate(retired);
            }
            foreach(var gate in new[]{source.RedGate,source.BlueGate})
                foreach(Transform child in gate.Cast<Transform>().ToArray())
                    if(child.name!="Tactical Gate") UnityEngine.Object.DestroyImmediate(child.gameObject);

            Transform template=source.Monster;
            foreach(Transform child in template.Cast<Transform>().ToArray())
                if(child.name!="Series Sprite" && child.name!="Contact shadow") UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(var component in template.GetComponents<Component>())
                if(component is MeshFilter || component is MeshRenderer || component is Collider) UnityEngine.Object.DestroyImmediate(component);
            var visual=template.GetComponentInChildren<DemoSpriteVisual>(true);
            if(visual==null) throw new InvalidOperationException("Missing current deployment template visual.");
            var sprite=visual.Renderer;
            sprite.sprite=card.BattleSprite; sprite.enabled=true; sprite.color=Color.white;
            visual.transform.rotation=Camera.main.transform.rotation;
            template.name="Weiweimei Deployment Template";
            template.position=source.Waypoints[0]; template.gameObject.SetActive(false);

            Transform map=GameObject.Find(TacticalMapTools.RootName).transform;
            foreach(Transform child in map.Cast<Transform>().ToArray())
                if(!child.gameObject.activeSelf) UnityEngine.Object.DestroyImmediate(child.gameObject);

            var view=controller.CombatView;
            if(view==null) view=new GameObject("Kroos engagement presentation").AddComponent<EngagementView>();
            var kroos=view.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            if(kroos==null)
            {
                kroos=new GameObject("Kroos combat sprite",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                kroos.transform.SetParent(view.transform,false);
            }
            kroos.sprite=combat.KroosSprite; kroos.sharedMaterial=sprite.sharedMaterial;
            kroos.transform.position=combat.DefenderPosition; kroos.transform.rotation=Camera.main.transform.rotation;
            kroos.sortingOrder=DemoSpriteVisual.GetSortingOrder(Camera.main,combat.DefenderPosition)+30;
            var viewData=new SerializedObject(view);
            viewData.FindProperty("kroos").objectReferenceValue=kroos; viewData.ApplyModifiedPropertiesWithoutUndo();
            var controllerData=new SerializedObject(controller);
            controllerData.FindProperty("combatView").objectReferenceValue=view; controllerData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combat);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(InitialDemoTools.ScenePath,true)};
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);

            var candidates=new HashSet<string>(RetiredImages.Select(p=>"Assets/_Game/Art/"+p));
            if(Directory.Exists("Assets/_Game/Materials"))
                foreach(string file in Directory.GetFiles("Assets/_Game/Materials","*.mat")) candidates.Add(file.Replace('\\','/'));
            candidates.Add("Assets/_Game/Scenes/Bootstrap.unity");
            candidates.Add("Assets/_Game/Data/Units/U02.asset"); candidates.Add("Assets/_Game/Data/Units/U03.asset");
            var retained=new List<string>();
            foreach(string path in AssetDatabase.GetAllAssetPaths())
            {
                if(!path.StartsWith("Assets/",StringComparison.Ordinal) || candidates.Contains(path)) continue;
                string ext=Path.GetExtension(path);
                if(ext==".unity" || ext==".prefab" || ext==".asset" || ext==".mat" || ext==".controller" || ext==".anim") retained.Add(path);
            }
            var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(retained.ToArray(),true));
            var blocked=candidates.Where(p=>dependencies.Contains(p)).ToArray();
            if(blocked.Length!=0) throw new InvalidOperationException("Retired assets still referenced: "+string.Join(", ",blocked));
            var deleted=new List<string>();
            foreach(string path in candidates.OrderBy(p=>p))
            {
                string absolute=Path.GetFullPath(path);
                string allowed=Path.GetFullPath("Assets/_Game")+Path.DirectorySeparatorChar;
                if(!absolute.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe cleanup path: "+path);
                if(!File.Exists(path)) continue;
                if(!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("Could not delete retired asset: "+path);
                deleted.Add(path);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("artifacts/legacy-cleanup");
            File.WriteAllLines("artifacts/legacy-cleanup/deleted-assets.txt",deleted);
            DemoArtTools.Validate();
            Debug.Log("LEGACY_CLEANUP_PASS: "+deleted.Count+" assets removed; current authored defender and hidden comic template; one scene.");
        }

        public static void BuildCurrent()
        {
            foreach(string folder in new[]{"Assets/_Game/Materials","Assets/_Game/Art/Environment/Portal","Assets/_Game/Art/Environment/Tilesets"})
            {
                string allowed=Path.GetFullPath("Assets/_Game")+Path.DirectorySeparatorChar;
                if(!Path.GetFullPath(folder).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe folder path.");
                if(Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any()) AssetDatabase.DeleteAsset(folder);
            }
            EditorSceneManager.OpenScene(InitialDemoTools.ScenePath,OpenSceneMode.Single);
            var camera=Camera.main;
            var target=new RenderTexture(1280,720,24);
            var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes("artifacts/legacy-cleanup/editor-preview.png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget; RenderTexture.active=oldActive;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            BaselineTools.BuildWindows();
        }
    }
}
