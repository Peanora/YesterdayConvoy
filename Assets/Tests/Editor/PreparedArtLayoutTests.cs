using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YesterdayConvoy.Editor;

namespace YesterdayConvoy.Tests
{
    public sealed class PreparedArtLayoutTests
    {
        [Test]public void CurrentCatalogsExportOnlyRuntimeDependenciesWithoutDecodingTextures()
        {
            int loads=ArtMemoryCache.TextureLoads;
            string[] files=PreparedArtTools.RuntimeOutputFiles();
            string catalogs=Path.Combine(Application.dataPath,"Data/ArtCatalogs");
            string[] runtimeRoots={"MonsterCombatArt","DefenderArt","TalulahOriginal","OriginiumSlugOriginal","TalulahUnboundVfx","OriginalCombatVfx"};
            string[] expected=runtimeRoots.SelectMany(root=>Directory.GetFiles(Path.Combine(catalogs,root),"manifest.json",SearchOption.AllDirectories))
                .Select(path=>path.Substring(catalogs.Length+1).Replace('\\','/')).OrderBy(path=>path,StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expected,files.Where(path=>path.EndsWith("/manifest.json",StringComparison.Ordinal)).ToArray());
            Assert.That(files.Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(files.Length));
            Assert.That(files.Where(path=>path.EndsWith(".png",StringComparison.Ordinal)).ToArray(),Is.EqualTo(new[]{"ShopTrial/originium.png"}));
            Assert.That(files.All(path=>path.EndsWith("/manifest.json",StringComparison.Ordinal)||path.EndsWith(".ycart",StringComparison.Ordinal)||path=="ShopTrial/originium.png"));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test]public void ExtraActionsAndBoundPhasesExportWhileUnboundAndRetiredCacheFilesStayOut()
        {
            string cache=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/PreparedArt"));
            string marker=Path.Combine(cache,"MonsterCombatArt","retired-layout-test-"+Guid.NewGuid().ToString("N")+".png.ycart");
            Directory.CreateDirectory(Path.GetDirectoryName(marker));File.WriteAllText(marker,"retired cached derivative");
            try
            {
                string[] files=PreparedArtTools.RuntimeOutputFiles();
                Assert.That(files.Any(path=>path.StartsWith("TalulahOriginal/ExtraClips/",StringComparison.Ordinal)&&path.EndsWith(".ycart",StringComparison.Ordinal)));
                Assert.That(files,Does.Contain("TalulahUnboundVfx/ambient-B-wing-00.png.ycart"));
                Assert.That(files.Any(path=>path.StartsWith("TalulahUnboundVfx/orb-",StringComparison.Ordinal)),Is.False);
                Assert.That(files.Any(path=>path.Contains(Path.GetFileName(marker))),Is.False);
            }
            finally{File.Delete(marker);}
        }

        [Test]public void ExportRejectsProjectAndSourceDirectoriesBeforeCleaningAnyFiles()
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            foreach(string folder in new[]{project,Application.dataPath,Path.Combine(project,"Packages"),Path.Combine(project,"ProjectSettings"),Path.GetDirectoryName(project)})
                Assert.Throws<InvalidOperationException>(()=>PreparedArtTools.ExportRuntimeArt(Path.Combine(folder,"SourceSafetyProbe.exe")),folder);
        }
    }
}
