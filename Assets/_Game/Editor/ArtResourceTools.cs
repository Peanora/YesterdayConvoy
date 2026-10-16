using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Imports the small approved art library without opening or changing any scene.</summary>
    public static class ArtResourceTools
    {
        private const string Root = "Assets/_Game/Art";

        [MenuItem("Yesterday Convoy/Art/Configure and Validate Library")]
        public static void ConfigureAndValidate()
        {
            AssetDatabase.Refresh();
            var verified = new List<string>();
            int icons = 0, textures = 0, sounds = 0, fonts = 0;
            foreach (string file in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".png")
                {
                    bool icon = path.StartsWith(Root + "/UI/Icons/", StringComparison.Ordinal);
                    bool particle = path.StartsWith(Root + "/VFX/Common/", StringComparison.Ordinal);
                    if (!icon && !particle) continue; // Future character/map imports keep their own settings.
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || !importer.DoesSourceTextureHaveAlpha())
                        throw new InvalidOperationException("Missing texture importer or alpha: " + path);
                    importer.textureType = icon ? TextureImporterType.Sprite : TextureImporterType.Default;
                    if (icon)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 256;
                        importer.spritePivot = new Vector2(0.5f, 0.5f);
                    }
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 1024;
                    importer.SaveAndReimport();
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture == null || texture.width == 0 || texture.height == 0)
                        throw new InvalidOperationException("Texture did not load: " + path);
                    if (icon)
                    {
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (sprite == null || texture.width != 256 || texture.height != 256)
                            throw new InvalidOperationException("Icon is not a 256px sprite: " + path);
                        icons++;
                    }
                    else textures++;
                }
                else if (extension == ".ogg")
                {
                    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null) throw new InvalidOperationException("Missing audio importer: " + path);
                    bool music = path.Contains("/BGM/");
                    AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                    settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.75f;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = !music;
                    importer.SaveAndReimport();
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip == null || clip.length <= 0 || clip.samples <= 0)
                        throw new InvalidOperationException("Audio did not decode: " + path);
                    Debug.Log($"ART_AUDIO: {path}; seconds={clip.length:F3}; channels={clip.channels}.");
                    sounds++;
                }
                else if (path == Root + "/Fonts/NotoSansSC-Regular.otf")
                {
                    Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
                    if (font == null) throw new InvalidOperationException("Font did not load: " + path);
                    const string required = "昨日圆车生命防御费用召唤羁绊冷却暂停胜利失败ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789，。！？★→";
                    foreach (char character in required)
                        if (!font.HasCharacter(character))
                            throw new InvalidOperationException($"Font missing U+{(int)character:X4}: {path}");
                    fonts++;
                }
                else continue;
                verified.Add(path);
            }
            if (icons < 12 || textures < 6 || sounds < 7 || fonts != 1)
                throw new InvalidOperationException($"Incomplete art library: icons={icons}, textures={textures}, audio={sounds}, fonts={fonts}.");
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("artifacts/art-research");
            File.WriteAllLines("artifacts/art-research/unity-verified-paths.txt", verified);
            Debug.Log($"ART_IMPORT_PASS: icons={icons}; textures={textures}; audio={sounds}; fonts={fonts}; total={verified.Count}. Import validation only; no gameplay or scene replacement.");
        }
    }
}
