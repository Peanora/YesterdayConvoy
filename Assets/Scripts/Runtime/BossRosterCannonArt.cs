using System;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Real source mesh/UV/albedo. The approved simplified display material is explicit.
    internal sealed class BossRosterCannonArt:IDisposable
    {
        internal const string Root="BossRosterFx/Devices/Cannon";
        [Serializable] sealed class Model {public Geometry mesh;}
        [Serializable] sealed class Geometry {public Vector3[] vertices;public Vector2[] uv;public int[] triangles;}
        internal static ArtSequence TextureSequence=>new ArtSequence{Root=Root,Key="main",Name="Original cannon albedo",Count=1,Width=1024,Height=1024,Columns=1,
            Ppu=1,PivotX=.5f,PivotY=.5f,Duration=1,Fps=1,Pages=new[]{new ArtSequence.Page{file="main.png",count=1,width=1024,height=1024}}};
        internal readonly Mesh Mesh;internal readonly Material Material;
        internal BossRosterCannonArt()
        {
            var data=JsonUtility.FromJson<Model>(File.ReadAllText(ArtSourcePaths.Resolve(Root+"/manifest.json")));
            if(data?.mesh==null||data.mesh.vertices==null||data.mesh.uv==null||data.mesh.triangles==null||data.mesh.vertices.Length!=data.mesh.uv.Length||data.mesh.triangles.Length%3!=0)
                throw new InvalidDataException("Invalid original cannon mesh");
            foreach(int index in data.mesh.triangles)if(index<0||index>=data.mesh.vertices.Length)throw new InvalidDataException("Invalid cannon triangle");
            Mesh=new Mesh{name="Original trap_196_cnnon / 1332 vertices"};Mesh.vertices=data.mesh.vertices;Mesh.uv=data.mesh.uv;Mesh.triangles=data.mesh.triangles;Mesh.RecalculateBounds();
            var shader=Resources.Load<Shader>("BossRosterCannonStructure");if(shader==null)throw new InvalidDataException("Missing approved cannon structure shader");
            Material=new Material(shader){name="Original cannon albedo / approved simplified material",mainTexture=TextureSequence.Frames[0].texture};
        }
        public void Dispose(){UnityEngine.Object.Destroy(Mesh);UnityEngine.Object.Destroy(Material);}
    }
}
