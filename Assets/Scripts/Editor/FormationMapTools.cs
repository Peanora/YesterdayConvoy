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
    /// <summary>Bakes approved source placements into editable, scene-bound prefabs.</summary>
    public static class FormationMapTools
    {
        const string Art = "Assets/Art/Environment/FormationVariants";
        const string MapRoot = "Formation map variants";
        const float FloorHeight = .12f;
        static readonly (string folder, FormationBattleMaps.Board board)[] Maps =
        {
            ("SyracuseStreet", FormationBattleMaps.Board.Street),
            ("GildedHall", FormationBattleMaps.Board.Hall),
            ("BlueIceValley", FormationBattleMaps.Board.Ice)
        };

        [Serializable] sealed class Catalog
        {
            public string[] rows;
            public Part[] parts;
            public NativeMaterial[] materials;
            public Finish[] finishes;
            public Placement[] placements;
            public FormationGuardPositions[] guards;
        }
        [Serializable] sealed class Part { public string name; public float[] vertices, uv; public Surface[] surfaces; }
        [Serializable] sealed class Surface { public string material; public int[] indices; }
        [Serializable] sealed class NativeMaterial { public string name, atlas; public bool cutout; }
        [Serializable] sealed class Finish { public string name; public Color tint; public float mix; }
        [Serializable] sealed class Placement
        {
            public string part, finish;
            public float column, row, top, angle;
            public Vector3 size;
            public bool tile;
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Author Approved Map Variants")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount)
                .Any(index => SceneManager.GetSceneAt(index).isDirty))
                throw new InvalidOperationException("Save open scenes and stop Play before authoring the maps.");
            var scene = EditorSceneManager.OpenScene(FormationBattleTools.ScenePath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<FormationBattleController>();
            if (controller == null || controller.terrain == null)
                throw new InvalidOperationException("The default formation board must be authored first.");
            var formerRoot = scene.GetRootGameObjects().SingleOrDefault(root => root.name == MapRoot);
            if (formerRoot != null) Object.DestroyImmediate(formerRoot);
            var mapRoot = new GameObject(MapRoot);
            var maps = mapRoot.AddComponent<FormationBattleMaps>();
            controller.maps = maps;
            BindCanyon(scene, maps);
            foreach (var (folder, board) in Maps)
            {
                string directory = Art + "/" + folder;
                var builder = new MapBuilder(directory, board);
                var prefab = builder.SavePrefab();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mapRoot.transform);
                var authoring = instance.GetComponent<FormationBattleTerrainAuthoring>();
                switch (board)
                {
                    case FormationBattleMaps.Board.Street: maps.street = authoring; break;
                    case FormationBattleMaps.Board.Hall: maps.hall = authoring; break;
                    case FormationBattleMaps.Board.Ice: maps.ice = authoring; break;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the formation board bindings.");
            Validate();
        }

        internal static void BindCanyon(Scene scene, FormationBattleMaps maps)
        {
            if (maps == null) return;
            var roots = scene.GetRootGameObjects();
            var courtyard = roots.Single(root => root.name == FormationNativeMapTools.RootName).transform;
            maps.canyonScenery = new[]
            {
                roots.Single(root => root.name.StartsWith("Authored rectangle", StringComparison.Ordinal)),
                roots.Single(root => root.name == FormationCanyonTools.DressingName),
                courtyard.Find("Original LS-5 surrounding city").gameObject,
                courtyard.Find("Original LS-5 raised perimeter").gameObject
            };
            maps.sunlight = Object.FindObjectsByType<Light>().Single(light => light.type == LightType.Directional);
            EditorUtility.SetDirty(maps);
        }

        [MenuItem("Yesterday Convoy/Formation Battle/Validate Saved Map Variants")]
        public static void Validate()
        {
            var maps = Object.FindAnyObjectByType<FormationBattleMaps>();
            if (maps == null || maps.sunlight == null || maps.canyonScenery.Any(root => root == null || !root.activeSelf))
                throw new InvalidOperationException("Missing formation map bindings or Canyon scenery.");
            var snapshots = new Dictionary<FormationBattleMaps.Board, FormationBattleTerrainLayout>();
            foreach (var (folder, board) in Maps)
            {
                var authoring = board switch
                {
                    FormationBattleMaps.Board.Street => maps.street,
                    FormationBattleMaps.Board.Hall => maps.hall,
                    _ => maps.ice
                };
                if (authoring == null || authoring.gameObject.activeSelf || authoring.guardPositions.Length != 2 ||
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(authoring.gameObject) != Art + "/" + folder + "/Board.prefab")
                    throw new InvalidOperationException("Map prefab missing or enabled at startup: " + folder);
                var layout = authoring.BuildLayout();
                snapshots.Add(board, layout);
                var tiles = authoring.transform.Find("Board cells");
                if (tiles.childCount != FormationBattleModel.Width * FormationBattleModel.Height ||
                    authoring.GetComponentsInChildren<MeshRenderer>(true).Any(renderer =>
                        renderer.sharedMaterials.Any(mat => mat == null || !AssetDatabase.GetAssetPath(mat).StartsWith(Art + "/" + folder + "/", StringComparison.Ordinal))))
                    throw new InvalidOperationException("Incomplete tiles or mixed source materials in " + folder);
                for (int row = 0; row < FormationBattleModel.Height; row++)
                    for (int col = 0; col < FormationBattleModel.Width; col++)
                    {
                        var cell = new Vector2(col, row);
                        if (col <= FormationBattleModel.DeploymentColumns + 1 && !layout.IsWalkable(cell) ||
                            layout.IsWalkable(cell) && !layout.TryExit(cell, out _))
                            throw new InvalidOperationException($"No route to a blue gate on {folder} at {cell}.");
                    }
                foreach (var formation in authoring.guardPositions)
                {
                    var start = new Vector2(11, 3);
                    var cells = formation.front.Concat(formation.rear).Append(formation.reconstruction).Append(formation.chess);
                    if (formation.front.Length != 4 || formation.rear.Length != 5 ||
                        cells.Distinct().Count() != cells.Count() ||
                        formation.front.Any(cell => !layout.IsWalkable(cell)) ||
                        formation.rear.Any(cell => !layout.CanPlaceDefender(cell) || !layout.TryApproach(start, cell, .8f, out _, out _)) ||
                        !layout.IsWalkable(formation.reconstruction) || !layout.IsWalkable(formation.chess))
                        throw new InvalidOperationException("Illegal guard formation or skill cell in " + folder);
                }
            }
            foreach (int seed in new[] { 17, 20261117 })
            {
                var shop = new ShopTrialModel(FormationCampaignRules.AllUnits.Select(role =>
                    new ShopTrialModel.Role { Id = role.CardId, Name = role.Name, Cost = role.Cost }), seed);
                shop.EnableFormationEconomy();
                shop.Preset(Array.Empty<ShopTrialModel.Piece>(), Array.Empty<string>(), 3, 0, 8);
                var run = new FormationCampaignRun(shop, seed);
                while (!run.Finished)
                {
                    var node = run.CurrentNode;
                    while (run.HasBlockingChoice)
                        if (!run.Choose(0).Success) throw new InvalidOperationException("Fixture choice failed for " + node.Id);
                    snapshots.TryGetValue(FormationBattleMaps.ForNode(node.Id, node.Plane), out var layout);
                    var model = new FormationBattleModel(seed, layout);
                    model.BindCampaign(run);
                    if (model.Defenders.Count != node.DefenderCount)
                        throw new InvalidOperationException("Saved layout did not produce the intended defenders for " + node.Id);
                    if (node.DefenderCount != 0)
                    {
                        run.SettleBattle(true, 1, model.Defenders.Count, 0, false);
                        foreach (var ball in run.PendingBalls.ToArray())
                        {
                            run.BeginBall(ball.Id);
                            run.CompleteBall(ball.Id);
                        }
                    }
                    while (run.HasBlockingChoice)
                        if (!run.Choose(0).Success) throw new InvalidOperationException("Fixture settlement choice failed for " + node.Id);
                    if (!run.Finished && !run.AdvanceNode())
                        throw new InvalidOperationException("Fixture failed to advance from " + node.Id);
                }
            }
            Debug.Log("FORMATION_VARIANT_MAPS_PASS: 3 saved boards, 528 cells, guard formations and blue-gate routes");
        }

        sealed class MapBuilder
        {
            readonly string directory, title;
            readonly Catalog catalog;
            readonly Dictionary<string, Mesh> meshes;
            readonly Dictionary<string, Part> parts;
            readonly Dictionary<string, Finish> finishes;
            readonly Dictionary<string, NativeMaterial> nativeMaterials;
            readonly Dictionary<(string, string), Material> materials = new();
            readonly Shader shader;

            public MapBuilder(string directory, FormationBattleMaps.Board board)
            {
                this.directory = directory;
                title = FormationBattleMaps.DisplayName(board);
                catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(directory + "/source.json"));
                foreach (string folder in new[] { "Meshes", "Materials" })
                    if (!AssetDatabase.IsValidFolder(directory + "/" + folder)) AssetDatabase.CreateFolder(directory, folder);
                parts = catalog.parts.ToDictionary(part => part.name);
                finishes = catalog.finishes.ToDictionary(finish => finish.name);
                nativeMaterials = catalog.materials.ToDictionary(material => material.name);
                meshes = catalog.parts.ToDictionary(part => part.name, ImportMesh);
                shader = Shader.Find("YesterdayConvoy/Formation Canyon Surface");
                if (shader == null) throw new InvalidOperationException("Missing formation board surface shader.");
            }

            public GameObject SavePrefab()
            {
                var root = new GameObject(title);
                try
                {
                    var board = root.AddComponent<FormationBattleTerrainAuthoring>();
                    board.regions = Regions().ToArray();
                    board.guardPositions = catalog.guards;
                    var cells = new GameObject("Board cells").transform;
                    var surroundings = new GameObject("Surroundings").transform;
                    cells.SetParent(root.transform, false);
                    surroundings.SetParent(root.transform, false);
                    foreach (var placement in catalog.placements) Place(placement, placement.tile ? cells : surroundings);
                    board.BuildLayout();
                    root.SetActive(false);
                    return PrefabUtility.SaveAsPrefabAsset(root, directory + "/Board.prefab") ??
                        throw new InvalidOperationException("Could not save " + title);
                }
                finally { Object.DestroyImmediate(root); }
            }

            IEnumerable<FormationBattleTerrainRegion> Regions()
            {
                var cells = catalog.placements.Where(placement => placement.tile)
                    .ToDictionary(placement => new Vector2Int((int)placement.column, (int)placement.row));
                int width = FormationBattleModel.Width, height = FormationBattleModel.Height;
                int firstColumn = FormationBattleModel.DeploymentColumns + 1;
                var markers = new (char kind, float top)[width, height];
                for (int y = 0; y < height; y++)
                    for (int x = firstColumn; x < width - 1; x++)
                    {
                        char kind = catalog.rows[height - y - 1][x - firstColumn];
                        if (kind == 'H' || kind == 'P' || kind == 'X' || kind == 'A')
                            markers[x, y] = (kind, kind == 'X' ? .5f : kind == 'A' ? .13f : cells[new Vector2Int(x, y)].top);
                    }
                for (int y = 0; y < height; y++)
                    for (int x = firstColumn; x < width - 1; x++)
                    {
                        var marker = markers[x, y];
                        if (marker.kind == default) continue;
                        int right = x + 1;
                        while (right < width - 1 && markers[right, y] == marker) right++;
                        int top = y + 1;
                        while (top < height && Enumerable.Range(x, right - x).All(col => markers[col, top] == marker)) top++;
                        for (int row = y; row < top; row++)
                            for (int col = x; col < right; col++) markers[col, row] = default;
                        yield return new FormationBattleTerrainRegion(new RectInt(x, y, right - x, top - y),
                            marker.top, marker.kind == 'H');
                    }
            }

            void Place(Placement placement, Transform parent)
            {
                var node = new GameObject(placement.tile ? $"Tile {placement.column:00},{placement.row:00}" : placement.part,
                    typeof(MeshFilter), typeof(MeshRenderer)).transform;
                node.SetParent(parent, false);
                var mesh = meshes[placement.part];
                node.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = parts[placement.part].surfaces.Select(slot => MaterialFor(slot.material, placement.finish)).ToArray();
                var point = FormationBattleController.WorldPoint(new Vector2(placement.column, placement.row));
                point.y = FloorHeight + placement.top;
                node.SetPositionAndRotation(point, Quaternion.Euler(0, placement.angle, 0));
                var size = mesh.bounds.size;
                var target = placement.size;
                if (!placement.tile && target.x >= 30 && target.z >= 18)
                {
                    // The gameplay cameras frame actors and sidebars, with more room past the map edges than the preview.
                    target.x = Mathf.Max(target.x, 58);
                    target.z = Mathf.Max(target.z, 36);
                }
                node.localScale = new Vector3(target.x / size.x,
                    size.y > .0001f ? target.y / size.y : 1, target.z / size.z);
            }

            Mesh ImportMesh(Part part)
            {
                var mesh = new Mesh { name = part.name };
                var vertices = part.vertices;
                mesh.vertices = Enumerable.Range(0, vertices.Length / 3).Select(i =>
                    new Vector3(vertices[3 * i], vertices[3 * i + 1], vertices[3 * i + 2])).ToArray();
                mesh.uv = Enumerable.Range(0, part.uv.Length / 2).Select(i => new Vector2(part.uv[2 * i], part.uv[2 * i + 1])).ToArray();
                mesh.subMeshCount = part.surfaces.Length;
                for (int i = 0; i < part.surfaces.Length; i++) mesh.SetTriangles(part.surfaces[i].indices, i);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return Save(mesh, "Meshes/" + part.name + ".asset");
            }

            Material MaterialFor(string sourceName, string finishName)
            {
                var key = (sourceName, finishName);
                if (materials.TryGetValue(key, out var material)) return material;
                var source = nativeMaterials[sourceName];
                string atlas = directory + "/Textures/" + source.atlas;
                var importer = AssetImporter.GetAtPath(atlas) as TextureImporter ??
                    throw new InvalidOperationException("Missing native atlas " + atlas);
                if (importer.maxTextureSize != 4096 || importer.alphaIsTransparency != source.cutout)
                {
                    importer.maxTextureSize = 4096;
                    importer.alphaIsTransparency = source.cutout;
                    importer.SaveAndReimport();
                }
                var name = sourceName + (string.IsNullOrEmpty(finishName) ? "" : "_" + finishName);
                var fresh = new Material(shader) { name = name, mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlas) };
                if (finishes.TryGetValue(finishName, out var finish))
                {
                    fresh.SetColor("_Tint", finish.tint.gamma);
                    fresh.SetFloat("_TintMix", finish.mix);
                }
                fresh.SetFloat("_Fill", .26f);
                if (source.cutout)
                {
                    fresh.SetFloat("_Cutoff", .40f);
                    fresh.renderQueue = (int)RenderQueue.AlphaTest;
                }
                material = Save(fresh, "Materials/" + name + ".mat");
                materials.Add(key, material);
                return material;
            }

            T Save<T>(T asset, string relative) where T : Object
            {
                string path = directory + "/" + relative;
                var existing = AssetDatabase.LoadAssetAtPath<T>(path);
                if (existing == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                return existing;
            }
        }
    }
}
