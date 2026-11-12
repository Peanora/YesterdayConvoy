using System.Collections;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
 public sealed class PramanixOutputSceneTests
 {
  [UnityTest]public IEnumerator LinearHdrSamplesReachGammaDisplayWithoutLosingAdditiveRgb()
  {
   Assume.That(QualitySettings.activeColorSpace,Is.EqualTo(ColorSpace.Gamma));
   var cameraObject=new GameObject("Pramanix output calibration camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
   camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
   camera.transform.position=new Vector3(0,3,-3);camera.transform.LookAt(Vector3.zero);
   var texture=new Texture2D(1,1,TextureFormat.RGBA32,false,true);texture.SetPixel(0,0,new Color32(32,48,64,0));texture.Apply();
   var sprite=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
   var material=new Material(Resources.Load<Shader>("PramanixNative"));material.SetFloat("_HdrRange",16);material.SetVector("_PramanixAnchor",new Vector4(0,.012f,0,0));
   var actor=new GameObject("Exact additive calibration pixel",typeof(SpriteRenderer));actor.layer=29;actor.transform.rotation=camera.transform.rotation;
   var renderer=actor.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
   var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var read=new Texture2D(64,64,TextureFormat.RGBA32,false,true);var previous=RenderTexture.active;camera.targetTexture=target;
   try{
    yield return null;camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,64,64),0,0);read.Apply();var actual=read.GetPixel(32,32);
    float Display(byte encoded){float radiance=encoded/255f*encoded/255f*16;return Mathf.Clamp01(radiance<=.0031308f?radiance*12.92f:1.055f*Mathf.Pow(radiance,1/2.4f)-.055f);}
    Assert.That(actual.r,Is.EqualTo(Display(32)).Within(2/255f),"Linear HDR requires the approved display conversion in this Gamma project.");
    Assert.That(actual.g,Is.EqualTo(Display(48)).Within(2/255f));Assert.That(actual.b,Is.EqualTo(Display(64)).Within(2/255f),"RGB must survive even when source alpha is zero.");
   }finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(actor);Object.Destroy(material);Object.Destroy(sprite);Object.Destroy(texture);Object.Destroy(cameraObject);}
  }
  [UnityTest]public IEnumerator CasterFieldRemainsVisibleAboveTheSavedMapFivePlatformHeight()
  {
   var previousScene=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("Map05HeatExchange");SceneManager.SetActiveScene(scene);
   const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
   var assembly=typeof(ManualBattle).Assembly;var art=assembly.GetType("YesterdayConvoy.OriginalCombatVfxArt");
   var clip=art.GetMethod("Get",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"defender/D27/sbell2_skill_03_buff_01"});
   var sequence=clip.GetType().GetField("Sequence",flags).GetValue(clip);yield return (IEnumerator)sequence.GetType().GetMethod("Warm",flags).Invoke(sequence,null);
   var cameraObject=new GameObject("Map05 platform depth camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<29;camera.orthographic=true;camera.orthographicSize=4;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,8,-5);camera.transform.LookAt(new Vector3(0,.72f,0));
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=29;floor.transform.position=new Vector3(0,.739f/2,0);floor.transform.localScale=new Vector3(8,.739f,8);var floorMaterial=new Material(Shader.Find("Unlit/Color")){color=Color.black};floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
   var root=new GameObject("Map05 exact original field");var type=assembly.GetType("YesterdayConvoy.OriginalCombatVfxLayer");var layer=Activator.CreateInstance(type,flags,null,new object[]{root.transform},null);
   var target=new RenderTexture(256,256,24);var read=new Texture2D(256,256,TextureFormat.RGBA32,false,true);var previous=RenderTexture.active;camera.targetTexture=target;
   try{
    type.GetMethod("Begin",flags).Invoke(layer,null);type.GetMethod("Show",flags).Invoke(layer,new object[]{"defender/D27/sbell2_skill_03_buff_01",.6d,new Vector3(0,.72f,0),camera,false,28,true,0f});type.GetMethod("End",flags).Invoke(layer,null);
    var renderer=root.GetComponentInChildren<SpriteRenderer>();renderer.gameObject.layer=29;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
    Assert.That(block.GetVector("_PramanixAnchor").y,Is.GreaterThan(floor.GetComponent<Renderer>().bounds.max.y+.005f),"The field's actual depth plane must be above the saved .739 cap.");
    yield return null;camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();
    Assert.That(read.GetPixels32().Count(p=>p.r+p.g+p.b>12),Is.GreaterThan(20),"The original field must reach actual GPU pixels above the platform.");
   }finally{((IDisposable)layer).Dispose();camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(root);Object.Destroy(floor);Object.Destroy(floorMaterial);Object.Destroy(cameraObject);SceneManager.SetActiveScene(previousScene);}
   yield return SceneManager.UnloadSceneAsync(scene);
  }
 }
}
