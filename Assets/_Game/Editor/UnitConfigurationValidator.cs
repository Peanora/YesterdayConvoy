using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace YesterdayConvoy.Editor
{
    public sealed class UnitConfigurationIssue
    {
        public UnitDefinition Asset { get; }
        public string AssetPath { get; }
        public string FieldName { get; }
        public string Message { get; }

        public UnitConfigurationIssue(UnitDefinition asset, string assetPath, string fieldName, string message)
        {
            Asset = asset;
            AssetPath = assetPath;
            FieldName = fieldName;
            Message = message;
        }

        public override string ToString() => $"{AssetPath} [{FieldName}]: {Message}";
    }

    /// <summary>Read-only validation shared by the editor menu, build gate and Edit Mode tests.</summary>
    public static class UnitConfigurationValidator
    {
        public static UnitDefinition[] FindProjectDefinitions()
        {
            return AssetDatabase.FindAssets("t:UnitDefinition", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<UnitDefinition>)
                .ToArray();
        }

        public static List<UnitConfigurationIssue> Validate(IReadOnlyList<UnitDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            var issues = new List<UnitConfigurationIssue>();
            var firstPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            if (definitions.Count == 0)
                issues.Add(new UnitConfigurationIssue(null, "Assets", "definitions", "No UnitDefinition assets found."));

            for (int index = 0; index < definitions.Count; index++)
            {
                UnitDefinition definition = definitions[index];
                if (definition == null)
                {
                    issues.Add(new UnitConfigurationIssue(null, $"definitions[{index}]", "asset", "Missing unit asset."));
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(definition);
                if (string.IsNullOrEmpty(path))
                    path = $"<unsaved:{definition.name}>";

                void Add(string field, string message) => issues.Add(new UnitConfigurationIssue(definition, path, field, message));

                string id = definition.UnitId;
                if (string.IsNullOrWhiteSpace(id))
                    Add("unitId", "Stable ID is required.");
                else if (id != id.Trim())
                    Add("unitId", "Stable ID must not have leading or trailing whitespace.");
                else if (firstPaths.TryGetValue(id, out string firstPath))
                    Add("unitId", $"Duplicate ID '{id}'; first declared at {firstPath}.");
                else
                    firstPaths.Add(id, path);

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                    Add("displayName", "Display name is required.");
                float speed = definition.MoveSpeed;
                if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f)
                    Add("moveSpeed", "Move speed must be finite and greater than zero.");
                if (definition.GateDamage <= 0)
                    Add("gateDamage", "Gate damage must be an integer greater than zero.");
            }

            return issues;
        }
    }
}
