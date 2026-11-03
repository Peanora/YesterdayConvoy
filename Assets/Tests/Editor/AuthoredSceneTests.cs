using System.IO;
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
            Assert.That(Directory.GetFiles("Assets","*.unity",SearchOption.AllDirectories).Length,Is.EqualTo(2));
            EditorSceneManager.OpenScene(ShopMapTools.ScenePath);
            var root=Object.FindAnyObjectByType<AuthoredBattlefield>();Assert.That(root,Is.Not.Null);
            var renderers=root.GetComponentsInChildren<Renderer>();Assert.That(renderers.Length,Is.GreaterThan(500));
            foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials){Assert.That(material,Is.Not.Null);Assert.That(AssetDatabase.Contains(material),Is.True,renderer.name);}
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){Assert.That(filter.sharedMesh,Is.Not.Null);Assert.That(AssetDatabase.Contains(filter.sharedMesh),Is.True,filter.name);}
            SceneOrganizationTools.Validate();
        }
    }
}
