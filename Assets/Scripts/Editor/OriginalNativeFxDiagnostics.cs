using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Explicit diagnostic only. The normal game and the saved scene never run it.
    [InitializeOnLoad]
    public static class OriginalNativeFxDiagnostics
    {
        static bool done;
        [Serializable] sealed class Snapshot {public string key,id;public double age;public Vector3 position;public Quaternion rotation;public Row[] renderers;}
        [Serializable] sealed class Row
        {
            public string path,type;public bool enabled,active;public int order,particles;public Bounds bounds;public string mode,alignment,streams;
            public Vector3 localPosition,localScale;public Quaternion localRotation;public Mat[] materials;
        }
        [Serializable] sealed class Mat {public string name,shader,keywords;public int queue;public float opacity,srcBlend,dstBlend,ztest,zwrite;public Color mainColor;public Vector4 clock;}
        static OriginalNativeFxDiagnostics(){EditorApplication.update+=Tick;}
        static string Argument(string key)
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:null;}
        static void Tick()
        {
            if(done||!EditorApplication.isPlaying)return;string directory=Argument("-nativeFxDiagnosticDir");if(directory==null)return;
            var tags=Object.FindObjectsByType<OriginalNativeFxTag>(FindObjectsSortMode.None).Where(t=>t.Key.StartsWith("defender/D26/",StringComparison.Ordinal)).ToArray();
            var range=tags.FirstOrDefault(t=>t.Key.EndsWith("/svash2_skill_03_range_01",StringComparison.Ordinal)&&t.SampleAge>=2.95);
            if(range==null||Camera.main==null)return;done=true;
            try{Capture(directory,Camera.main,tags,range);Debug.Log("ORIGINAL_NATIVE_D26_DIAGNOSTIC_PASS "+directory);}
            catch(Exception error){Debug.LogException(error);}
        }
        static void Capture(string directory,Camera camera,OriginalNativeFxTag[] tags,OriginalNativeFxTag range)
        {
            Directory.CreateDirectory(directory);var native=Object.FindObjectsByType<OriginalNativeFxTag>(FindObjectsSortMode.None).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();
            var enabled=native.ToDictionary(r=>r,r=>r.enabled);var orders=native.ToDictionary(r=>r,r=>r.sortingOrder);
            var previous=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;float aspect=camera.aspect;
            var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var read=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            string Relative(Transform t,Transform root){var names=new List<string>();while(t!=null&&t!=root){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);}
            Row Describe(Renderer renderer,Transform root)
            {
                var row=new Row{path=Relative(renderer.transform,root),type=renderer.GetType().Name,enabled=renderer.enabled,active=renderer.gameObject.activeInHierarchy,order=renderer.sortingOrder,bounds=renderer.bounds,localPosition=renderer.transform.localPosition,localRotation=renderer.transform.localRotation,localScale=renderer.transform.localScale};
                if(renderer is ParticleSystemRenderer psr)
                {var streams=new List<ParticleSystemVertexStream>();psr.GetActiveVertexStreams(streams);row.mode=psr.renderMode.ToString();row.alignment=psr.alignment.ToString();row.streams=string.Join(",",streams);row.particles=psr.GetComponent<ParticleSystem>().particleCount;}
                row.materials=renderer.sharedMaterials.Select(m=>m==null?null:new Mat{name=m.name,shader=m.shader.name,keywords=string.Join(",",m.shaderKeywords),queue=m.renderQueue,opacity=F(m,"_Opacity"),srcBlend=F(m,"_SrcBlend"),dstBlend=F(m,"_DstBlend"),ztest=F(m,"_ZTest"),zwrite=F(m,"_ZWrite"),mainColor=m.HasProperty("_MainColor")?m.GetColor("_MainColor"):Color.clear,clock=m.HasProperty("_OriginalFxTime")?m.GetVector("_OriginalFxTime"):Vector4.zero}).ToArray();return row;
            }
            void Save(string name,Func<Renderer,bool> show)
            {
                foreach(var renderer in native)renderer.enabled=enabled[renderer]&&show(renderer);
                camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,1280,720),0,0);read.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),read.EncodeToPNG());
            }
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=1280f/720;
                for(int i=0;i<tags.Length;i++)
                {
                    var tag=tags[i];var snapshot=new Snapshot{key=tag.Key,id=tag.InstanceId,age=tag.SampleAge,position=tag.transform.position,rotation=tag.transform.rotation,renderers=tag.GetComponentsInChildren<Renderer>(true).Select(r=>Describe(r,tag.transform)).ToArray()};
                    string name=tag.Key.Substring(tag.Key.LastIndexOf('/')+1)+"-"+i;File.WriteAllText(Path.Combine(directory,name+".json"),JsonUtility.ToJson(snapshot,true));
                    Save(name,r=>r.transform.IsChildOf(tag.transform));
                }
                var rangeRenderers=new HashSet<Renderer>(range.GetComponentsInChildren<Renderer>(true));
                Save("full",r=>true);Save("without-range",r=>!rangeRenderers.Contains(r));
                Save("range-PS",r=>rangeRenderers.Contains(r)&&r is ParticleSystemRenderer);
                Save("range-ground",r=>rangeRenderers.Contains(r)&&r is MeshRenderer&&(r.name.StartsWith("ground",StringComparison.Ordinal)||r.name.StartsWith("snow_plane",StringComparison.Ordinal)||r.name.StartsWith("smoke_plane",StringComparison.Ordinal)));
                Save("range-walls",r=>rangeRenderers.Contains(r)&&r is MeshRenderer&&r.transform.name.Contains("wall"));
                foreach(var renderer in rangeRenderers)renderer.sortingOrder=-2000;
                Save("full-range-order-low-diagnostic",r=>true);
            }
            finally
            {
                foreach(var renderer in native){renderer.enabled=enabled[renderer];renderer.sortingOrder=orders[renderer];}
                camera.targetTexture=previous;camera.rect=rect;camera.aspect=aspect;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(read);
            }
        }
        static float F(Material material,string name)=>material.HasProperty(name)?material.GetFloat(name):float.NaN;
    }
}
