using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Optional approved local media. Public clones never substitute another character's art.
    internal sealed class LocalMonsterArt
    {
        private static readonly HashSet<string> Tasks = new HashSet<string> { "M02", "M03", "M04", "M05", "M06", "M07", "M08", "M09", "M10" };
        private static readonly Dictionary<string, LocalMonsterArt> Cache = new Dictionary<string, LocalMonsterArt>();
        private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        internal Sprite Portrait { get; private set; }
        internal float ReferenceSpeed { get; private set; }
        internal float Height { get; private set; }
        internal float DeathDuration => clips["death"].Duration;
        internal Clip this[string key] => clips[key];
        [Serializable] private sealed class Manifest
        { public int version; public string task; public float ppu, referenceSpeed, height; public ClipData[] clips; }
        [Serializable] private sealed class ClipData
        { public string key; public int count, cell, columns; public float duration, pivotX, pivotY; public Page[] pages; }
        [Serializable] private sealed class Page { public string file; public int count, width, height; }
        internal sealed class Clip
        {
            internal Sprite[] Frames;
            internal float Duration;
            internal Sprite At(double time, bool loop, out int index)
            {
                time = Math.Max(0, time);
                if (loop) time %= Duration;
                index = Mathf.Clamp((int)(time / Duration * Frames.Length + 1e-5), 0, Frames.Length - 1);
                return Frames[index];
            }
        }
        internal static LocalMonsterArt For(string cardId)
        {
            if (cardId == null || !cardId.StartsWith("CARD_M", StringComparison.Ordinal)) return null;
            string task = cardId.Substring(5);
            if (!Tasks.Contains(task)) return null;
            if (Cache.TryGetValue(task, out var prior)) return prior;
            Cache.Add(task, null);
            string root = Path.Combine(Application.streamingAssetsPath, "MonsterCombatArt", task);
            var allocated = new List<UnityEngine.Object>();
            try
            {
                var data = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root, "manifest.json")));
                if (data == null || data.version != 1 || data.task != task || !Positive(data.ppu) || !Positive(data.referenceSpeed) ||
                    !Positive(data.height) || data.clips?.Length != 4) throw new InvalidDataException("Invalid monster manifest.");
                var art = new LocalMonsterArt { ReferenceSpeed = data.referenceSpeed, Height = data.height };
                var required = new HashSet<string> { "idle", "walk", "attack", "death" };
                foreach (var item in data.clips)
                {
                    if (!required.Remove(item.key) || item.count <= 0 || item.count > 600 || !Positive(item.duration) ||
                        item.cell <= 0 || item.cell > 512 || item.columns <= 0 || item.columns > 16 || item.pages == null || item.pages.Length == 0 ||
                        !Finite(item.pivotX) || !Finite(item.pivotY) || item.pivotX < 0 || item.pivotX > 1 || item.pivotY < 0 || item.pivotY > 1)
                        throw new InvalidDataException("Invalid monster clip.");
                    var frames = new List<Sprite>();
                    foreach (var page in item.pages)
                    {
                        var texture = Load(root, page.file, allocated);
                        if (page.width != texture.width || page.height != texture.height || page.width > 2048 || page.height > 2048 ||
                            page.count <= 0 || page.width != item.columns * item.cell || page.height % item.cell != 0 ||
                            page.count > (long)item.columns * (page.height / item.cell)) throw new InvalidDataException("Atlas layout mismatch.");
                        for (int i = 0; i < page.count; i++)
                        {
                            var frame = Sprite.Create(texture, new Rect(i % item.columns * item.cell, page.height - (i / item.columns + 1) * item.cell,
                                item.cell, item.cell), new Vector2(item.pivotX, item.pivotY), data.ppu, 0, SpriteMeshType.FullRect);
                            frame.name = task + "/" + item.key + "/" + frames.Count.ToString("000");
                            frames.Add(frame); allocated.Add(frame);
                        }
                    }
                    if (frames.Count != item.count) throw new InvalidDataException("Frame count mismatch.");
                    art.clips.Add(item.key, new Clip { Frames = frames.ToArray(), Duration = item.duration });
                }
                var portrait = Load(root, "portrait.png", allocated);
                art.Portrait = Sprite.Create(portrait, new Rect(0, 0, portrait.width, portrait.height), new Vector2(.5f, 0), data.ppu);
                allocated.Add(art.Portrait); Cache[task] = art;
                Debug.Log("MONSTER_LOCAL_ART_LOADED: " + task + "; approved idle/walk/attack/death; no active skill.");
                return art;
            }
            catch (Exception e)
            {
                foreach (var value in allocated) if (value != null)
                { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
                Debug.LogWarning("MONSTER_LOCAL_ART_UNAVAILABLE: " + task + "; " + e.Message + "; test remains pending.");
                return null;
            }
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool Positive(float n) => Finite(n) && n > 0;
        private static Texture2D Load(string root, string file, List<UnityEngine.Object> allocated)
        {
            if (string.IsNullOrEmpty(file) || Path.GetFileName(file) != file || file.Contains(":") || !file.EndsWith(".png", StringComparison.Ordinal))
                throw new InvalidDataException("Expected local PNG filename.");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false); allocated.Add(texture);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(root, file)), false)) throw new InvalidDataException("PNG decode failed.");
            texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            texture.Compress(true); texture.Apply(false, true); return texture;
        }
    }
}
