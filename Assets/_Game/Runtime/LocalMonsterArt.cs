using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    // Approved media ships with source. Missing media never substitutes another character's art.
    internal sealed class LocalMonsterArt
    {
        private static readonly HashSet<string> Tasks = new HashSet<string> {
            "M02", "M03", "M04", "M05", "M06", "M07", "M08", "M09", "M10",
            "M11", "M12", "M13", "M14", "M15", "M16", "M17", "M18", "M19", "M20", "M21", "M22", "M23", "M24", "M25", "M26", "M27", "M28", "M29", "M30", "M31", "M32", "M33", "M34", "M35", "M36", "M37", "M38" };
        private static readonly Dictionary<string, LocalMonsterArt> Cache = new Dictionary<string, LocalMonsterArt>();
        private readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        internal Sprite Portrait { get; private set; }
        internal float ReferenceSpeed { get; private set; }
        internal float Height { get; private set; }
        internal bool HasAttack => clips.ContainsKey("attack");
        internal bool HasClip(string key) => clips.ContainsKey(key);
        internal float DeathDuration => clips["death"].Duration;
        internal Clip this[string key] => clips[key];
        [Serializable] private sealed class Manifest
        { public int version; public bool noAttackClip; public string task; public float ppu, referenceSpeed, height; public ClipData[] clips; }
        [Serializable] private sealed class ClipData
        { public string key; public int count, cell, columns; public float duration, pivotX, pivotY; public Page[] pages; }
        [Serializable] private sealed class Page { public string file; public int count, width, height; }
        [Serializable] private sealed class ExtraManifest { public ClipData[] clips; }
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
                    !Positive(data.height) || data.clips?.Length != (data.noAttackClip ? 3 : 4) || data.noAttackClip && task!="M33") throw new InvalidDataException("Invalid monster manifest.");
                var art = new LocalMonsterArt { ReferenceSpeed = data.referenceSpeed, Height = data.height };
                var required = new HashSet<string> { "idle", "walk", "death" };
                if(!data.noAttackClip) required.Add("attack");
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
                // Only load the approved clip actually used by this role. Other optional atlases stay unloaded.
                string[] extraKeys=task=="M14" ? new[]{"extra_skill_1"} : task=="M28" ? new[]{"extra_attack_2","extra_skill_1"} :
                    task=="M34" ? new[]{"extra_skill_begin","extra_skill_idle","extra_skill_move"} :
                    task=="M32" ? new[]{"extra_c1_attack","extra_c1_idle","extra_c1_move","extra_c1_skill_2","extra_c1_skill_1_begin","extra_c1_skill_1_end","extra_c1_skill_1_loop","extra_c2_attack_1","extra_c2_die_1","extra_c2_idle_1","extra_c2_move_1","extra_c2_skill_2","extra_c2_skill_3","extra_c2_skill_1_begin","extra_c2_skill_1_end","extra_c2_skill_1_loop","extra_c2_stun","extra_reborn"} :
                    task=="M31" ? new[]{"extra_skill_down_start","extra_skill_down_loop","extra_skill_down_end","extra_skill_front_start","extra_skill_front_loop","extra_skill_front_end","extra_skill_up_start","extra_skill_up_loop","extra_skill_up_end"} :
                    task=="M30" ? new[]{"extra_die_2","extra_idle_2","extra_move_2","extra_revive"} : Array.Empty<string>();
                if(extraKeys.Length>0)
                {
                    string extraRoot=Path.Combine(root,"ExtraClips");
                    var extra=JsonUtility.FromJson<ExtraManifest>(File.ReadAllText(Path.Combine(extraRoot,"manifest.json")));
                    foreach(var item in extra.clips) if(Array.IndexOf(extraKeys,item.key)>=0)
                    {
                        var frames=new List<Sprite>();
                        foreach(var page in item.pages)
                        {
                            var texture=Load(extraRoot,page.file,allocated);
                            if(texture.width!=page.width || texture.height!=page.height || item.cell<=0 || item.columns<=0 || page.count<=0 ||
                                page.width!=item.columns*item.cell || page.height%item.cell!=0 || page.count>item.columns*(page.height/item.cell))
                                throw new InvalidDataException("Extra atlas layout mismatch.");
                            for(int i=0;i<page.count;i++)
                            {
                                var frame=Sprite.Create(texture,new Rect(i%item.columns*item.cell,page.height-(i/item.columns+1)*item.cell,item.cell,item.cell),
                                    new Vector2(item.pivotX,item.pivotY),data.ppu,0,SpriteMeshType.FullRect);
                                frame.name=task+"/"+item.key+"/"+frames.Count.ToString("000");
                                frames.Add(frame);allocated.Add(frame);
                            }
                        }
                        if(frames.Count!=item.count || !Positive(item.duration)) throw new InvalidDataException("Extra frame count mismatch.");
                        art.clips.Add(item.key,new Clip {Frames=frames.ToArray(),Duration=item.duration});
                    }
                    foreach(string key in extraKeys) if(!art.HasClip(key)) throw new InvalidDataException("Required effect animation missing: "+key);
                }
                var portrait = Load(root, "portrait.png", allocated);
                art.Portrait = Sprite.Create(portrait, new Rect(0, 0, portrait.width, portrait.height), new Vector2(.5f, 0), data.ppu);
                allocated.Add(art.Portrait); Cache[task] = art;
                Debug.Log("MONSTER_LOCAL_ART_LOADED: " + task + "; approved idle/walk/death; attack clip="+art.HasAttack+"; no active skill.");
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
