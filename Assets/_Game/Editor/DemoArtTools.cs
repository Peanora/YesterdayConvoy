using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class DemoArtTools
    {
        public const string CardPath="Assets/_Game/Data/Cards/WisadelApril.asset";
        public static void Validate()
        {
            InitialDemoTools.Validate();
            var card=AssetDatabase.LoadAssetAtPath<CardDefinition>(CardPath);
            var demo=UnityEngine.Object.FindAnyObjectByType<InitialDemo>();
            var view=UnityEngine.Object.FindAnyObjectByType<DemoCardView>();
            if(card==null || card.RecruitCost!=5 || card.Portrait==null || card.BattleSprite==null || demo.Card!=card || view==null)
                throw new InvalidOperationException("Current comic card references are incomplete.");
            ComicArtTools.Validate(card);
            TacticalMapTools.ValidatePresentation();
            Debug.Log("DEMO_ART_PASS: current comic card/frames, authored Kroos and tactical map.");
        }

        public static Sprite ImportSprite(string path, float visibleHeight)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = source.GetPixels32();
            int minX = source.width, maxX = -1, minY = source.height, maxY = -1;
            for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                    if (pixels[y * source.width + x].a > 16)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            if (maxX < minX) throw new InvalidOperationException("Empty sprite: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = visibleHeight > 0 ? (maxY - minY + 1) / visibleHeight : 512;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = visibleHeight > 0 ? new Vector2((minX + maxX + 1) / (2f * source.width), minY / (float)source.height) : new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            UnityEngine.Object.DestroyImmediate(source);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

    }
}
