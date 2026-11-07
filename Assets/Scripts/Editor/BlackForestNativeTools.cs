using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Applies already-authored native art. Runtime never reads source bundles or rebuilds terrain.
    public static class BlackForestNativeTools
    {
        const string Art="Assets/Art/Environment/BlackForestNative";
        [MenuItem("Yesterday Convoy/Black Forest/Apply Approved Native Environment")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before changing saved map art.");
            var scene=EditorSceneManager.OpenScene(BlackForestMapTools.ScenePath);
            var field=Object.FindAnyObjectByType<AuthoredBattlefield>();
            var tiles=field.GetComponentsInChildren<BlackForestTile>(true);
            if(tiles.Length!=238)throw new InvalidOperationException("Expected the approved authored terrain.");
            var geometry=tiles[0].transform.parent;
            foreach(var tile in tiles)foreach(var child in tile.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            foreach(var child in field.transform.Cast<Transform>().Where(c=>c!=geometry).ToArray())Object.DestroyImmediate(child.gameObject);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/NativeForest.prefab");
            if(prefab==null)throw new InvalidOperationException("Missing portable native environment.");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,field.transform);
            PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            instance.name="Native tree roots, flora and gates - editable saved objects";
            foreach(var tile in tiles){
                string coordinate=((char)('A'+tile.coordinate.x-1))+tile.coordinate.y.ToString("00");
                var mesh=instance.transform.Cast<Transform>().FirstOrDefault(c=>c.name.StartsWith(coordinate+" / ",StringComparison.Ordinal));
                if(mesh!=null)mesh.SetParent(tile.transform,true);
                tile.name=coordinate+" / "+(tile.surface==BlackForestSurface.Inaccessible?"Inaccessible":tile.surface==BlackForestSurface.HighGround?"Native raised stone":tile.surface==BlackForestSurface.Moss?"Native moss leaves":"Native root-pattern floor (visual only)");
            }
            var data=Object.FindAnyObjectByType<BlackForestBootstrap>().encounter;
            data.surfaces=AssetDatabase.FindAssets("t:Material",new[]{Art}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();EditorUtility.SetDirty(data);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.48f,.54f,.53f);RenderSettings.ambientEquatorColor=new Color(.31f,.36f,.33f);RenderSettings.ambientGroundColor=new Color(.16f,.19f,.19f);
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(48,-28,0);light.color=new Color(.92f,1,.95f);light.intensity=1;light.shadowStrength=.6f;}
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Validate();BlackForestMapTools.RenderPreview();
            Debug.Log("BLACK_FOREST_NATIVE_APPLIED: saved original meshes/textures and tile_start/tile_end gates, 238 editable cells, no gameplay changes");
        }
        [MenuItem("Yesterday Convoy/Black Forest/Validate Native Environment")]
        public static void Validate()
        {
            BlackForestMapTools.Validate();var field=Object.FindAnyObjectByType<AuthoredBattlefield>();
            var renderers=field.GetComponentsInChildren<Renderer>(true);
            if(renderers.OfType<LineRenderer>().Any())throw new InvalidOperationException("Placeholder gate lines must be removed.");
            foreach(var renderer in renderers)foreach(var mat in renderer.sharedMaterials){
                if(mat==null||!AssetDatabase.GetAssetPath(mat).StartsWith(Art+"/",StringComparison.Ordinal))throw new InvalidOperationException("Non-native map material: "+renderer.name);
                if(!mat.shader.isSupported)throw new InvalidOperationException("Unsupported map shader: "+mat.name);
                if(mat.mainTexture==null)throw new InvalidOperationException("Missing source map texture: "+mat.name);
            }
            int red=field.GetComponentsInChildren<Transform>(true).Count(t=>t.name.Contains(" / Native tile_start")),blue=field.GetComponentsInChildren<Transform>(true).Count(t=>t.name.Contains(" / Native tile_end"));
            if(red!=3||blue!=2)throw new InvalidOperationException("Native gate counts differ from approved layout.");
            foreach(var r in renderers){var mesh=r.GetComponent<MeshFilter>();if(mesh!=null&&!AssetDatabase.GetAssetPath(mesh.sharedMesh).StartsWith(Art+"/",StringComparison.Ordinal)&&mesh.sharedMesh.name!="Plane")throw new InvalidOperationException("Non-native map mesh: "+r.name);}
            Debug.Log("BLACK_FOREST_NATIVE_ASSETS_PASS: original map art, 3 tile_start / 2 tile_end, persistent meshes/materials/textures, no placeholder lines");
        }
        public static void Build(){Validate();BaselineTools.BuildWindows();}
    }
}
