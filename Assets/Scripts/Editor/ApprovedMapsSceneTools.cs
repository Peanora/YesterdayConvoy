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
    /// <summary>Editor-only authoring. Play never calls this builder or replaces saved terrain.</summary>
    public static class ApprovedMapsSceneTools
    {
        const string SourcePath = "Assets/Data/ApprovedMaps/layouts.json";
        const string Data = "Assets/Data/Resources/ApprovedMaps";
        const string Art = "Assets/Art/Environment/ApprovedMaps";
        const string Host = "Assets/Scenes/MonsterTrial.unity";
        const string Evidence = "Builds/ApprovedMapsEvidence";
        static readonly string[] ImageNames = { "01-ruins", "02-outskirts", "03-downtown", "04-tunnel", "05-broken", "06-pyrite", "07-prison", "08-dossoles" };
        static readonly int[] RedCounts = { 3, 3, 3, 2, 2, 3, 2, 2 };
        static readonly int[] BlueCounts = { 2, 2, 2, 2, 1, 2, 1, 2 };
        static bool refreshTaskMaterials;

        [Serializable] sealed class Source { public Layout[] maps; }
        [Serializable] sealed class Cell { public int x, y; public Vector2Int Value => new Vector2Int(x, y); }
        [Serializable] sealed class Path { public string id, exitId; public Cell[] cells; }
        [Serializable] sealed class Guard { public Cell cell; public bool high; }
        [Serializable] sealed class Portal { public string routeId; public int sourcePointIndex, destinationPointIndex; public float transitSeconds = 1.5f; }
        [Serializable] sealed class Layout
        {
            public int index, width, height;
            public string id, title, sceneName;
            public string[] rows;
            public Path[] routes;
            public Guard[] guards;
            public Portal[] portals;
            public Cell[] heatCells;
            public char Tile(int x, int y) => x < 0 || x >= width || y < 0 || y >= height ? 'X' : rows[y][x];
            public Vector3 World(int x, int y, float elevation = 0) => new Vector3(x - (width - 1) * .5f, elevation, (height - 1) * .5f - y);
        }

        [MenuItem("Yesterday Convoy/Approved Maps/Create Missing Saved Maps")]
        public static void Apply() => Author(false);

        // Explicit iteration entry for the eight scenes owned by this task. Normal Apply preserves edited scenes.
        public static void RebuildTaskScenes() => Author(true);

        static Source ReadSource()
        {
            if (!File.Exists(SourcePath)) throw new InvalidOperationException("Approved V2 layout source missing: " + SourcePath);
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(SourcePath));
            if (source?.maps == null || source.maps.Length != 8) throw new InvalidOperationException("Exactly eight approved maps required.");
            foreach (var map in source.maps)
            {
                if (map.index < 1 || map.index > 8 || map.id != "approved-map-" + map.index.ToString("00") || map.width != 14 || map.height != 10 ||
                    map.rows == null || map.rows.Length != map.height || map.rows.Any(r => r.Length != map.width))
                    throw new InvalidOperationException("Invalid approved grid: " + map.id);
                if (map.routes == null || map.routes.Length != RedCounts[map.index - 1] || map.routes.Select(r => r.exitId).Distinct().Count() != BlueCounts[map.index - 1])
                    throw new InvalidOperationException("Approved gate count differs: " + map.id);
            }
            if (source.maps.Select(m => m.id).Distinct().Count() != 8 || source.maps.Select(m => m.sceneName).Distinct().Count() != 8)
                throw new InvalidOperationException("Duplicate approved map or scene identity.");
            return source;
        }

        static void CheckEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring or validating maps.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save or close dirty scenes before map authoring.");
        }

        static void Author(bool rebuild)
        {
            CheckEditor();
            var source = ReadSource();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            refreshTaskMaterials = rebuild;
            try
            {
                EnsureFolder(Data); EnsureFolder(Art); EnsureFolder(Art + "/Materials"); EnsureFolder(Art + "/Meshes");
                ImportConceptThumbnails();
                foreach (var layout in source.maps.OrderBy(m => m.index))
                {
                    var map = CreateProfile(layout);
                    // Opening another scene unloads currently unreferenced ScriptableObjects.
                    // Persist new assets first, then acquire the instances belonging to the new scene.
                    AssetDatabase.SaveAssets();
                    string scenePath = ScenePath(layout);
                    if (File.Exists(scenePath) && !rebuild) continue;
                    if (!File.Exists(scenePath) && !AssetDatabase.CopyAsset(Host, scenePath)) throw new InvalidOperationException("Cannot create map scene: " + scenePath);
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    map = LoadProfile(layout);
                    var bindings = Object.FindAnyObjectByType<InitialDemo>();
                    var controller = Object.FindAnyObjectByType<ManualBattleController>();
                    if (bindings == null || controller == null || Camera.main == null) throw new InvalidOperationException("Map host bindings missing.");
                    PrepareHost(bindings, map);
                    var root = new GameObject(layout.title + " / saved editable battlefield");
                    root.AddComponent<AuthoredBattlefield>();
                    var builder = new Geometry(layout, map, root.transform);
                    builder.Build();
                    // The bootstrap owns heat-marker descendants; the actual terrain is already saved.
                    var startup = root.AddComponent<ApprovedMapBootstrap>(); startup.map = LoadProfile(layout);
                    ConfigureLighting(layout.index);
                    SetCamera();
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    if (AssetDatabase.AssetPathToGUID(scenePath) == AssetDatabase.AssetPathToGUID(Host)) throw new InvalidOperationException("Scene copied the host GUID.");
                }
                AssetDatabase.SaveAssets();
                Debug.Log("APPROVED_MAPS_AUTHORED: eight saved independent scenes, 1120 editable tiles, 20 fixed routes; existing source scenes unchanged.");
            }
            finally { refreshTaskMaterials = false; RestoreSetup(setup); }
        }

        static ApprovedMapDefinition CreateProfile(Layout layout)
        {
            string path = Data + "/Map" + layout.index.ToString("00") + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<ApprovedMapDefinition>(path);
            if (profile != null) return profile; // Inspector edits remain the source of truth after creation.
            var original = Resources.Load<DefenderTrialDefinition>("DefenderTrial");
            if (original == null || original.battle == null) throw new InvalidOperationException("Existing eight-defender source missing.");
            profile = ScriptableObject.CreateInstance<ApprovedMapDefinition>();
            profile.name = "Map" + layout.index.ToString("00"); profile.index = layout.index; profile.id = layout.id; profile.title = layout.title;
            profile.sceneName = layout.sceneName; profile.width = layout.width; profile.height = layout.height; profile.gridRows = (string[])layout.rows.Clone(); profile.platformHeight = .72f;
            profile.preview = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Concepts/" + ImageNames[layout.index - 1] + ".png");
            if (profile.preview == null) throw new InvalidOperationException("Approved concept thumbnail missing: " + ImageNames[layout.index - 1]);
            var encounter = ScriptableObject.CreateInstance<DefenderTrialDefinition>(); encounter.name = profile.name + "Battle";
            encounter.mapId = layout.id; encounter.mapTitle = layout.title; encounter.battle = original.battle; encounter.terrainOnly = false;
            encounter.lanes = layout.routes.Select(r => new TacticalLane { id = r.id, exitId = r.exitId, points = r.cells.Select(c => layout.World(c.x, c.y)).ToArray() }).ToArray();
            encounter.route = (Vector3[])encounter.lanes[0].points.Clone(); encounter.raised = Array.Empty<Vector2Int>(); encounter.rosterCards = Array.Empty<ManualBattleDefinition>();
            var placements = new List<DefenderPlacement>(); int ground = 0, high = 0;
            string[] groundIds = { "D01", "D04", "D03" }, highIds = { "D07", "D08", "D10", "D09", "D12" };
            foreach (var entry in layout.guards)
            {
                string id = entry.high ? highIds[high++ % highIds.Length] : groundIds[ground++ % groundIds.Length];
                var source = original.defenders.Single(d => d.artId == id);
                var copy = JsonUtility.FromJson<DefenderPlacement>(JsonUtility.ToJson(source));
                copy.id = layout.id + "-guard-" + (placements.Count + 1).ToString("00");
                copy.position = layout.World(entry.cell.x, entry.cell.y, entry.high ? profile.platformHeight : 0); placements.Add(copy);
            }
            encounter.defenders = placements.ToArray(); encounter.surfaces = Array.Empty<Material>();
            AssetDatabase.CreateAsset(encounter, Data + "/Map" + layout.index.ToString("00") + "Battle.asset"); profile.encounter = encounter;
            profile.mechanics = new ApprovedMapMechanics {
                portals = (layout.portals ?? Array.Empty<Portal>()).Select(p => new ApprovedPortal { routeId = p.routeId, sourcePointIndex = p.sourcePointIndex, destinationPointIndex = p.destinationPointIndex, transitSeconds = p.transitSeconds }).ToArray(),
                heatPumps = (layout.heatCells ?? Array.Empty<Cell>()).Select((c, i) => new ApprovedHeatPump { position = layout.World(c.x, c.y), period = 10, warning = 1.5f, active = 2, dps = 12, phase = i * 3.3f }).ToArray()
            };
            AssetDatabase.CreateAsset(profile, path); return profile;
        }

        static void PrepareHost(InitialDemo bindings, ApprovedMapDefinition map)
        {
            // Detach compatibility transforms before removing the old authored environment.
            var compatibility = new GameObject("Hidden original battle bindings").transform;
            foreach (var t in new[] { bindings.Monster, bindings.RedGate, bindings.BlueGate })
                if (t != null) { t.SetParent(compatibility, true); t.gameObject.SetActive(false); }
            foreach (var field in Object.FindObjectsByType<AuthoredBattlefield>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(field.gameObject);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "Industrial Map" || root.name == "Approved map startup" || root.name == "Hidden original battle bindings" && root.transform != compatibility)
                    Object.DestroyImmediate(root);
            foreach (var bootstrap in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (bootstrap != null && bootstrap.GetType().Name.EndsWith("Bootstrap", StringComparison.Ordinal)) Object.DestroyImmediate(bootstrap);
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) renderer.enabled = false;
            bindings.RedGate.position = map.encounter.route[0]; bindings.BlueGate.position = map.encounter.route[map.encounter.route.Length - 1];
            var serialized = new SerializedObject(bindings); var points = serialized.FindProperty("waypoints"); points.arraySize = map.encounter.route.Length;
            for (int i = 0; i < points.arraySize; i++) points.GetArrayElementAtIndex(i).vector3Value = map.encounter.route[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetCamera()
        {
            var camera = Camera.main; camera.orthographic = false; camera.fieldOfView = 24; camera.rect = new Rect(0, 0, 1, 1);
            camera.transform.rotation = Quaternion.Euler(60, 0, 0);
            camera.transform.position = camera.transform.rotation * new Vector3(0, -1, -24) + new Vector3(0, 0, .4f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .069f, .081f);
        }

        static void ConfigureLighting(int index)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = index == 3 ? new Color(.39f, .42f, .57f) : new Color(.55f, .58f, .61f);
            RenderSettings.ambientEquatorColor = index == 1 || index == 6 ? new Color(.45f, .39f, .32f) : new Color(.35f, .39f, .42f);
            RenderSettings.ambientGroundColor = new Color(.19f, .21f, .23f);
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(48, -32, 0); light.intensity = index == 3 ? .65f : 1.05f;
                    light.color = index == 1 ? new Color(1, .85f, .68f) : index == 6 ? new Color(1, .95f, .80f) : new Color(.89f, .95f, 1);
                    light.shadows = LightShadows.Soft; light.shadowStrength = .55f;
                }
        }

        sealed class Geometry
        {
            readonly Layout layout; readonly float platformHeight; readonly Transform root;
            readonly Mesh cube, quad, cylinder, bevel;
            readonly Material floor, alternate, wall, cap, steel, dark, edge, amber, red, blue, water, wood, wet;
            readonly HashSet<Vector2Int> guardCells;
            Transform props;
            public Geometry(Layout source, ApprovedMapDefinition map, Transform parent)
            {
                layout = source; platformHeight = map.platformHeight; root = parent;
                cube = PrimitiveMesh("UnitCube", PrimitiveType.Cube); cylinder = PrimitiveMesh("UnitCylinder", PrimitiveType.Cylinder);
                quad = QuadMesh(); bevel = BevelMesh();
                var palette = Palettes[layout.index - 1];
                Texture2D concrete = Texture("concrete-color.jpg"), metal = Texture("trim-01-color.png"), metal2 = Texture("trim-02-color.png"), asphalt = Texture("asphalt-color.jpg");
                floor = Material("Floor", palette.Floor * (layout.index == 3 ? .85f : .68f), layout.index == 3 ? asphalt : concrete, layout.index == 3 ? .63f : .16f);
                alternate = Material("FloorAlternate", palette.Floor * (layout.index == 3 ? .79f : .64f), layout.index == 3 ? asphalt : concrete, .19f);
                wall = Material("Wall", palette.Wall, concrete, .12f); cap = Material("PlatformCap", palette.Cap * 1.10f, concrete, .17f);
                steel = Material("Steel", palette.Steel, metal, .32f, .40f); dark = Material("Recess", new Color(.075f, .091f, .107f), null, .10f);
                edge = Material("Edges", palette.Edge, metal2, .25f, .25f); amber = Material("Safety", new Color(.74f, .56f, .19f), null, .1f);
                red = Material("Entry", new Color(.94f, .17f, .20f), null, .1f, 0, true); blue = Material("Exit", new Color(.16f, .67f, .95f), null, .1f, 0, true);
                wood = Material("Wood", new Color(.49f, .35f, .21f), null, .12f); wet = Material("WetPatches", palette.Floor * .7f, null, .88f);
                water = WaterMaterial(); guardCells = new HashSet<Vector2Int>(layout.guards.Select(g => g.cell.Value));
            }

            public void Build()
            {
                var terrain = new GameObject("Terrain / 14 x 10 editable cells").transform; terrain.SetParent(root, false);
                props = new GameObject("Theme structures / editable saved meshes").transform; props.SetParent(root, false);
                Box(props, "Surrounding recessed foundation", new Vector3(0, -1.2f, 0), new Vector3(19, .28f, 15), dark);
                for (int y = 0; y < layout.height; y++) for (int x = 0; x < layout.width; x++)
                {
                    char kind = layout.Tile(x, y); var tile = new GameObject(((char)('A' + x)) + (y + 1).ToString("00") + " / " + KindName(kind)).transform;
                    tile.SetParent(terrain, false); tile.localPosition = layout.World(x, y);
                    var metadata = tile.gameObject.AddComponent<ApprovedMapTile>(); metadata.cell = new Vector2Int(x, y); metadata.kind = kind.ToString();
                    switch (kind)
                    {
                        case 'P': Pit(tile); break;
                        case 'W': Water(tile, x, y); break;
                        case 'H': Raised(tile, x, y); break;
                        case 'B': Bridge(tile, x, y, layout.index == 8); break;
                        default: Floor(tile, x, y); if (kind == 'T') Tunnel(tile); if (kind == 'E') Heat(tile); break;
                    }
                }
                var encounter = LoadEncounter(layout);
                foreach (var lane in encounter.lanes) Gate(lane.id + " / Red entry", lane.points[0], red);
                foreach (var group in encounter.lanes.GroupBy(l => l.exitId)) Gate(group.Key + " / Blue exit", group.First().points.Last(), blue);
                ThemeLandmarks(); Boundary();
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                // Model/material imports may also refresh asset instances. Write to the current
                // persistent encounter rather than an instance retained before OpenScene/import.
                encounter = LoadEncounter(layout);
                encounter.surfaces = root.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
                EditorUtility.SetDirty(encounter); AssetDatabase.SaveAssets();
            }

            void Floor(Transform t, int x, int y)
            {
                Box(t, "Floor foundation", new Vector3(0, -.17f, 0), new Vector3(.995f, .31f, .995f), wall);
                Plane(t, "Textured ground slab", new Vector3(0, .002f, 0), new Vector2(.973f, .973f), (x + y) % 4 == 0 ? alternate : floor);
                if ((x * 3 + y) % 7 == 0) Box(t, "Inset utility cover", new Vector3(.17f, .009f, .17f), new Vector3(.27f, .012f, .29f), steel);
                if (layout.index == 3 && (x + y * 5) % 4 == 0) Plane(t, "Wet asphalt reflection patch", new Vector3(.08f, .006f, -.12f), new Vector2(.40f, .20f), wet);
                if (y % 3 == 1 && layout.Tile(x, y - 1) == 'H')
                    Box(t, "Continuous worn service boundary", new Vector3(0, .013f, .47f), new Vector3(.985f, .008f, .022f), layout.index == 7 ? edge : amber);
            }

            void Raised(Transform t, int x, int y)
            {
                float h = platformHeight;
                Box(t, "Raised concrete body", new Vector3(0, h * .5f - .02f, 0), new Vector3(.995f, h, .995f), wall);
                MeshObject(t, "Beveled platform cap", bevel, new Vector3(0, h - .027f, 0), new Vector3(.98f, .054f, .98f), cap);
                foreach (var d in Directions)
                {
                    if (layout.Tile(x + d.x, y + d.y) == 'H') continue;
                    Vector3 side = new Vector3(d.x * .485f, h - .006f, -d.y * .485f);
                    Box(t, "Exposed platform rim", side, d.x == 0 ? new Vector3(.97f, .018f, .027f) : new Vector3(.027f, .018f, .97f), edge);
                    Vector3 panel = new Vector3(d.x * .501f, h * .39f, -d.y * .501f);
                    Box(t, "Exposed wall service inset", panel, d.x == 0 ? new Vector3(.68f, .19f, .018f) : new Vector3(.018f, .19f, .68f), dark);
                }
                // Theme-specific equipment replaces the previous repeated fan on arbitrary cells.
            }

            void Pit(Transform t)
            {
                Plane(t, "Open pit / deep black bottom", Vector3.down * 1.04f, new Vector2(.92f, .92f), dark);
                foreach (int sign in new[] { -1, 1 })
                {
                    Box(t, "Pit vertical lining X", new Vector3(sign * .455f, -.46f, 0), new Vector3(.085f, .94f, 1), wall);
                    Box(t, "Pit vertical lining Z", new Vector3(0, -.46f, sign * .455f), new Vector3(.83f, .94f, .085f), wall);
                    Box(t, "Pit steel rim X", new Vector3(sign * .46f, .012f, 0), new Vector3(.08f, .026f, 1), steel);
                    Box(t, "Pit steel rim Z", new Vector3(0, .012f, sign * .46f), new Vector3(.87f, .026f, .08f), steel);
                    foreach (int corner in new[] { -1, 1 }) Box(t, "Pit warning corner", new Vector3(sign * .37f, .03f, corner * .46f), new Vector3(.17f, .013f, .075f), amber);
                }
            }

            void Water(Transform t, int x, int y)
            {
                float depth = layout.index == 2 ? -.62f : -.39f;
                Plane(t, "Recessed static water surface", Vector3.up * depth, Vector2.one, water);
                foreach (var d in Directions)
                {
                    char neighbor = layout.Tile(x + d.x, y + d.y); if (neighbor == 'W' || neighbor == 'X') continue;
                    Box(t, "Vertical concrete quay wall", new Vector3(d.x * .495f, depth * .5f - .025f, -d.y * .495f), d.x == 0 ? new Vector3(1, -depth + .08f, .07f) : new Vector3(.07f, -depth + .08f, 1), wall);
                }
            }

            void Bridge(Transform t, int x, int y, bool timber)
            {
                Plane(t, "Water beneath bridge", Vector3.down * .62f, Vector2.one, water);
                Box(t, "Bridge load-bearing deck", new Vector3(0, -.09f, 0), new Vector3(.98f, .16f, .98f), steel);
                if (timber)
                {
                    for (int n = 0; n < 8; n++)
                    {
                        Box(t, "Individual timber plank", new Vector3(-.42f + n * .12f, .003f, 0), new Vector3(.106f, .04f, .93f), wood);
                        foreach (float z in new[] { -.35f, .35f }) Box(t, "Bridge plank fastener", new Vector3(-.42f + n * .12f, .026f, z), new Vector3(.025f, .012f, .025f), dark);
                    }
                    foreach (float z in new[] { -.48f, .48f }) Beam(t, "Timber bridge edge", new Vector3(-.48f, .05f, z), new Vector3(.48f, .05f, z), .04f, steel);
                }
                else
                {
                    Plane(t, "Short public road bridge surface", Vector3.up * .003f, new Vector2(.93f, .97f), floor);
                    foreach (float xSide in new[] { -.47f, .47f })
                    {
                        Beam(t, "Bridge safety rail", new Vector3(xSide, .23f, -.49f), new Vector3(xSide, .23f, .49f), .026f, steel);
                        foreach (float z in new[] { -.40f, .40f }) Beam(t, "Bridge rail post", new Vector3(xSide, .02f, z), new Vector3(xSide, .24f, z), .026f, edge);
                    }
                    Box(t, "Bridge left support", new Vector3(-.39f, -.38f, 0), new Vector3(.13f, .57f, .19f), wall);
                    Box(t, "Bridge right support", new Vector3(.39f, -.38f, 0), new Vector3(.13f, .57f, .19f), wall);
                }
            }

            void Tunnel(Transform t)
            {
                Plane(t, "Tunnel dark opening", Vector3.up * .017f, new Vector2(.65f, .68f), dark);
                foreach (float x in new[] { -.39f, .39f }) Box(t, "Tunnel steel jamb", new Vector3(x, .1f, 0), new Vector3(.1f, .19f, .91f), steel);
                foreach (float z in new[] { -.39f, .39f }) Box(t, "Tunnel header", new Vector3(0, .1f, z), new Vector3(.7f, .19f, .1f), steel);
                for (int i = 0; i < 5; i++) Box(t, "Tunnel hazard stripe", new Vector3(-.30f + i * .15f, .202f, -.39f), new Vector3(.075f, .012f, .09f), amber);
                for (int i = 0; i < 4; i++) Box(t, "Descending visible tunnel stair", new Vector3(0, -.015f - i * .06f, .22f - i * .13f), new Vector3(.56f, .06f, .11f), steel);
            }

            void Heat(Transform t)
            {
                Box(t, "Heat pump recessed frame", Vector3.up * .02f, new Vector3(.83f, .035f, .84f), dark);
                var signal = Plane(t, "Heat pump / actual simulation status signal", Vector3.up * .041f, new Vector2(.72f, .70f), Material("HeatSignal", new Color(.20f, .17f, .13f), null, .1f));
                var marker = signal.gameObject.AddComponent<ApprovedHeatMarker>();
                var position = new Vector2Int(Mathf.RoundToInt(t.localPosition.x + (layout.width - 1) * .5f), Mathf.RoundToInt((layout.height - 1) * .5f - t.localPosition.z));
                marker.index = Array.FindIndex(layout.heatCells ?? Array.Empty<Cell>(), c => c.x == position.x && c.y == position.y);
                if (marker.index < 0) throw new InvalidOperationException("A heat-pump tile is missing its simulation configuration.");
                for (int i = 0; i < 7; i++) Box(t, "Individual heat-exchanger slat", new Vector3(-.31f + i * .105f, .048f, 0), new Vector3(.044f, .035f, .70f), steel);
                foreach (float z in new[] { -.43f, .43f }) for (int i = 0; i < 4; i++)
                    Box(t, "Heat pump caution inset", new Vector3(-.28f + i * .185f, .026f, z), new Vector3(.09f, .023f, .06f), amber);
            }

            void ThemeLandmarks()
            {
                switch (layout.index)
                {
                    case 1:
                        SafeDecor(1, 1, 2, 2, "01 / street maintenance canopy", g => {
                            Box(g, "Worn cantilever shelter roof", new Vector3(0, .62f, 0), new Vector3(1.86f, .10f, 1.70f), steel);
                            for (int n = 0; n < 6; n++) Box(g, "Shelter metal roof rib", new Vector3(-.76f + n * .30f, .68f, 0), new Vector3(.035f, .022f, 1.63f), edge);
                            foreach (float x in new[] { -.78f, .78f }) foreach (float z in new[] { -.72f, .72f }) Beam(g, "Shelter thin support", new Vector3(x, 0, z), new Vector3(x, .61f, z), .055f, steel);
                            Box(g, "Maintenance service counter", new Vector3(0, .19f, .45f), new Vector3(1.25f, .38f, .36f), wall);
                            Lamp(g, new Vector3(0, .53f, -.78f), new Vector3(1.30f, .055f, .04f), new Color(1, .69f, .27f));
                        });
                        break;
                    case 2:
                        SafeDecor(7, 1, 2, 2, "02 / flood-pump control house", g => {
                            House(g, 1.82f, 1.7f, .55f, "Flood control service house");
                            Vent(g, new Vector3(-.38f, .56f, .15f), .65f);
                            Cylinder(g, "Pump pressure column", new Vector3(.50f, .43f, -.12f), new Vector3(.34f, .43f, .34f), steel);
                            Pipe(g, new Vector3(.50f, .70f, -.12f), new Vector3(.50f, .70f, -.70f), .075f);
                            Lamp(g, new Vector3(-.18f, .38f, -.87f), new Vector3(.70f, .075f, .025f), new Color(.85f, .82f, .54f));
                        });
                        foreach (int x in new[] { 3, 10 }) foreach (int y in new[] { 4, 5 }) {
                            var p = layout.World(x, y);
                            foreach (float side in new[] { -.42f, .42f }) Box(props, "Bridge yellow edge marking", p + new Vector3(side, .018f, 0), new Vector3(.025f, .012f, .95f), amber);
                        }
                        break;
                    case 3:
                        SafeDecor(5, 1, 4, 1, "03 / visible viaduct and night lamps", g => {
                            Box(g, "Visible raised viaduct road deck", new Vector3(0, .39f, 0), new Vector3(3.88f, .15f, .88f), steel);
                            foreach (float x in new[] { -1.55f, -.45f, .55f, 1.55f }) Box(g, "Viaduct short concrete pier", new Vector3(x, .17f, 0), new Vector3(.20f, .35f, .47f), wall);
                            foreach (float z in new[] { -.42f, .42f }) Beam(g, "Viaduct continuous railing", new Vector3(-1.88f, .65f, z), new Vector3(1.88f, .65f, z), .032f, edge);
                            for (int x = -1; x <= 1; x++) Box(g, "Viaduct road divider dash", new Vector3(x, .474f, 0), new Vector3(.45f, .008f, .035f), cap);
                            Lamp(g, new Vector3(0, .29f, -.45f), new Vector3(3.55f, .065f, .025f), new Color(.12f, .66f, .92f));
                            foreach (float x in new[] { -1.62f, 1.62f }) {
                                Beam(g, "Viaduct lamp post", new Vector3(x, .48f, .28f), new Vector3(x, .98f, .28f), .035f, steel);
                                Lamp(g, new Vector3(x, .98f, .18f), new Vector3(.22f, .045f, .18f), new Color(.80f, .57f, 1));
                            }
                        });
                        break;
                    case 4:
                        for (int y = 0; y < 8; y++) AtHigh(6, y, p => {
                            Box(props, "Long postal service wall", p + Vector3.up * .17f, new Vector3(.54f, .34f, .99f), wall);
                            Box(props, "Postal wall steel seam", p + new Vector3(-.29f, .16f, 0), new Vector3(.025f, .27f, .68f), steel);
                        });
                        SafeDecor(6, 4, 2, 2, "04 / postal sorting terminal", g => {
                            House(g, 1.83f, 1.75f, .65f, "Postal sorting room");
                            Box(g, "Postal orange routing fascia", new Vector3(0, .48f, -.90f), new Vector3(1.35f, .16f, .035f), amber);
                            for (int i = 0; i < 6; i++) Box(g, "Postal roll-up shutter slat", new Vector3(-.38f, .10f + i * .06f, -.91f), new Vector3(.68f, .025f, .04f), dark);
                            Lamp(g, new Vector3(.46f, .43f, -.92f), new Vector3(.35f, .09f, .025f), new Color(1, .68f, .29f));
                        });
                        break;
                    case 5:
                        SafeDecor(6, 5, 2, 2, "05 / industrial heat-exchanger bank", g => {
                            Box(g, "Heat exchanger steel footing", new Vector3(0, .08f, 0), new Vector3(1.84f, .16f, 1.8f), dark);
                            Box(g, "Twin exchanger radiator housing", new Vector3(0, .42f, .23f), new Vector3(1.72f, .66f, .98f), steel);
                            for (int i = 0; i < 11; i++) Box(g, "Radiator deep vertical fin", new Vector3(-.75f + i * .15f, .43f, -.29f), new Vector3(.035f, .54f, .15f), edge);
                            foreach (float x in new[] { -.52f, .52f }) {
                                Cylinder(g, "Large exchanger fan well", new Vector3(x, .76f, .24f), new Vector3(.53f, .035f, .53f), dark);
                                for (int i = 0; i < 4; i++) { var blade = Box(g, "Large exchanger fan blade", new Vector3(x, .80f, .24f), new Vector3(.47f, .025f, .065f), edge); blade.localRotation = Quaternion.Euler(0, i * 45, 0); }
                            }
                            foreach (float z in new[] { -.71f, -.47f }) Pipe(g, new Vector3(-.83f, .25f, z), new Vector3(.83f, .25f, z), .105f);
                            WarningBand(g, new Vector3(0, .018f, -.87f), 1.65f);
                        });
                        SafeDecor(9, 5, 1, 2, "05 / connected pressure receiver", g => {
                            Cylinder(g, "Tall connected pressure receiver", new Vector3(0, .53f, 0), new Vector3(.60f, .53f, .60f), steel);
                            foreach (float y in new[] { .23f, .75f }) Cylinder(g, "Receiver circumferential band", new Vector3(0, y, 0), new Vector3(.65f, .035f, .65f), dark);
                            Pipe(g, new Vector3(0, .24f, -.35f), new Vector3(0, .24f, -.8f), .085f);
                        });
                        break;
                    case 6:
                        for (int x = 2; x < 12; x++) AtHigh(x, 0, p => {
                            Box(props, "Rear static freight conveyor", p + Vector3.up * .13f, new Vector3(.995f, .21f, .57f), dark);
                            for (int n = 0; n < 5; n++) Box(props, "Conveyor transverse roller", p + new Vector3(-.40f + n * .20f, .25f, 0), new Vector3(.055f, .055f, .50f), steel);
                            foreach (float z in new[] { -.34f, .34f }) Box(props, "Conveyor side frame", p + new Vector3(0, .22f, z), new Vector3(.99f, .12f, .045f), edge);
                        });
                        SafeDecor(8, 1, 2, 2, "06 / freight hopper and conveyor terminal", g => {
                            foreach (float x in new[] { -.66f, .66f }) foreach (float z in new[] { -.62f, .62f }) Box(g, "Freight hopper support leg", new Vector3(x, .31f, z), new Vector3(.09f, .62f, .09f), steel);
                            MeshObject(g, "Tapered mineral freight hopper", HopperMesh(), new Vector3(0, .70f, 0), new Vector3(1.65f, .65f, 1.50f), steel);
                            Plane(g, "Hopper dark open mineral load", new Vector3(0, 1.031f, 0), new Vector2(1.37f, 1.25f), dark);
                            foreach (float x in new[] { -.79f, .79f }) Beam(g, "Hopper upper rim", new Vector3(x, 1.04f, -.72f), new Vector3(x, 1.04f, .72f), .06f, edge);
                            WarningBand(g, new Vector3(0, .42f, -.76f), 1.40f);
                            Box(g, "Conveyor discharge housing", new Vector3(0, .16f, .72f), new Vector3(.64f, .32f, .43f), dark);
                        });
                        break;
                    case 7:
                        SafeDecor(3, 4, 1, 3, "07 / continuous quarantine room wing", g => {
                            House(g, .88f, 2.85f, .70f, "Quarantine L long wing");
                            for (int i = 0; i < 3; i++) {
                                Box(g, "Quarantine observation window", new Vector3(.451f, .40f, -.92f + i * .92f), new Vector3(.035f, .22f, .55f), dark);
                                Lamp(g, new Vector3(.47f, .19f, -.92f + i * .92f), new Vector3(.028f, .055f, .62f), new Color(.23f, .73f, .64f));
                            }
                        });
                        SafeDecor(3, 7, 3, 1, "07 / continuous quarantine return wing", g => {
                            House(g, 2.85f, .88f, .70f, "Quarantine L short wing");
                            for (int i = 0; i < 3; i++) {
                                Box(g, "Frosted quarantine observation panel", new Vector3(-.93f + i * .93f, .40f, -.452f), new Vector3(.62f, .24f, .032f), steel);
                                Lamp(g, new Vector3(-.93f + i * .93f, .17f, -.475f), new Vector3(.66f, .045f, .025f), new Color(.23f, .73f, .64f));
                            }
                        });
                        break;
                    case 8:
                        AtHigh(10, 4, p => Container(p, .78f));
                        SafeDecor(12, 5, 1, 1, "08 / dockside cargo crane", g => {
                            Cylinder(g, "Crane circular turntable", new Vector3(0, .09f, 0), new Vector3(.73f, .09f, .73f), dark);
                            Box(g, "Crane control cabin", new Vector3(0, .36f, 0), new Vector3(.60f, .50f, .62f), steel);
                            Box(g, "Crane cabin window", new Vector3(0, .42f, -.32f), new Vector3(.43f, .23f, .02f), dark);
                            Beam(g, "Crane lattice rising boom", new Vector3(.12f, .64f, 0), new Vector3(.30f, 1.45f, .04f), .105f, steel);
                            Beam(g, "Crane outboard loading arm", new Vector3(.30f, 1.45f, .04f), new Vector3(1.17f, 1.25f, .04f), .10f, steel);
                            Beam(g, "Crane boom tension brace", new Vector3(.10f, .70f, .04f), new Vector3(1.14f, 1.24f, .04f), .026f, edge);
                            Beam(g, "Crane suspended hook cable", new Vector3(1.10f, 1.23f, .04f), new Vector3(1.10f, .60f, .04f), .015f, dark);
                            Box(g, "Crane yellow cargo hook", new Vector3(1.10f, .56f, .04f), new Vector3(.13f, .15f, .09f), amber);
                            WarningBand(g, new Vector3(0, .022f, -.40f), .70f);
                        });
                        foreach (int x in new[] { 0, 3, 6, 9, 12 })
                            Box(props, "Breakwater buttress", new Vector3(x - 6.5f, -.22f, -5.05f), new Vector3(.34f, .75f, .52f), wall);
                        break;
                }
            }

            void SafeDecor(int x, int y, int width, int depth, string name, Action<Transform> build)
            {
                for (int row = y; row < y + depth; row++) for (int col = x; col < x + width; col++)
                    if (layout.Tile(col, row) != 'H' || guardCells.Contains(new Vector2Int(col, row)))
                        throw new InvalidOperationException("Required landmark footprint intersects a road or defender: " + name);
                var group = new GameObject("Landmark / " + name).transform; group.SetParent(props, false);
                group.localPosition = (layout.World(x, y, platformHeight) + layout.World(x + width - 1, y + depth - 1, platformHeight)) * .5f;
                build(group);
            }

            void House(Transform g, float width, float depth, float height, string name)
            {
                Box(g, name + " / continuous building walls", new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), wall);
                Box(g, name + " / unified metal roof", new Vector3(0, height + .035f, 0), new Vector3(width + .04f, .07f, depth + .04f), steel);
                foreach (float x in new[] { -width * .43f, width * .43f }) Box(g, name + " / structural pilaster", new Vector3(x, height * .47f, -depth * .51f), new Vector3(.055f, height * .85f, .045f), edge);
            }

            void WarningBand(Transform g, Vector3 p, float width)
            {
                Box(g, "Sparse black safety band", p, new Vector3(width, .035f, .07f), dark);
                for (int i = 0; i < Mathf.CeilToInt(width / .21f); i++) Box(g, "Yellow safety stripe", p + new Vector3(-width * .44f + i * .21f, .019f, 0), new Vector3(.09f, .018f, .072f), amber);
            }

            void Lamp(Transform g, Vector3 p, Vector3 size, Color color)
            {
                string suffix = color.r > .5f && color.b > .8f ? "VioletLamp" : color.b > .7f ? "BlueLamp" : color.g > color.r ? "TealLamp" : "WarmLamp";
                Box(g, "Visible " + suffix, p, size, Material(suffix, color, null, .1f, 0, true));
            }

            void Island(Vector2Int[] cells, string name, bool industrial)
            {
                foreach (var c in cells) AtHigh(c.x, c.y, p => {
                    if (guardCells.Contains(c)) return;
                    if (industrial) Vent(props, p, .72f); else Prop("Prop_Crate3.fbx", p, .55f, .38f, c.x * 13);
                });
            }

            void AtHigh(int x, int y, Action<Vector3> build)
            {
                if (layout.Tile(x, y) != 'H' || guardCells.Contains(new Vector2Int(x, y))) return;
                build(layout.World(x, y, platformHeight));
            }

            void CellFacade(Vector3 p, string name)
            {
                Box(props, name, p + new Vector3(0, .16f, .05f), new Vector3(.92f, .32f, .8f), wall);
                Box(props, "Quarantine teal access panel", p + new Vector3(0, .13f, -.364f), new Vector3(.58f, .23f, .018f), edge);
                for (int i = 0; i < 5; i++) Box(props, "Observation grille bar", p + new Vector3(-.24f + i * .12f, .15f, -.384f), new Vector3(.026f, .18f, .025f), dark);
                Box(props, "Quarantine corridor light", p + new Vector3(0, -.18f, -.509f), new Vector3(.45f, .045f, .02f), edge);
            }

            void Container(Vector3 p, float size)
            {
                Box(props, "Corrugated cargo container", p + Vector3.up * .32f, new Vector3(size, .64f, size), steel);
                for (int i = 0; i < 7; i++) Box(props, "Container corrugation", p + new Vector3(-size * .4f + i * size * .133f, .32f, -size * .51f), new Vector3(.025f, .55f, .035f), edge);
                foreach (float x in new[] { -.2f, .2f }) Beam(props, "Container door latch", p + new Vector3(x, .08f, -size * .53f), p + new Vector3(x, .55f, -size * .53f), .022f, dark);
            }

            void Boundary()
            {
                foreach (int side in new[] { -1, 1 })
                {
                    Box(props, "Perimeter foundation fascia", new Vector3(side * 7.28f, -.25f, 0), new Vector3(.40f, .68f, 10.8f), wall);
                    for (int y = 0; y < 6; y++) Box(props, "Perimeter expansion joint", new Vector3(side * 7.5f, -.18f, -4.5f + y * 1.8f), new Vector3(.025f, .38f, .065f), dark);
                }
                for (int x = 0; x < 8; x++)
                {
                    var p = new Vector3(-7 + x * 2, .50f, 5.55f);
                    Box(props, "Rear enclosing industrial structure", p, new Vector3(1.92f, 1.45f, .72f), wall);
                    Box(props, "Rear structural pilaster", p + new Vector3(.8f, .03f, -.39f), new Vector3(.12f, 1.5f, .12f), steel);
                    if (x % 2 == 0) Box(props, "Rear utility light", p + new Vector3(0, .08f, -.377f), new Vector3(.65f, .10f, .03f), edge);
                }
            }

            void Gate(string name, Vector3 p, Material color)
            {
                var gate = new GameObject(name).transform; gate.SetParent(root, false); gate.localPosition = p;
                Box(gate, "Gate inset threshold", new Vector3(0, .014f, 0), new Vector3(.8f, .02f, .8f), dark);
                var corners = new[] { new Vector3(-.43f, .04f, -.43f), new Vector3(.43f, .04f, -.43f), new Vector3(.43f, .04f, .43f), new Vector3(-.43f, .04f, .43f) };
                for (int i = 0; i < 4; i++)
                {
                    var a = corners[i]; var b = corners[(i + 1) % 4];
                    Beam(gate, "Gate bottom frame", a, b, .024f, color); Beam(gate, "Gate upper frame", a + Vector3.up * .82f, b + Vector3.up * .82f, .024f, color);
                    Beam(gate, "Gate vertical frame", a, a + Vector3.up * .82f, .024f, color);
                }
            }

            void Vent(Transform parent, Vector3 p, float size)
            {
                Box(parent, "Service vent housing", p + Vector3.up * .16f, new Vector3(size, .32f, size * .78f), steel);
                Box(parent, "Vent inset", p + new Vector3(0, .16f, -size * .40f), new Vector3(size * .8f, .20f, .018f), dark);
                for (int i = 0; i < 5; i++) Box(parent, "Vent grille slat", p + new Vector3(0, .07f + i * .045f, -size * .42f), new Vector3(size * .78f, .012f, .025f), edge);
                Cylinder(parent, "Vent top fan rim", p + new Vector3(0, .325f, 0), new Vector3(size * .36f, .013f, size * .36f), dark);
                for (int i = 0; i < 4; i++)
                {
                    var blade = Box(parent, "Static ventilation fan blade", p + new Vector3(0, .344f, 0), new Vector3(size * .53f, .012f, size * .065f), edge);
                    blade.localRotation = Quaternion.Euler(0, i * 45, 0);
                }
            }

            void Pipe(Transform parent, Vector3 a, Vector3 b, float radius)
            {
                var t = Cylinder(parent, "Saved service pipe", (a + b) * .5f, new Vector3(radius * 2, Vector3.Distance(a, b) * .5f, radius * 2), steel);
                t.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
                foreach (float f in new[] { .12f, .88f })
                {
                    var ring = Cylinder(parent, "Pipe joint collar", Vector3.Lerp(a, b, f), new Vector3(radius * 2.5f, radius * .24f, radius * 2.5f), dark);
                    ring.localRotation = t.localRotation;
                }
            }

            void Prop(string filename, Vector3 p, float footprint, float maxHeight, float yaw)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Industrial/Models/" + filename);
                if (asset == null) throw new InvalidOperationException("Reusable industrial model missing: " + filename);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, props); instance.name = "Reused industrial model / " + filename;
                instance.transform.localPosition = Vector3.zero; instance.transform.localRotation = Quaternion.Euler(0, yaw, 0); instance.transform.localScale = Vector3.one;
                var renderers = instance.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) throw new InvalidOperationException("Model has no renderer: " + filename);
                Bounds Bounds() { var b = renderers[0].bounds; foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds); return b; }
                var bounds = Bounds(); float scale = Mathf.Min(footprint / Mathf.Max(.001f, Mathf.Max(bounds.size.x, bounds.size.z)), maxHeight / Mathf.Max(.001f, bounds.size.y));
                instance.transform.localScale = Vector3.one * scale; bounds = Bounds(); instance.transform.position += p - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select((m, i) => i % 2 == 0 ? steel : wall).ToArray();
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            }

            Transform Box(Transform parent, string name, Vector3 p, Vector3 size, Material material) => MeshObject(parent, name, cube, p, size, material);
            Transform Cylinder(Transform parent, string name, Vector3 p, Vector3 size, Material material) => MeshObject(parent, name, cylinder, p, size, material);
            Transform Plane(Transform parent, string name, Vector3 p, Vector2 size, Material material) => MeshObject(parent, name, quad, p, new Vector3(size.x, 1, size.y), material);
            void Beam(Transform parent, string name, Vector3 a, Vector3 b, float width, Material material)
            { var t = Box(parent, name, (a + b) * .5f, new Vector3(width, Vector3.Distance(a, b), width), material); t.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized); }
            Transform MeshObject(Transform parent, string name, Mesh mesh, Vector3 p, Vector3 scale, Material material)
            {
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); var t = go.transform; t.SetParent(parent, false); t.localPosition = p; t.localScale = scale;
                go.GetComponent<MeshFilter>().sharedMesh = mesh; var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true; return t;
            }
            Material Material(string suffix, Color tint, Texture2D texture, float gloss, float metallic = 0, bool glow = false)
            {
                string path = Art + "/Materials/Map" + layout.index.ToString("00") + "-" + suffix + ".mat"; var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (saved != null && !refreshTaskMaterials) return saved;
                var material = saved != null ? saved : new Material(Shader.Find("Standard")) { name = "Map" + layout.index.ToString("00") + "-" + suffix };
                tint.a = 1; material.color = tint; material.mainTexture = texture; material.enableInstancing = true;
                material.SetFloat("_Glossiness", gloss); material.SetFloat("_Metallic", metallic);
                if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", tint * .65f); }
                if (saved == null) AssetDatabase.CreateAsset(material, path); else EditorUtility.SetDirty(material);
                return material;
            }
            Material WaterMaterial()
            {
                string path = Art + "/Materials/Map" + layout.index.ToString("00") + "-Water.mat"; var saved = AssetDatabase.LoadAssetAtPath<Material>(path); if (saved != null) return saved;
                var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Materials/Industrial/CanalSurface.shader");
                if (shader == null) throw new InvalidOperationException("Existing static canal shader missing.");
                var material = new Material(shader) { name = "Map" + layout.index.ToString("00") + "-Water" };
                material.SetColor("_Deep", layout.index == 8 ? new Color(.035f, .22f, .23f) : new Color(.025f, .065f, .085f));
                material.SetColor("_Shallow", layout.index == 8 ? new Color(.10f, .46f, .43f) : new Color(.075f, .19f, .21f));
                AssetDatabase.CreateAsset(material, path); return material;
            }
            static Texture2D Texture(string filename)
            {
                var result = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/Industrial/Textures/" + filename);
                if (result == null) throw new InvalidOperationException("Existing industrial surface missing: " + filename); return result;
            }
        }

        readonly struct Palette
        {
            public readonly Color Floor, Wall, Cap, Steel, Edge;
            public Palette(Color floor, Color wall, Color cap, Color steel, Color edge) { Floor = floor; Wall = wall; Cap = cap; Steel = steel; Edge = edge; }
        }
        static readonly Palette[] Palettes = {
            new Palette(new Color(.70f,.66f,.58f),new Color(.35f,.35f,.34f),new Color(.65f,.61f,.54f),new Color(.39f,.40f,.38f),new Color(.70f,.63f,.46f)),
            new Palette(new Color(.51f,.56f,.63f),new Color(.29f,.34f,.40f),new Color(.48f,.53f,.59f),new Color(.39f,.44f,.49f),new Color(.65f,.65f,.55f)),
            new Palette(new Color(.31f,.36f,.48f),new Color(.24f,.28f,.37f),new Color(.40f,.44f,.54f),new Color(.29f,.35f,.43f),new Color(.38f,.62f,.72f)),
            new Palette(new Color(.63f,.62f,.58f),new Color(.36f,.38f,.39f),new Color(.56f,.56f,.52f),new Color(.43f,.39f,.35f),new Color(.60f,.49f,.33f)),
            new Palette(new Color(.53f,.55f,.54f),new Color(.30f,.33f,.34f),new Color(.50f,.51f,.46f),new Color(.45f,.39f,.30f),new Color(.66f,.52f,.28f)),
            new Palette(new Color(.71f,.61f,.43f),new Color(.39f,.36f,.30f),new Color(.61f,.55f,.42f),new Color(.41f,.40f,.35f),new Color(.70f,.61f,.36f)),
            new Palette(new Color(.48f,.51f,.50f),new Color(.24f,.29f,.29f),new Color(.45f,.48f,.46f),new Color(.29f,.37f,.36f),new Color(.34f,.57f,.53f)),
            new Palette(new Color(.72f,.70f,.62f),new Color(.37f,.40f,.40f),new Color(.62f,.64f,.59f),new Color(.36f,.47f,.53f),new Color(.72f,.66f,.43f))
        };
        static readonly Vector2Int[] Directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        static string KindName(char kind) => kind == 'H' ? "raised platform" : kind == 'P' ? "open pit / terrain only" : kind == 'W' ? "recessed water" : kind == 'B' ? "bridge deck" : kind == 'T' ? "paired tunnel" : kind == 'E' ? "heat pump" : "walkable ground";

        static Mesh PrimitiveMesh(string name, PrimitiveType type)
        {
            string path = Art + "/Meshes/" + name + ".asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved != null) return saved;
            var temporary = GameObject.CreatePrimitive(type); var mesh = Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(temporary);
            mesh.name = name; AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Mesh QuadMesh()
        {
            string path = Art + "/Meshes/GroundQuad.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved != null) return saved;
            var mesh = new Mesh { name = "GroundQuad", vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) }, uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, triangles = new[] { 0, 1, 2, 0, 2, 3 } };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Mesh BevelMesh()
        {
            string path = Art + "/Meshes/BeveledPlatformCap.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved != null) return saved;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3[] Ring(float radius, float y) => new[] { new Vector3(-radius, y, -radius), new Vector3(-radius, y, radius), new Vector3(radius, y, radius), new Vector3(radius, y, -radius) };
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            { int i = vertices.Count; vertices.AddRange(new[] { a, b, c, d }); uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }); triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            var bottom = Ring(.5f, -.5f); var shoulder = Ring(.5f, .25f); var top = Ring(.44f, .5f);
            Face(top[0], top[1], top[2], top[3]); Face(bottom[3], bottom[2], bottom[1], bottom[0]);
            for (int i = 0; i < 4; i++) { int j = (i + 1) % 4; Face(bottom[i], bottom[j], shoulder[j], shoulder[i]); Face(shoulder[i], shoulder[j], top[j], top[i]); }
            var mesh = new Mesh { name = "BeveledPlatformCap" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        static Mesh HopperMesh()
        {
            string path = Art + "/Meshes/TaperedFreightHopper.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved != null) return saved;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3[] Ring(float radius, float y) => new[] { new Vector3(-radius, y, -.5f), new Vector3(-radius, y, .5f), new Vector3(radius, y, .5f), new Vector3(radius, y, -.5f) };
            var low = Ring(.23f, -.5f); var high = Ring(.5f, .5f);
            for (int n = 0; n < 4; n++)
            {
                int next = (n + 1) % 4, start = vertices.Count;
                vertices.AddRange(new[] { low[n], low[next], high[next], high[n] }); uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "TaperedFreightHopper" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        [MenuItem("Yesterday Convoy/Approved Maps/Validate Saved Maps")]
        public static void Validate()
        {
            CheckEditor(); var source = ReadSource(); var setup = EditorSceneManager.GetSceneManagerSetup(); int totalRoutes = 0;
            try
            {
                foreach (var layout in source.maps)
                {
                    var profile = AssetDatabase.LoadAssetAtPath<ApprovedMapDefinition>(Data + "/Map" + layout.index.ToString("00") + ".asset");
                    if (profile == null || profile.id != layout.id || profile.encounter == null || profile.preview == null) throw new InvalidOperationException("Map asset incomplete: " + layout.id);
                    var encounter = profile.encounter; _ = encounter.Snapshot();
                    if (encounter.lanes.Length != layout.routes.Length || encounter.defenders.Length != layout.guards.Length) throw new InvalidOperationException("Map roster or route count drift: " + layout.id);
                    for (int i = 0; i < layout.routes.Length; i++)
                    {
                        var expected = layout.routes[i]; var actual = encounter.lanes[i];
                        if (actual.id != expected.id || actual.exitId != expected.exitId || actual.points.Length != expected.cells.Length) throw new InvalidOperationException("Map lane identity drift.");
                        for (int n = 0; n < actual.points.Length; n++)
                        {
                            var c = expected.cells[n]; if ((actual.points[n] - layout.World(c.x, c.y)).sqrMagnitude > .000001f) throw new InvalidOperationException("Approved route point changed.");
                            if ("GBTE".IndexOf(layout.Tile(c.x, c.y)) < 0) throw new InvalidOperationException("Route enters pit, water or high platform.");
                            if (n == 0) continue;
                            var previous = expected.cells[n - 1]; int distance = Mathf.Abs(c.x - previous.x) + Mathf.Abs(c.y - previous.y);
                            if (distance != 1 && !(layout.portals ?? Array.Empty<Portal>()).Any(p => p.routeId == actual.id && p.sourcePointIndex == n - 1 && p.destinationPointIndex == n)) throw new InvalidOperationException("Undeclared route jump.");
                        }
                    }
                    for (int i = 0; i < layout.guards.Length; i++)
                    {
                        var guard = layout.guards[i]; var p = layout.World(guard.cell.x, guard.cell.y, guard.high ? profile.platformHeight : 0);
                        if ((encounter.defenders[i].position - p).sqrMagnitude > .000001f) throw new InvalidOperationException("Approved guard cell changed.");
                        if (guard.high != (layout.Tile(guard.cell.x, guard.cell.y) == 'H')) throw new InvalidOperationException("Guard terrain layer differs.");
                    }
                    var scene = EditorSceneManager.OpenScene(ScenePath(layout), OpenSceneMode.Single);
                    profile = LoadProfile(layout); encounter = profile.encounter;
                    var field = Object.FindAnyObjectByType<AuthoredBattlefield>(); var startup = Object.FindAnyObjectByType<ApprovedMapBootstrap>();
                    if (field == null || startup == null || startup.map != profile) throw new InvalidOperationException("Saved map startup or battlefield missing.");
                    var tiles = field.GetComponentsInChildren<ApprovedMapTile>(true);
                    if (tiles.Length != layout.width * layout.height || tiles.Select(t => t.cell).Distinct().Count() != tiles.Length) throw new InvalidOperationException("Missing or duplicated saved tiles.");
                    foreach (var tile in tiles) if (tile.kind != layout.Tile(tile.cell.x, tile.cell.y).ToString() || (tile.transform.position - layout.World(tile.cell.x, tile.cell.y)).sqrMagnitude > .000001f) throw new InvalidOperationException("Saved tile metadata or position drift.");
                    foreach (var filter in field.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh == null || !AssetDatabase.Contains(filter.sharedMesh)) throw new InvalidOperationException("Missing or temporary map mesh: " + filter.name);
                    foreach (var renderer in field.GetComponentsInChildren<Renderer>(true)) foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || !AssetDatabase.Contains(material) || !material.shader.isSupported) throw new InvalidOperationException("Missing or unsupported persistent material: " + renderer.name);
                        if (material.HasProperty("_MainTex") && material.mainTexture != null)
                        {
                            string texture = AssetDatabase.GetAssetPath(material.mainTexture);
                            if (texture.StartsWith(Art + "/Concepts/", StringComparison.Ordinal) || texture.StartsWith(Art + "/Thumbnails/", StringComparison.Ordinal))
                                throw new InvalidOperationException("A concept or thumbnail image must not be used as the map floor.");
                        }
                    }
                    if (field.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Decoration introduces collision rules.");
                    var names = field.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
                    if (!names.Any(n => n.StartsWith("Landmark / " + layout.index.ToString("00") + " / ", StringComparison.Ordinal)))
                        throw new InvalidOperationException("Required visible theme landmark missing: " + layout.id);
                    if (names.Count(n => n.EndsWith(" / Red entry", StringComparison.Ordinal)) != RedCounts[layout.index - 1] || names.Count(n => n.EndsWith(" / Blue exit", StringComparison.Ordinal)) != BlueCounts[layout.index - 1]) throw new InvalidOperationException("Saved gate count drift.");
                    foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0) throw new InvalidOperationException("Missing map script: " + t.name);
                    totalRoutes += encounter.lanes.Length;
                }
                if (totalRoutes != 20) throw new InvalidOperationException("Expected twenty approved routes.");
                Debug.Log("APPROVED_MAPS_SAVED_ASSETS_PASS: 8 scenes / 1120 editable tiles / 20 exact V2 routes / matching gates and guard cells / persistent meshes and materials / no concept-floor images.");
            }
            finally { RestoreSetup(setup); }
        }

        [MenuItem("Yesterday Convoy/Approved Maps/Render Saved Map Previews")]
        public static void RenderPreviews()
        {
            CheckEditor(); var source = ReadSource(); var setup = EditorSceneManager.GetSceneManagerSetup(); Directory.CreateDirectory(Evidence); EnsureFolder(Art + "/Thumbnails");
            try
            {
                foreach (var layout in source.maps)
                {
                    EditorSceneManager.OpenScene(ScenePath(layout), OpenSceneMode.Single); var camera = Camera.main;
                    var target = new RenderTexture(1920, 1080, 24); var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
                    var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                    try
                    {
                        camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                        byte[] png = image.EncodeToPNG(); string name = "Map" + layout.index.ToString("00");
                        File.WriteAllBytes(Evidence + "/" + name + "-editor.png", png);
                        string thumbnail = Art + "/Thumbnails/" + name + ".png"; File.WriteAllBytes(thumbnail, png);
                        AssetDatabase.ImportAsset(thumbnail, ImportAssetOptions.ForceUpdate);
                        var importer = AssetImporter.GetAtPath(thumbnail) as TextureImporter;
                        if (importer == null) throw new InvalidOperationException("Actual scene thumbnail importer missing: " + thumbnail);
                        importer.textureType = TextureImporterType.Default; importer.maxTextureSize = 512; importer.mipmapEnabled = false;
                        importer.isReadable = false; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
                        var profile = LoadProfile(layout); profile.preview = AssetDatabase.LoadAssetAtPath<Texture2D>(thumbnail);
                        if (profile.preview == null) throw new InvalidOperationException("Actual scene thumbnail missing after import: " + thumbnail);
                        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
                    }
                    finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target); }
                }
                AssetDatabase.SaveAssets();
                Debug.Log("APPROVED_MAPS_EDITOR_PREVIEWS_SAVED: " + Evidence + "; all eight map previews now use actual saved-scene thumbnails.");
            }
            finally { RestoreSetup(setup); }
        }

        public static void BuildWindows() { Validate(); BaselineTools.BuildWindows(); }
        static string ScenePath(Layout layout) => "Assets/Scenes/" + layout.sceneName + ".unity";
        static ApprovedMapDefinition LoadProfile(Layout layout)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ApprovedMapDefinition>(Data + "/Map" + layout.index.ToString("00") + ".asset");
            if (profile == null || profile.encounter == null) throw new InvalidOperationException("Persistent approved map profile missing after scene/import transition: " + layout.id);
            return profile;
        }
        static DefenderTrialDefinition LoadEncounter(Layout layout)
        {
            var encounter = AssetDatabase.LoadAssetAtPath<DefenderTrialDefinition>(Data + "/Map" + layout.index.ToString("00") + "Battle.asset");
            if (encounter == null) throw new InvalidOperationException("Persistent approved encounter missing after scene/import transition: " + layout.id);
            return encounter;
        }
        static void RestoreSetup(SceneSetup[] setup) { if (setup.Any(s => s.isLoaded && !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        static void ImportConceptThumbnails()
        {
            foreach (string name in ImageNames)
            {
                string path = Art + "/Concepts/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Approved concept image is not imported: " + path);
                if (importer.maxTextureSize == 1024 && !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Clamp) continue;
                importer.maxTextureSize = 1024; importer.mipmapEnabled = false; importer.isReadable = false;
                importer.textureType = TextureImporterType.Default; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return; string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'); EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder)))) throw new InvalidOperationException("Cannot create map asset folder: " + folder);
        }
    }
}
