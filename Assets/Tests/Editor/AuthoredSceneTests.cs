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
        [Test]public void BothMapsExistAsSceneAssetsAndShopTerrainHasPersistentMaterialsAndMeshes()
        {
            var saved=Directory.GetFiles("Assets","*.unity",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).ToArray();
            var catalog=GameSceneCatalog.BuildPaths();CollectionAssert.AreEquivalent(saved,catalog,"Every authored map belongs to the game scene catalog.");
            var guids=catalog.Select(AssetDatabase.AssetPathToGUID).ToArray();
            Assert.That(guids.All(g=>!string.IsNullOrEmpty(g)),Is.True);Assert.That(guids.Distinct().Count(),Is.EqualTo(catalog.Length),"Copied scenes require independent GUIDs.");
            foreach(string path in catalog)Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path),Is.Not.Null,path);
            EditorSceneManager.OpenScene(ShopMapTools.ScenePath);
            var root=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(root,Is.Not.Null);
            var renderers=root.GetComponentsInChildren<Renderer>();Assert.That(renderers.Length,Is.GreaterThan(500));
            foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials){Assert.That(material,Is.Not.Null);Assert.That(AssetDatabase.Contains(material),Is.True,renderer.name);}
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){Assert.That(filter.sharedMesh,Is.Not.Null);Assert.That(AssetDatabase.Contains(filter.sharedMesh),Is.True,filter.name);}
            SceneOrganizationTools.Validate();
        }
    }
}
