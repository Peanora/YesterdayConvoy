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
        [UnityTest]public IEnumerator NativeProjectilePreservesSourceChildPoseAndNoseFollowsAllFourMovementDirections()
        {
            const string key="defender/D32/chen3_skill_03_trail_01_dir_right";
            yield return OriginalNativeFxCatalog.Prepare(new[]{key});
            var parent=new GameObject("Native projectile source direction fixture");var cameraObject=new GameObject("Native projectile fixture camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var layer=new OriginalNativeFxLayer(parent.transform);
            try
            {
                foreach(var direction in new[]{Vector3.right,Vector3.forward,Vector3.left,Vector3.back})
                {
                    layer.Clear();layer.Begin(0,camera);
                    Assert.That(layer.TryShowPath(key,"source-direction",0,new[]{new OriginalNativeFxPathPoint(Vector3.zero,0,direction)},new OriginalNativeFxPose(Vector3.zero,Quaternion.Euler(90,0,0),direction)),Is.True);layer.End();
                    var tag=parent.GetComponentInChildren<OriginalNativeFxTag>();var authored=tag.transform.Find("static_offset/rotation_z");
                    Assert.That(Quaternion.Angle(authored.localRotation,Quaternion.Euler(0,0,90)),Is.LessThan(.001f),"The source rotation_z pose must survive the explicit body-transform direction driver.");
                    var head=tag.transform.Find("static_offset/rotation_z/long/longtou");
                    // The actual source chenlong texture's nose lies along the
                    // mesh local -X axis (verified UV vertex747/source hash).
                    var nose=head.TransformVector(Vector3.left);nose.y=0;
                    Assert.That(Vector3.Dot(nose.normalized,direction),Is.GreaterThan(.999f),"The source head geometry must face its real projectile movement, not a guessed named-node axis.");
                }
            }
            finally{layer.Dispose();Object.DestroyImmediate(parent);Object.DestroyImmediate(cameraObject);}
        }
        [Test]public void NativeTrailKeepsEmittedWorldVerticesWhenTheHeadTurnsAndEmitterMoves()
        {
            var root=new GameObject("Trail history snapshot fixture");root.AddComponent<OriginalNativeFxTag>();
            var emitter=new GameObject("original emitter");emitter.transform.SetParent(root.transform,false);emitter.transform.localPosition=new Vector3(-.8f,.2f,0);
            var source=emitter.AddComponent<TrailRenderer>();source.time=10;source.minVertexDistance=.01f;source.widthMultiplier=.2f;source.widthCurve=AnimationCurve.Constant(0,1,1);
            var cameraObject=new GameObject("Trail history camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,5,-5);camera.transform.LookAt(Vector3.zero);
            var trail=new OriginalNativeFxTrail(source,"fixture");
            try
            {
                var straight=new[]{new OriginalNativeFxPathPoint(Vector3.left*2,0,Vector3.right),new OriginalNativeFxPathPoint(Vector3.left,1,Vector3.right),new OriginalNativeFxPathPoint(Vector3.zero,2,Vector3.right)};
                trail.Present(2,straight,new OriginalNativeFxPose(Vector3.zero,Quaternion.identity,Vector3.right),camera);
                var filter=root.GetComponentInChildren<MeshFilter>();var before=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).Take(4).ToArray();Assert.That(before.Length,Is.EqualTo(4));
                root.transform.SetPositionAndRotation(Vector3.forward*.5f,Quaternion.Euler(0,-90,0));
                var corner=straight.Concat(new[]{new OriginalNativeFxPathPoint(Vector3.zero,2,Vector3.forward,true),new OriginalNativeFxPathPoint(Vector3.forward*.5f,2.5,Vector3.forward)}).ToArray();
                trail.Present(2.5,corner,new OriginalNativeFxPose(Vector3.forward*.5f,Quaternion.identity,Vector3.forward),camera);
                var after=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
                for(int i=0;i<before.Length;i++)Assert.That(Vector3.Distance(before[i],after[i]),Is.LessThan(.00001f),"Old trail vertices are world snapshots, independent of the current emitter transform.");
                Assert.That(Vector3.Distance((after[after.Length-2]+after[after.Length-1])*.5f,source.transform.position),Is.LessThan(.00001f),"The temporary live head follows continuously between durable history samples.");
            }
            finally{trail.Dispose();Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);}
        }
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
