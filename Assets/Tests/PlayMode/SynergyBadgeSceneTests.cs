using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class SynergyBadgeSceneTests
    {
        [Test]public void NineImportedMasksUploadToGpuWithTransparencyAndDifferentSilhouettes()
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(GraphicsDeviceType.Null),"This test requires the GPU PlayMode run.");
            var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(64,64,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var pixels=new Texture2D(64,64,TextureFormat.RGBA32,false,true);var shapes=new HashSet<int>();
            try
            {
                foreach(var entry in SynergyTrialCatalog.Entries)
                {
                    var icon=SynergyBadgePresentation.IconFor(entry.Tag);Assert.That(icon,Is.Not.Null,entry.Tag);
                    Graphics.Blit(icon,target);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();
                    var data=pixels.GetPixels32();Assert.That(data[0].a,Is.Zero,entry.Tag+" must not upload an opaque square background.");
                    Assert.That(data.Count(pixel=>pixel.a>180),Is.GreaterThan(8),entry.Tag+" must have visible glyph pixels.");
                    Assert.That(data.Count(pixel=>pixel.a<10),Is.GreaterThan(512),entry.Tag+" must retain transparent cutouts and margins.");
                    int shape=17;unchecked{foreach(var pixel in data)shape=shape*31+(pixel.a>90?1:0);}
                    Assert.That(shapes.Add(shape),Is.True,entry.Tag+" cannot silently use another synergy's fallback silhouette.");
                }
                var plates=Enum.GetValues(typeof(SynergyVisualGrade)).Cast<SynergyVisualGrade>().Select(SynergyBadgePresentation.FrameFor).Concat(new[]{SynergyBadgePresentation.CounterFor()});
                foreach(var plate in plates)
                {
                    Assert.That(plate,Is.Not.Null);Graphics.Blit(plate,target);RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();var data=pixels.GetPixels32();
                    Assert.That(data[0].a,Is.LessThan(32),"Plate/counter corners remain transparent; a faint shadow is allowed.");
                    Assert.That(data.Count(pixel=>pixel.a>180),Is.GreaterThan(8),"A plate must upload a real visible body.");
                }
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.Destroy(pixels);}
        }
    }
}
