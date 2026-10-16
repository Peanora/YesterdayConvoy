using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy.Editor
{
    public static class MovementPrototypeTools
    {
        private static readonly Vector3[] InitialPoints =
        {
            new Vector3(-5, -1.5f, 0), new Vector3(-2, -1.5f, 0),
            new Vector3(-2, 1, 0), new Vector3(2.5f, 1, 0),
            new Vector3(2.5f, -1.5f, 0), new Vector3(5, -1.5f, 0)
        };

        [MenuItem("Yesterday Convoy/Movement/Initialize Demo")]
        public static void Initialize()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(BaselineTools.EntryScene, OpenSceneMode.Single);
            EnsureInCurrentScene();
            ValidateScene();
        }

        public static void EnsureInCurrentScene()
        {
            if (SceneManager.GetActiveScene().path != BaselineTools.EntryScene)
                throw new InvalidOperationException("Open the Bootstrap entry scene before creating the movement demo.");
            if (UnityEngine.Object.FindObjectsByType<MovementPrototype>(FindObjectsInactive.Include).Length > 0)
            {
                ValidateScene();
                Debug.Log("MOVEMENT_SCENE_SKIP: existing prototype and authored values preserved.");
                return;
            }
            if (GameObject.Find("Movement Prototype") != null)
                throw new InvalidOperationException("Movement Prototype object exists without its expected component; refusing overwrite.");
            UnitDefinition definition = AssetDatabase.LoadAssetAtPath<UnitDefinition>(UnitConfigurationTools.SampleFolder + "/U01.asset");
            if (definition == null)
                throw new InvalidOperationException("Missing U01.asset; initialize T01 prototype units first.");

            if (!AssetDatabase.IsValidFolder("Assets/_Game/Materials"))
                AssetDatabase.CreateFolder("Assets/_Game", "Materials");
            Material pathMaterial = MaterialIfMissing("Route", new Color(0.55f, 0.66f, 0.72f));
            Material unitMaterial = MaterialIfMissing("Unit", new Color(1f, 0.69f, 0.22f));
            Material startMaterial = MaterialIfMissing("Start", new Color(0.25f, 0.72f, 0.49f));
            Material endMaterial = MaterialIfMissing("Endpoint", new Color(0.25f, 0.62f, 1f));

            var root = new GameObject("Movement Prototype", typeof(MovementPrototype), typeof(MovementSmokeCheck));
            var lineObject = new GameObject("Fixed Route", typeof(LineRenderer));
            lineObject.transform.SetParent(root.transform, false);
            LineRenderer line = lineObject.GetComponent<LineRenderer>();
            line.sharedMaterial = pathMaterial;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.12f;
            line.numCornerVertices = 6;
            line.numCapVertices = 6;
            line.positionCount = InitialPoints.Length;
            line.SetPositions(InitialPoints);

            Quad("Start Marker", InitialPoints[0] + Vector3.forward * 0.2f, 0.8f, startMaterial, root.transform);
            Quad("Blue Endpoint Marker", InitialPoints[InitialPoints.Length - 1] + Vector3.forward * 0.2f, 0.8f, endMaterial, root.transform);
            GameObject unitObject = Quad("Prototype U01", InitialPoints[0], 0.42f, unitMaterial, root.transform);
            PrototypeUnit unit = unitObject.AddComponent<PrototypeUnit>();
            var serializedUnit = new SerializedObject(unit);
            serializedUnit.FindProperty("definition").objectReferenceValue = definition;
            serializedUnit.ApplyModifiedPropertiesWithoutUndo();

            var serializedPrototype = new SerializedObject(root.GetComponent<MovementPrototype>());
            SerializedProperty points = serializedPrototype.FindProperty("waypoints");
            points.arraySize = InitialPoints.Length;
            for (int index = 0; index < InitialPoints.Length; index++)
                points.GetArrayElementAtIndex(index).vector3Value = InitialPoints[index];
            serializedPrototype.FindProperty("unit").objectReferenceValue = unit;
            serializedPrototype.FindProperty("pathLine").objectReferenceValue = line;
            serializedPrototype.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), BaselineTools.EntryScene))
                throw new InvalidOperationException("Could not save movement demo scene.");
            Debug.Log("MOVEMENT_SCENE_CREATE: one U01, six waypoints, 15 world units.");
        }

        public static void ValidateScene()
        {
            ValidateScene(SceneManager.GetActiveScene());
        }

        public static void ValidateScene(Scene scene)
        {
            var prototypes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MovementPrototype>(true)).ToArray();
            var units = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PrototypeUnit>(true)).ToArray();
            if (prototypes.Length != 1 || units.Length != 1)
                throw new InvalidOperationException("Bootstrap: expected one MovementPrototype and one PrototypeUnit.");
            MovementPrototype prototype = prototypes[0];
            if (prototype.Unit != units[0] || prototype.Unit.Definition == null || prototype.PathLine == null)
                throw new InvalidOperationException("Bootstrap/Movement Prototype: missing or mismatched unit/configuration/path renderer reference.");
            if (!prototype.isActiveAndEnabled || !prototype.Unit.gameObject.activeInHierarchy ||
                !prototype.PathLine.enabled || !prototype.PathLine.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Bootstrap/Movement Prototype: demo objects must be active and enabled.");
            if (!prototype.Unit.transform.IsChildOf(prototype.transform) || prototype.GetComponent<MovementSmokeCheck>() == null)
                throw new InvalidOperationException("Bootstrap/Movement Prototype: unit ownership or smoke-check binding is missing.");
            try
            {
                new RouteMotion(prototype.CopyWorldWaypoints(), prototype.Unit.Definition.MoveSpeed);
            }
            catch (ArgumentException error)
            {
                throw new InvalidOperationException($"{scene.name}/Movement Prototype [waypoints/MoveSpeed]: {error.Message}", error);
            }
            var unitRenderer = prototype.Unit.GetComponent<Renderer>();
            if (unitRenderer == null || unitRenderer.sharedMaterial == null || prototype.PathLine.sharedMaterial == null)
                throw new InvalidOperationException("Bootstrap/Movement Prototype: missing visual material.");
            Debug.Log("MOVEMENT_SCENE_PASS: one configured unit, valid route and visual references.");
        }

        private static Material MaterialIfMissing(string name, Color color)
        {
            string path = "Assets/_Game/Materials/Prototype" + name + ".mat";
            if (File.Exists(path))
            {
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null)
                    throw new InvalidOperationException("Not a material: " + path);
                return existing;
            }
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                throw new InvalidOperationException("Built-in Sprites/Default shader is unavailable.");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Quad(string name, Vector3 position, float size, Material material, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.position = position;
            quad.transform.localScale = new Vector3(size, size, 1);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad;
        }
    }
}
