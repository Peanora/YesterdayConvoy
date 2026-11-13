using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class ChenDawnRenderingSceneTests
    {
        [UnityTest] public IEnumerator OriginalProjectedDragonHeadsKeepTheirBelowPivotImageAboveRealTerrain()
        {
            var cameraObject=new GameObject("Chen projected head depth camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=4;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.transform.position=new Vector3(0,12,-8);camera.transform.LookAt(Vector3.zero);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=29;floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(20,1,20);
            var floorMaterial=new Material(Shader.Find("Unlit/Color"));floorMaterial.color=new Color(.2f,.2f,.2f);floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var root=new GameObject("Chen original heads");var layer=new OriginalCombatVfxLayer(root.transform);
            var target=new RenderTexture(256,256,24);var read=new Texture2D(256,256,TextureFormat.RGB24,false);var previous=RenderTexture.active;camera.targetTexture=target;
            Color32[] Capture(){camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
            int Difference(Color32[] a,Color32[] b){int pixels=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>12)pixels++;return pixels;}
            try
            {
                yield return null;
                foreach(string direction in new[]{"right","left","up","down"})
                {
                    var clip=OriginalCombatVfxArt.Get("defender/D32/chen3_skill_03_trail_01_dir_"+direction);yield return clip.Sequence.Warm();
                    Assert.That(clip.GroundDepth,Is.True);Assert.That(clip.Directional,Is.False,"Direction was applied to the original 3D node before sampling.");
                    layer.Begin();layer.End();var baseline=Capture();
                    layer.Begin();Assert.That(layer.Show(clip.Key,1,Vector3.zero,camera,false,32,true),Is.True);layer.End();
                    var renderer=layer.Renderers.Single(r=>r.enabled);renderer.gameObject.layer=29;
                    var correct=Capture();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_GroundDepth",0);renderer.SetPropertyBlock(block);var upright=Capture();
                    int preserved=Difference(correct,baseline),clipped=Difference(upright,baseline);
                    Debug.Log("CHEN_HEAD_DEPTH_GPU "+direction+" upright="+clipped+" projected="+preserved);
                    Assert.That(preserved,Is.GreaterThan(20));Assert.That(preserved,Is.GreaterThan(clipped*1.25+4),"Keep the legal below-pivot dragon image, without changing source pixels or PPU.");
                }
            }
            finally
            {
                layer.Dispose();camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(root);Object.Destroy(floor);Object.Destroy(floorMaterial);Object.Destroy(cameraObject);
            }
        }
    }
}
