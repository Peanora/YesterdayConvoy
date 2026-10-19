using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class UnitConfigurationTools
    {
        public const string SampleFolder = "Assets/_Game/Data/Units";

        [MenuItem("Yesterday Convoy/Configuration/Ensure Current Unit")]
        public static void CreatePrototypeUnits()
        {
            EnsureFolder("Assets/_Game/Data");
            EnsureFolder(SampleFolder);
            CreateIfMissing("U01", "维维美", 0.75f, 1);
            AssetDatabase.SaveAssets();
            ValidateProject();
        }

        [MenuItem("Yesterday Convoy/Configuration/Validate Units")]
        public static void ValidateProject()
        {
            UnitDefinition[] definitions = UnitConfigurationValidator.FindProjectDefinitions();
            var issues = UnitConfigurationValidator.Validate(definitions);
            if (issues.Count != 0)
                throw new BuildFailedException("UNIT_CONFIGURATION_FAIL: " + issues.Count + " issue(s)\n" +
                                               string.Join("\n", issues.Select(issue => issue.ToString())));
            Debug.Log($"UNIT_CONFIGURATION_PASS: {definitions.Length} unit definitions; no errors.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void CreateIfMissing(string id, string label, float speed, int damage)
        {
            string path = $"{SampleFolder}/{id}.asset";
            if (File.Exists(path))
            {
                if (AssetDatabase.LoadAssetAtPath<UnitDefinition>(path) == null)
                    throw new InvalidOperationException("Existing asset is not a UnitDefinition: " + path);
                Debug.Log("UNIT_SAMPLE_SKIP: " + path);
                return;
            }

            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("unitId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = label;
            serialized.FindProperty("moveSpeed").floatValue = speed;
            serialized.FindProperty("gateDamage").intValue = damage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, path);
            Debug.Log("UNIT_SAMPLE_CREATE: " + path);
        }
    }
}
