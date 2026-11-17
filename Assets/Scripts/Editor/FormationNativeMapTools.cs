using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Explicit authoring only. The playable scene contains every saved native mesh before Play.
    public static class FormationNativeMapTools
    {
        public const string RootName="LS5 native formation courtyard";
        const string GatePrefab="Assets/Art/Environment/BlackForestNative/NativeForest.prefab";
        static readonly Rect FloorFrame=new Rect(200,108,792,365);
        static readonly string[] FloorNames={"0242","0275","0106","0267","0272","0110","0107","0273","0263","0234","0264","0114","0274","0270","0116","0233"};

        [MenuItem("Yesterday Convoy/Formation Battle/Apply Original LS-5 Environment")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes first.");
            var catalog=FormationNativeAssetTools.EnsureImported();
            var scene=EditorSceneManager.OpenScene(FormationBattleTools.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            if(controller==null||controller.battleCamera==null)throw new InvalidOperationException("Formation scene bindings missing.");
            foreach(var old in scene.GetRootGameObjects().Where(g=>g.name==RootName||g.name.StartsWith("Painted courtyard",StringComparison.Ordinal)||g.name.StartsWith("Industrial courtyard",StringComparison.Ordinal)).ToArray())Object.DestroyImmediate(old);
            var terrain=scene.GetRootGameObjects().Single(g=>g.name.StartsWith("Authored rectangle",StringComparison.Ordinal));
            foreach(var child in terrain.transform.Cast<Transform>().Where(t=>!t.name.StartsWith("Tile ",StringComparison.Ordinal)).ToArray())Object.DestroyImmediate(child.gameObject);
            var tiles=terrain.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Tile ",StringComparison.Ordinal)).ToArray();
            if(tiles.Length!=112)throw new InvalidOperationException("Expected the original 112 saved tile objects.");
            foreach(var tile in tiles)
            {
                var parts=tile.name.Substring(5).Split(',');int col=int.Parse(parts[0]),row=int.Parse(parts[1]);
                var source=FormationNativeAssetTools.FindPartName("S_Ground_tiles_Qcity_"+FloorNames[(col+row*5)%FloorNames.Length]);
                tile.gameObject.SetActive(true);tile.position=FormationBattleController.WorldPoint(new Vector2(col,row))+Vector3.up*.0200154f;
                tile.rotation=Quaternion.identity;tile.localScale=Vector3.one;
                var filter=tile.GetComponent<MeshFilter>()??tile.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=FormationNativeAssetTools.GetMesh(source);
                var renderer=tile.GetComponent<MeshRenderer>()??tile.gameObject.AddComponent<MeshRenderer>();
                renderer.enabled=true;renderer.sharedMaterials=source.submeshes.Select(s=>FormationNativeAssetTools.GetMaterial(s.material)).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;renderer.lightmapIndex=-1;
                foreach(var collider in tile.GetComponents<Collider>())Object.DestroyImmediate(collider);
            }
            var root=new GameObject(RootName).transform;
            var surroundings=new GameObject("Original LS-5 perimeter parts").transform;surroundings.SetParent(root,false);
            int dressed=0;
            foreach(var source in catalog.parts)
            {
                if(source.name=="BG_Qcity")
                {
                    var sky=new GameObject("Original LS-5 surrounding city").transform;sky.SetParent(root,false);sky.localScale=new Vector3(1.5f,1,1.8f);
                    FormationNativeAssetTools.CreatePart(source,sky,true);continue;
                }
                // Source-only occluders describe the original narrow corridor. They have no
                // corresponding building after widening and would cast floating giant shadows.
                if(source.castShadows==(int)ShadowCastingMode.ShadowsOnly)continue;
                var sourceSize=source.SourceBoundsMax-source.SourceBoundsMin;
                // Tiny rooftop fittings lose their supporting baked building when the corridor is
                // widened. Keep them in the source catalogue rather than leave floating fragments.
                if(source.SourceBoundsMin.y>2.5f&&sourceSize.x<.8f&&sourceSize.y<.8f&&sourceSize.z<.8f)continue;
                var p=source.SourcePosition;
                // The native stage has a narrow central corridor. Move its complete service bands out
                // around our fixed open rectangle, retaining each band's original relative prop positions.
                if(p.z>=1.45f)p.z+=3.05f;
                else if(p.z<=-1.45f)p.z-=3.05f;
                else if(p.x<=-4.45f)p.x-=3.05f;
                else if(p.x>=4.45f)p.x+=3.05f;
                else continue;
                var localMin=source.SourceBoundsMin-source.SourcePosition;
                var localMax=source.SourceBoundsMax-source.SourcePosition;
                if(p.x+localMax.x> -7.05f&&p.x+localMin.x<7.05f&&p.z+localMax.z> -4.05f&&p.z+localMin.z<4.05f)
                {
                    if(p.z>=0)p.z=4.12f-localMin.z;else p.z=-4.12f-localMax.z;
                }
                var go=FormationNativeAssetTools.CreatePart(source,surroundings);go.transform.localPosition=p+Vector3.up*.14f;dressed++;
            }
            CreateGates(root);
            SetupLighting(scene);
            FrameCamera(controller.battleCamera);
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            Validate();CapturePreview();Debug.Log("FORMATION_LS5_AUTHOR_PASS: 112 original floor cells, "+dressed+" source perimeter parts, original background mesh, 16 full-cell native gates");
        }

        static void CreateGates(Transform root)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefab);
            if(prefab==null)throw new InvalidOperationException("Existing original gate resources missing.");
            foreach(bool red in new[]{true,false})
            {
                var source=prefab.GetComponentsInChildren<Transform>(true).First(t=>t.name==(red?"R1 / Native tile_start":"B1 / Native tile_end"));
                for(int row=0;row<8;row++)
                {
                    var go=Object.Instantiate(source.gameObject,root);go.name=(red?"Red":"Blue")+" gate row "+row;
                    go.transform.localPosition=FormationBattleController.WorldPoint(new Vector2(red?6:13,row));
                    go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
                    foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                    foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>(true)){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                }
            }
        }

        [Serializable] sealed class SettingsList{public SourceSetting[] settings;}
        [Serializable] sealed class SourceSetting{public string type,name;public LightData data;public float[] worldMatrix;}
        [Serializable] sealed class LightData{public float m_Intensity;public Color m_Color;}
        static void SetupLighting(Scene scene)
        {
            foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray())Object.DestroyImmediate(light.gameObject);
            var records=JsonUtility.FromJson<SettingsList>("{\"settings\":"+File.ReadAllText(FormationNativeAssetTools.ArtPath+"/ls5-scene-settings.json")+"}");
            var source=records.settings.Single(s=>s.type=="Light"&&s.name=="MainLight");var m=source.worldMatrix;
            var forward=new Vector3(m[2],-m[10],m[6]);var up=new Vector3(m[1],-m[9],m[5]);
            var sun=new GameObject("Original LS-5 sun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;
            // The source mixed/baked lighting and game shaders are not available here. Copying
            // its warm direct term alone makes Standard surfaces ochre, without the original
            // baked balance. Keep the source direction and probe, with a scene-local neutral key.
            sun.transform.rotation=Quaternion.LookRotation(forward,up);sun.color=new Color(.92f,.94f,1f);sun.intensity=1.35f;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.45f;sun.shadowBias=.035f;sun.shadowNormalBias=.2f;
            RenderSettings.fog=false;RenderSettings.skybox=null;
            FormationNativeLightingTools.ApplySourceAmbientProbe();
        }

        static Vector3[] Corners()=>new[]{new Vector2(-.5f,-.5f),new Vector2(13.5f,-.5f),new Vector2(13.5f,7.5f),new Vector2(-.5f,7.5f)}.Select(FormationBattleController.WorldPoint).ToArray();
        static Rect ProjectionBounds(Camera camera)
        {
            var points=Corners().Select(p=>VirtualPoint(camera,p)).ToArray();
            float x=points.Min(p=>p.x),y=points.Min(p=>p.y);return new Rect(x,y,points.Max(p=>p.x)-x,points.Max(p=>p.y)-y);
        }
        public static Vector2 VirtualPoint(Camera camera,Vector3 point){var p=camera.WorldToViewportPoint(point);return new Vector2(p.x*1280,(1-p.y)*720);}
        static void FrameCamera(Camera camera)
        {
            camera.orthographic=false;camera.usePhysicalProperties=false;camera.fieldOfView=40;camera.lensShift=Vector2.zero;
            camera.rect=new Rect(0,0,1,1);camera.aspect=1280f/720;camera.nearClipPlane=.1f;camera.farClipPlane=180;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.25f,.25f);
            camera.cullingMask=~(1<<31);camera.transform.rotation=Quaternion.Euler(60,0,0);
            float distance=18;var focus=new Vector3(0,.12f,0);
            for(int pass=0;pass<12;pass++)
            {
                camera.transform.position=focus-camera.transform.forward*distance;
                for(int center=0;center<5;center++)
                {
                    var bounds=ProjectionBounds(camera);float ppu=720/(2*distance*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f));
                    camera.transform.position+=camera.transform.right*(bounds.center.x-FloorFrame.center.x)/ppu+camera.transform.up*(FloorFrame.center.y-bounds.center.y)/ppu;
                }
                var fitted=ProjectionBounds(camera);float factor=Mathf.Max(fitted.width/FloorFrame.width,fitted.height/FloorFrame.height);
                focus=camera.transform.position+camera.transform.forward*distance;
                if(factor<=1.001f)break;distance*=factor*1.002f;
            }
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Validate Original LS-5 Environment")]
        public static void Validate()
        {
            FormationBattleTools.Validate();var scene=SceneManager.GetActiveScene();
            var root=scene.GetRootGameObjects().Single(g=>g.name==RootName);var camera=Object.FindAnyObjectByType<FormationBattleController>().battleCamera;
            // Camera.aspect is an editor override, not a serialized scene property. Validate the
            // actual virtual 16:9 presentation, independent of the batch editor's Game View size.
            camera.aspect=1280f/720;
            var tiles=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("Tile ",StringComparison.Ordinal)).ToArray();
            if(tiles.Length!=112)throw new InvalidOperationException("Native floor must keep 112 saved cells.");
            foreach(var tile in tiles)
            {
                var mesh=tile.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh==null||!AssetDatabase.GetAssetPath(mesh).StartsWith(FormationNativeAssetTools.ArtPath+"/",StringComparison.Ordinal))throw new InvalidOperationException("Non-native floor mesh: "+tile.name);
            }
            foreach(string color in new[]{"Red","Blue"})for(int row=0;row<8;row++)
            {
                var gate=root.GetComponentsInChildren<Transform>().Single(t=>t.name==color+" gate row "+row);
                if(gate.localScale!=Vector3.one||gate.GetComponentsInChildren<MeshRenderer>().Length!=4||gate.GetComponentsInChildren<Collider>().Length>0)throw new InvalidOperationException("Gate must keep all original full-size transparent layers.");
                var expected=FormationBattleController.WorldPoint(new Vector2(color=="Red"?6:13,row));
                if(Vector3.Distance(gate.position,expected)>.0001f)throw new InvalidOperationException("Gate is not centred on its full cell.");
            }
            var projected=ProjectionBounds(camera);
            if(projected.xMin<180||projected.xMax>1012||projected.yMin<65||projected.yMax>485)throw new InvalidOperationException("Saved native map projects outside original A safe frame: "+projected);
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterials.Any(m=>m==null||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))))throw new InvalidOperationException("Unsaved native material.");
            Debug.Log("FORMATION_LS5_VALIDATE_PASS: native source meshes, 112 square cells, 8+8 full native transparent gates, source 60deg/40FOV camera, floorFrame="+projected);
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Capture Original LS-5 Scene")]
        public static void CapturePreview()
        {
            var camera=Object.FindAnyObjectByType<FormationBattleController>().battleCamera;
            var original=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                camera.aspect=1280f/720;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                Directory.CreateDirectory("artifacts/formation-ls5");File.WriteAllBytes("artifacts/formation-ls5/editor-native-map.png",pixels.EncodeToPNG());
            }
            finally{camera.targetTexture=original;RenderTexture.active=active;Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
