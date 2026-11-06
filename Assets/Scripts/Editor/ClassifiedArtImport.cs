using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    // These imported previews are not the runtime atlas: the decoder still reads the original PNG bytes.
    public sealed class ClassifiedArtImport:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!ArtSourcePaths.IsSourceTexture(assetPath))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false;
            importer.isReadable=false;
            importer.alphaIsTransparency=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;
            importer.maxTextureSize=2048;
        }
    }
}
