using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class OriginalCombatVfxLayerSceneTests
    {
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        [UnityTest]public IEnumerator PoolPreservesHdrAndWorldScaleAndReleasesExpiredFramesAtTheSameClock()
        {
            var assembly=typeof(ManualBattle).Assembly;var reader=assembly.GetType("YesterdayConvoy.OriginalCombatVfxArt");
            var clip=reader.GetMethod("Get",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"common/common_hit_01"});
            var sequence=clip.GetType().GetField("Sequence",Flags).GetValue(clip);
            yield return (IEnumerator)sequence.GetType().GetMethod("Warm",Flags).Invoke(sequence,null);
            var root=new GameObject("Native layer pool test");var cameraObject=new GameObject("Native layer camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,8,-5);camera.transform.LookAt(Vector3.zero);
            var type=assembly.GetType("YesterdayConvoy.OriginalCombatVfxLayer");var layer=Activator.CreateInstance(type,Flags,null,new object[]{root.transform},null);
            Action<string> call=name=>type.GetMethod(name,Flags).Invoke(layer,null);
            object Show(double age)=>type.GetMethod("Show",Flags).Invoke(layer,new object[]{"common/common_hit_01",age,Vector3.zero,camera,false,7,false,0f});
            try
            {
                double age=Math.Min(.1,(float)clip.GetType().GetProperty("Duration",Flags).GetValue(clip)*.5);
                call("Begin");Assert.That(Show(age),Is.EqualTo(true));call("End");
                var renderers=((System.Collections.Generic.IEnumerable<SpriteRenderer>)type.GetProperty("Renderers",Flags).GetValue(layer)).ToArray();Assert.That(renderers.Length,Is.EqualTo(1));var r=renderers[0];var sprite=r.sprite;
                Assert.That(r.enabled,Is.True);Assert.That(r.transform.lossyScale,Is.EqualTo(Vector3.one));Assert.That(r.transform.position,Is.EqualTo(Vector3.zero));
                var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Assert.That(block.GetFloat("_HdrRange"),Is.EqualTo(clip.GetType().GetField("HdrRange",Flags).GetValue(clip)));
                yield return null;call("Begin");Show(age);call("End");Assert.That(r.sprite,Is.SameAs(sprite),"A repeated simulated age cannot advance on Unity wall time.");
                call("Begin");Assert.That(Show(1000),Is.EqualTo(false));call("End");Assert.That(r.enabled,Is.False);Assert.That(type.GetProperty("VisibleCount",Flags).GetValue(layer),Is.EqualTo(0));
                call("Clear");Assert.That(r.enabled,Is.False,"Cleanup hides renderers before deferred Destroy.");
            }
            finally{((IDisposable)layer).Dispose();UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(cameraObject);}
            yield return null;
        }
        [UnityTest]public IEnumerator GroundProjectionKeepsBothNativeHalvesFootUvAndRealDepthOcclusion()
        {
            Assume.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null),"Requires a real graphics device.");
            const string key="defender/D27/sbell2_skill_03_start_01";
            var assembly=typeof(ManualBattle).Assembly;var reader=assembly.GetType("YesterdayConvoy.OriginalCombatVfxArt");
            var clip=reader.GetMethod("Get",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{key});
            var sequence=clip.GetType().GetField("Sequence",Flags).GetValue(clip);
            yield return (IEnumerator)sequence.GetType().GetMethod("Warm",Flags).Invoke(sequence,null);
            var root=new GameObject("Ground native projection test");var cameraObject=new GameObject("Ground projection camera");var camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=4;camera.aspect=16f/9;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,8,-5);
            Vector3 anchor=new Vector3(0,.02f,0);camera.transform.LookAt(anchor);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Opaque real floor";floor.layer=30;floor.transform.localScale=Vector3.one*2;
            var shader=Shader.Find("Unlit/Color");Assert.That(shader,Is.Not.Null);var floorMaterial=new Material(shader);floorMaterial.color=new Color(.12f,.13f,.14f);
            floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Opaque above-ground occluder";blocker.layer=30;
            blocker.transform.position=new Vector3(0,1,0);blocker.transform.localScale=new Vector3(12,.5f,12);blocker.GetComponent<Renderer>().sharedMaterial=floorMaterial;blocker.SetActive(false);
            var type=assembly.GetType("YesterdayConvoy.OriginalCombatVfxLayer");var layer=Activator.CreateInstance(type,Flags,null,new object[]{root.transform},null);
            Action<string> call=name=>type.GetMethod(name,Flags).Invoke(layer,null);
            void Show(string method)
            {
                call("Begin");Assert.That(type.GetMethod(method,Flags).Invoke(layer,new object[]{key,.55d,anchor,camera,false,7,false,0f}),Is.EqualTo(true));call("End");
                foreach(var r in root.GetComponentsInChildren<SpriteRenderer>()){r.gameObject.layer=30;r.transform.parent.gameObject.layer=30;}
            }
            var target=new RenderTexture(320,180,24);var pixels=new Texture2D(320,180,TextureFormat.RGB24,false);var previous=RenderTexture.active;camera.targetTexture=target;
            Color32[] Capture()
            {
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,320,180),0,0);pixels.Apply();return pixels.GetPixels32();
            }
            int Changed(Color32[] a,Color32[] b,int fromY,int toY)
            {
                int count=0;for(int y=fromY;y<toY;y++)for(int x=0;x<320;x++){int i=y*320+x;if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>12)count++;}return count;
            }
            // Sprite.bounds has an artificial Z extent; only its actual XY image plane
            // belongs in the native projection comparison.
            Vector2 ProjectCorner(SpriteRenderer r,Vector3 corner)=>camera.WorldToScreenPoint(r.transform.TransformPoint(new Vector3(corner.x,corner.y,0)));
            try
            {
                camera.orthographic=true;camera.ResetProjectionMatrix();Show("Show");var r=root.GetComponentInChildren<SpriteRenderer>();var sprite=r.sprite;var uv=sprite.uv;
                Vector2 low=ProjectCorner(r,sprite.bounds.min),high=ProjectCorner(r,sprite.bounds.max);
                Show("ShowGround");Assert.That(r.sprite,Is.SameAs(sprite));CollectionAssert.AreEqual(uv,r.sprite.uv);Assert.That(r.transform.position,Is.EqualTo(anchor));
                Assert.That(r.sprite.pixelsPerUnit,Is.EqualTo(sequence.GetType().GetField("Ppu",Flags).GetValue(sequence)));
                Assert.That(Vector3.Dot(r.transform.forward,Vector3.down),Is.GreaterThan(.999f));
                Assert.That(camera.orthographic,Is.True);Assert.That(Vector2.Distance(low,ProjectCorner(r,sprite.bounds.min)),Is.LessThan(.01f));
                Assert.That(Vector2.Distance(high,ProjectCorner(r,sprite.bounds.max)),Is.LessThan(.01f),"The actual XY image-plane corners agree under orthographic projection.");
                // Match the game's light perspective. Perspective depth naturally changes
                // distant corners slightly; verify visibility and occlusion, not an affine identity.
                camera.orthographic=false;camera.fieldOfView=24;camera.transform.rotation=Quaternion.Euler(60,0,0);camera.transform.position=anchor-camera.transform.forward*24;camera.ResetProjectionMatrix();
                r.enabled=false;var baseline=Capture();Show("Show");var billboard=Capture();Show("ShowGround");
                Assert.That(r.transform.position,Is.EqualTo(anchor));Assert.That(r.sprite,Is.SameAs(sprite));CollectionAssert.AreEqual(uv,r.sprite.uv);
                var ground=Capture();int foot=Mathf.RoundToInt(camera.WorldToScreenPoint(anchor).y);
                int lower=Changed(ground,baseline,0,foot),upper=Changed(ground,baseline,foot,180),oldLower=Changed(billboard,baseline,0,foot);
                Assert.That(upper,Is.GreaterThan(20));Assert.That(lower,Is.GreaterThan(20),"The horizontal API must retain the lower native half.");
                Assert.That(oldLower,Is.GreaterThan(20),"The approved Pramanix billboard now reconstructs ground depth while preserving the source capture shape.");
                blocker.SetActive(true);r.enabled=false;var occludedBaseline=Capture();r.enabled=true;var occluded=Capture();
                Assert.That(Changed(occluded,occludedBaseline,0,180),Is.LessThan(4),"Opaque geometry above the ground still occludes the effect; no Always depth bypass.");
            }
            finally
            {
                ((IDisposable)layer).Dispose();camera.targetTexture=null;RenderTexture.active=previous;target.Release();
                UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(floorMaterial);UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(blocker);UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(cameraObject);
            }
            yield return null;
        }
    }
}
