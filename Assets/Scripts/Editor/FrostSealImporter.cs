using UnityEditor;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public sealed class FrostSealImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Art/VFX/Skills/FrostNova/Resources/FrostSeal/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Compressed;importer.compressionQuality=100;
        }
    }
}
