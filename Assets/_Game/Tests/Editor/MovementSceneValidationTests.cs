using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class MovementSceneValidationTests
    {
        private Scene temporaryScene;

        [SetUp]
        public void SetUp()
        {
            temporaryScene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.ClosePreviewScene(temporaryScene);
        }

        [Test]
        public void MissingUnitBindingIsRejected()
        {
            CreateSceneObjects(temporaryScene, false);
            var error = Assert.Throws<InvalidOperationException>(() => MovementPrototypeTools.ValidateScene(temporaryScene));
            Assert.That(error.Message, Does.Contain("missing or mismatched"));
            TestContext.WriteLine("EXPECTED_SCENE_REJECTION: " + error.Message);
        }

        [Test]
        public void ZeroLengthAuthoredRouteIsRejectedWithFieldDiagnostic()
        {
            CreateSceneObjects(temporaryScene, true);
            var error = Assert.Throws<InvalidOperationException>(() => MovementPrototypeTools.ValidateScene(temporaryScene));
            Assert.That(error.Message, Does.Contain("waypoints/MoveSpeed"));
            Assert.That(error.Message, Does.Contain("nonzero segment"));
            TestContext.WriteLine("EXPECTED_SCENE_REJECTION: " + error.Message);
        }

        private static void CreateSceneObjects(Scene scene, bool bindUnit)
        {
            var root = new GameObject("Test Prototype", typeof(MovementPrototype), typeof(MovementSmokeCheck));
            SceneManager.MoveGameObjectToScene(root, scene);
            var unitObject = new GameObject("Test Unit", typeof(PrototypeUnit));
            unitObject.transform.SetParent(root.transform);
            var lineObject = new GameObject("Test Route", typeof(LineRenderer));
            lineObject.transform.SetParent(root.transform);
            var unit = unitObject.GetComponent<PrototypeUnit>();
            var unitData = new SerializedObject(unit);
            unitData.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnitDefinition>(UnitConfigurationTools.SampleFolder + "/U01.asset");
            unitData.ApplyModifiedPropertiesWithoutUndo();
            var data = new SerializedObject(root.GetComponent<MovementPrototype>());
            data.FindProperty("unit").objectReferenceValue = bindUnit ? unit : null;
            data.FindProperty("pathLine").objectReferenceValue = lineObject.GetComponent<LineRenderer>();
            var points = data.FindProperty("waypoints");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).vector3Value = Vector3.zero;
            points.GetArrayElementAtIndex(1).vector3Value = Vector3.zero;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
