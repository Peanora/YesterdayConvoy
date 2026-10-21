using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Editor-only review of approved local media. Does not register a card or simulate combat.</summary>
    public sealed class M08ArcherArtPreview : EditorWindow
    {
        private static readonly string[] Keys = { "idle", "walk", "attack" };
        private static readonly string[] Labels = { "待机", "行走", "射击" };
        private const int FrameCount = 60;
        private const int FramesPerSecond = 60;
        private Texture2D frame, portrait;
        private int clip, loadedClip = -1, loadedFrame = -1;
        private bool playing = true;
        private double started, nextRepaint;
        private string error;
        private static string Root => Path.Combine(Application.streamingAssetsPath, "ArcherOriginal");

        [MenuItem("Yesterday Convoy/Art/M08 Archer Approved Preview")]
        public static void Open()
        {
            var window = GetWindow<M08ArcherArtPreview>("M08 弩手美术");
            window.minSize = new Vector2(1100, 720);
            window.Show();
        }

        private void OnEnable() { started = EditorApplication.timeSinceStartup; }
        private void OnDisable()
        {
            if (frame != null) DestroyImmediate(frame);
            if (portrait != null) DestroyImmediate(portrait);
        }
        private void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing && now >= nextRepaint) { nextRepaint = now + 1.0 / FramesPerSecond; Repaint(); }
        }
        private void OnGUI()
        {
            GUILayout.Label("M08 普通弩手 · 已确认 V1 美术", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("卡面、待机、原地行走和射击共用同一原版模型。仅美术预览；尚未注册可玩战斗样板。", MessageType.Info);
            if (!File.Exists(Path.Combine(Root, "manifest.json")))
            {
                EditorGUILayout.HelpBox("本地可选素材未安装。请按 docs/art/M08-ARCHER-INTEGRATION.md 安装，不从公共仓库自动下载原作媒体。", MessageType.Warning);
                return;
            }
            int selected = GUILayout.Toolbar(clip, Labels, GUILayout.Width(400));
            if (selected != clip) { clip = selected; loadedFrame = -1; started = EditorApplication.timeSinceStartup; }
            bool nextPlaying = EditorGUILayout.Toggle("播放（原速 60fps）", playing);
            if (nextPlaying != playing)
            {
                if (nextPlaying) started = EditorApplication.timeSinceStartup - Math.Max(loadedFrame, 0) / (double)FramesPerSecond;
                playing = nextPlaying;
            }
            int index = playing ? (int)((EditorApplication.timeSinceStartup - started) * FramesPerSecond) % FrameCount : Math.Max(loadedFrame, 0);
            if (!playing) index = EditorGUILayout.IntSlider("逐帧查看", index, 0, FrameCount - 1);
            try
            {
                if (portrait == null) portrait = ReadTexture(Path.Combine(Root, "portrait.png"));
                if (index != loadedFrame || clip != loadedClip)
                {
                    if (frame != null) DestroyImmediate(frame);
                    frame = null;
                    frame = ReadTexture(Path.Combine(Root, Keys[clip], index.ToString("0000") + ".png"));
                    loadedFrame = index; loadedClip = clip;
                }
                error = null;
            }
            catch (Exception e) { error = e.Message; playing = false; }
            if (error != null) { EditorGUILayout.HelpBox(error, MessageType.Error); return; }
            Rect area = GUILayoutUtility.GetRect(position.width - 20, Math.Max(500, position.height - 210));
            EditorGUI.DrawRect(area, new Color(.07f, .10f, .14f));
            float cardWidth = 210;
            GUI.DrawTexture(new Rect(area.x + 10, area.y + 45, cardWidth, 294), portrait, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(area.x + 15, area.y + 15, 210, 24), "已确认卡面（模型排版）");
            float detailWidth = Math.Min(600, area.width - 470);
            GUI.Label(new Rect(area.x + 240, area.y + 15, 300, 24), Labels[clip] + " · 放大 / 帧 " + index.ToString("00"));
            GUI.DrawTexture(new Rect(area.x + 225, area.y + 30, detailWidth, detailWidth), frame, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(area.xMax - 220, area.y + 15, 210, 40), "预计游戏尺寸\n待机角色约 112px 高");
            GUI.DrawTexture(new Rect(area.xMax - 220, area.y + 145, 210, 210), frame, ScaleMode.ScaleToFit, true);
            GUILayout.Label("固定根点 (280,550) / 640×640透明画布；预计尺寸未校准至实机场景。原版媒体仅本地保存。", EditorStyles.wordWrappedLabel);
        }

        private static Texture2D ReadTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) throw new InvalidDataException("Invalid PNG: " + path);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
            catch { DestroyImmediate(texture); throw; }
        }

        // Also provides a real Unity compile/load check without opening or changing a scene.
        public static void ValidateLocalPackage()
        {
            if (!File.Exists(Path.Combine(Root, "manifest.json"))) throw new FileNotFoundException("M08 media manifest is missing.");
            foreach (string key in Keys)
                for (int i = 0; i < FrameCount; i++)
                {
                    var value = ReadTexture(Path.Combine(Root, key, i.ToString("0000") + ".png"));
                    bool valid = value.width == 640 && value.height == 640;
                    DestroyImmediate(value);
                    if (!valid) throw new InvalidDataException("M08 frame dimensions must be 640x640.");
                }
            var card = ReadTexture(Path.Combine(Root, "portrait.png"));
            bool cardValid = card.width == 600 && card.height == 840;
            DestroyImmediate(card);
            if (!cardValid) throw new InvalidDataException("M08 card dimensions must be 600x840.");
            Debug.Log("M08_ARCHER_ART_VALIDATED: portrait and 180 frames; idle/walk/attack. No gameplay registered.");
        }
    }
}
