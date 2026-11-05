using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public sealed class FireRainNativeImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Art/VFX/Skills/Talulah/Resources/TalulahFireRain/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.mipmapEnabled=false;
            t.alphaIsTransparency=false;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;
            t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;
            t.textureCompression=TextureImporterCompression.CompressedHQ;t.compressionQuality=100;
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.BC7,compressionQuality=100});
        }
    }
}
