using System;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class FireRainArtTools
    {
        private const string Root="Assets/_Game/Art/VFX/Skills/Talulah/Resources/";
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"FireRainEmbers.png");
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=1024; importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp; importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false; importer.sRGBTexture=true;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"FireRainEmbers.png");
            if(texture==null || texture.width!=640 || texture.height!=640)
                throw new InvalidOperationException("Expected approved 25-frame, 640x640 transparent ember atlas.");
            foreach(string name in new[]{"FireRainScreen","FireRainAdditive"})
            {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+name+".shader");
                if(shader==null) throw new InvalidOperationException("Missing fire shader: "+name);
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    if(message.severity.ToString()=="Error") throw new InvalidOperationException(message.message);
            }
            Debug.Log("FIRE_RAIN_ART_PASS: 640x640 alpha atlas / 25 frames, two shaders; no scene or combat config changes.");
        }
    }
}
