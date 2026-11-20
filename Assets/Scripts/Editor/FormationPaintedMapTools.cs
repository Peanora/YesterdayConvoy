using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    /// <summary>Explicit edit-time authoring of the approved painted courtyard. Never called by Play.</summary>
    public static class FormationPaintedMapTools
    {
        public const string RootName="Painted courtyard · approved 2.5D environment";
        public const string ImagePath="Assets/Art/Environment/FormationBattle/Painted/industrial-courtyard.png";
        const string Assets="Assets/Art/Environment/FormationBattle/Painted";
        const float ImageWidth=1672,ImageHeight=941;
        public static readonly Rect BattleViewport=new Rect(0,65,1280,500);
        static readonly Vector2 TopLeft=new Vector2(211,237),TopRight=new Vector2(1467,237);
        static readonly Vector2 BottomLeft=new Vector2(172,774),BottomRight=new Vector2(1507,774);
        static readonly Rect CameraViewport=new Rect(0,155f/720,1,500f/720);
        const float GroundY=.12f;

        struct Calibration
        {
            public float Pitch,FieldOfView;
            public Vector3 Position;
        }

        public static float CalibratedPitch=>SolveCamera().Pitch;

        public static void Apply()
        {
            throw new InvalidOperationException("The painted formation map has been superseded by the confirmed original LS-5 environment. Use FormationNativeMapTools.Apply.");
        }
        static void ApplyRetiredReference()
        {
            CheckEditor();
            if(!File.Exists(ImagePath))throw new InvalidOperationException("Approved courtyard image is missing: "+ImagePath);
            Directory.CreateDirectory(Assets+"/Meshes");AssetDatabase.Refresh();
            ConfigureImage();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            if(texture==null)throw new InvalidOperationException("Approved painted image did not import.");
            var scene=EditorSceneManager.OpenScene(FormationBattleTools.ScenePath,OpenSceneMode.Single);
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            if(controller==null||controller.battleCamera==null)throw new InvalidOperationException("Formation controller/camera binding missing.");
            var camera=controller.battleCamera;SetCamera(camera);

            // These are the two exact task-owned visual roots. Existing character resources are untouched.
            foreach(var old in scene.GetRootGameObjects().Where(g=>g.name==RootName||g.name=="Industrial courtyard · saved geometry").ToArray())
                Object.DestroyImmediate(old);
            var tiles=All<Transform>(scene).Where(t=>t.name.StartsWith("Tile ",StringComparison.Ordinal)).ToArray();
            if(tiles.Length!=112)throw new InvalidOperationException("Expected the original 112 editable tile objects; found "+tiles.Length);
            foreach(var renderer in All<MeshRenderer>(scene))
                if(!renderer.name.StartsWith("Tile ",StringComparison.Ordinal))renderer.gameObject.SetActive(false);

            var root=new GameObject(RootName).transform;
            var background=Material("ApprovedBackground",Shader.Find("Unlit/Texture"),texture,Color.white);
            var floor=ProjectedFloor(texture,camera);
            CreateBackground(root,camera,background);
            foreach(var tile in tiles)
            {
                ParseCell(tile.name,out int col,out int row);
                Vector3 centre=FormationBattleController.WorldPoint(new Vector2(col,row));
                tile.gameObject.SetActive(true);tile.position=centre;tile.rotation=Quaternion.identity;tile.localScale=Vector3.one;
                var vertices=new[]{new Vector3(-.5f,0,-.4f),new Vector3(-.5f,0,.4f),new Vector3(.5f,0,.4f),new Vector3(.5f,0,-.4f)};
                var uv=vertices.Select(v=>ViewportUV(camera,centre+v)).ToArray();
                var mesh=SaveMesh("Tile-"+col.ToString("00")+"-"+row.ToString("00"),vertices,uv);
                var filter=tile.GetComponent<MeshFilter>()??tile.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
                var renderer=tile.GetComponent<MeshRenderer>()??tile.gameObject.AddComponent<MeshRenderer>();renderer.enabled=true;renderer.sharedMaterial=floor;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                foreach(var collider in tile.GetComponents<Collider>())Object.DestroyImmediate(collider);
            }

            var red=Material("EntryRed",Shader.Find("Unlit/Color"),null,new Color(.59f,.19f,.16f));
            var blue=Material("ExitBlue",Shader.Find("Unlit/Color"),null,new Color(.13f,.43f,.59f));
            var footing=Material("GateFooting",Shader.Find("Unlit/Color"),null,new Color(.16f,.20f,.23f));
            var cube=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/ApprovedMaps/Meshes/UnitCube.asset");
            if(cube==null)throw new InvalidOperationException("Saved unit cube mesh is missing.");
            var gates=Group(root,"Low gate markers · visual boundaries only");
            for(int row=0;row<8;row++)
            {Gate(gates,"Red",5.5f,row,red,footing,cube);Gate(gates,"Blue",13,row,blue,footing,cube);}
            RenderSettings.fog=false;
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            Validate();
        }

        static void CheckEditor()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring painted scenery.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes before explicit map authoring.");
        }

        static void ConfigureImage()
        {
            var importer=AssetImporter.GetAtPath(ImagePath) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Courtyard PNG importer is missing.");
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
        }

        static Calibration SolveCamera()
        {
            // Solve a yaw/roll-zero perspective camera for the image's floor quadrilateral.
            // The complete image is mapped into the agreed 1280x500 battle viewport.
            double scaleX=BattleViewport.width/ImageWidth,scaleY=BattleViewport.height/ImageHeight;
            double farScale=(TopRight.x-TopLeft.x)*scaleX/14,nearScale=(BottomRight.x-BottomLeft.x)*scaleX/14;
            double centreY=BattleViewport.height*.5;
            double farViewY=(centreY-TopLeft.y*scaleY)/farScale;
            double nearViewY=(centreY-BottomLeft.y*scaleY)/nearScale;
            double sine=(farViewY-nearViewY)/6.4;
            if(sine<=0||sine>=1)throw new InvalidOperationException("Approved floor cannot be fitted by a level rectangular board.");
            double pitch=Math.Asin(sine),cosine=Math.Cos(pitch);
            double focalPixels=6.4*cosine/(1/farScale-1/nearScale);
            double depth=(focalPixels/farScale+focalPixels/nearScale)*.5,viewY=(farViewY+nearViewY)*.5;
            double cameraHeight=Math.Sin(pitch)*depth-Math.Cos(pitch)*viewY;
            double cameraZ=-Math.Cos(pitch)*depth-Math.Sin(pitch)*viewY;
            double farOffset=(TopLeft.x+TopRight.x)*.5*scaleX-BattleViewport.width*.5;
            double nearOffset=(BottomLeft.x+BottomRight.x)*.5*scaleX-BattleViewport.width*.5;
            // The source has a half-pixel left/right asymmetry: least-squares x leaves <.2 virtual px residual.
            double cameraX=-(farScale*farOffset+nearScale*nearOffset)/(farScale*farScale+nearScale*nearScale);
            return new Calibration{Pitch=(float)(pitch*180/Math.PI),FieldOfView=(float)(2*Math.Atan(BattleViewport.height*.5/focalPixels)*180/Math.PI),Position=new Vector3((float)cameraX,GroundY+(float)cameraHeight,(float)cameraZ)};
        }

        static void SetCamera(Camera camera)
        {
            var fit=SolveCamera();camera.orthographic=false;camera.usePhysicalProperties=false;
            camera.fieldOfView=fit.FieldOfView;camera.lensShift=Vector2.zero;
            camera.transform.rotation=Quaternion.Euler(fit.Pitch,0,0);camera.transform.position=fit.Position;
            camera.rect=CameraViewport;camera.aspect=BattleViewport.width/BattleViewport.height;
            camera.nearClipPlane=.1f;camera.farClipPlane=500;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.035f,.05f,.075f);
        }

        static Vector2 ViewportUV(Camera camera,Vector3 world)
        {var p=camera.WorldToViewportPoint(world);return new Vector2(p.x,p.y);}

        public static Vector2 VirtualPoint(Camera camera,Vector3 world)
        {var p=ViewportUV(camera,world);return new Vector2(BattleViewport.x+p.x*BattleViewport.width,BattleViewport.y+(1-p.y)*BattleViewport.height);}

        // Projective, not bilinear, interpolation preserves the source's widening near edge.
        public static Vector2 ReferenceGridPoint(float col,float row)
        {
            double s=(col+.5)/14,t=(7.5-row)/8;
            double dx1=TopRight.x-BottomRight.x,dx2=BottomLeft.x-BottomRight.x;
            double dx3=TopLeft.x-TopRight.x+BottomRight.x-BottomLeft.x;
            double dy1=TopRight.y-BottomRight.y,dy2=BottomLeft.y-BottomRight.y;
            double dy3=TopLeft.y-TopRight.y+BottomRight.y-BottomLeft.y;
            double denominator=dx1*dy2-dx2*dy1;
            double g=(dx3*dy2-dx2*dy3)/denominator,h=(dx1*dy3-dx3*dy1)/denominator;
            double x=((TopRight.x-TopLeft.x+g*TopRight.x)*s+(BottomLeft.x-TopLeft.x+h*BottomLeft.x)*t+TopLeft.x)/(g*s+h*t+1);
            double y=((TopRight.y-TopLeft.y+g*TopRight.y)*s+(BottomLeft.y-TopLeft.y+h*BottomLeft.y)*t+TopLeft.y)/(g*s+h*t+1);
            return new Vector2(BattleViewport.x+(float)x/ImageWidth*BattleViewport.width,BattleViewport.y+(float)y/ImageHeight*BattleViewport.height);
        }

        static void CreateBackground(Transform root,Camera camera,Material material)
        {
            float depth=Vector3.Dot(FormationBattleController.WorldPoint(new Vector2(6.5f,3.5f))-camera.transform.position,camera.transform.forward)+40;
            var vertices=new[]{camera.ViewportToWorldPoint(new Vector3(0,0,depth)),camera.ViewportToWorldPoint(new Vector3(0,1,depth)),camera.ViewportToWorldPoint(new Vector3(1,1,depth)),camera.ViewportToWorldPoint(new Vector3(1,0,depth))};
            var mesh=SaveMesh("ApprovedBackgroundPlane",vertices,new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
            var go=new GameObject("Approved image background · painted environment, not 3D buildings",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }

        static Mesh SaveMesh(string name,Vector3[] vertices,Vector2[] uv)
        {
            string path=Assets+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;
            if(create)mesh=new Mesh{name=name};else mesh.Clear();
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);return mesh;
        }

        static Material Material(string name,Shader shader,Texture texture,Color color)
        {
            if(shader==null)throw new InvalidOperationException("Missing painted map shader: "+name);
            string path=Assets+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);bool create=material==null;
            if(create)material=new Material(shader){name=name};else material.shader=shader;
            material.mainTexture=texture;if(material.HasProperty("_Color"))material.SetColor("_Color",color);material.enableInstancing=false;
            if(create)AssetDatabase.CreateAsset(material,path);else EditorUtility.SetDirty(material);return material;
        }

        static Material ProjectedFloor(Texture2D texture,Camera camera)
        {
            string path=Assets+"/ProjectedFloor.shader";
            if(!File.Exists(path)||File.ReadAllText(path)!=FloorShader){File.WriteAllText(path,FloorShader);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
            var material=Material("ApprovedProjectedFloor",AssetDatabase.LoadAssetAtPath<Shader>(path),texture,Color.white);
            Matrix4x4 projector=camera.projectionMatrix*camera.worldToCameraMatrix;
            material.SetVector("_ProjectRow0",projector.GetRow(0));material.SetVector("_ProjectRow1",projector.GetRow(1));material.SetVector("_ProjectRow3",projector.GetRow(3));
            EditorUtility.SetDirty(material);return material;
        }

        const string FloorShader=@"Shader ""YesterdayConvoy/Approved Painted Formation Floor""
{
 Properties { _MainTex(""Approved courtyard"",2D)=""white""{} _Color(""Tint"",Color)=(1,1,1,1)
 _ProjectRow0(""Saved projector row 0"",Vector)=(1,0,0,0) _ProjectRow1(""Saved projector row 1"",Vector)=(0,1,0,0) _ProjectRow3(""Saved projector row 3"",Vector)=(0,0,1,0) }
 SubShader { Tags { ""RenderType""=""Opaque"" ""Queue""=""Geometry"" }
 Pass { Cull Back ZWrite On
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include ""UnityCG.cginc""
 sampler2D _MainTex; float4 _ProjectRow0,_ProjectRow1,_ProjectRow3;
 struct Input {float4 vertex:POSITION;}; struct Output {float4 vertex:SV_POSITION;float3 paint:TEXCOORD0;};
 Output vert(Input v) {Output o;float4 world=mul(unity_ObjectToWorld,v.vertex);o.vertex=UnityObjectToClipPos(v.vertex);o.paint=float3(dot(_ProjectRow0,world),dot(_ProjectRow1,world),dot(_ProjectRow3,world));return o;}
 fixed4 frag(Output i):SV_Target {float2 uv=i.paint.xy/i.paint.z*.5+.5;return tex2D(_MainTex,uv);}
 ENDCG
 } }
 Fallback Off
}";

        static Transform Group(Transform parent,string name)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}

        static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,Mesh cube)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=cube;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }

        static void Gate(Transform parent,string color,float col,int row,Material marker,Material footing,Mesh cube)
        {
            var gate=Group(parent,color+" gate row "+row);gate.localPosition=FormationBattleController.WorldPoint(new Vector2(col,row));
            const float halfWidth=.22f,halfDepth=.195f,length=.12f,offset=0;
            foreach(float x in new[]{-halfWidth,halfWidth})foreach(float z in new[]{-halfDepth,halfDepth})
            {
                Box(gate,"Ground corner x",new Vector3(x-Mathf.Sign(x)*length*.5f,offset+.016f,z),new Vector3(length,.018f,.022f),marker,cube);
                Box(gate,"Ground corner z",new Vector3(x,offset+.016f,z-Mathf.Sign(z)*length*.5f),new Vector3(.022f,.018f,length),marker,cube);
            }
            foreach(float z in new[]{-halfDepth,halfDepth})
            {
                Box(gate,"Low side footing",new Vector3(0,offset+.018f,z),new Vector3(.075f,.036f,.075f),footing,cube);
                Box(gate,"Short coloured side marker",new Vector3(0,offset+.105f,z),new Vector3(.032f,.174f,.032f),marker,cube);
            }
        }

        static void ParseCell(string name,out int col,out int row)
        {
            var parts=name.Substring(5).Split(',');
            if(parts.Length!=2||!int.TryParse(parts[0],out col)||!int.TryParse(parts[1],out row))throw new InvalidOperationException("Invalid saved tile name: "+name);
        }

        static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();

        public static void Validate()
        {
            FormationBattleTools.Validate();
            var scene=SceneManager.GetActiveScene();var controller=Object.FindAnyObjectByType<FormationBattleController>();var camera=controller.battleCamera;
            camera.aspect=BattleViewport.width/BattleViewport.height;
            if(camera.orthographic||Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y,0))>.001f||Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.z,0))>.001f)
                throw new InvalidOperationException("Painted map requires its saved yaw/roll-zero calibrated perspective camera.");
            var root=scene.GetRootGameObjects().SingleOrDefault(g=>g.name==RootName);if(root==null)throw new InvalidOperationException("Painted courtyard root missing.");
            if(scene.GetRootGameObjects().Any(g=>g.name=="Industrial courtyard · saved geometry"&&g.activeSelf))throw new InvalidOperationException("Rejected industrial geometry remains visible.");
            var tiles=All<Transform>(scene).Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Tile ",StringComparison.Ordinal)).ToArray();
            if(tiles.Length!=112)throw new InvalidOperationException("Painted map must retain 112 independently saved floor tiles.");
            float worst=0;
            foreach(var tile in tiles)
            {
                ParseCell(tile.name,out int col,out int row);var mesh=tile.GetComponent<MeshFilter>()?.sharedMesh;var renderer=tile.GetComponent<MeshRenderer>();
                if(mesh==null||mesh.vertexCount!=4||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh))||renderer==null||!renderer.enabled||renderer.sharedMaterial?.mainTexture==null)
                    throw new InvalidOperationException("Unsaved painted floor tile: "+tile.name);
                if(Vector3.Distance(tile.position,FormationBattleController.WorldPoint(new Vector2(col,row)))>.0001f)throw new InvalidOperationException("Painted floor centre drift: "+tile.name);
                worst=Mathf.Max(worst,Vector2.Distance(VirtualPoint(camera,tile.position),ReferenceGridPoint(col,row)));
            }
            foreach(var cell in new[]{new Vector2(-.5f,-.5f),new Vector2(-.5f,7.5f),new Vector2(13.5f,-.5f),new Vector2(13.5f,7.5f)})
                worst=Mathf.Max(worst,Vector2.Distance(VirtualPoint(camera,FormationBattleController.WorldPoint(cell)),ReferenceGridPoint(cell.x,cell.y)));
            if(worst>1)throw new InvalidOperationException("Approved image / world floor projection drift: "+worst+" virtual pixels.");
            var all=root.GetComponentsInChildren<Transform>();
            foreach(string color in new[]{"Red","Blue"})for(int row=0;row<8;row++)
            {
                var gate=all.Single(t=>t.name==color+" gate row "+row);Vector3 expected=FormationBattleController.WorldPoint(new Vector2(color=="Red"?5.5f:13,row));
                if(Vector3.Distance(gate.position,expected)>.0001f||gate.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Painted gate position/collider is invalid.");
                var renderers=gate.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                if(bounds.size.y>.24f||bounds.size.z>.48f)throw new InvalidOperationException("Gate markers must remain low and separated.");
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial==null||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.sharedMaterial))||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh)))
                    throw new InvalidOperationException("Painted scenery has an unsaved mesh/material reference.");
            Debug.Log("FORMATION_PAINTED_MAP_VALIDATE_PASS: approved full-image background, 112 independent projected floor meshes, 8+8 low separated gates; pitch="+camera.transform.eulerAngles.x.ToString("F5")+"; worstProjectionPixels="+worst.ToString("F5"));
        }
    }
}
