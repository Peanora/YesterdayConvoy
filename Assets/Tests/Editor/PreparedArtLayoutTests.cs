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
        [Serializable] sealed class Manifest {public Clip[] clips;}
        [Serializable] sealed class Clip {public Page[] pages;}
        [Serializable] sealed class Page {public string file;}

        [Test]public void CurrentCatalogsExportOnlyRuntimeDependenciesWithoutDecodingTextures()
        {
            int loads=ArtMemoryCache.TextureLoads;
            string[] files=PreparedArtTools.RuntimeOutputFiles();
            string catalogs=Path.Combine(Application.dataPath,"Data/ArtCatalogs");
            // Derive the expected package inventory from the authored catalogs, independently
            // of the exporter's private root list. New runtime roots must not silently disappear.
            string[] runtimeRoots=Directory.GetDirectories(catalogs)
                .Where(folder=>Directory.GetFiles(folder,"manifest.json",SearchOption.AllDirectories).Length>0)
                .Select(Path.GetFileName).ToArray();
            foreach(string root in runtimeRoots)
                Assert.That(Directory.Exists(ArtSourcePaths.ImageDirectory(root)),Is.True,"Unsupported classified image root: "+root);
            string[] expected=runtimeRoots.SelectMany(root=>Directory.GetFiles(Path.Combine(catalogs,root),"manifest.json",SearchOption.AllDirectories))
                .Select(path=>path.Substring(catalogs.Length+1).Replace('\\','/')).OrderBy(path=>path,StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expected,files.Where(path=>path.EndsWith("/manifest.json",StringComparison.Ordinal)).ToArray());
            Assert.That(files.Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(files.Length));
            Assert.That(files.Where(path=>path.EndsWith(".png",StringComparison.Ordinal)).ToArray(),Is.EqualTo(new[]{"ShopTrial/originium.png"}));
            Assert.That(files.All(path=>path.EndsWith("/manifest.json",StringComparison.Ordinal)||path.EndsWith(".ycart",StringComparison.Ordinal)||path=="ShopTrial/originium.png"));
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test]public void CurrentTalulahBodyAndBoundPhasesExportWithoutStaleCacheFiles()
        {
            string cache=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/PreparedArt"));
            string marker=Path.Combine(cache,"MonsterCombatArt","retired-layout-test-"+Guid.NewGuid().ToString("N")+".png.ycart");
            Directory.CreateDirectory(Path.GetDirectoryName(marker));File.WriteAllText(marker,"retired cached derivative");
            try
            {
                string[] files=PreparedArtTools.RuntimeOutputFiles();
                Assert.That(files,Does.Contain("BossRoster/B16/main/manifest.json"));
                Assert.That(files.Any(path=>path.StartsWith("BossRoster/B16/main/",StringComparison.Ordinal)&&path.EndsWith(".ycart",StringComparison.Ordinal)));
                Assert.That(files.Any(path=>path.StartsWith("TalulahOriginal/",StringComparison.Ordinal)),Is.False);
                Assert.That(files,Does.Contain("TalulahUnboundVfx/ambient-B-wing-00.png.ycart"));
                foreach(string phase in new[]{"ignite-A-caster","ignite-A-target","ignite-B-caster","ignite-B-target"})
                    Assert.That(files,Does.Contain("TalulahUnboundVfx/"+phase+"-00.png.ycart"),"Actually bound formation Talulah HDR page");
                Assert.That(files.Any(path=>path.StartsWith("TalulahUnboundVfx/orb-",StringComparison.Ordinal)),Is.False);
                Assert.That(files.Any(path=>path.Contains(Path.GetFileName(marker))),Is.False);
            }
            finally{File.Delete(marker);}
        }

        [Test]public void EveryDeclaredBossBodyAndHdrPageIsExportedWithoutTextureDecoding()
        {
            int loads=ArtMemoryCache.TextureLoads;
            var files=new System.Collections.Generic.HashSet<string>(PreparedArtTools.RuntimeOutputFiles(),StringComparer.Ordinal);
            string catalogs=Path.Combine(Application.dataPath,"Data/ArtCatalogs");
            foreach(string logicalRoot in new[]{"BossRoster","BossRosterFx"})
            {
                var manifests=Directory.GetFiles(Path.Combine(catalogs,logicalRoot),"manifest.json",SearchOption.AllDirectories);
                Assert.That(manifests.Length,Is.GreaterThan(0),logicalRoot);
                foreach(string manifest in manifests)
                {
                    string root=Path.GetDirectoryName(manifest).Substring(catalogs.Length+1).Replace('\\','/');
                    var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest));
                    Assert.That(data?.clips,Is.Not.Empty,manifest);
                    foreach(var clip in data.clips)
                    {
                        Assert.That(clip.pages,Is.Not.Empty,manifest);
                        foreach(var page in clip.pages)
                        {
                            string relative=root+"/"+page.file;
                            Assert.That(File.Exists(ArtSourcePaths.Resolve(relative)),Is.True,relative);
                            Assert.That(files.Contains(relative+".ycart"),Is.True,"Declared body/HDR source was omitted: "+relative);
                        }
                    }
                }
            }
            Assert.That(ArtMemoryCache.TextureLoads,Is.EqualTo(loads));
        }

        [Test]public void ExportRejectsProjectAndSourceDirectoriesBeforeCleaningAnyFiles()
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            foreach(string folder in new[]{project,Application.dataPath,Path.Combine(project,"Packages"),Path.Combine(project,"ProjectSettings"),Path.GetDirectoryName(project)})
                Assert.Throws<InvalidOperationException>(()=>PreparedArtTools.ExportRuntimeArt(Path.Combine(folder,"SourceSafetyProbe.exe")),folder);
        }
    }
}
