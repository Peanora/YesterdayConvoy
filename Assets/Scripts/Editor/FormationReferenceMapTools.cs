using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Explicit scene authoring. Play creates actors, never this saved environment.
    public static class FormationReferenceMapTools
    {
        const string TerrainRoot = "Defender terrain";
        const string DressingRoot = "Reference industrial dressing";
        const string ArtPath = FormationBoardMaterialTools.ArtPath;
        static Material concrete, raised, trim, recess, amber, lamp;
        static Mesh bevel;

        [MenuItem("Yesterday Convoy/Formation Battle/Apply Approved Concrete Battlefield")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save dirty scenes first.");
            // The existing native importer retains source meshes, gates and their provenance.
            FormationNativeMapTools.Apply();
            var scene = SceneManager.GetActiveScene();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == TerrainRoot || g.name == DressingRoot).ToArray())
                Object.DestroyImmediate(old);
            var controller = Object.FindAnyObjectByType<FormationBattleController>();
            concrete = FormationBoardMaterialTools.GetConcreteMaterial();
            raised = FormationBoardMaterialTools.GetConcreteMaterial(true);
            trim = FormationBoardMaterialTools.GetTrimMaterial();
            recess = Solid("Industrial recess", new Color(.105f,.112f,.118f), .09f, .15f);
            amber = Solid("Worn safety ochre", new Color(.48f,.32f,.12f), .19f, .25f);
            lamp = Solid("Warm strip lamp", new Color(.92f,.66f,.31f), .15f, 0, true);
            bevel = BeveledMesh();

            // Original white ledge geometry becomes a restrained grey service band.
            var native = scene.GetRootGameObjects().Single(g => g.name == FormationNativeMapTools.RootName);
            var rim = native.GetComponentsInChildren<Transform>().Single(t => t.name == "Original LS-5 raised perimeter");
            foreach (var renderer in rim.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => raised).ToArray();
            foreach(var material in native.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Distinct())
                if(material.HasProperty("_ConcreteMode")&&material.GetFloat("_ConcreteMode")<.5f)
                {
                    material.SetColor("_CheckerTint",new Color(.78f,.78f,.78f,1));
                    material.SetFloat("_ReflectionGain",.38f);EditorUtility.SetDirty(material);
                }

            var terrainObject = new GameObject(TerrainRoot);
            var authoring = terrainObject.AddComponent<FormationBattleTerrainAuthoring>();
            authoring.regions = new[]
            {
                new FormationBattleTerrainRegion { cells = new RectInt(FormationBattleModel.Width-5,1,2,2), height=.48f, allowDefender=true },
                new FormationBattleTerrainRegion { cells = new RectInt(FormationBattleModel.Width-4,FormationBattleModel.Height-3,2,2), height=.48f, allowDefender=true },
                new FormationBattleTerrainRegion { cells = new RectInt(FormationBattleModel.Width-10,1,2,1), height=.36f, allowDefender=false },
                new FormationBattleTerrainRegion { cells = new RectInt(FormationBattleModel.Width-7,4,2,1), height=.36f, allowDefender=false }
            };
            controller.terrain = authoring;
            foreach (var region in authoring.regions)
                if (region.allowDefender) Platform(terrainObject.transform, region);
                else Barricade(terrainObject.transform, region);

            Dress(new GameObject(DressingRoot).transform);
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l => l.type == LightType.Directional);
            sun.name = "Warm industrial daylight";
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-.62f,-1.2f,-.72f));
            sun.color = new Color(1,.955f,.88f); sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f;
            sun.shadowBias = .025f; sun.shadowNormalBias = .08f;
            // Source ambient remains available for the imported industrial materials. The new
            // concrete branch has neutral diffuse fill and does not inherit old corridor baking.
            RenderSettings.fog = false;
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(authoring);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Validate(); Capture();
            Debug.Log("FORMATION_REFERENCE_AUTHOR_PASS: saved warm-grey concrete board, two raised platforms, two barricades, industrial perimeter and full-cell gates");
        }

        static Vector3 Center(RectInt cells) => FormationBattleController.WorldPoint(new Vector2(cells.x + (cells.width-1)*.5f, cells.y + (cells.height-1)*.5f));

        static void Platform(Transform parent, FormationBattleTerrainRegion region)
        {
            var root = new GameObject("Raised firing platform " + region.cells.position).transform; root.SetParent(parent,false);
            var p = Center(region.cells); float top = p.y + region.height;
            Box(root,"Beveled steel platform foundation",p + Vector3.up*(region.height*.45f),new Vector3(1.98f,region.height*.9f,1.98f),trim);
            var source = FormationNativeAssetTools.FindPartName("S_Ground_tiles_Qcity_0275");
            for (int x=region.cells.xMin;x<region.cells.xMax;x++) for(int row=region.cells.yMin;row<region.cells.yMax;row++)
            {
                var tile = FormationNativeAssetTools.CreatePart(source,root);
                tile.name = "Saved concrete platform surface " + x + "," + row;
                tile.transform.position = FormationBattleController.WorldPoint(new Vector2(x,row)) + Vector3.up*(region.height+.0200154f);
                tile.GetComponent<MeshRenderer>().sharedMaterials = source.submeshes.Select(_=>raised).ToArray();
            }
            foreach(int side in new[]{-1,1})
            {
                Box(root,"Steel cap long edge",new Vector3(p.x,top-.015f,p.z+side*.972f),new Vector3(2.04f,.055f,.055f),trim);
                Box(root,"Steel cap end edge",new Vector3(p.x+side*.972f,top-.015f,p.z),new Vector3(.055f,.055f,2.04f),trim);
                Box(root,"Recessed side seam",new Vector3(p.x,top-.19f,p.z+side*.994f),new Vector3(1.72f,.045f,.018f),recess);
                for(int end=-1;end<=1;end+=2)
                {
                    Box(root,"Reinforced corner plate",new Vector3(p.x+end*.91f,p.y+region.height*.43f,p.z+side*.996f),new Vector3(.11f,region.height*.78f,.035f),trim);
                    Box(root,"Small edge fastener",new Vector3(p.x+end*.87f,top+.005f,p.z+side*.87f),new Vector3(.045f,.022f,.045f),trim);
                }
            }
        }

        static void Barricade(Transform parent, FormationBattleTerrainRegion region)
        {
            var root = new GameObject("Low industrial barricade " + region.cells.position).transform; root.SetParent(parent,false);
            var p=Center(region.cells); float h=region.height;
            Box(root,"Low weighted base",p+Vector3.up*.045f,new Vector3(1.94f,.09f,.88f),recess);
            Box(root,"Chamfered concrete barrier core",p+Vector3.up*(h*.51f),new Vector3(1.85f,h*.92f,.75f),raised);
            foreach(int side in new[]{-1,1})
            {
                Box(root,"Inset front armor panel",p+new Vector3(0,h*.5f,side*.385f),new Vector3(1.36f,h*.64f,.025f),trim);
                Box(root,"Top metal shoulder",p+new Vector3(side*.78f,h-.015f,0),new Vector3(.24f,.07f,.79f),trim);
                Beam(root,"Sloped reinforcement",p+new Vector3(side*.43f,.04f,-.415f),p+new Vector3(side*.76f,h-.025f,-.415f),.055f,trim);
                Box(root,"End impact protector",p+new Vector3(side*.91f,h*.47f,0),new Vector3(.11f,h*.9f,.83f),trim);
            }
            Box(root,"Thin top worn concrete strip",p+Vector3.up*(h+.002f),new Vector3(1.2f,.015f,.43f),concrete);
        }

        static void Dress(Transform root)
        {
            float extra=FormationBattleModel.Width-14,halfExtra=extra*.5f;
            float halfDepth=FormationBattleModel.Height*.5f;
            float seam=FormationBattleController.WorldPoint(new Vector2(FormationBattleModel.BattleAreaStart,0)).x;
            // Save the two board structures as separate renderers. A single broad slab would
            // remain shared by both fixed views and reveal the hidden half as an empty platform.
            void ViewBox(string name,Vector3 position,Vector3 size,Material material)
            {
                float left=position.x-size.x*.5f,right=position.x+size.x*.5f;
                if(left>=seam||right<=seam){Box(root,name,position,size,material);return;}
                var leftSize=new Vector3(seam-left,size.y,size.z);
                var rightSize=new Vector3(right-seam,size.y,size.z);
                Box(root,name+" / left view",new Vector3((left+seam)*.5f,position.y,position.z),leftSize,material);
                Box(root,name+" / right view",new Vector3((seam+right)*.5f,position.y,position.z),rightSize,material);
            }
            void ViewBeam(string name,Vector3 from,Vector3 to,float width,Material material)
            {
                if((from.x<seam&&to.x>seam)||(to.x<seam&&from.x>seam))
                {
                    var cut=Vector3.Lerp(from,to,(seam-from.x)/(to.x-from.x));
                    Beam(root,name+" / first view",from,cut,width,material);
                    Beam(root,name+" / second view",cut,to,width,material);
                }
                else Beam(root,name,from,to,width,material);
            }
            void ViewRail(Vector3 origin,float length,bool alongZ)
            {
                float left=origin.x-length*.5f,right=origin.x+length*.5f;
                if(!alongZ&&left<seam&&right>seam)
                {
                    Rail(root,new Vector3((left+seam)*.5f,origin.y,origin.z),seam-left,false);
                    Rail(root,new Vector3((seam+right)*.5f,origin.y,origin.z),right-seam,false);
                }
                else Rail(root,origin,length,alongZ);
            }
            Box(root,"Lower industrial service yard",new Vector3(0,-2.05f,0),new Vector3(70,.15f,50),trim);
            Box(root,"Front reward shelf",new Vector3(-6.65f,.75f,-5.7f),new Vector3(6.1f,.36f,1.55f),trim);
            ViewBox("Dark continuous board substructure",new Vector3(0,-.36f,0),new Vector3(16.0f+extra,.76f,FormationBattleModel.Height+1.85f),recess);
            ViewBox("Rear service gantry deck",new Vector3(0,.06f,halfDepth+1.98f),new Vector3(21+extra,.12f,2.1f),trim);
            foreach(int side in new[]{-1,1})
            {
                ViewBox("Perimeter metal edge",new Vector3(0,.10f,side*(halfDepth+.98f)),new Vector3(16.05f+extra,.13f,.11f),trim);
                ViewRail(new Vector3(0,.14f,side*(halfDepth+1.02f)),15.85f+extra,false);
                ViewRail(new Vector3(side*(8.04f+halfExtra),.14f,0),FormationBattleModel.Height+1.95f,true);
                for(int x=-7-(int)halfExtra;x<=7+(int)halfExtra;x+=2)
                {
                    ViewBox("Small recessed perimeter light",new Vector3(x,.18f,side*(halfDepth+.965f)),new Vector3(.32f,.024f,.045f),lamp);
                    ViewBox("Under-deck steel post",new Vector3(x,-.96f,side*(halfDepth+.78f)),new Vector3(.12f,1.1f,.13f),trim);
                    ViewBeam("Under-deck diagonal brace",new Vector3(x-.8f,-1.46f,side*(halfDepth+.78f)),new Vector3(x+.8f,-.45f,side*(halfDepth+.78f)),.075f,trim);
                }
                ViewBox("Long side service ledge",new Vector3(side*(9.05f+halfExtra),-.08f,0),new Vector3(1.5f,.3f,FormationBattleModel.Height+5.8f),trim);
                for(int bay=0;bay<5;bay++)
                {
                    float z=-5.5f+bay*2.8f;
                    ViewBox("Side factory panel",new Vector3(side*(10.45f+halfExtra),.65f,z),new Vector3(.22f,4.1f,2.76f),trim);
                    ViewBox("Side factory recessed panel",new Vector3(side*(10.30f+halfExtra),.6f,z),new Vector3(.08f,3.5f,2.3f),recess);
                    ViewBox("Side factory girder",new Vector3(side*(10.18f+halfExtra),.65f,z-1.27f),new Vector3(.18f,4.15f,.11f),trim);
                    ViewBeam("Side factory cross brace",new Vector3(side*(10.15f+halfExtra),-1.1f,z-1.1f),new Vector3(side*(10.15f+halfExtra),2.32f,z+1.1f),.075f,trim);
                    ViewBox("Side service lamp",new Vector3(side*(10.05f+halfExtra),.14f,z),new Vector3(.045f,.04f,.42f),lamp);
                }
            }
            // Layered wall panels and service lines keep a detailed industrial background behind
            // the native buildings instead of exposing a stretched blurry city texture.
            for(int i=-3-Mathf.CeilToInt(halfExtra/3.25f);i<=3+Mathf.CeilToInt(halfExtra/3.25f);i++)
            {
                float x=i*3.25f;
                ViewBox("Factory rear wall module",new Vector3(x,1.75f,halfDepth+3.15f),new Vector3(3.2f,4.8f,.24f),trim);
                ViewBox("Recessed factory wall panel",new Vector3(x,1.65f,halfDepth+2.995f),new Vector3(2.75f,3.65f,.08f),recess);
                ViewBox("Factory upright girder",new Vector3(x-1.53f,1.75f,halfDepth+2.92f),new Vector3(.14f,4.85f,.21f),trim);
                ViewBox("Raised service window band",new Vector3(x,3.08f,halfDepth+2.90f),new Vector3(2.5f,.58f,.06f),trim);
                for(int slit=0;slit<5;slit++)
                    ViewBox("Ventilation slit",new Vector3(x-.9f+slit*.45f,3.08f,halfDepth+2.855f),new Vector3(.025f,.51f,.018f),recess);
                ViewBox("Warm wall lamp",new Vector3(x,.73f,halfDepth+2.82f),new Vector3(.52f,.045f,.055f),lamp);
                ViewBeam("Factory panel diagonal",new Vector3(x-1.3f,-.1f,halfDepth+2.89f),new Vector3(x+1.3f,2.7f,halfDepth+2.89f),.065f,trim);
            }
            for(int pipe=0;pipe<2;pipe++)
            {
                float y=.32f+pipe*.29f;
                ViewBox("Long factory service duct",new Vector3(0,y,halfDepth+2.74f),new Vector3(23+extra,.09f,.12f),pipe==0?amber:trim);
                for(int x=-10-(int)halfExtra;x<=10+(int)halfExtra;x+=2)
                    ViewBox("Duct collar",new Vector3(x,y,halfDepth+2.74f),new Vector3(.055f,.13f,.16f),trim);
            }
        }

        static void Rail(Transform root,Vector3 origin,float length,bool alongZ)
        {
            Vector3 axis=alongZ?Vector3.forward:Vector3.right;
            Beam(root,"Thin metal guardrail",origin-axis*length*.5f+Vector3.up*.59f,origin+axis*length*.5f+Vector3.up*.59f,.035f,amber);
            Beam(root,"Lower safety rail",origin-axis*length*.5f+Vector3.up*.27f,origin+axis*length*.5f+Vector3.up*.27f,.027f,trim);
            int count=Mathf.CeilToInt(length/1.6f);
            for(int i=0;i<=count;i++)
            {
                var p=origin+axis*((i/(float)count-.5f)*length);
                Beam(root,"Handrail post",p,p+Vector3.up*.62f,.035f,trim);
            }
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<MeshFilter>().sharedMesh=bevel;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            return go.transform;
        }
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material material)
        {
            var beam=Box(parent,name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),material);
            beam.rotation=Quaternion.LookRotation(b-a);
        }
        static Material Solid(string name,Color color,float smoothness,float metallic,bool emissive=false)
        {
            string path=ArtPath+"/Materials/"+name.Replace(' ','_')+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",smoothness);m.SetFloat("_Metallic",metallic);
            if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.3f);}
            EditorUtility.SetDirty(m);return m;
        }
        static Mesh BeveledMesh()
        {
            string path=ArtPath+"/IndustrialBeveledBox.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh!=null)return mesh;
            var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();
            Vector2[] outline={new Vector2(-.44f,-.5f),new Vector2(.44f,-.5f),new Vector2(.5f,-.44f),new Vector2(.5f,.44f),new Vector2(.44f,.5f),new Vector2(-.44f,.5f),new Vector2(-.5f,.44f),new Vector2(-.5f,-.44f)};
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int k=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                indices.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
            Vector3 At(int i,float y,float scale)=>new Vector3(outline[i].x*scale,y,outline[i].y*scale);
            for(int i=0;i<8;i++)
            {
                int j=(i+1)%8;
                Quad(At(i,-.5f,.94f),At(i,-.44f,1),At(j,-.44f,1),At(j,-.5f,.94f));
                Quad(At(i,-.44f,1),At(i,.44f,1),At(j,.44f,1),At(j,-.44f,1));
                Quad(At(i,.44f,1),At(i,.5f,.94f),At(j,.5f,.94f),At(j,.44f,1));
                int k=vertices.Count;vertices.AddRange(new[]{new Vector3(0,.5f,0),At(j,.5f,.94f),At(i,.5f,.94f)});uv.AddRange(new[]{new Vector2(.5f,.5f),outline[j]+Vector2.one*.5f,outline[i]+Vector2.one*.5f});indices.AddRange(new[]{k,k+1,k+2});
            }
            mesh=new Mesh{name="Saved industrial chamfered box"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Validate Concrete Battlefield")]
        public static void Validate()
        {
            FormationNativeMapTools.Validate();
            var c=Object.FindAnyObjectByType<FormationBattleController>();
            if(c.terrain==null||c.terrain.regions.Length!=4)throw new InvalidOperationException("Expected four saved defender terrain regions.");
            if(c.terrain.regions.Count(r=>r.allowDefender)!=2)throw new InvalidOperationException("Expected two firing platforms.");
            foreach(var region in c.terrain.regions)
                if(region.cells.xMin<FormationBattleModel.DeploymentColumns+2||region.cells.xMax>FormationBattleModel.Width-1||region.cells.yMin<0||region.cells.yMax>FormationBattleModel.Height)throw new InvalidOperationException("Defender terrain must leave formation and blue-gate columns clear.");
            if(c.terrain.GetComponentsInChildren<Renderer>().Length<12)throw new InvalidOperationException("Saved terrain geometry missing.");
            Debug.Log("FORMATION_REFERENCE_VALIDATE_PASS: persistent concrete floor, four saved terrain regions, clear left formation and blue gates");
        }
        public static void Capture()
        {
            var camera=Object.FindAnyObjectByType<FormationBattleController>().battleCamera;
            var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();Directory.CreateDirectory("artifacts/formation-concrete");File.WriteAllBytes("artifacts/formation-concrete/editor-map.png",pixels.EncodeToPNG());}
            finally{camera.targetTexture=prior;RenderTexture.active=active;Object.DestroyImmediate(pixels);rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
