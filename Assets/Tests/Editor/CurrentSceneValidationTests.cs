using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class CurrentSceneValidationTests
    {
        [Test]
        public void AuthoredSceneUsesCurrentArtAndNoRetiredPreviewObjects()
        {
            EditorSceneManager.OpenScene(InitialDemoTools.ScenePath,OpenSceneMode.Single);
            DemoArtTools.Validate();
            var source=Object.FindAnyObjectByType<InitialDemo>();
            var c=Object.FindAnyObjectByType<ManualBattleController>();
            Assert.That(source.Monster.gameObject.activeSelf,Is.False);
            var template=source.Monster.GetComponentInChildren<DemoSpriteVisual>(true);
            Assert.That(template.Renderer.sprite,Is.SameAs(source.Card.BattleSprite));
            Assert.That(c.CombatView,Is.Not.Null);
            var defender=c.CombatView.DefenderVisual;
            Assert.That(defender.enabled,Is.True);
            Assert.That(defender.sprite,Is.SameAs(c.Definition.Engagement.KroosSprite));
            Assert.That(defender.transform.position,Is.EqualTo(c.Definition.Engagement.DefenderPosition));
            foreach(string name in new[]{"Defense Tower","Tower Range","Tower Shot","Raised Tactical Board"})
                Assert.That(GameObject.Find(name),Is.Null,name);
            var catalog=GameSceneCatalog.BuildPaths();CollectionAssert.AreEqual(catalog,EditorBuildSettings.scenes.Select(s=>s.path),"The build must retain every authored scene in catalog order.");
            Assert.That(EditorBuildSettings.scenes.All(s=>s.enabled),Is.True);
            Assert.That(EditorBuildSettings.scenes[0].path,Is.EqualTo(CampaignTools.CampaignScene));
        }

        [Test]
        public void RetiredSpriteAndBootstrapFilesAreAbsent()
        {
            foreach(string path in new[]{"Assets/Scenes/Bootstrap.unity",
                "Assets/Scripts/Runtime/DemoWalkPuppet.cs",
                "Assets/Art/Characters/Defenders/grumpy-cannon.png",
                "Assets/Art/Characters/Defenders/kroos-battle.png",
                "Assets/Art/Characters/Monsters/Boss/wisadel-april-battle.png"})
            {
                Assert.That(File.Exists(path),Is.False,path);
                Assert.That(File.Exists(path+".meta"),Is.False,path+".meta");
            }
        }
    }
}
