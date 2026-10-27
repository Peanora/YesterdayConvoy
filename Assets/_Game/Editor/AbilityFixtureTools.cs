using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    /// <summary>Neutral private regression hosts, excluded from the playable build and map menu.</summary>
    public static class AbilityFixtureTools
    {
        public static Vector3[] RouteFor(int i)
        {
            if(i==1)return new[]{P(-6,-2),P(-4,-2),P(-4,2),P(0,2),P(0,-1),P(4,-1),P(4,3),P(6,3)};
            if(i==2)return new[]{P(-6,1),P(-3,1),P(-3,-2),P(2,-2),P(2,2),P(6,2)};
            return new[]{P(-6,2),P(-2,2),P(-2,-2),P(3,-2),P(3,1),P(6,1)};
        }
        static Vector3 P(float x,float z)=>new Vector3(x,0,z);
        public static Scene NewHost()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Regression camera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.rotation=Quaternion.Euler(60,0,0);camera.transform.position=camera.transform.rotation*new Vector3(0,0,-24);
            camera.fieldOfView=32;camera.backgroundColor=new Color(.07f,.09f,.11f);
            var light=new GameObject("Regression light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(60,-20,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Neutral regression floor";floor.transform.position=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(16,.2f,11);
            floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Industrial/Deck Limestone.mat");
            return scene;
        }
        public static void Generate()
        {
            const string folder="Assets/_Game/Tests/Fixtures";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string[] names={"01-IndustrialTransfer","02-SnowRelay","03-ReedCanal"};
            for(int i=0;i<3;i++)
            {
                var scene=NewHost();var c=new GameObject("Private ability regression").AddComponent<FacilityTrialController>();c.mapIndex=i;c.route=RouteFor(i);
                string[] ids=i==0?new[]{"M19","M07","M29","M31","M32","M38"}:i==1?new[]{"M14"}:new[]{"M28","M30","M26","M33","M34"};
                c.roles=ids.Select(id=>Resources.Load<ManualBattleDefinition>("MonsterTests/"+id)).ToArray();
                c.deployableTiles=Enumerable.Range(-6,13).SelectMany(x=>Enumerable.Range(-3,8).Select(z=>P(x,z))).ToArray();
                c.interfaceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansSC-Regular.otf");
                EditorSceneManager.SaveScene(scene,folder+"/"+names[i]+".unity");
            }
            GuerrillaTrialTools.Generate();PatriotTrialTools.Generate();AssetDatabase.SaveAssets();
            Debug.Log("ABILITY_FIXTURES_PASS: private neutral hosts preserve teammate mechanics; no scene-switch UI.");
        }
    }
}
