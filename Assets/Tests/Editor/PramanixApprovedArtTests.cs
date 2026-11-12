using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class PramanixApprovedArtTests
    {
        [Serializable] sealed class Manifest{public Clip[] clips;}
        [Serializable] sealed class Clip{public string key;public float duration;public int count;public Page[] pages;}
        [Serializable] sealed class Page{public string file,rawResource;public int width,height;}
        [Serializable] sealed class Provenance{public Source[] imports;}
        [Serializable] sealed class Source{public string rawResource,rgbaSha256;}
        [Test] public void ApprovedPramanixHdrPagesAreTypedLosslessLinearSourcePayloads()
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtSourcePaths.Resolve("OriginalCombatVfx/Defenders/manifest.json")));
            var source=JsonUtility.FromJson<Provenance>(File.ReadAllText(ArtSourcePaths.Resolve("OriginalCombatVfx/Defenders/D27-approved-v5-sources.json")));
            var clips=manifest.clips.Where(c=>c.key.StartsWith("defender/D27/",StringComparison.Ordinal)).ToArray();
            Assert.That(clips.Length,Is.EqualTo(7));Assert.That(clips.Sum(c=>c.pages.Length),Is.EqualTo(9));
            Assert.That(clips.Single(c=>c.key.EndsWith("/sbell2_skill_03_start_01",StringComparison.Ordinal)).duration,Is.EqualTo(49f/30).Within(.00001));
            Assert.That(clips.Single(c=>c.key.EndsWith("/sbell2_skill_03_hit",StringComparison.Ordinal)).duration,Is.EqualTo(1.5f));
            foreach(var page in clips.SelectMany(c=>c.pages))
            {
                var asset=Resources.Load<TextAsset>(page.rawResource);Assert.That(asset,Is.Not.Null,page.rawResource);
                try
                {
                    using(var stream=new MemoryStream(asset.bytes))using(var gzip=new GZipStream(stream,CompressionMode.Decompress))using(var output=new MemoryStream())
                    {
                        gzip.CopyTo(output);byte[] bytes=output.ToArray();Assert.That(bytes.Length,Is.EqualTo(page.width*page.height*4));
                        using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),
                            Is.EqualTo(source.imports.Single(s=>s.rawResource==page.rawResource).rgbaSha256),page.rawResource+" must match the user-approved capture byte for byte.");
                    }
                }
                finally{Resources.UnloadAsset(asset);}
            }
            Assert.That(Resources.Load<Shader>("PramanixNative"),Is.Not.Null);Assert.That(Resources.Load<Shader>("ActorProjectedShadow"),Is.Not.Null);
        }
    }
}
