using System;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class PatriotArtTools
    {
        const string Root="Assets/Art/VFX/Skills/Patriot/Resources/PatriotVfx";
        [MenuItem("Yesterday Convoy/Art/Configure Patriot runtime atlases")]
        public static void Configure()
        {
            int count=0;
            foreach(string id in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}))
            {
                string path=AssetDatabase.GUIDToAssetPath(id);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                // RGB in zero-alpha pixels is the original additive light: never alpha-bleed or erase it.
                importer.sRGBTexture=false;importer.alphaIsTransparency=false;importer.mipmapEnabled=false;importer.isReadable=false;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
                var windows=importer.GetPlatformTextureSettings("Standalone");windows.overridden=true;windows.maxTextureSize=2048;
                windows.format=TextureImporterFormat.BC7;windows.compressionQuality=100;windows.crunchedCompression=false;
                importer.SetPlatformTextureSettings(windows);importer.SaveAndReimport();count++;
            }
            if(count!=99)throw new InvalidOperationException("Patriot expected 99 runtime atlas pages, got "+count);
            Debug.Log("PATRIOT_IMPORT_PASS: "+count+" BC7 Windows pages; original PNG bytes retained.");
        }
    }
}
