using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class SilverAshWaveRenderingSceneTests
    {
        [Serializable] sealed class Sample
        {
            public string key,direction;public float age,tipX,expectedTipX,authoredNoseDot,bakedUvNoseDot;
            public int pixels,particles,headVertices;public bool sourceStaticPose,sourceTexture,passed;
        }
        [Serializable] sealed class Report
        {
            public string scope="Native original PS/Animator isolated top view. Origin anchored, source-axis/one-displacement adaptation; not a recovered original client manager. Each PNG has age .15 left / .29 right. No damage or actor fixture.";
            public string graphics;public List<Sample> samples=new List<Sample>();
        }
        static string Output()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-silverAshWaveDiagnosticDir");
            if(at<0||at+1>=args.Length)return null;string path=Path.GetFullPath(args[at+1]);
            if(path.StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Keep wave diagnostics outside Assets.");
            Directory.CreateDirectory(path);return path;
        }
        // UV+X points to the bird nose in every original sheet frame. For self02
        // it points to the crescent's convex -X mesh edge, whose own authored
        // Z180 rotation makes it tip-space +X. This is not an AABB long-axis test.
        static Vector3 UvForward(Mesh mesh)
        {
            var vertices=mesh.vertices;var uv=mesh.uv;if(vertices.Length==0||uv.Length!=vertices.Length)return Vector3.zero;
            Vector3 center=Vector3.zero;float mean=0;for(int i=0;i<vertices.Length;i++){center+=vertices[i];mean+=uv[i].x;}center/=vertices.Length;mean/=vertices.Length;
            Vector3 result=Vector3.zero;for(int i=0;i<vertices.Length;i++)result+=(vertices[i]-center)*(uv[i].x-mean);
            result.y=0;return result.normalized;
        }
        [UnityTest]public IEnumerator ThreeOriginalWavesKeepSourceAnimationAndFaceFourGroundDirections()
        {
            var keys=new[]{"defender/D26/svash2_skill_03_trail_02","defender/D26/svash2_skill_03_trail_left_01","defender/D26/svash2_skill_03_trail_right_01"};
            yield return OriginalNativeFxCatalog.Prepare(keys);
            string directory=Output();var report=new Report{graphics=SystemInfo.graphicsDeviceName};
            var parent=new GameObject("SilverAsh original wave calibration");var cameraObject=new GameObject("SilverAsh top view calibration camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=6;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.transform.SetPositionAndRotation(Vector3.up*20,Quaternion.LookRotation(Vector3.down,Vector3.forward));camera.nearClipPlane=.01f;camera.farClipPlane=50;
            var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);var read=new Texture2D(512,512,TextureFormat.RGBA32,false);var pair=new Texture2D(1024,512,TextureFormat.RGBA32,false);
            var mesh=new Mesh();var layer=new OriginalNativeFxLayer(parent.transform);var previous=RenderTexture.active;camera.targetTexture=target;
            var directions=new[]{Vector3.right,Vector3.forward,Vector3.left,Vector3.back};var names=new[]{"right","up","left","down"};
            Color32[] Capture(){camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,512,512),0,0);read.Apply();return read.GetPixels32();}
            try
            {
                foreach(string key in keys)for(int heading=0;heading<directions.Length;heading++)
                {
                    layer.Clear();var direction=directions[heading];var frames=new Color32[2][];float[] ages={.15f,.29f},expected={2.27858691f,3.19537186f};
                    for(int sample=0;sample<ages.Length;sample++)
                    {
                        float age=ages[sample];layer.Begin(age,camera);
                        Assert.That(layer.TryShow(key,"silver-calibration",age,new OriginalNativeFxPose(Vector3.zero,DefenderSkillVfxView.SilverAshWaveRotation(direction),direction),false,.3),Is.True);layer.End();
                        var tag=parent.GetComponentInChildren<OriginalNativeFxTag>();foreach(var transform in tag.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=29;
                        var offset=tag.transform.Find("static_offset");var rotation=offset.Find("rotation_y");var tip=rotation.Find("ctrl_rotation/tip");bool self=key.EndsWith("/svash2_skill_03_trail_02",StringComparison.Ordinal);
                        var head=tip.Find(self?"slash_pattern":"bird");var renderer=head.GetComponent<ParticleSystemRenderer>();
                        var forward=head.TransformVector(self?Vector3.left:Vector3.right);forward.y=0;
                        mesh.Clear();renderer.BakeMesh(mesh,camera,ParticleSystemBakeMeshOptions.BakePosition|ParticleSystemBakeMeshOptions.BakeRotationAndScale);
                        var uvForward=UvForward(mesh);frames[sample]=Capture();int pixels=frames[sample].Count(c=>c.r+c.g+c.b>12);
                        bool texture=self?renderer.mesh!=null&&renderer.mesh.name=="char_svash2_mesh_01_3":renderer.sharedMaterials.Any(m=>m!=null&&m.GetTexture("_MainTex")?.name=="char_savsh2_04");
                        var row=new Sample{key=key,direction=names[heading],age=age,tipX=tip.localPosition.x,expectedTipX=expected[sample],
                            authoredNoseDot=Vector3.Dot(forward.normalized,direction),bakedUvNoseDot=Vector3.Dot(uvForward,direction),pixels=pixels,
                            particles=renderer.GetComponent<ParticleSystem>().particleCount,headVertices=mesh.vertexCount,sourceTexture=texture,
                            sourceStaticPose=Quaternion.Angle(offset.localRotation,Quaternion.Euler(0,0,90))<.001f&&Quaternion.Angle(rotation.localRotation,Quaternion.identity)<.001f&&Mathf.Abs(offset.localPosition.z+.02f)<.00001f};
                        row.passed=row.pixels>32&&row.particles>0&&row.headVertices>0&&row.authoredNoseDot>.999f&&row.bakedUvNoseDot>.9f&&row.sourceStaticPose&&row.sourceTexture&&Mathf.Abs(row.tipX-row.expectedTipX)<.025f;
                        report.samples.Add(row);Debug.Log("SILVER_ASH_WAVE_DIRECTION_GPU "+JsonUtility.ToJson(row));
                    }
                    if(directory!=null)
                    {
                        var pixels=new Color32[1024*512];for(int y=0;y<512;y++){Array.Copy(frames[0],y*512,pixels,y*1024,512);Array.Copy(frames[1],y*512,pixels,y*1024+512,512);}pair.SetPixels32(pixels);pair.Apply();
                        File.WriteAllBytes(Path.Combine(directory,key.Substring(key.LastIndexOf('/')+1)+"-"+names[heading]+"-015-029.png"),pair.EncodeToPNG());
                    }
                }
                if(directory!=null)File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(report,true));
                Assert.That(report.samples.Count,Is.EqualTo(24));Assert.That(report.samples.All(s=>s.passed),Is.True,JsonUtility.ToJson(report,true));
            }
            finally
            {
                layer.Dispose();camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(read);Object.DestroyImmediate(pair);Object.DestroyImmediate(mesh);Object.DestroyImmediate(parent);Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
