using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Local, approved flipbooks. Original media stays outside the public source repository.</summary>
    internal sealed class OriginalTalulahArt
    {
        private const string CardId = "CARD_TALULAH_TRIAL";
        private static bool attempted;
        private static OriginalTalulahArt cached;
        private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        internal Sprite Portrait { get; private set; }
        internal float ReferenceSpeed { get; private set; }
        internal float AttackContact { get; private set; }

        [Serializable] private sealed class Manifest
        {
            public int version;
            public string cardId;
            public float ppu, referenceSpeed, attackContact;
            public ClipData[] clips;
        }
        [Serializable] private sealed class ClipData
        {
            public string key;
            public float duration, fps, pivotX, pivotY;
            public int count, cellWidth, cellHeight, columns;
            public PageData[] pages;
        }
        [Serializable] private sealed class PageData { public string file; public int width, height, count; }

        internal sealed class Clip
        {
            internal readonly Sprite[] Frames;
            internal readonly float Duration;
            internal Clip(Sprite[] frames, float duration) { Frames = frames; Duration = duration; }
            internal Sprite At(double time, bool loop, out int index)
            {
                double normalized = loop ? time / Duration - Math.Floor(time / Duration) : Math.Max(0, Math.Min(1, time / Duration));
                index = Mathf.Clamp((int)(normalized * Frames.Length), 0, Frames.Length - 1);
                return Frames[index];
            }
        }

        internal Clip this[string key] => clips[key];
        internal static Sprite BattleSprite(CardDefinition card) => For(card)?.clips["idle"].Frames[0] ?? card.BattleSprite;
        internal static Sprite CardPortrait(CardDefinition card) => For(card)?.Portrait ?? card.Portrait;

        internal static OriginalTalulahArt For(CardDefinition card)
        {
            if (card == null || card.CardId != CardId) return null;
            if (attempted) return cached;
            attempted = true;
            string root = Path.Combine(Application.streamingAssetsPath, "TalulahOriginal");
            string manifestPath = Path.Combine(root, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("TALULAH_ORIGINAL_UNAVAILABLE: local original-art package is not installed; retained prototype art is used. See docs/art/TALULAH-ORIGINAL-INTEGRATION.md.");
                return null;
            }
            var allocated = new List<UnityEngine.Object>();
            try
            {
                var data = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
                if (data.version != 1 || data.cardId != CardId || data.ppu <= 0 || data.referenceSpeed <= 0 ||
                    data.attackContact <= 0 || data.clips == null || data.clips.Length != 4)
                    throw new InvalidDataException("Invalid original-art manifest.");
                var art = new OriginalTalulahArt { ReferenceSpeed = data.referenceSpeed, AttackContact = data.attackContact };
                foreach (var item in data.clips)
                {
                    if (item.key != "idle" && item.key != "walk" && item.key != "attack" && item.key != "cast")
                        throw new InvalidDataException("Unknown clip.");
                    if (item.count <= 0 || item.count > 600 || item.duration <= 0 || item.cellWidth <= 0 || item.cellHeight <= 0 ||
                        item.columns <= 0 || item.pages == null || item.pages.Length == 0)
                        throw new InvalidDataException("Incomplete clip.");
                    var frames = new List<Sprite>(item.count);
                    foreach (var page in item.pages)
                    {
                        var texture = LoadTexture(root, page.file, allocated);
                        if (texture.width != page.width || texture.height != page.height || page.width > 2048 || page.height > 2048 ||
                            page.count <= 0 || page.count * 1L > item.columns * (page.height / item.cellHeight) ||
                            item.columns * item.cellWidth != page.width)
                            throw new InvalidDataException("Atlas layout mismatch.");
                        for (int i = 0; i < page.count; i++)
                        {
                            var rect = new Rect(i % item.columns * item.cellWidth,
                                page.height - (i / item.columns + 1) * item.cellHeight, item.cellWidth, item.cellHeight);
                            var frame = Sprite.Create(texture, rect, new Vector2(item.pivotX, item.pivotY), data.ppu, 0, SpriteMeshType.FullRect);
                            frame.name = "TalulahOriginal/" + item.key + "/" + frames.Count.ToString("000");
                            frames.Add(frame); allocated.Add(frame);
                        }
                    }
                    if (frames.Count != item.count) throw new InvalidDataException("Frame count mismatch.");
                    art.clips.Add(item.key, new Clip(frames.ToArray(), item.duration));
                }
                var portraitTexture = LoadTexture(root, "portrait.png", allocated);
                art.Portrait = Sprite.Create(portraitTexture, new Rect(0, 0, portraitTexture.width, portraitTexture.height), new Vector2(.5f, 0), data.ppu);
                art.Portrait.name = "TalulahOriginal/portrait"; allocated.Add(art.Portrait);
                cached = art;
                Debug.Log("TALULAH_ORIGINAL_LOADED: approved idle/walk/attack/cast; no Spine runtime installed.");
                return cached;
            }
            catch (Exception e)
            {
                foreach (var value in allocated) if (value != null) UnityEngine.Object.Destroy(value);
                Debug.LogWarning("TALULAH_ORIGINAL_INVALID: " + e.Message + "; retained prototype art is used.");
                return null;
            }
        }

        private static Texture2D LoadTexture(string root, string file, List<UnityEngine.Object> allocated)
        {
            if (string.IsNullOrEmpty(file) || Path.GetFileName(file) != file || !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Atlas filename must be a local PNG.");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            allocated.Add(texture);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(root, file)), false))
                throw new InvalidDataException("PNG decode failed.");
            texture.name = "TalulahOriginal/" + file;
            texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            // Atlases are shared by every unit. DXT5 keeps this prototype's local flipbooks within a modest GPU budget.
            texture.Compress(true); texture.Apply(false, true);
            return texture;
        }
    }
}
