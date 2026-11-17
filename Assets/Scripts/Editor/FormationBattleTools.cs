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
    public static class FormationBattleTools
    {
        public const string ScenePath="Assets/Scenes/FormationBattle.unity";
        const string Art="Assets/Art/Environment/FormationBattle";
        [MenuItem("Yesterday Convoy/Formation Battle/Create Missing Scene")]
        public static void Author()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes first.");
            if(!Directory.Exists(Art))Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var ground=Material("Steel",new Color(.25f,.30f,.33f));
                var ally=Material("Deployment",new Color(.20f,.32f,.34f));
                var enemy=Material("Defence",new Color(.35f,.29f,.28f));
                var trim=Material("Trim",new Color(.11f,.16f,.19f));
                var cyan=Material("BlueGate",new Color(.14f,.66f,.80f));
                var marker=Material("Marker",new Color(.68f,.76f,.74f));
                var root=new GameObject("Authored rectangle · "+FormationBattleModel.Width+" x "+FormationBattleModel.Height+" · edit and save terrain here");
                for(int row=0;row<FormationBattleModel.Height;row++)for(int col=0;col<FormationBattleModel.Width;col++)
                {
                    Vector3 p=FormationBattleController.WorldPoint(new Vector2(col,row));p.y=-.08f;
                    Box(root.transform,$"Tile {col:00},{row:00}",p,new Vector3(.975f,.16f,.975f),col<FormationBattleModel.DeploymentColumns?ally:col>=FormationBattleModel.DeploymentColumns+2&&col<FormationBattleModel.Width-1?enemy:col==FormationBattleModel.Width-1?cyan:ground);
                    if(col==FormationBattleModel.DeploymentColumns)Box(root.transform,"Centre seam",p+new Vector3(-.5f,.1f,0),new Vector3(.035f,.012f,.40f),marker);
                }
                Box(root.transform,"Saved solid platform",new Vector3(0,-.24f,0),new Vector3(FormationBattleModel.Width+.5f,.30f,FormationBattleModel.Height+.5f),trim);
                for(int side=-1;side<=1;side+=2)
                {
                    Box(root.transform,"Industrial edge rail",new Vector3(0,.04f,side*(FormationBattleModel.Height*.5f+.38f)),new Vector3(FormationBattleModel.Width+.6f,.18f,.12f),trim);
                    for(int x=-FormationBattleModel.Width/2+1;x<FormationBattleModel.Width/2;x+=3)Box(root.transform,"Edge marker",new Vector3(x,.14f,side*(FormationBattleModel.Height*.5f+.38f)),new Vector3(.34f,.03f,.14f),marker);
                }
                for(int row=0;row<FormationBattleModel.Height;row++)Box(root.transform,"Blue threshold",FormationBattleController.WorldPoint(new Vector2(FormationBattleModel.Width-.48f,row)),new Vector3(.12f,.32f,.96f),cyan);
                var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
                camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=3.40f;
                camera.transform.rotation=Quaternion.Euler(60,0,0);camera.transform.position=new Vector3(0,17.32f,-10)+camera.transform.up*.45f;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.08f,.105f);
                camera.nearClipPlane=.1f;camera.farClipPlane=100;
                // Matching viewport is set each frame by the controller, independent of resolution.
                var controller=new GameObject("Formation Battle · runtime actors and UI only").AddComponent<FormationBattleController>();controller.battleCamera=camera;
                controller.actorTemplate=ShaderMaterial("Actors","Sprites/Default");controller.lineTemplate=ShaderMaterial("Lines","Hidden/Internal-Colored");
                var light=new GameObject("Soft daylight",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;
                light.transform.rotation=Quaternion.Euler(65,-25,0);light.shadows=LightShadows.Soft;
                RenderSettings.ambientLight=new Color(.57f,.65f,.72f);RenderSettings.fog=false;
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            AssetDatabase.SaveAssets();GameSceneCatalog.UpdateBuildSettings();Validate();
        }
        static Material Material(string name,Color color)
        {
            string path=Art+"/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
            var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(m,path);return m;
        }
        static Material ShaderMaterial(string name,string shader)
        {
            string path=Art+"/"+name+".mat";var prior=AssetDatabase.LoadAssetAtPath<Material>(path);if(prior!=null)return prior;
            var material=new Material(Shader.Find(shader)){name=name};AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Box(Transform root,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
        [MenuItem("Yesterday Convoy/Formation Battle/Validate")]
        public static void Validate()
        {
            if(!File.Exists(ScenePath))throw new InvalidOperationException("Formation scene is not authored.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var c=Object.FindAnyObjectByType<FormationBattleController>();
            if(c==null||c.battleCamera==null)throw new InvalidOperationException("Formation scene bindings missing.");
            int tiles=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).Count(t=>t.name.StartsWith("Tile ",StringComparison.Ordinal));
            if(tiles!=FormationBattleModel.Width*FormationBattleModel.Height)throw new InvalidOperationException("Saved terrain tile count: "+tiles);
            foreach(var r in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MeshRenderer>()))
                if(r.sharedMaterial==null||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(r.sharedMaterial)))throw new InvalidOperationException("Unsaved terrain material.");
            Debug.Log("FORMATION_SCENE_VALIDATE_PASS: "+tiles+" saved editable tiles, persistent materials, independent scene");
        }
    }
}
