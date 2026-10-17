using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    public static class LayeredCharacterTools
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/Characters/U01Visual.prefab";
        public const string LibraryPath = "Assets/_Game/Art/Characters/Monsters/Boss/U01Layers/LayeredSprites.asset";
        public const string LabScene = "Assets/_Game/Scenes/AnimationLab.unity";
        private const string Art = "Assets/_Game/Art/Characters/Monsters/Boss/";

        [MenuItem("Yesterday Convoy/Animation/Build Layered Sample")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            if (!File.Exists(PrefabPath)) CreatePrefab();
            IntegrateDemo();
            if (!File.Exists(LabScene)) CreateLab();
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == LabScene)) scenes.Add(new EditorBuildSettingsScene(LabScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            ValidateAssets();
            Debug.Log("LAYERED_SAMPLE_PASS: authored sprites, independent joints, visual prefab and animation lab.");
        }

        private static void ImportAtlas(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static void CreatePrefab()
        {
            ImportAtlas(Art + "U01Layers/front-parts.png");
            ImportAtlas(Art + "U01Layers/rear-parts.png");
            var library = AssetDatabase.LoadAssetAtPath<LayeredSpriteLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<LayeredSpriteLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            else
            {
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(LibraryPath))
                    if (sub != library) UnityEngine.Object.DestroyImmediate(sub, true);
                var values = new SerializedObject(library);
                values.FindProperty("sprites").ClearArray(); values.ApplyModifiedPropertiesWithoutUndo();
            }
            var owner = new GameObject("U01 Visual", typeof(SpriteRenderer), typeof(DemoSpriteVisual), typeof(BipedVisualRig), typeof(BipedVisualPresentation));
            try
            {
                var rig = owner.GetComponent<BipedVisualRig>();
                var author = new LayeredPuppetAuthor(owner.transform, library);
                var front = new LayeredPuppetAuthor.Atlas(Art + "U01Layers/front-parts.png");
                var rear = new LayeredPuppetAuthor.Atlas(Art + "U01Layers/rear-parts.png");
                rig.Front = author.CreateRig(Source("wisadel-april-battle.png"), false); author.Finish(rig.Front, front, false);
                rig.Back = author.CreateRig(Source("wisadel-rear.png"), true); author.Finish(rig.Back, rear, true);
                rig.Side = author.CreateTurnRig(Source("wisadel-side.png"), false); author.Finish(rig.Side, front, false);
                rig.RearQuarter = author.CreateTurnRig(Source("wisadel-rear-quarter.png"), true); author.Finish(rig.RearQuarter, rear, true);
                foreach (var view in rig.Views)
                {
                    view.Root.gameObject.SetActive(view == rig.Front);
                    for(int i=0;i<view.Parts.Count;i++) view.Parts[i].Renderer.sortingOrder=i;
                }
                var sprite = owner.GetComponent<SpriteRenderer>(); sprite.sprite = Source("wisadel-april-battle.png"); sprite.enabled = false;
                var visual = new SerializedObject(owner.GetComponent<DemoSpriteVisual>());
                visual.FindProperty("sprite").objectReferenceValue = sprite;
                visual.FindProperty("rearPose").objectReferenceValue = Source("wisadel-rear.png");
                visual.FindProperty("sidePose").objectReferenceValue = Source("wisadel-side.png");
                visual.FindProperty("rearQuarterPose").objectReferenceValue = Source("wisadel-rear-quarter.png");
                visual.FindProperty("presentation").objectReferenceValue = owner.GetComponent<BipedVisualPresentation>();
                visual.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Assets/_Game/Prefabs/Characters"); AssetDatabase.Refresh();
                PrefabUtility.SaveAsPrefabAsset(owner, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        private static Sprite Source(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);

        private static void IntegrateDemo()
        {
            var scene = EditorSceneManager.OpenScene(InitialDemoTools.ScenePath, OpenSceneMode.Single);
            InitialDemo demo = UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform previous = demo.Monster.Find("Series Sprite");
            GameObject instance;
            if (previous != null && PrefabUtility.GetCorrespondingObjectFromSource(previous.gameObject) == prefab) instance = previous.gameObject;
            else
            {
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, demo.Monster);
                instance.name = "Series Sprite";
            }
            var values = new SerializedObject(instance.GetComponent<DemoSpriteVisual>());
            values.FindProperty("movingDemo").objectReferenceValue = demo;
            values.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<DemoSpriteVisual>());
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateLab()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 11, -18); cameraObject.transform.LookAt(new Vector3(0, 0, 0));
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.055f,0.06f,0.085f);
            var light = new GameObject("Preview light", typeof(Light)).GetComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(50,-35,0);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Preview ground";
            ground.transform.position = new Vector3(0,-0.1f,0); ground.transform.localScale = new Vector3(12,0.12f,6);
            ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/DemoFloor.mat");
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var lab = new GameObject("Animation Lab", typeof(AnimationLab)).GetComponent<AnimationLab>();
            Transform actor = new GameObject("Biped sample").transform;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), actor);
            var values = new SerializedObject(lab);
            values.FindProperty("biped").objectReferenceValue = instance.GetComponent<DemoSpriteVisual>();
            values.FindProperty("interfaceFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansSC-Regular.otf");
            values.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, LabScene);
        }

        public static void ValidateAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var library = AssetDatabase.LoadAssetAtPath<LayeredSpriteLibrary>(LibraryPath);
            if (prefab == null || !prefab.GetComponent<BipedVisualRig>().IsComplete || library == null || library.Sprites.Count != 52)
                throw new InvalidOperationException("Layered sample: missing prefab parts or persisted sprites.");
            if (prefab.GetComponentsInChildren<MeshFilter>(true).Length != 0)
                throw new InvalidOperationException("Layered prefab must use authored sprite parts, not runtime cutout meshes.");
        }
    }
}
