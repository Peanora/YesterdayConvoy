using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Imports source-derived parts only. Scene ownership stays with FormationNativeMapTools.
    public static class FormationNativeAssetTools
    {
        public const string ArtPath = "Assets/Art/Environment/FormationNative";
        public const string ManifestPath = ArtPath + "/ls5-native.json";
        static NativeCatalog cached;
        static string importedHash;

        [Serializable] public sealed class NativeCatalog
        {
            public int version;
            public string sourceGame, sourceStage, resourceVersion, coordinateConversion;
            public NativeTexture[] textures;
            public NativeMaterial[] materials;
            public NativePart[] parts;
        }
        [Serializable] public sealed class SourceReference { public string file, pathId; }
        [Serializable] public sealed class NativeTexture
        {
            public string name, file, sha256;
            public int width, height, sourceFormat;
            public bool normalMap, linear;
            public SourceReference source;
        }
        [Serializable] public sealed class TextureSlot
        {
            public string property, file, pathId;
            public float[] scale, offset;
        }
        [Serializable] public sealed class FloatProperty { public string name; public float value; }
        [Serializable] public sealed class ColorProperty { public string name; public float[] value; }
        [Serializable] public sealed class NativeMaterial
        {
            public string name, key;
            public SourceReference source;
            public TextureSlot[] textures;
            public FloatProperty[] floats;
            public ColorProperty[] colors;
            public string[] keywords;
            public int queue;
        }
        [Serializable] public sealed class NativeSubmesh { public int[] triangles; public string material; }
        [Serializable] public sealed class SourceTransform
        {
            public float[] position, rotation, scale, worldMatrix;
        }
        [Serializable] public sealed class NativePart
        {
            public int id, staticBatchFirst, staticBatchCount, lightmapIndex, castShadows;
            public string name, sourceMeshName;
            public SourceReference renderer, sourceMesh;
            public SourceTransform sourceTransform;
            public float[] position, boundsMin, boundsMax, vertices, normals, uv, uv2, colors, lightmapScaleOffset;
            public NativeSubmesh[] submeshes;
            public bool enabled, receiveShadows;
            public Vector3 SourcePosition => V3(position);
            public Vector3 SourceBoundsMin => V3(boundsMin);
            public Vector3 SourceBoundsMax => V3(boundsMax);
            public string AssetName => "Part_" + id.ToString("D3") + "_" + Safe(name);
        }

        [MenuItem("Yesterday Convoy/Formation Native/Import Original LS-5 Parts")]
        public static void Import() { EnsureImported(); Validate(); }

        public static NativeCatalog EnsureImported()
        {
            if (!File.Exists(ManifestPath)) throw new FileNotFoundException("Missing LS-5 source manifest", ManifestPath);
            var hash = Hash128.Compute(File.ReadAllText(ManifestPath)).ToString();
            if (cached != null && importedHash == hash) return cached;
            cached = JsonUtility.FromJson<NativeCatalog>(File.ReadAllText(ManifestPath));
            if (cached == null || cached.parts == null || cached.parts.Length != 143)
                throw new InvalidOperationException("Expected the complete 143-part LS-5 export.");
            Directory.CreateDirectory(ArtPath + "/Meshes");
            Directory.CreateDirectory(ArtPath + "/Materials");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var texture in cached.textures)
            {
                var path = ArtPath + "/" + texture.file;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing native texture " + path);
                var desiredType = texture.normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                bool changed = importer.textureType != desiredType || importer.sRGBTexture == texture.linear ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < Math.Max(texture.width, texture.height);
                if (!changed) continue;
                importer.textureType = desiredType;
                importer.sRGBTexture = !texture.linear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = Math.Max(1024, Math.Max(texture.width, texture.height));
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            foreach (var source in cached.materials) ImportMaterial(source);
            foreach (var source in cached.parts) ImportMesh(source);
            AssetDatabase.SaveAssets();
            importedHash = hash;
            Debug.Log("FORMATION_NATIVE_ASSETS_IMPORTED: 143 source-derived LS-5 parts, 12 textures, 5 materials; no scene modified");
            return cached;
        }

        public static NativePart FindPartName(string name)
        {
            var part = EnsureImported().parts.FirstOrDefault(x => x.name == name);
            return part ?? throw new ArgumentException("Unknown original LS-5 part " + name);
        }
        public static NativePart[] FindParts(string nameFragment) => EnsureImported().parts
            .Where(x => x.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        public static Mesh GetMesh(NativePart source) => AssetDatabase.LoadAssetAtPath<Mesh>(ArtPath + "/Meshes/" + source.AssetName + ".asset");
        public static Material GetMaterial(string sourceName) => AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/Materials/" + Safe(sourceName) + ".mat");

        public static GameObject CreatePart(string name, Transform parent = null, bool useSourcePosition = false)
            => CreatePart(FindPartName(name), parent, useSourcePosition);
        public static GameObject CreatePart(NativePart source, Transform parent = null, bool useSourcePosition = false)
        {
            EnsureImported();
            var go = new GameObject(source.name + " / original LS-5");
            go.transform.SetParent(parent, false);
            if (useSourcePosition) go.transform.localPosition = source.SourcePosition;
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(source);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.submeshes.Select(x => GetMaterial(x.material)).ToArray();
            renderer.shadowCastingMode = (ShadowCastingMode)source.castShadows;
            renderer.receiveShadows = source.receiveShadows;
            // Baked UV2 is already in atlas space. Never multiply it by the original renderer ST again.
            renderer.lightmapScaleOffset = new Vector4(1, 1, 0, 0);
            renderer.lightmapIndex = -1;
            return go;
        }

        static void ImportMaterial(NativeMaterial source)
        {
            var path = ArtPath + "/Materials/" + Safe(source.name) + ".mat";
            var shader = Shader.Find(source.name == "MT_Qcity_BG" ? "Unlit/Texture" : "Standard");
            if (shader == null) throw new InvalidOperationException("Missing compatible native material shader");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.shaderKeywords = Array.Empty<string>();
            foreach (var slot in source.textures)
            {
                if (!material.HasProperty(slot.property) || string.IsNullOrEmpty(slot.file)) continue;
                material.SetTexture(slot.property, AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + "/" + slot.file));
                material.SetTextureScale(slot.property, V2(slot.scale));
                material.SetTextureOffset(slot.property, V2(slot.offset));
            }
            foreach (var value in source.floats) if (material.HasProperty(value.name)) material.SetFloat(value.name, value.value);
            foreach (var value in source.colors) if (material.HasProperty(value.name)) material.SetColor(value.name, C4(value.value));
            foreach (var keyword in source.keywords ?? Array.Empty<string>()) material.EnableKeyword(keyword);
            material.renderQueue = source.queue;
            if ((source.keywords ?? Array.Empty<string>()).Contains("_ALPHATEST_ON"))
            {
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            EditorUtility.SetDirty(material);
        }

        static void ImportMesh(NativePart source)
        {
            var path = ArtPath + "/Meshes/" + source.AssetName + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear();
            mesh.name = source.name + " / source " + source.renderer.pathId;
            mesh.indexFormat = source.vertices.Length / 3 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = V3s(source.vertices);
            if (source.normals?.Length == source.vertices.Length) mesh.normals = V3s(source.normals);
            if (source.uv?.Length == source.vertices.Length / 3 * 2) mesh.uv = V2s(source.uv);
            if (source.uv2?.Length == source.vertices.Length / 3 * 2) mesh.uv2 = V2s(source.uv2);
            if (source.colors?.Length == source.vertices.Length / 3 * 4)
                mesh.colors = Enumerable.Range(0, source.colors.Length / 4).Select(i => new Color(source.colors[i*4], source.colors[i*4+1], source.colors[i*4+2], source.colors[i*4+3])).ToArray();
            mesh.subMeshCount = source.submeshes.Length;
            for (int i = 0; i < source.submeshes.Length; i++) mesh.SetTriangles(source.submeshes[i].triangles, i);
            if (source.normals == null || source.normals.Length == 0) mesh.RecalculateNormals();
            if (mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
        }

        [MenuItem("Yesterday Convoy/Formation Native/Validate Original LS-5 Parts")]
        public static void Validate()
        {
            var data = EnsureImported();
            foreach (var source in data.parts)
            {
                var mesh = GetMesh(source);
                if (mesh == null || mesh.vertexCount != source.vertices.Length / 3 || mesh.subMeshCount != source.submeshes.Length)
                    throw new InvalidOperationException("Native mesh mismatch " + source.name);
                if (mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)
                    throw new InvalidOperationException("Lost native UVs/normals " + source.name);
                foreach (var submesh in source.submeshes)
                    if (GetMaterial(submesh.material) == null || submesh.triangles.Any(i => i < 0 || i >= mesh.vertexCount))
                        throw new InvalidOperationException("Invalid native material/index " + source.name);
            }
            Debug.Log("FORMATION_NATIVE_ASSETS_PASS: all 143 LS-5 mesh parts retain source UVs, normals, submeshes and material references");
        }

        static string Safe(string value) => string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_'));
        public static Vector3 V3(float[] value) => new Vector3(value[0], value[1], value[2]);
        static Vector2 V2(float[] value) => new Vector2(value[0], value[1]);
        static Color C4(float[] value) => new Color(value[0], value[1], value[2], value[3]);
        static Vector3[] V3s(float[] values) => Enumerable.Range(0, values.Length / 3).Select(i => new Vector3(values[i*3],values[i*3+1],values[i*3+2])).ToArray();
        static Vector2[] V2s(float[] values) => Enumerable.Range(0, values.Length / 2).Select(i => new Vector2(values[i*2],values[i*2+1])).ToArray();
    }
}
