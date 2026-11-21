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
    public static class FormationCanyonTools
    {
        public const string ArtPath = "Assets/Art/Environment/FormationCanyon";
        internal const string DressingName = "Formation Canyon · Guard Protocol 02";
        const float FloorHeight = .12f;
        const float WaterDepth = -.30f;
        static readonly RectInt Basin = new RectInt(12,1,2,1);
        static readonly RectInt Ridge = new RectInt(15,4,2,1);
        static readonly string[] Road = { "S_Ground_tiles_wilds_133322", "S_Ground_tiles_wilds_133301", "S_Ground_tiles_wilds_133316" };
        static readonly string[] Cracked = { "S_Ground_hide01_850", "S_Ground_hide01_853", "S_Ground_hide01_1479" };
        static readonly string[] Sandstone = { "S_Ground_out_14_2754", "S_Ground_out_14_2763", "S_Ground_out_14_2782", "S_Ground_out_14_2756" };
        static readonly string[] Cliff = { "S_Outworld_rock_678", "S_Outworld_rock_680", "S_Outworld_rock_683", "S_Outworld_rock_686" };
        const string Planks = "S_Outworld_out_2771";
        const string Stone = "S_Outworld_stone_542";
        const string Water = "S_outworld_water_44";
        const string High = "S_outworld_highground218";
        const string Cactus = "S_Outworld_out_2778";

        [Serializable] sealed class Catalog { public Part[] parts; }
        [Serializable] sealed class Part { public string name, sourceName; public float[] vertices, normals, uv; public Surface[] surfaces; }
        [Serializable] sealed class Surface { public string material; public int[] triangles; }

        [MenuItem("Yesterday Convoy/Formation Battle/Author Formation Canyon")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(index => SceneManager.GetSceneAt(index).isDirty))
                throw new InvalidOperationException("Save open scenes and stop Play before authoring the canyon.");
            var scene = EditorSceneManager.OpenScene(FormationBattleTools.ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var controller = Object.FindAnyObjectByType<FormationBattleController>();
            var courtyard = roots.Single(root => root.name==FormationNativeMapTools.RootName).transform;
            var tiles = roots.Single(root => root.name.StartsWith("Authored rectangle",StringComparison.Ordinal)).transform;
            var terrain = controller.terrain.transform;
            var shelf = roots.SelectMany(root=>root.GetComponentsInChildren<Transform>())
                .Single(node=>node.name=="Front reward shelf");
            var shelfBounds = shelf.GetComponent<MeshRenderer>().bounds;
            shelf.SetParent(null,true);
            foreach (var root in roots.Where(root=>root.name==DressingName || root.name=="Reference industrial dressing" ||
                root.name=="Industrial service deck backdrop"))
                Object.DestroyImmediate(root);
            foreach (var name in new[] {"Original LS-5 perimeter parts","Foreground moved outward for deployment visibility"})
                if (courtyard.Find(name) is Transform old && old!=null) Object.DestroyImmediate(old.gameObject);
            foreach (var child in terrain.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);

            var art = new Dressing(new GameObject(DressingName).transform);
            shelf.SetParent(art.Root,true);
            art.Fit(shelf,Sandstone[1],Top(shelfBounds),shelfBounds.size);
            foreach (var edge in courtyard.Find("Original LS-5 raised perimeter").GetComponentsInChildren<MeshRenderer>())
                art.Fit(edge.transform,art.Pick(Sandstone),Top(edge.bounds),edge.bounds.size);

            for (int row=0;row<FormationBattleModel.Height;row++)
                for (int column=0;column<FormationBattleModel.Width;column++)
                {
                    var cell=new Vector2Int(column,row);
                    var tile=tiles.Find($"Tile {column:00},{row:00}");
                    string source=art.Floor(cell);
                    art.Fit(tile,source,Point(column,row),new Vector3(1,.17f,1));
                    tile.GetComponent<MeshRenderer>().enabled=!Basin.Contains(cell);
                }

            controller.terrain.regions = new[]
            {
                new FormationBattleTerrainRegion(new RectInt(17,1,2,2),.48f,true),
                new FormationBattleTerrainRegion(new RectInt(18,5,2,2),.48f,true),
                new FormationBattleTerrainRegion(Basin,WaterDepth,false),
                new FormationBattleTerrainRegion(Ridge,.36f,false)
            };
            foreach (var region in controller.terrain.regions.Where(region=>region.allowDefender))
                for (int col=region.cells.xMin;col<region.cells.xMax;col++)
                    for (int row=region.cells.yMin;row<region.cells.yMax;row++)
                        art.Add(terrain,"Native high station",High,Point(col,row,FloorHeight+region.height),
                            new Vector3(1,region.height+.11f,1));
            art.Landscape(terrain);
            art.Root.gameObject.AddComponent<FormationCanyonWater>().Bind(controller,art.Surfaces.ToArray());
            var sun=Object.FindObjectsByType<Light>().Single(light=>light.type==LightType.Directional);
            sun.name="Canyon reflected sunlight";
            sun.color=new Color(1,.94f,.84f);
            sun.intensity=.87f;
            sun.shadowStrength=.70f;
            sun.transform.rotation=Quaternion.LookRotation(new Vector3(.30f,-1,-.37f));
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.40f,.43f,.48f);
            RenderSettings.ambientEquatorColor=new Color(.31f,.32f,.33f);
            RenderSettings.ambientGroundColor=new Color(.24f,.22f,.17f);
            RenderSettings.ambientIntensity=1;
            controller.terrain.BuildLayout();
            FormationMapTools.BindCanyon(scene, controller.maps);
            EditorUtility.SetDirty(controller.terrain);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save Formation Canyon.");
            Validate();
            Debug.Log("FORMATION_CANYON_AUTHOR_PASS");
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Validate Formation Canyon")]
        public static void Validate()
        {
            FormationNativeMapTools.ValidateSavedGeometry(renderer=>renderer.sharedMaterials.All(material=>
                material.shader.name=="YesterdayConvoy/Formation Canyon Surface"),ArtPath+"/");
            var controller=Object.FindAnyObjectByType<FormationBattleController>();
            var layout=controller.terrain.BuildLayout();
            bool WalkRow(int row)=>layout.CanTravel(new Vector2(10,row),new Vector2(21,row));
            if (!WalkRow(0)||!WalkRow(3)||!layout.CanTravel(new Vector2(14,0),new Vector2(14,3))||
                layout.CanTravel(new Vector2(11,1),new Vector2(16,1)))
                throw new InvalidOperationException("Canyon must leave the banks and the bridge open while blocking the recessed basin.");
            if (GameObject.Find(DressingName)?.GetComponent<FormationCanyonWater>()==null)
                throw new InvalidOperationException("Canyon water motion is not saved.");
            for (int row=Basin.yMin;row<Basin.yMax;row++)
                for (int column=Basin.xMin;column<Basin.xMax;column++)
                    if (GameObject.Find($"Tile {column:00},{row:00}").GetComponent<MeshRenderer>().enabled ||
                        layout.CanPlaceDefender(new Vector2(column,row)))
                        throw new InvalidOperationException("Canyon basin height or occupation does not match the saved mesh.");
            Debug.Log("FORMATION_CANYON_VALIDATE_PASS");
        }

        sealed class Dressing
        {
            readonly Dictionary<string,Part> parts;
            readonly Dictionary<string,Mesh> meshes;
            readonly Dictionary<string,Material> materials;
            readonly System.Random random=new System.Random(14831);
            public readonly Transform Root;
            public readonly List<Renderer> Surfaces=new List<Renderer>();

            public Dressing(Transform root)
            {
                Root=root;
                var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(ArtPath+"/source.json"));
                parts=catalog.parts.ToDictionary(part=>part.name);
                meshes=catalog.parts.ToDictionary(part=>part.name,ImportMesh);
                materials=catalog.parts.SelectMany(part=>part.surfaces).Select(slot=>slot.material)
                    .Distinct().ToDictionary(name=>name,ImportMaterial);
            }
            public string Floor(Vector2Int cell)
            {
                int col=cell.x,row=cell.y;
                if (col==14&&row>=1&&row<=2) return Planks;
                if (col<12&&row==2 || col>=12&&row==3 || (col==10||col==11)&&row==3) return Pick(Road);
                if (row>5&&col%6<4 || row<=1&&col%7<4 || col>=4&&col<=6&&row>=3&&row<=4) return Pick(Cracked);
                return Pick(Sandstone);
            }
            public void Landscape(Transform terrain)
            {
                AddWater(terrain,"Native basin water",Basin.xMin+(Basin.width-1)*.5f,Basin.yMin,FloorHeight+WaterDepth,2.02f,1.02f);
                AddWater(Root,"Native canyon river",10.5f,2.5f,-.79f,83,62);
                for(int col=Basin.xMin;col<Basin.xMax;col++)
                {
                    Add(terrain,"Native bank lip",Pick(Sandstone),Point(col,.55f,.088f),new Vector3(1.01f,.31f,.19f));
                    Add(terrain,"Native bank lip",Pick(Sandstone),Point(col,1.46f,.093f),new Vector3(1.01f,.29f,.19f));
                }
                foreach(var (col,row,width,depth,top) in new[]
                {
                    (11.78f,.72f,.47f,.42f,.15f),(12.09f,1.27f,.55f,.40f,.10f),
                    (13.07f,.64f,.45f,.33f,.12f),(13.23f,.74f,.45f,.34f,.065f),(13.29f,1.40f,.45f,.38f,.13f)
                })
                    Add(terrain,"Native shoreline stone",Stone,Point(col,row,top),new Vector3(width,.24f,depth),Range(-42,42));
                for(int col=Ridge.xMin;col<Ridge.xMax;col++)
                    Add(terrain,"Native low outcrop",Stone,Point(col,Ridge.yMin,FloorHeight+.36f),new Vector3(.98f,.36f,.93f),Range(-12,12));
                foreach(var (col,row) in new[] {(12.15f,.72f),(12.41f,.68f),(13.19f,1.38f),(11.93f,1.22f),(13.25f,.75f)})
                    Plant("Reed patch",col,row,.015f,Range(.38f,.60f));
                foreach(var (col,row) in new[] {(12.4f,5.58f),(15.5f,6.65f),(19.53f,4.4f),(20.16f,6.38f),
                    (10.25f,7.52f),(20.8f,7.46f),(4.45f,5.46f),(6.48f,1.43f),(8.5f,7.47f)})
                    Plant(random.Next(2)==0?Cactus:"Low grass",col,row,FloorHeight,Range(.42f,.68f));
                foreach(float center in new[]{-.8f,2,5.6f,8.85f,11.7f,15,18.2f,21.6f,24.8f})
                {
                    Add(Root,"Native canyon wall",Pick(Cliff),Point(center,9.78f+Range(-.1f,.55f),2.28f+Range(-.27f,.54f)),
                        new Vector3(Range(3,4.2f),Range(3,4.2f),Range(3,4.3f)),Range(-30,26));
                    Add(Root,"Native rear cliff",Pick(Cliff),Point(center,10.68f,3.15f+Range(-.12f,.42f)),
                        new Vector3(3.9f,4.38f,3.7f),Range(-31,31));
                }
                foreach(var (col,row) in new[]{(22.1f,-1.54f),(22.8f,.54f),(22.95f,2.67f),(22.84f,5.22f),(-1.02f,2f),(-1.06f,5.4f)})
                    Add(Root,"Native canyon shoulder",Pick(Cliff),Point(col,row,.59f),new Vector3(2.3f,2.8f,3),Range(-25,26));
                foreach(var (col,row) in new[]{(13f,-1.4f),(17.1f,-1.31f),(13.88f,7.82f),(16.7f,8.02f),
                    (21.5f,7.81f),(22.5f,1.8f)})
                    Add(Root,"Native fallen rock",Stone,Point(col,row,row>0?.31f:-.06f),new Vector3(1.25f,.37f,1.02f),Range(-40,40));
                Add(Root,"Native rear sand bank","S_Outworld_plane_188",Point(12,14.2f,-.63f),new Vector3(35,.35f,6.4f));
            }
            void Plant(string id,float column,float row,float ground,float scale)
            {
                var source=meshes[id].bounds.size*scale;
                Add(Root,parts[id].sourceName,id,Point(column,row,ground+source.y),source,Range(-40,40));
            }
            void AddWater(Transform parent,string name,float column,float row,float height,float width,float depth)
            {
                var surface=Add(parent,name,Water,Point(column,row,height),new Vector3(width,0,depth));
                surface.shadowCastingMode=ShadowCastingMode.Off;
                Surfaces.Add(surface);
            }
            public MeshRenderer Add(Transform parent,string name,string id,Vector3 position,Vector3 size,float yaw=0)
            {
                var node=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)).transform;
                node.SetParent(parent,false);
                Fit(node,id,position,size,yaw);
                return node.GetComponent<MeshRenderer>();
            }
            public void Fit(Transform node,string id,Vector3 top,Vector3 size,float yaw=0)
            {
                var mesh=meshes[id];
                var bounds=mesh.bounds.size;
                node.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=node.GetComponent<MeshRenderer>();
                renderer.sharedMaterials=parts[id].surfaces.Select(surface=>materials[surface.material]).ToArray();
                renderer.lightmapIndex=-1;
                node.SetPositionAndRotation(top,Quaternion.Euler(0,yaw,0));
                node.localScale=new Vector3(size.x/bounds.x,bounds.y>.001f?size.y/bounds.y:1,size.z/bounds.z);
            }
            public string Pick(string[] choices)=>choices[random.Next(choices.Length)];
            float Range(float min,float max)=>min+(max-min)*(float)random.NextDouble();
            static Mesh ImportMesh(Part part)
            {
                var mesh=new Mesh {name=part.sourceName};
                mesh.vertices=Enumerable.Range(0,part.vertices.Length/3).Select(index=>
                    new Vector3(part.vertices[index*3],part.vertices[index*3+1],part.vertices[index*3+2])).ToArray();
                mesh.normals=Enumerable.Range(0,part.normals.Length/3).Select(index=>
                    new Vector3(part.normals[index*3],part.normals[index*3+1],part.normals[index*3+2])).ToArray();
                mesh.uv=Enumerable.Range(0,part.uv.Length/2).Select(index=>
                    new Vector2(part.uv[index*2],part.uv[index*2+1])).ToArray();
                mesh.subMeshCount=part.surfaces.Length;
                for(int slot=0;slot<part.surfaces.Length;slot++)mesh.SetTriangles(part.surfaces[slot].triangles,slot);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return Save(mesh,part.name.Replace(' ','_')+".asset");
            }
            static Material ImportMaterial(string name)
            {
                bool water=name.EndsWith("_water",StringComparison.Ordinal);
                var shader=Shader.Find(water?"YesterdayConvoy/Formation Canyon Water":"YesterdayConvoy/Formation Canyon Surface");
                if(shader==null)throw new InvalidOperationException("Formation Canyon shader not imported.");
                var mat=new Material(shader){name=name};
                if(water)
                {
                    mat.mainTexture=Texture("TX_AutochessSand_shuidi_A");
                    mat.SetTexture("_BumpMap",Texture("TX_AutochessSand_shuidi_N",true));
                }
                else if(name.EndsWith("_grass",StringComparison.Ordinal))
                {
                    mat.mainTexture=Texture("TX_AutochessSand_Grass_A");
                    mat.SetFloat("_Cutoff",.48f);
                    mat.SetFloat("_Fill",.32f);
                    mat.renderQueue=(int)RenderQueue.AlphaTest;
                }
                else mat.mainTexture=Texture(name.EndsWith("_sand",StringComparison.Ordinal)?"TX_AutochessSand_sand_A":"TX_AutochessSand_A");
                return Save(mat,name+".mat");
            }
            static Texture2D Texture(string name,bool normal=false)
            {
                string path=ArtPath+"/Textures/"+name+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter ??
                    throw new InvalidOperationException("Missing original texture "+name);
                var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                if(importer.textureType!=type||importer.maxTextureSize!=4096)
                {
                    importer.textureType=type;
                    importer.maxTextureSize=4096;
                    importer.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            static Asset Save<Asset>(Asset fresh,string name) where Asset:Object
            {
                var path=ArtPath+"/"+name;
                var current=AssetDatabase.LoadAssetAtPath<Asset>(path);
                if(current==null){AssetDatabase.CreateAsset(fresh,path);return fresh;}
                EditorUtility.CopySerialized(fresh,current);
                Object.DestroyImmediate(fresh);
                return current;
            }
        }
        static Vector3 Top(Bounds bounds)=>new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
        static Vector3 Point(float column,float row,float height=FloorHeight)
        {
            var position=FormationBattleController.WorldPoint(new Vector2(column,row));
            position.y=height;
            return position;
        }
    }
}
