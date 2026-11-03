using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    // These are ordinary texture atlases sliced by the read-only runtime manifest.
    public sealed class DefenderVfxImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>1;
        public static void ReimportAndValidate()
        {
            const string folder="Assets/Art/VFX/Skills/Defenders/Resources/DefenderVfx";
            int count=0;
            foreach(string file in System.IO.Directory.GetFiles(folder,"*.png"))
            {
                string path=file.Replace('\\','/');AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                if(AssetDatabase.LoadAssetAtPath<Texture2D>(path)==null)throw new System.IO.InvalidDataException("Invalid VFX texture: "+path);
                count++;
            }
            Debug.Log("DEFENDER_VFX_IMPORT_PASS: "+count+" 2D atlases");
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Art/VFX/Skills/Defenders/Resources/DefenderVfx/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Compressed;importer.compressionQuality=100;
        }
    }
}
