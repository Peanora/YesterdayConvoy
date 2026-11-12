using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace YesterdayConvoy.Tests
{
    public sealed class SixStarExactRenderingSceneTests
    {
        [Serializable] sealed class Manifest {public Entry[] clips;}
        [Serializable] sealed class Entry {public string key;public float loopStart;}
        [UnityTest]public IEnumerator ExactShaderDisplaysLinearHdrAndKeepsAlphaZeroRgb()
        {
            Assume.That(QualitySettings.activeColorSpace,Is.EqualTo(ColorSpace.Gamma));
            var cameraObject=new GameObject("Six-star calibration camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.transform.position=new Vector3(0,3,-3);camera.transform.LookAt(Vector3.zero);
            var texture=new Texture2D(1,1,TextureFormat.RGBA32,false,true);texture.SetPixel(0,0,new Color32(32,48,64,0));texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            var material=new Material(Resources.Load<Shader>("SixStarNative"));material.SetFloat("_HdrRange",16);material.SetVector("_PlaneAnchor",new Vector4(0,.012f,0,0));
            var actor=new GameObject("Exact additive native sample",typeof(SpriteRenderer));actor.layer=29;actor.transform.rotation=camera.transform.rotation;
            actor.GetComponent<SpriteRenderer>().sprite=sprite;actor.GetComponent<SpriteRenderer>().sharedMaterial=material;
            var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var read=new Texture2D(64,64,TextureFormat.RGBA32,false,true);var previous=RenderTexture.active;camera.targetTexture=target;
            try
            {
                yield return null;
                foreach(int depth in new[]{0,1})
                {
                    material.SetFloat("_GroundDepth",depth);camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,64,64),0,0);read.Apply();var actual=read.GetPixel(32,32);
                    float Display(byte encoded){float radiance=encoded/255f*encoded/255f*16;return Mathf.Clamp01(radiance<=.0031308f?radiance*12.92f:1.055f*Mathf.Pow(radiance,1/2.4f)-.055f);}
                    Assert.That(actual.r,Is.EqualTo(Display(32)).Within(2/255f));Assert.That(actual.g,Is.EqualTo(Display(48)).Within(2/255f));Assert.That(actual.b,Is.EqualTo(Display(64)).Within(2/255f));
                }
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(actor);Object.Destroy(material);Object.Destroy(sprite);Object.Destroy(texture);Object.Destroy(cameraObject);}
        }
        [UnityTest]public IEnumerator ActualYuFieldUsesExactPagesAndRetainsLightAtItsMeasuredWarmBoundary()
        {
            var clip=OriginalCombatVfxArt.Get("defender/D22/yu_skill3_buff_01");Assert.That(clip.ExactRgba,Is.True);Assert.That(clip.LoopStart,Is.GreaterThan(0));yield return clip.Sequence.Warm();
            var cameraObject=new GameObject("Six-star original field camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=4;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.transform.position=new Vector3(0,8,-5);camera.transform.LookAt(Vector3.zero);
            var root=new GameObject("Exact Yu source pool");var layer=new OriginalCombatVfxLayer(root.transform);
            var target=new RenderTexture(256,256,24);var read=new Texture2D(256,256,TextureFormat.RGBA32,false,true);var previous=RenderTexture.active;camera.targetTexture=target;
            try
            {
                yield return null;int[] pixels=new int[2];double[] ages={clip.LoopEnd-1.0/30,clip.LoopEnd+.00001};
                for(int i=0;i<2;i++)
                {
                    layer.Begin();Assert.That(layer.ShowGround(clip.Key,ages[i],Vector3.zero,camera,false,28,true),Is.True);layer.End();
                    var renderer=layer.Renderers.Single(r=>r.enabled);renderer.gameObject.layer=29;
                    Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("YesterdayConvoy/Six Star Native HDR"));
                    Assert.That(renderer.transform.parent.localScale,Is.EqualTo(Vector3.one),"Native camera projection must not be flattened a second time.");
                    camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();pixels[i]=read.GetPixels32().Count(p=>p.r+p.g+p.b>12);
                }
                Debug.Log("SIXSTAR_WARM_GPU "+pixels[0]+"->"+pixels[1]);Assert.That(pixels[0],Is.GreaterThan(20));Assert.That(pixels[1],Is.GreaterThan(pixels[0]*.5));
            }
            finally{layer.Dispose();camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(root);Object.Destroy(cameraObject);}
        }
        [UnityTest]public IEnumerator EveryMeasuredWarmFieldKeepsActualGpuOutputAtItsOwnLoopBoundary()
        {
            var data=JsonUtility.FromJson<Manifest>(System.IO.File.ReadAllText(ArtSourcePaths.Resolve("OriginalCombatVfx/Defenders/manifest.json")));
            var entries=data.clips.Where(c=>c.loopStart>0).ToArray();Assert.That(entries.Length,Is.GreaterThan(15));
            var cameraObject=new GameObject("Measured native field loop camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.12f,.12f);
            var root=new GameObject("Measured native field boundaries");var layer=new OriginalCombatVfxLayer(root.transform);var target=new RenderTexture(128,128,24);var read=new Texture2D(128,128,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;camera.targetTexture=target;
            Color32[] Capture(){camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();return read.GetPixels32();}
            try
            {
                foreach(var entry in entries)
                {
                    var clip=OriginalCombatVfxArt.Get(entry.key);Assert.That(clip.ExactRgba,Is.True);yield return clip.Sequence.Warm();
                    var sprite=clip.At(clip.LoopStart,true);camera.orthographicSize=Mathf.Max(.2f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y)*.6f);
                    var rotation=Quaternion.Euler(60,0,0);camera.transform.SetPositionAndRotation(rotation*(sprite.bounds.center-Vector3.forward*24),rotation);
                    layer.Begin();layer.End();var baseline=Capture();
                    (int pixels,long energy) Difference(Color32[] actual)
                    {
                        int pixels=0;long energy=0;for(int i=0;i<actual.Length;i++){int delta=Math.Abs(actual[i].r-baseline[i].r)+Math.Abs(actual[i].g-baseline[i].g)+Math.Abs(actual[i].b-baseline[i].b);if(delta>4)pixels++;energy+=delta;}
                        return(pixels,energy);
                    }
                    layer.Begin();Assert.That(layer.Show(clip.Key,(clip.LoopStart+clip.LoopEnd)*.5,Vector3.zero,camera,false,28,true),Is.True);layer.End();foreach(var r in layer.Renderers)r.gameObject.layer=29;
                    var reference=Difference(Capture());Assert.That(reference.pixels,Is.GreaterThanOrEqualTo(4),clip.Key+" needs a measurable native steady reference.");
                    foreach(double age in new[]{clip.LoopEnd-1d/30,clip.LoopEnd+.00001,clip.LoopEnd+(clip.LoopEnd-clip.LoopStart)-.00001})
                    {
                        layer.Begin();Assert.That(layer.Show(clip.Key,age,Vector3.zero,camera,false,28,true),Is.True);layer.End();foreach(var r in layer.Renderers)r.gameObject.layer=29;
                        var actual=Difference(Capture());Debug.Log("SIXSTAR_WARM_BOUNDARY "+clip.Key+" age="+age+" pixels="+actual.pixels+"/"+reference.pixels+" energy="+actual.energy+"/"+reference.energy);
                        Assert.That(actual.pixels,Is.GreaterThanOrEqualTo(Math.Max(4,reference.pixels*.3)),clip.Key+" must retain its measured steady region, not a one-pixel residue.");
                        Assert.That(actual.energy,Is.GreaterThanOrEqualTo(reference.energy*.3),clip.Key+" must not return to a near-empty cold-start frame.");
                    }
                }
            }
            finally{layer.Dispose();camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(root);Object.Destroy(cameraObject);}
        }
        [UnityTest]public IEnumerator SignedBoneScaleDoesNotLeakFromAMountedWeaponIntoTheNextPooledEffect()
        {
            var clip=OriginalCombatVfxArt.Get("defender/D23/monstr_skill_03_buff_01");yield return clip.Sequence.Warm();
            var root=new GameObject("Original signed weapon-scale pool");var cameraObject=new GameObject("Original signed weapon-scale camera");var camera=cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0,8,-5),Quaternion.Euler(60,0,0));var layer=new OriginalCombatVfxLayer(root.transform);
            try
            {
                layer.Begin();Assert.That(layer.ShowScaled(clip.Key,1,Vector3.zero,camera,true,33,new Vector2(-.28f,.35f),true,18),Is.True);layer.End();
                var renderer=layer.Renderers.Single();Assert.That(renderer.transform.parent.localScale,Is.EqualTo(new Vector3(-.28f,.35f,1)));Assert.That(renderer.transform.localScale,Is.EqualTo(Vector3.one));
                layer.Begin();Assert.That(layer.Show(clip.Key,1,Vector3.zero,camera,false,33,true),Is.True);layer.End();
                Assert.That(layer.Renderers.Single(),Is.SameAs(renderer));Assert.That(renderer.transform.parent.localScale,Is.EqualTo(Vector3.one));Assert.That(renderer.flipX,Is.False);
            }
            finally{layer.Dispose();Object.Destroy(root);Object.Destroy(cameraObject);}
        }
    }
}
