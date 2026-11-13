using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderFootprintSceneTests
    {
        const int Layer=29,Size=640;
        // Include the latest original .5-3 second staggered child activation.
        const double SampleAge=4;
        static readonly Vector3 Origin=new Vector3(256,0,256);
        sealed class Case
        {
            internal string Id,Raw;internal DefenderSkillKind Kind;
            internal Case(string id,string raw,DefenderSkillKind kind){Id=id;Raw=raw;Kind=kind;}
            internal string Key=>"defender/"+Id+"/"+Raw;
        }
        static readonly Case[] Cases={
            new Case("D25","hsgma2_skill_03_range_01",DefenderSkillKind.HoshigumaInferno),
            new Case("D26","svash2_skill_03_range_01",DefenderSkillKind.SilverAshReign),
            new Case("D28","wang_skill_03_range",DefenderSkillKind.WangGambit),
            new Case("D29","demkni_skill_03_range",DefenderSkillKind.SariaCalcification),
            new Case("D30","mlynar_skill_03_buff_03",DefenderSkillKind.MlynarGlory),
            new Case("D32","chen3_skill_03_range_01_dir_right",DefenderSkillKind.ChenDawnstreak),
            new Case("D33","aglna2_skill_03_range_line_01",DefenderSkillKind.AngelinaWishes),
            new Case("D34","clemnt_skill_03_range",DefenderSkillKind.ClemensiaCorrosion)
        };
        [Serializable] sealed class Evidence
        {
            public string scope="Top-down isolated diagnostic, not gameplay footage or proof that source FX matches the declared cells. Compare the two images manually; smoke/glow may extend beyond marked cells.";
            public string graphicsDevice;public int size=Size;public double sampleAge=SampleAge;
            public Vector3 isolationOrigin=Origin;public List<Entry> effects=new List<Entry>();
        }
        [Serializable] sealed class Entry
        {
            public string id,key,lineDriver;public Vector2Int[] cells;public Vector3 facing;
            public int changedPixels,renderers,particleCount,lineVertices,meshVertices;public float orthographicSize;
        }

        // This exercises real native rendering. Visible pixels and finite vertices
        // are automated checks; the footprint/FX silhouette comparison is human QA.
        [UnityTest] public IEnumerator OriginalNativeFieldsRenderAgainstActualCellsInAnIsolatedTopDownDiagnostic()
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.Not.EqualTo(GraphicsDeviceType.Null),"This test requires an actual graphics device; do not run with -nographics.");
            string output=EvidenceDirectory();
            var evidence=new Evidence{graphicsDevice=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName};
            float priorTime=Time.timeScale;var priorActive=RenderTexture.active;
            GameObject root=null,caseRoot=null;RenderTexture target=null;Texture2D read=null;
            Material gridMaterial=null,outlineMaterial=null;OriginalNativeFxLayer layer=null;
            try
            {
                Time.timeScale=0;
                foreach(var item in Cases)Assert.That(OriginalNativeFxCatalog.Supports(item.Key),Is.True,item.Key);
                yield return OriginalNativeFxCatalog.Prepare(Cases.Select(c=>c.Key));
                var shader=Resources.Load<Shader>("NativeDefenders/Shaders/AlphaBlend");
                Assert.That(shader!=null&&shader.isSupported,Is.True,"Resource shader for diagnostic grid/overlay.");
                gridMaterial=LineMaterial(shader,2000);outlineMaterial=LineMaterial(shader,4000);
                root=new GameObject("Defender footprint isolated GPU diagnostic");
                var camera=new GameObject("Footprint top-down diagnostic camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform,false);camera.enabled=false;camera.cullingMask=1<<Layer;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.015f,.02f,1);
                camera.orthographic=true;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=40;
                camera.allowHDR=false;camera.allowMSAA=false;
                target=new RenderTexture(Size,Size,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
                read=new Texture2D(Size,Size,TextureFormat.RGB24,false);
                foreach(var item in Cases)
                {
                    var battle=Fixture(item);var defender=battle.Defender;
                    var cells=battle.DefenderSkillCells(defender);var facing=battle.DefenderSkillFacing(defender);
                    Assert.That(cells,Is.Not.Null.And.Not.Empty,item.Id+" actual gameplay cells");
                    Assert.That(facing,Is.EqualTo(Vector3.right),item.Id+" fixture must use right-facing source art.");
                    caseRoot=new GameObject("Footprint diagnostic / "+item.Id);caseRoot.transform.SetParent(root.transform,false);
                    var grid=new GameObject("Diagnostic grid: one world unit per cell");grid.transform.SetParent(caseRoot.transform,false);
                    Bounds bounds=CellBounds(cells,facing);
                    camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.5f+1.5f;
                    camera.transform.SetPositionAndRotation(bounds.center+Vector3.up*20,Quaternion.Euler(90,0,0));
                    DrawGrid(grid.transform,bounds,gridMaterial);
                    foreach(var cell in cells)
                    {
                        Vector3 center=CellPoint(cell.x,cell.y,facing,0);
                        Assert.That(battle.DefenderSkillContains(defender,center),Is.True,item.Id+" cell center "+cell);
                    }
                    var baseline=Capture(camera,target,read);
                    var native=new GameObject("Original native field");native.transform.SetParent(caseRoot.transform,false);
                    layer=new OriginalNativeFxLayer(native.transform);layer.Begin(SampleAge,camera);
                    if(item.Id=="D33")ShowProductionFlightLine(native,layer,battle,defender,camera);
                    else Assert.That(layer.TryShow(item.Key,"footprint/"+item.Id,SampleAge,
                        new OriginalNativeFxPose(Origin,Quaternion.Euler(90,0,0),facing),true),Is.True,item.Key);
                    layer.End();SetLayer(native.transform);
                    // No native geometry/texture/width/gradient is replaced by the
                    // overlay. It is a separate diagnostic renderer shown later.
                    var shown=Capture(camera,target,read);int pixels=ChangedPixels(baseline,shown);
                    var renderers=native.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                    var lines=native.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.enabled&&l.gameObject.activeInHierarchy).ToArray();
                    var meshes=native.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.gameObject.activeInHierarchy&&m.sharedMesh!=null).ToArray();
                    foreach(var mesh in meshes)Assert.That(Finite(mesh.sharedMesh.bounds.min)&&Finite(mesh.sharedMesh.bounds.max),Is.True,item.Id+" native mesh bounds");
                    foreach(var line in lines)for(int i=0;i<line.positionCount;i++)Assert.That(Finite(line.GetPosition(i)),Is.True,item.Id+" source line vertex "+i);
                    var entry=new Entry{id=item.Id,key=item.Key,cells=cells.ToArray(),facing=facing,changedPixels=pixels,renderers=renderers.Length,
                        particleCount=native.GetComponentsInChildren<ParticleSystem>(true).Sum(p=>p.particleCount),
                        lineVertices=lines.Sum(l=>l.positionCount),meshVertices=meshes.Sum(m=>m.sharedMesh.vertexCount),orthographicSize=camera.orthographicSize,
                        lineDriver=item.Id=="D33"?"Production DefenderSkillVfxView.PresentFlightRange":"OriginalNativeFxLayer.TryShow"};
                    evidence.effects.Add(entry);
                    if(output!=null)
                    {
                        File.WriteAllBytes(Path.Combine(output,item.Id+"-original-grid.png"),read.EncodeToPNG());
                        var overlay=new GameObject("Declared gameplay cells boundary overlay");overlay.transform.SetParent(caseRoot.transform,false);
                        DrawBoundary(overlay.transform,cells,facing,outlineMaterial);Capture(camera,target,read);
                        File.WriteAllBytes(Path.Combine(output,item.Id+"-cells-overlay.png"),read.EncodeToPNG());
                    }
                    Debug.Log("DEFENDER_FOOTPRINT_GPU "+item.Id+" visiblePixels="+pixels+" cells="+cells.Count+" lineVertices="+entry.lineVertices+" shapeMatch=MANUAL_REVIEW");
                    // Write failure evidence as well, when explicitly requested.
                    if(output!=null)File.WriteAllText(Path.Combine(output,"diagnostic.json"),JsonUtility.ToJson(evidence,true));
                    Assert.That(renderers,Is.Not.Empty,item.Id+" has actual enabled native renderers.");
                    Assert.That(pixels,Is.GreaterThan(8),item.Id+" has no visible native pixels against its isolated grid baseline.");
                    if(item.Id=="D33")Assert.That(entry.lineVertices,Is.GreaterThan(4),"Production flight range must set actual source LineRenderer vertices.");
                    layer.Dispose();layer=null;caseRoot.SetActive(false);Object.Destroy(caseRoot);caseRoot=null;yield return null;
                }
            }
            finally
            {
                layer?.Dispose();if(caseRoot!=null)caseRoot.SetActive(false);if(root!=null){root.SetActive(false);Object.Destroy(root);}
                RenderTexture.active=priorActive;if(target!=null){target.Release();Object.Destroy(target);}if(read!=null)Object.Destroy(read);
                if(gridMaterial!=null)Object.Destroy(gridMaterial);if(outlineMaterial!=null)Object.Destroy(outlineMaterial);Time.timeScale=priorTime;
            }
        }

        static ManualBattle Fixture(Case item)
        {
            var skill=new DefenderSkillSettings(item.Kind,1,1,1,30,effectRadius:6,targets:3);
            var guard=new DefenderSettings(item.Id,item.Id,item.Id,Origin,100000,20,0,8,2,windup:.3f,actionDuration:1,
                skill:skill,relayPosition:Origin);
            var combat=new EngagementSettings(unitAttack:0,unitRange:.1f,unitInterval:100,canAttack:false,hasSkill:false,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0);
            var battle=new ManualBattle(new ManualBattleSettings("footprint-target","footprint-target",30,1,12,1,.1f,100000,.001f,1,1,1,1),
                new[]{Origin+Vector3.right*.25f,Origin+Vector3.right*30},Origin,combat,roster:new[]{guard});
            battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);
            // Wang can naturally place its first chess piece before the ready
            // skill. Let that real action finish rather than forcing a cast/SP.
            for(int i=0;i<60&&battle.Defender.SkillCasts==0;i++)battle.Advance(.05f);
            Assert.That(battle.Defender.SkillCasts,Is.EqualTo(1),item.Id+" fixture must capture a real skill-facing event.");
            return battle;
        }
        static void ShowProductionFlightLine(GameObject root,OriginalNativeFxLayer layer,ManualBattle battle,DefenderState defender,Camera camera)
        {
            var director=root.AddComponent<DefenderSkillVfxView>();director.enabled=false;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var field=typeof(DefenderSkillVfxView).GetField("original",flags);
            var method=typeof(DefenderSkillVfxView).GetMethod("PresentFlightRange",flags);
            Assert.That(field,Is.Not.Null);Assert.That(method,Is.Not.Null);
            field.SetValue(director,layer);method.Invoke(director,new object[]{battle,defender,camera,SampleAge});
        }
        static Material LineMaterial(Shader shader,int queue)
        {
            var material=new Material(shader){renderQueue=queue};material.SetTexture("_MainTex",Texture2D.whiteTexture);
            material.SetColor("_TintColor",new Color(.5f,.5f,.5f,.5f));material.SetFloat("_ZTest",8);
            material.SetFloat("_PremultiplyAlpha",5);return material;
        }
        static void Line(Transform parent,Vector3 from,Vector3 to,Material material,Color color,float width)
        {
            var line=new GameObject("Diagnostic line").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.gameObject.layer=Layer;
            line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;line.SetPosition(0,from);line.SetPosition(1,to);
            line.widthMultiplier=width;line.startColor=line.endColor=color;line.alignment=LineAlignment.View;
        }
        static void DrawGrid(Transform root,Bounds bounds,Material material)
        {
            int left=Mathf.FloorToInt(bounds.min.x-Origin.x)-1,right=Mathf.CeilToInt(bounds.max.x-Origin.x)+1;
            int down=Mathf.FloorToInt(bounds.min.z-Origin.z)-1,up=Mathf.CeilToInt(bounds.max.z-Origin.z)+1;
            var color=new Color(.2f,.21f,.23f,1);
            for(int x=left;x<=right;x++)Line(root,Origin+new Vector3(x-.5f,-.03f,down-.5f),Origin+new Vector3(x-.5f,-.03f,up-.5f),material,color,.012f);
            for(int z=down;z<=up;z++)Line(root,Origin+new Vector3(left-.5f,-.03f,z-.5f),Origin+new Vector3(right-.5f,-.03f,z-.5f),material,color,.012f);
            Line(root,Origin+new Vector3(-.15f,-.02f,0),Origin+new Vector3(.15f,-.02f,0),material,Color.magenta,.025f);
            Line(root,Origin+new Vector3(0,-.02f,-.15f),Origin+new Vector3(0,-.02f,.15f),material,Color.magenta,.025f);
        }
        static Vector3 CellPoint(float col,float row,Vector3 facing,float height)
            =>Origin+facing*col+new Vector3(-facing.z,0,facing.x)*row+Vector3.up*height;
        static Bounds CellBounds(IReadOnlyList<Vector2Int> cells,Vector3 facing)
        {
            var bounds=new Bounds(CellPoint(cells[0].x,cells[0].y,facing,0),Vector3.zero);
            foreach(var cell in cells)foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})bounds.Encapsulate(CellPoint(cell.x+x*.5f,cell.y+y*.5f,facing,0));
            return bounds;
        }
        static void DrawBoundary(Transform root,IReadOnlyList<Vector2Int> cells,Vector3 facing,Material material)
        {
            var occupied=new HashSet<Vector2Int>(cells);var color=new Color(.5f,1,.15f,1);
            foreach(var c in cells)
            {
                void Edge(Vector2Int neighbour,float x1,float y1,float x2,float y2)
                {if(!occupied.Contains(c+neighbour))Line(root,CellPoint(c.x+x1,c.y+y1,facing,.08f),CellPoint(c.x+x2,c.y+y2,facing,.08f),material,color,.035f);}
                Edge(Vector2Int.down,-.5f,-.5f,.5f,-.5f);Edge(Vector2Int.right,.5f,-.5f,.5f,.5f);
                Edge(Vector2Int.up,.5f,.5f,-.5f,.5f);Edge(Vector2Int.left,-.5f,.5f,-.5f,-.5f);
            }
        }
        static void SetLayer(Transform node){node.gameObject.layer=Layer;foreach(Transform child in node)SetLayer(child);}
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
        static Color32[] Capture(Camera camera,RenderTexture target,Texture2D read)
        {
            camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,Size,Size),0,0);read.Apply();return read.GetPixels32();
        }
        static int ChangedPixels(Color32[] baseline,Color32[] shown)
        {int count=0;for(int i=0;i<shown.Length;i++)if(Math.Abs(shown[i].r-baseline[i].r)+Math.Abs(shown[i].g-baseline[i].g)+Math.Abs(shown[i].b-baseline[i].b)>12)count++;return count;}
        static string EvidenceDirectory()
        {
            var args=Environment.GetCommandLineArgs();string path=null;
            for(int i=0;i<args.Length;i++)
            {
                if(args[i].StartsWith("-defenderFootprintDir=",StringComparison.Ordinal))path=args[i].Substring("-defenderFootprintDir=".Length);
                if(args[i]=="-defenderFootprintDir"){Assert.That(i+1,Is.LessThan(args.Length));path=args[++i];}
            }
            if(path==null)return null;Assert.That(string.IsNullOrWhiteSpace(path),Is.False);
            path=Path.GetFullPath(path);Directory.CreateDirectory(path);return path;
        }
    }
}
