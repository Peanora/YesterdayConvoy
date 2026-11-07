using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace YesterdayConvoy.Editor
{
    public static class BlackForestMapTools
    {
        public const string ScenePath="Assets/Scenes/BlackForestCrossing.unity";
        [MenuItem("Yesterday Convoy/Scenes/Open Black Forest Crossing")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
        [MenuItem("Yesterday Convoy/Black Forest/Validate Saved Map")]
        public static void Validate()
        {
            BlackForestLayout.Validate();EditorSceneManager.OpenScene(ScenePath);var root=Object.FindAnyObjectByType<AuthoredBattlefield>();
            if(root==null||Object.FindObjectsByType<BlackForestTile>(FindObjectsSortMode.None).Length!=238)throw new InvalidOperationException("Missing authored forest tiles");
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh==null||!AssetDatabase.Contains(filter.sharedMesh))throw new InvalidOperationException("Transient or missing map mesh: "+filter.name);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))if(r.sharedMaterials.Any(m=>m==null||!AssetDatabase.Contains(m)))throw new InvalidOperationException("Transient or missing map material: "+r.name);
            if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Decorations must not introduce collision or line-of-sight rules");
            var data=Object.FindAnyObjectByType<BlackForestBootstrap>().encounter;_=data.Snapshot();
            if(data.LaneSnapshot().Length!=3||data.lanes.Select(l=>l.exitId).Distinct().Count()!=2||!data.terrainOnly)throw new InvalidOperationException("Wrong gate or encounter configuration");
            Debug.Log("BLACK_FOREST_SAVED_ASSETS_PASS: terrain, meshes, materials and lane configuration are persistent and editable");
        }
        public static void RenderPreview()
        {
            Directory.CreateDirectory("Builds/BlackForestEvidence");var camera=Camera.main;var rt=new RenderTexture(1920,1080,24);var before=camera.targetTexture;camera.targetTexture=rt;camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes("Builds/BlackForestEvidence/editor-map.png",image.EncodeToPNG());camera.targetTexture=before;RenderTexture.active=active;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
        }
        public static void Build()=>BlackForestNativeTools.Build();
    }
}
