using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesterdayConvoy.Editor;
namespace YesterdayConvoy.Tests
{
    public sealed class AuthoredSceneTests
    {
        [Test]public void TheSingleBuildSceneRetainsItsEditableTerrainAndPersistentReferences()
        {
            var expected=new[]{FormationBattleTools.ScenePath};
            var saved=Directory.GetFiles("Assets/Scenes","*.unity",SearchOption.AllDirectories)
                .Select(path=>path.Replace('\\','/'));
            CollectionAssert.AreEqual(expected,saved);
            CollectionAssert.AreEqual(expected,GameSceneCatalog.BuildPaths());
            CollectionAssert.AreEqual(expected,EditorBuildSettings.scenes.Select(scene=>scene.path));
            Assert.That(EditorBuildSettings.scenes.Single().enabled,Is.True);

            var scene=EditorSceneManager.OpenScene(FormationBattleTools.ScenePath);
            var roots=scene.GetRootGameObjects();
            var controller=roots.SelectMany(root=>root.GetComponentsInChildren<FormationBattleController>(true)).Single();
            Assert.That(controller.battleCamera,Is.Not.Null);
            Assert.That(controller.terrain,Is.Not.Null);
            Assert.That(roots.SelectMany(root=>root.GetComponentsInChildren<ManualBattleController>(true)),Is.Empty);
            var transforms=roots.SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).ToArray();
            Assert.That(transforms.Count(tile=>tile.name.StartsWith("Tile ",StringComparison.Ordinal)),
                Is.EqualTo(FormationBattleModel.Width*FormationBattleModel.Height));
            foreach(var transform in transforms)
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),Is.Zero,transform.name);
            foreach(var filter in roots.SelectMany(root=>root.GetComponentsInChildren<MeshFilter>(true)))
            {
                Assert.That(filter.sharedMesh,Is.Not.Null,filter.name);
                Assert.That(AssetDatabase.GetAssetPath(filter.sharedMesh),Is.Not.Empty,filter.name);
            }
            foreach(var renderer in roots.SelectMany(root=>root.GetComponentsInChildren<MeshRenderer>(true)))
                foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material,Is.Not.Null,renderer.name);
                    Assert.That(AssetDatabase.Contains(material),Is.True,renderer.name);
                }
        }

        [Test]public void EditedBoardTilePositionSurvivesSavingAndReopeningACopy()
        {
            string copy=AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/FormationEditRoundtrip.unity");
            Assert.That(AssetDatabase.CopyAsset(FormationBattleTools.ScenePath,copy),Is.True);
            try
            {
                const string tileName="Tile 05,06";
                var scene=EditorSceneManager.OpenScene(copy);
                var tile=GameObject.Find(tileName);
                Assert.That(tile,Is.Not.Null);
                Vector3 position=tile.transform.position+Vector3.up*.15f;
                tile.transform.position=position;
                EditorSceneManager.MarkSceneDirty(scene);
                Assert.That(EditorSceneManager.SaveScene(scene),Is.True);

                EditorSceneManager.OpenScene(FormationBattleTools.ScenePath);
                Assert.That(GameObject.Find(tileName).transform.position,Is.Not.EqualTo(position));
                EditorSceneManager.OpenScene(copy);
                Assert.That(GameObject.Find(tileName).transform.position,Is.EqualTo(position));
            }
            finally
            {
                EditorSceneManager.OpenScene(FormationBattleTools.ScenePath);
                AssetDatabase.DeleteAsset(copy);
            }
        }
    }
}
