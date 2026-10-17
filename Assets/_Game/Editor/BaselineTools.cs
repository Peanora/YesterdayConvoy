using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    /// <summary>Creates the entry scene once, checks its references, and builds that scene.</summary>
    public static class BaselineTools
    {
        public const string EntryScene = "Assets/_Game/Scenes/Bootstrap.unity";

        [MenuItem("Yesterday Convoy/Baseline/Initialize")]
        public static void Initialize()
        {
            if (File.Exists(InitialDemoTools.ScenePath))
            {
                InitialDemoTools.Initialize();
                return;
            }
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "YesterdayConvoy";
            PlayerSettings.productName = "昨日圆车";
            PlayerSettings.bundleVersion = "0.0.1";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            if (!File.Exists(EntryScene))
            {
                Directory.CreateDirectory("Assets/_Game/Scenes");
                AssetDatabase.Refresh();
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.16f, 0.20f);
                new GameObject("Bootstrap Status", typeof(BootstrapStatus));
                if (!EditorSceneManager.SaveScene(scene, EntryScene))
                    throw new InvalidOperationException("Could not save the entry scene.");
            }
            else
                EditorSceneManager.OpenScene(EntryScene, OpenSceneMode.Single);

            MovementPrototypeTools.EnsureInCurrentScene();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EntryScene, true) };
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("BASELINE_INITIALIZE_PASS: entry scene and version-control settings saved.");
        }

        [MenuItem("Yesterday Convoy/Baseline/Validate")]
        public static void Validate()
        {
            if (File.Exists(InitialDemoTools.ScenePath))
            {
                InitialDemoTools.Validate();
                return;
            }
            if (!File.Exists(EntryScene))
                throw new InvalidOperationException("Entry scene is missing.");
            EditorSceneManager.OpenScene(EntryScene, OpenSceneMode.Single);
            var markers = UnityEngine.Object.FindObjectsByType<BootstrapStatus>();
            if (markers.Length != 1 || Camera.main == null || !Camera.main.orthographic)
                throw new InvalidOperationException("Expected one entry marker and an orthographic main camera.");
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new InvalidOperationException("Missing scene script on " + child.name);
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length != 1 || !scenes[0].enabled || scenes[0].path != EntryScene)
                throw new InvalidOperationException("Build scene list does not match the entry scene.");
            MovementPrototypeTools.ValidateScene();
            Debug.Log("BASELINE_VALIDATE_PASS: entry scene, camera, scripts and build reference verified.");
        }

        [MenuItem("Yesterday Convoy/Baseline/Build Windows")]
        public static void BuildWindows()
        {
            if (File.Exists(DemoArtTools.CardPath)) DemoArtTools.Validate();
            else InitialDemoTools.Validate();
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = File.Exists(LayeredCharacterTools.LabScene) ? new[] { InitialDemoTools.ScenePath, LayeredCharacterTools.LabScene } : new[] { InitialDemoTools.ScenePath },
                locationPathName = "Builds/Windows/YesterdayConvoy.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("BASELINE_BUILD_PASS: " + report.summary.totalSize + " bytes; " + report.summary.totalErrors + " errors.");
        }
    }
}
