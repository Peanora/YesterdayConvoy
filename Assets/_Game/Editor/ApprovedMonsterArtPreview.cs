using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    /// <summary>Editor-only review of approved local media. Does not register a card or simulate combat.</summary>
    public sealed class ApprovedMonsterArtPreview : EditorWindow
    {
        private static readonly string[] Tasks = { "M02", "M03", "M04", "M05", "M06" };
        private static readonly string[] Names = { "猎狗", "游击队猎犬", "高能源石虫", "酸液源石虫", "冰爆源石虫" };
        private static readonly string[][] ClipKeys = { new[] { "idle", "walk", "attack" }, new[] { "idle", "run", "attack" }, new[] { "idle", "move", "attack", "death" }, new[] { "idle", "move", "attack" }, new[] { "idle", "move", "attack", "death" } };
        private static readonly string[][] ClipLabels = { new[] { "待机", "四足行走", "扑咬" }, new[] { "待机", "四足奔跑", "扑咬" }, new[] { "待机", "多足移动", "攻击", "死亡蓄爆" }, new[] { "待机", "蠕动", "喷吐" }, new[] { "待机", "多足移动", "攻击", "死亡蓄爆" } };
        private static readonly int[][] Counts = { new[] { 60, 48, 60 }, new[] { 60, 32, 60 }, new[] { 60, 60, 150, 60 }, new[] { 120, 120, 80 }, new[] { 60, 60, 150, 60 } };
        private int monster;
        private string[] Keys => ClipKeys[monster];
        private string[] Labels => ClipLabels[monster];
        private int FrameCount => Counts[monster][clip];
        private const int FramesPerSecond = 60;
        private Texture2D frame, portrait;
        private int clip, loadedClip = -1, loadedFrame = -1;
        private bool playing = true;
        private double started, nextRepaint;
        private string error;
        private string Root => Path.Combine(Application.streamingAssetsPath, "ApprovedMonsterArt", Tasks[monster]);

        [MenuItem("Yesterday Convoy/Art/Approved Monster Previews")]
        public static void Open()
        {
            var window = GetWindow<ApprovedMonsterArtPreview>("已确认怪物美术");
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
            int selectedMonster = EditorGUILayout.Popup("角色", monster, Names);
            if (selectedMonster != monster)
            {
                monster = selectedMonster; clip = 0; loadedFrame = -1; loadedClip = -1;
                if (frame != null) DestroyImmediate(frame);
                if (portrait != null) DestroyImmediate(portrait);
                frame = null; portrait = null; started = EditorApplication.timeSinceStartup;
            }
            GUILayout.Label(Tasks[monster] + " " + Names[monster] + " · 当前用户已确认V1", EditorStyles.boldLabel);
            if (monster == 4) EditorGUILayout.HelpBox("完整冰爆特效、寒冷和冻结尚未接入。", MessageType.Warning);
            if (monster == 3) EditorGUILayout.HelpBox("仅喷吐动作；酸液弹道、命中特效与削甲尚未接入。", MessageType.Warning);
            if (monster == 2) EditorGUILayout.HelpBox("死亡仅含蓄爆动作；完整爆炸特效未补。", MessageType.Warning);
            EditorGUILayout.HelpBox("卡面、待机、原地行走和射击共用同一原版模型。仅美术预览；尚未注册可玩战斗样板。", MessageType.Info);
            if (!File.Exists(Path.Combine(Root, "manifest.json")))
            {
                EditorGUILayout.HelpBox("本地可选素材未安装。请按 docs/art/MONSTER_ART_PREVIEWS.md 安装，不从公共仓库自动下载原作媒体。", MessageType.Warning);
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
            Rect area = GUILayoutUtility.GetRect(position.width - 20, Math.Max(500, position.height - 250));
            EditorGUI.DrawRect(area, new Color(.07f, .10f, .14f));
            float cardWidth = 210;
            GUI.DrawTexture(new Rect(area.x + 10, area.y + 45, cardWidth, 294), portrait, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(area.x + 15, area.y + 15, 210, 24), "已确认卡面（模型排版）");
            float detailWidth = Math.Min(600, area.width - 470);
            GUI.Label(new Rect(area.x + 240, area.y + 15, 300, 24), Labels[clip] + " · 放大 / 帧 " + index.ToString("00"));
            GUI.DrawTexture(new Rect(area.x + 225, area.y + 30, detailWidth, detailWidth), frame, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(area.xMax - 220, area.y + 15, 210, 40), "预计游戏尺寸\n尚未校准至实机");
            GUI.DrawTexture(new Rect(area.xMax - 220, area.y + 145, 210, 210), frame, ScaleMode.ScaleToFit, true);
            GUILayout.Label("固定根点 (320,490) / 640×640透明画布；预计尺寸未校准至实机场景。原版媒体仅本地保存。", EditorStyles.wordWrappedLabel);
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

        public static void ValidateLocalPackage()
        {
            for (int m = 0; m < Tasks.Length; m++)
            {
                string root = Path.Combine(Application.streamingAssetsPath, "ApprovedMonsterArt", Tasks[m]);
                if (!File.Exists(Path.Combine(root, "manifest.json"))) throw new FileNotFoundException("Missing manifest: " + Tasks[m]);
                for (int c = 0; c < ClipKeys[m].Length; c++)
                    for (int i = 0; i < Counts[m][c]; i++)
                    {
                        var texture = ReadTexture(Path.Combine(root, ClipKeys[m][c], i.ToString("0000") + ".png"));
                        bool valid = texture.width == 640 && texture.height == 640;
                        DestroyImmediate(texture);
                        if (!valid) throw new InvalidDataException("Expected 640x640 frame.");
                    }
                var card = ReadTexture(Path.Combine(root, "portrait.png"));
                bool cardValid = card.width == 600 && card.height == 840;
                DestroyImmediate(card);
                if (!cardValid) throw new InvalidDataException("Expected 600x840 portrait.");
            }
            Debug.Log("APPROVED_MONSTER_ART_VALIDATED: M02/M03/M04/M05/M06; no gameplay registered.");
        }
    }
}
