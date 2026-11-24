using System.IO;
using NUnit.Framework;

namespace YesterdayConvoy.Tests
{
    public sealed class CurrentSceneValidationTests
    {
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
