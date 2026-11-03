using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class UnitConfigurationTests
    {
        private string fixtureFolder;

        [SetUp]
        public void SetUp()
        {
            string name = "__T01Fixtures_" + Guid.NewGuid().ToString("N");
            fixtureFolder = "Assets/" + name;
            AssetDatabase.CreateFolder("Assets", name);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(fixtureFolder))
                AssetDatabase.DeleteAsset(fixtureFolder);
        }

        [Test]
        public void SavedValidDefinitionsAreDiscoveredAndNotModifiedByValidation()
        {
            UnitDefinition unit = CreateUnit("Valid", "T01_VALID", 1.5f, 4);
            string path = AssetDatabase.GetAssetPath(unit);
            string before = File.ReadAllText(path);
            var discovered = UnitConfigurationValidator.FindProjectDefinitions();

            Assert.That(discovered, Does.Contain(unit));
            Assert.That(UnitConfigurationValidator.Validate(discovered), Is.Empty);
            Assert.That(unit.UnitId, Is.EqualTo("T01_VALID"));
            Assert.That(unit.MoveSpeed, Is.EqualTo(1.5f));
            Assert.That(unit.GateDamage, Is.EqualTo(4));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void MissingIdReportsTheSavedAsset(string id)
        {
            ExpectIssue(CreateUnit("MissingId", id), "unitId");
        }

        [TestCase(" PADDED")]
        [TestCase("PADDED ")]
        public void PaddedIdIsRejectedWithoutChangingIt(string id)
        {
            UnitDefinition unit = CreateUnit("Padded", id);
            ExpectIssue(unit, "unitId");
            Assert.That(unit.UnitId, Is.EqualTo(id));
        }

        [Test]
        public void DuplicateIdReportsBothAssetPathsDespiteDifferentDisplayNames()
        {
            UnitDefinition first = CreateUnit("First", "T01_DUPLICATE", label: "First name");
            UnitDefinition second = CreateUnit("Second", "T01_DUPLICATE", label: "Other name");
            var issues = UnitConfigurationValidator.Validate(new[] { first, second });
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].FieldName, Is.EqualTo("unitId"));
            Assert.That(issues[0].AssetPath, Is.EqualTo(AssetDatabase.GetAssetPath(second)));
            Assert.That(issues[0].Message, Does.Contain(AssetDatabase.GetAssetPath(first)));
            TestContext.WriteLine("EXPECTED_REJECTION: " + issues[0]);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidMoveSpeedIsRejected(float speed)
        {
            ExpectIssue(CreateUnit("InvalidSpeed", "T01_SPEED", speed), "moveSpeed");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveGateDamageIsRejected(int damage)
        {
            ExpectIssue(CreateUnit("InvalidDamage", "T01_DAMAGE", damage: damage), "gateDamage");
        }

        [TestCase("")]
        [TestCase("   ")]
        public void MissingDisplayNameIsRejected(string label)
        {
            ExpectIssue(CreateUnit("MissingLabel", "T01_LABEL", label: label), "displayName");
        }

        [Test]
        public void MissingAssetAndEmptyCatalogAreRejected()
        {
            var missing = UnitConfigurationValidator.Validate(new UnitDefinition[] { null });
            Assert.That(missing, Has.Count.EqualTo(1));
            Assert.That(missing[0].FieldName, Is.EqualTo("asset"));
            Assert.That(UnitConfigurationValidator.Validate(Array.Empty<UnitDefinition>()), Has.Count.EqualTo(1));
        }

        [Test]
        public void BuildHookRejectsInvalidProjectAssetWithActionableDiagnostic()
        {
            UnitDefinition invalid = CreateUnit("BuildBlocked", "T01_BUILD_BLOCKED", damage: -5);
            var error = Assert.Throws<BuildFailedException>(
                () => new UnitConfigurationBuildCheck().OnPreprocessBuild(null));
            Assert.That(error.Message, Does.Contain("UNIT_CONFIGURATION_FAIL"));
            Assert.That(error.Message, Does.Contain(AssetDatabase.GetAssetPath(invalid)));
            Assert.That(error.Message, Does.Contain("gateDamage"));
            TestContext.WriteLine("EXPECTED_BUILD_REJECTION: " + error.Message);
        }

        private UnitDefinition CreateUnit(string filename, string id, float speed = 1f, int damage = 1, string label = "Test Unit")
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinition>();
            var serialized = new SerializedObject(unit);
            serialized.FindProperty("unitId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = label;
            serialized.FindProperty("moveSpeed").floatValue = speed;
            serialized.FindProperty("gateDamage").intValue = damage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string path = fixtureFolder + "/" + filename + ".asset";
            AssetDatabase.CreateAsset(unit, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
        }

        private static void ExpectIssue(UnitDefinition unit, string field)
        {
            string path = AssetDatabase.GetAssetPath(unit);
            string before = File.ReadAllText(path);
            var issues = UnitConfigurationValidator.Validate(new[] { unit });
            Assert.That(issues.Any(issue => issue.AssetPath == path && issue.FieldName == field), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo(before), "Validation must not repair or rewrite invalid authoring data.");
            foreach (var issue in issues)
                TestContext.WriteLine("EXPECTED_REJECTION: " + issue);
        }
    }
}
