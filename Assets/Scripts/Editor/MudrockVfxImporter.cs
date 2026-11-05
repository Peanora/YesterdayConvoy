using UnityEditor;
using UnityEngine;
namespace YesterdayConvoy.Editor
{
    public sealed class MudrockVfxImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Art/VFX/Mudrock/Resources/MudrockVfx/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.mipmapEnabled=false;
            // Preserve original premultiplied/additive RGB even where alpha is zero.
            t.alphaIsTransparency=false;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;
            t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Compressed;t.compressionQuality=100;
        }
    }
}
