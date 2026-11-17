using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    // Formation-only copies. Does not modify the source catalogue, old gate materials or any scene.
    public static class FormationBoardMaterialTools
    {
        public const string ArtPath = "Assets/Art/Environment/FormationBoard";
        public const string SurfaceShaderName = "YesterdayConvoy/FormationNativeSurface";
        public const string GateShaderName = "YesterdayConvoy/FormationNativeGate";
        public const string ReflectionPath = ArtPath + "/OriginalLS5Reflection.cubemap";
        static bool ready;

        [MenuItem("Yesterday Convoy/Formation Battle/Import Board Materials")]
        public static void EnsureImported()
        {
            if (ready) return;
            var surfaceShader = Shader.Find(SurfaceShaderName);
            var gateShader = Shader.Find(GateShaderName);
            if (surfaceShader == null || gateShader == null)
                throw new InvalidOperationException("Import FormationBoard surface and gate shaders before authoring.");
            var catalog = FormationNativeAssetTools.EnsureImported();
            Directory.CreateDirectory(ArtPath + "/Materials");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var reflection = EnsureReflection();
            var ambient = AssetDatabase.LoadAssetAtPath<Texture2D>(FormationNativeAssetTools.ArtPath + "/Textures/Lightmap-0_comp_light.png");
            if (ambient == null) throw new InvalidOperationException("Original LS-5 ambient atlas missing.");
            foreach (var source in catalog.materials)
                for (int checker = 0; checker < 3; checker++)
                    ImportMaterial(source, checker, surfaceShader, ambient, reflection);
            AssetDatabase.SaveAssets();
            ready = true;
            Debug.Log("FORMATION_BOARD_MATERIALS_PASS: formation-only source surface copies, atlas ambient, original reflection mips; no scene changed");
        }

        public static Material GetMaterial(string sourceMatName, int checker = 0)
        {
            EnsureImported();
            if (checker < 0 || checker > 2) throw new ArgumentOutOfRangeException(nameof(checker));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(sourceMatName, checker));
            return material != null ? material : throw new ArgumentException("Unknown LS-5 board source material: " + sourceMatName);
        }

        public static Material GetGateMaterial(Material source, bool red)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            EnsureImported();
            var path = ArtPath + "/Materials/" + Safe(source.name) + (red ? "_Red.mat" : "_Blue.mat");
            var target = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (target == null)
            {
                target = new Material(Shader.Find(GateShaderName));
                AssetDatabase.CreateAsset(target, path);
            }
            target.shader = Shader.Find(GateShaderName);
            target.name = source.name + (red ? " / board red" : " / board blue");
            target.SetTexture("_MainTex", source.mainTexture);
            target.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
            target.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
            target.SetFloat("_DstBlend", source.GetFloat("_DstBlend"));
            target.SetColor("_TintColor", red ? new Color(.45f,.11f,.13f,.75f) : new Color(.22f,.42f,.65f,.65f));
            target.renderQueue = source.renderQueue;
            EditorUtility.SetDirty(target);
            return target;
        }

        static string MaterialPath(string sourceName, int checker)
            => ArtPath + "/Materials/" + Safe(sourceName) + (checker == 0 ? "" : "_Checker" + checker) + ".mat";

        static void ImportMaterial(FormationNativeAssetTools.NativeMaterial source, int checker, Shader surfaceShader, Texture2D ambient, Cubemap reflection)
        {
            bool background = source.name == "MT_Qcity_BG";
            var shader = background ? Shader.Find("Unlit/Texture") : surfaceShader;
            var path = MaterialPath(source.name, checker);
            var target = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (target == null) { target = new Material(shader); AssetDatabase.CreateAsset(target, path); }
            target.shader = shader;
            target.name = source.name + (checker == 0 ? " / board" : " / board checker " + checker);
            target.shaderKeywords = Array.Empty<string>();
            foreach (var slot in source.textures)
            {
                if (!target.HasProperty(slot.property) || string.IsNullOrEmpty(slot.file)) continue;
                target.SetTexture(slot.property, AssetDatabase.LoadAssetAtPath<Texture2D>(FormationNativeAssetTools.ArtPath + "/" + slot.file));
                target.SetTextureScale(slot.property, new Vector2(slot.scale[0],slot.scale[1]));
                target.SetTextureOffset(slot.property, new Vector2(slot.offset[0],slot.offset[1]));
            }
            foreach (var value in source.floats)
                if (target.HasProperty(value.name)) target.SetFloat(value.name, value.value);
            foreach (var value in source.colors)
                if (target.HasProperty(value.name)) target.SetColor(value.name, new Color(value.value[0],value.value[1],value.value[2],value.value[3]));
            if (!background)
            {
                bool alphaClip = source.keywords.Contains("_ALPHATEST_ON");
                target.SetFloat("_AlphaClip", alphaClip ? 1 : 0);
                target.SetFloat("_EmissionEnabled", source.keywords.Contains("_EMISSION") ? 1 : 0);
                target.SetFloat("_AmbientGain", 5);
                target.SetFloat("_ReflectionGain", .93f);
                target.SetFloat("_ReflectionDecode", 5);
                target.SetTexture("_NativeAmbient", ambient);
                target.SetTexture("_NativeReflection", reflection);
                float tint = checker == 1 ? .93f : checker == 2 ? 1.10f : 1;
                target.SetColor("_CheckerTint", new Color(tint,tint,tint,1));
                target.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
                target.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            }
            EditorUtility.SetDirty(target);
        }

        static Cubemap EnsureReflection()
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath);
            if (cube != null) return cube;
            cube = new Cubemap(128, TextureFormat.RGBA32, true) { name = "Original LS5 ReflectionProbe-0 RGBM", filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            string[] faces = {"px","nx","py","ny","pz","nz"};
            for (int mip = 0; mip < 8; mip++)
                for (int face = 0; face < 6; face++)
                {
                    var path = ArtPath + "/ReflectionSource/ReflectionProbe-0-" + faces[face] + "-mip" + mip + ".png";
                    if (!File.Exists(path)) throw new FileNotFoundException("Original LS-5 reflection face missing", path);
                    var image = new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                    try
                    {
                        if (!ImageConversion.LoadImage(image,File.ReadAllBytes(path),false) || image.width != Math.Max(1,128 >> mip) || image.height != image.width)
                            throw new InvalidDataException("Original LS-5 reflection mip size mismatch: " + path);
                        cube.SetPixels(image.GetPixels(), (CubemapFace)face, mip);
                    }
                    finally { Object.DestroyImmediate(image); }
                }
            // Preserve exported RGBM channels and every source mip; do not recompute mip filtering.
            cube.Apply(false,false);
            AssetDatabase.CreateAsset(cube,ReflectionPath);
            return cube;
        }

        static string Safe(string value) => string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_'));
    }
}
