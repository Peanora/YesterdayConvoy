using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Private native-atlas import settings; unbound StreamingAssets are not preloaded.</summary>
    public sealed class TalulahNativeImport:AssetPostprocessor
    {
        const string Root="Assets/Art/VFX/Skills/Talulah/Resources/TalulahNative";
        void OnPreprocessTexture()
        {
            if(assetPath.StartsWith(Root+"/",System.StringComparison.Ordinal)&&assetPath.EndsWith(".png",System.StringComparison.OrdinalIgnoreCase))
                Apply((TextureImporter)assetImporter);
        }
        static void Apply(TextureImporter importer)
        {
            // A=0 pixels can still contain original additive light. Never alpha-bleed or erase RGB.
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.sRGBTexture=false;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
            importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;
            var windows=importer.GetPlatformTextureSettings("Standalone");windows.overridden=true;windows.maxTextureSize=2048;
            windows.format=TextureImporterFormat.BC7;windows.compressionQuality=100;windows.crunchedCompression=false;
            importer.SetPlatformTextureSettings(windows);
        }
        [MenuItem("Yesterday Convoy/Art/Configure Talulah native runtime atlases")]
        public static void Configure()
        {
            int count=0;
            foreach(var id in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));
                Apply(importer);importer.SaveAndReimport();count++;
            }
            Debug.Log("TALULAH_NATIVE_IMPORT_PASS: "+count+" BC7 pages; encoded source PNG bytes retained.");
        }
        public static void Diagnose()
        {
            string path=Root+"/ambient-A-foot-00.png";
            var direct=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var resource=Resources.Load<Texture2D>("TalulahNative/ambient-A-foot-00");
            Debug.Log("TALULAH_PAGE_DIAG: type="+AssetDatabase.GetMainAssetTypeAtPath(path)+"; direct="+(direct==null?"NULL":direct.width+"x"+direct.height)+"; resource="+(resource==null?"NULL":resource.width+"x"+resource.height));
            foreach(var value in Resources.LoadAll("TalulahNative"))Debug.Log("TALULAH_RESOURCE_DIAG: "+value.name+" "+value.GetType());
        }
    }
}
