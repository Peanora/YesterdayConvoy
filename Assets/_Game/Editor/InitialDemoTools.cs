using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class InitialDemoTools
    {
        public const string ScenePath = "Assets/_Game/Scenes/InitialDemo.unity";
        private static readonly Vector3[] Route =
        {
            new Vector3(-5, 0.04f, 1), new Vector3(-5, 0.04f, -1),
            new Vector3(-2, 0.04f, -1), new Vector3(-2, 0.04f, 0),
            new Vector3(2, 0.04f, 0), new Vector3(2, 0.04f, -1),
            new Vector3(5, 0.04f, -1), new Vector3(5, 0.04f, 1)
        };

        [MenuItem("Yesterday Convoy/Initial Demo/Open or Create")]
        public static void Initialize()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
                CreateScene();
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(BaselineTools.EntryScene, true)
            };
            AssetDatabase.SaveAssets();
            Validate();
        }

        public static void Validate()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var demos = UnityEngine.Object.FindObjectsByType<InitialDemo>();
            if (demos.Length != 1 || Camera.main == null || !Camera.main.orthographic ||
                Vector3.Dot(Camera.main.transform.forward, Vector3.down) < 0.3f)
                throw new InvalidOperationException("InitialDemo: expected one demo and an oblique orthographic camera.");
            var data = new SerializedObject(demos[0]);
            foreach (string field in new[] { "definition", "monster", "tower", "muzzle", "redGate", "blueGate", "shotBeam" })
                if (data.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException("InitialDemo: missing " + field);
            Debug.Log("INITIAL_DEMO_SCENE_PASS: monster, tower, red/blue gates and oblique camera.");
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(6, 14, -21);
            cameraObject.transform.LookAt(new Vector3(0, 0, 0.5f));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.14f, 0.17f);
            var lightObject = new GameObject("Key Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(50, -35, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.61f, 0.67f, 0.73f);

            Material baseMaterial = Material("Board", new Color(0.16f, 0.21f, 0.25f));
            Material floor = Material("Floor", new Color(0.37f, 0.44f, 0.49f));
            Material path = Material("Path", new Color(0.69f, 0.75f, 0.77f));
            Material towerMaterial = Material("Tower", new Color(0.20f, 0.27f, 0.31f));
            Material metal = Material("Metal", new Color(0.50f, 0.58f, 0.62f));
            Material red = Material("Red", new Color(0.88f, 0.18f, 0.19f));
            Material blue = Material("Blue", new Color(0.12f, 0.57f, 0.94f));
            Material amber = Material("Monster", new Color(0.92f, 0.49f, 0.17f));
            Material eyes = Material("Eyes", new Color(1f, 0.94f, 0.71f), true);
            Material beamMaterial = Material("Beam", new Color(1f, 0.76f, 0.27f), true);
            Material rangeMaterial = Material("Range", new Color(0.34f, 0.75f, 0.85f, 0.4f), true);

            var root = new GameObject("Initial Demo", typeof(InitialDemo));
            var board = new GameObject("Raised Tactical Board");
            Shape("Board Base", PrimitiveType.Cube, board.transform, new Vector3(0, -0.27f, 0), new Vector3(11.4f, 0.36f, 5.4f), baseMaterial);
            for (int x = -5; x <= 5; x++)
                for (int z = -2; z <= 2; z++)
                    Shape($"Tile {x},{z}", PrimitiveType.Cube, board.transform, new Vector3(x, -0.08f, z), new Vector3(0.95f, 0.16f, 0.95f), IsOnRoute(x, z) ? path : floor);

            Transform redGate = Gate("Red Gate", new Vector3(-5, 0, 1), red, baseMaterial);
            Transform blueGate = Gate("Blue Gate", new Vector3(5, 0, 1), blue, baseMaterial);
            var tower = new GameObject("Defense Tower").transform;
            tower.position = new Vector3(0, 0, 1);
            Shape("Base", PrimitiveType.Cylinder, tower, new Vector3(0, 0.13f, 0), new Vector3(0.85f, 0.13f, 0.85f), metal);
            Shape("Column", PrimitiveType.Cube, tower, new Vector3(0, 0.65f, 0), new Vector3(0.50f, 0.9f, 0.50f), towerMaterial);
            var turret = new GameObject("Turret").transform;
            turret.SetParent(tower, false);
            turret.localPosition = new Vector3(0, 1.22f, 0);
            Shape("Turret Head", PrimitiveType.Cube, turret, Vector3.zero, new Vector3(0.8f, 0.38f, 0.65f), metal);
            Shape("Barrel", PrimitiveType.Cube, turret, new Vector3(0, 0, 0.45f), new Vector3(0.18f, 0.18f, 0.8f), towerMaterial);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(turret, false);
            muzzle.localPosition = new Vector3(0, 0, 0.85f);
            var range = Line("Tower Range", rangeMaterial, 0.025f);
            range.loop = true;
            range.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                range.SetPosition(i, tower.position + new Vector3(Mathf.Cos(angle) * 2.8f, 0.03f, Mathf.Sin(angle) * 2.8f));
            }

            var monster = new GameObject("Monster U01").transform;
            monster.position = Route[0];
            Shape("Body", PrimitiveType.Capsule, monster, new Vector3(0, 0.43f, 0), new Vector3(0.50f, 0.38f, 0.50f), amber);
            Shape("Head", PrimitiveType.Sphere, monster, new Vector3(0, 0.86f, -0.015f), new Vector3(0.46f, 0.40f, 0.45f), amber);
            Shape("Left Foot", PrimitiveType.Cube, monster, new Vector3(-0.16f, 0.09f, -0.04f), new Vector3(0.20f, 0.18f, 0.3f), towerMaterial);
            Shape("Right Foot", PrimitiveType.Cube, monster, new Vector3(0.16f, 0.09f, -0.04f), new Vector3(0.20f, 0.18f, 0.3f), towerMaterial);
            Shape("Left Eye", PrimitiveType.Sphere, monster, new Vector3(-0.10f, 0.88f, -0.22f), new Vector3(0.085f, 0.10f, 0.05f), eyes);
            Shape("Right Eye", PrimitiveType.Sphere, monster, new Vector3(0.10f, 0.88f, -0.22f), new Vector3(0.085f, 0.10f, 0.05f), eyes);
            Shape("Left Horn", PrimitiveType.Cube, monster, new Vector3(-0.15f, 1.13f, 0.02f), new Vector3(0.10f, 0.26f, 0.10f), towerMaterial);
            Shape("Right Horn", PrimitiveType.Cube, monster, new Vector3(0.15f, 1.13f, 0.02f), new Vector3(0.10f, 0.26f, 0.10f), towerMaterial);

            LineRenderer beam = Line("Tower Shot", beamMaterial, 0.065f);
            beam.positionCount = 2;
            beam.enabled = false;
            var data = new SerializedObject(root.GetComponent<InitialDemo>());
            data.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnitDefinition>(UnitConfigurationTools.SampleFolder + "/U01.asset");
            data.FindProperty("monster").objectReferenceValue = monster;
            data.FindProperty("tower").objectReferenceValue = tower;
            data.FindProperty("muzzle").objectReferenceValue = muzzle;
            data.FindProperty("redGate").objectReferenceValue = redGate;
            data.FindProperty("blueGate").objectReferenceValue = blueGate;
            data.FindProperty("shotBeam").objectReferenceValue = beam;
            var points = data.FindProperty("waypoints");
            points.arraySize = Route.Length;
            for (int i = 0; i < Route.Length; i++) points.GetArrayElementAtIndex(i).vector3Value = Route[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save InitialDemo scene.");
        }

        private static bool IsOnRoute(int x, int z)
        {
            for (int i = 1; i < Route.Length; i++)
            {
                Vector3 a = Route[i - 1], b = Route[i];
                if (a.x == b.x && x == a.x && z >= Mathf.Min(a.z, b.z) && z <= Mathf.Max(a.z, b.z)) return true;
                if (a.z == b.z && z == a.z && x >= Mathf.Min(a.x, b.x) && x <= Mathf.Max(a.x, b.x)) return true;
            }
            return false;
        }

        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = "Assets/_Game/Materials/Demo" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find(unlit ? "Sprites/Default" : "Standard")) { color = color };
            if (!unlit) material.SetFloat("_Glossiness", 0.2f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform Gate(string name, Vector3 position, Material frame, Material dark)
        {
            var gate = new GameObject(name).transform;
            gate.position = position;
            Shape("Threshold", PrimitiveType.Cube, gate, new Vector3(0, 0.035f, 0), new Vector3(1.05f, 0.07f, 0.65f), dark);
            Shape("Left Pillar", PrimitiveType.Cube, gate, new Vector3(-0.46f, 0.8f, 0), new Vector3(0.16f, 1.6f, 0.22f), frame);
            Shape("Right Pillar", PrimitiveType.Cube, gate, new Vector3(0.46f, 0.8f, 0), new Vector3(0.16f, 1.6f, 0.22f), frame);
            Shape("Lintel", PrimitiveType.Cube, gate, new Vector3(0, 1.58f, 0), new Vector3(1.08f, 0.18f, 0.22f), frame);
            return gate;
        }

        private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            return shape;
        }

        private static LineRenderer Line(string name, Material material, float width)
        {
            var line = new GameObject(name, typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            return line;
        }
    }
}
