using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        internal const float FormationBoardUnfoldDuration = 5.5f;
        const float BoardFoldSettled = 3.93f;
        const float BoardUnfoldReturnStart = 4.58f;
        const float BoardSurfaceY = .12f;
        static readonly float[] FoldWidth = { 3, 3, 2 };
        static readonly float[] FoldStart = { .37f, .78f, 1.30f };
        static readonly float[] FoldEnd = { 2.32f, 2.95f, 3.80f };
        static readonly float[] FoldInitialAngle = { 82, -80, 84 };
        static readonly Quaternion BoardUnfoldStartRotation = Quaternion.Euler(51, 14, 0);

        FormationCampaignRun boardUnfoldRun;
        AudioSource boardUnfoldAudio;
        readonly List<BoardUnfoldPart> boardUnfoldParts = new List<BoardUnfoldPart>();
        readonly List<BoardUnfoldReveal> boardUnfoldReveals = new List<BoardUnfoldReveal>();
        bool boardUnfoldActorsWereActive;
        bool boardUnfoldPartsSettled;
        internal bool FormationBoardUnfoldActive => boardUnfoldRun != null;
        internal float FormationBoardUnfoldElapsed { get; private set; }

        // Capture the real scene meshes and restore their exact local poses at the end.
        // Only the presentation moves. The saved tile layout stays authoritative for combat.
        void BeginFormationBoardUnfold()
        {
            EndFormationBoardUnfold();
            if (Campaign?.OpeningRewardsPending != true || Campaign.IsPractice || maps == null ||
                maps.ActiveBoard != FormationBattleMaps.Board.Canyon || terrain == null ||
                maps.canyonScenery == null || maps.canyonScenery.Length < 4 ||
                maps.canyonScenery[0] == null || maps.canyonScenery[3] == null || battleCamera == null)
                return;

            var tiles = maps.canyonScenery[0].transform;
            if (tiles.childCount != FormationBattleModel.Width * FormationBattleModel.Height) return;
            foreach (Transform piece in tiles) boardUnfoldParts.Add(new BoardUnfoldPart(piece));
            foreach (Transform piece in terrain.transform) boardUnfoldParts.Add(new BoardUnfoldPart(piece));

            var courtyard = maps.canyonScenery[3].transform.parent;
            foreach (Transform piece in courtyard)
            {
                bool red = piece.name.StartsWith("Red gate row ", StringComparison.Ordinal);
                if (!red && !piece.name.StartsWith("Blue gate row ", StringComparison.Ordinal)) continue;
                int row = Mathf.RoundToInt(piece.position.z + (FormationBattleModel.Height - 1) * .5f);
                HideBoardUnfoldRenderers(piece, (red ? 3.95f : 4.13f) + row * .045f);
            }
            var shelf = maps.canyonScenery[1]?.transform.Find("Front reward shelf");
            if (shelf != null) HideBoardUnfoldRenderers(shelf, BoardUnfoldReturnStart);

            boardUnfoldRun = Campaign;
            FormationBoardUnfoldElapsed = 0;
            boardUnfoldPartsSettled = false;
            boardUnfoldActorsWereActive = actorRoot.gameObject.activeSelf;
            actorRoot.gameObject.SetActive(false);
            RefreshFormationOperatingVisibility();
            ApplyFormationBoardUnfold(0);

            if (boardUnfoldAudio == null)
            {
                var clip = Resources.Load<AudioClip>("formation-board-unfold");
                if (clip != null)
                {
                    boardUnfoldAudio = gameObject.AddComponent<AudioSource>();
                    boardUnfoldAudio.playOnAwake = false;
                    boardUnfoldAudio.clip = clip;
                }
            }
            if (boardUnfoldAudio != null)
            {
                boardUnfoldAudio.mute = !soundEnabled;
                boardUnfoldAudio.Play();
            }
        }

        void HideBoardUnfoldRenderers(Transform root, float visibleAt)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                boardUnfoldReveals.Add(new BoardUnfoldReveal(renderer, renderer.forceRenderingOff, visibleAt));
                renderer.forceRenderingOff = true;
            }
        }

        void TickFormationBoardUnfold(float deltaTime)
        {
            if (boardUnfoldRun != Campaign || !Campaign.OpeningRewardsPending)
            {
                EndFormationBoardUnfold();
                return;
            }

            FormationBoardUnfoldElapsed = Mathf.Min(FormationBoardUnfoldElapsed + deltaTime, FormationBoardUnfoldDuration);
            ApplyFormationBoardUnfold(FormationBoardUnfoldElapsed);
            if (FormationBoardUnfoldElapsed >= FormationBoardUnfoldDuration) EndFormationBoardUnfold();
        }

        void ApplyFormationBoardUnfold(float time)
        {
            if (!boardUnfoldPartsSettled)
            {
                if (time >= BoardFoldSettled) RestoreBoardUnfoldParts();
                else
                {
                    float lift = Mathf.Lerp(-.92f, 0, UnfoldEase(time, 0, 1.60f));
                    // Each absolute panel angle relaxes on a delay. The previous outside
                    // edge remains the next panel's pivot, including while it is folded.
                    SetBoardUnfoldPanel(0, Vector3.up * lift, Quaternion.identity, Vector3.zero);
                    FoldBoardUnfoldWing(-1, lift, time, 0);
                    FoldBoardUnfoldWing(1, lift, time, .13f);
                }
            }

            foreach (var entry in boardUnfoldReveals)
                if (entry.Renderer != null && time >= entry.VisibleAt)
                    entry.Renderer.forceRenderingOff = entry.WasHidden;

            float arc = UnfoldEase(time, .10f, 4.12f);
            var rotation = Quaternion.Slerp(BoardUnfoldStartRotation, FormationCameraRotation, arc);
            var focus = Vector3.up * Mathf.Lerp(.94f, BoardSurfaceY, arc);
            var overhead = focus - rotation * Vector3.forward * Mathf.Lerp(22.3f, 20.3f, arc);
            float returning = UnfoldEase(time, BoardUnfoldReturnStart, FormationBoardUnfoldDuration);
            battleCamera.transform.SetPositionAndRotation(Vector3.Lerp(overhead, formationLeftPreparationPose, returning), rotation);
        }

        void FoldBoardUnfoldWing(int side, float lift, float time, float delay)
        {
            float authoredX = side * 3f;
            var hinge = new Vector3(authoredX, BoardSurfaceY + lift, 0);
            for (int index = 0; index < FoldWidth.Length; index++)
            {
                float end = FoldEnd[index] + delay;
                float open = UnfoldEase(time, FoldStart[index] + delay, end);
                float after = time - end;
                float settling = after is > 0 and < .42f ? .48f * Mathf.Sin(after * 20) * Mathf.Exp(-after * 8) : 0;
                var rotation = Quaternion.AngleAxis(side * ((1 - open) * FoldInitialAngle[index] + settling), Vector3.forward);
                var authoredHinge = new Vector3(authoredX, BoardSurfaceY, 0);
                SetBoardUnfoldPanel(side * (index + 1), hinge, rotation, authoredHinge);
                float width = FoldWidth[index] * side;
                hinge += rotation * (Vector3.right * width);
                authoredX += width;
            }
        }

        void SetBoardUnfoldPanel(int panel, Vector3 pivot, Quaternion rotation, Vector3 originalPivot)
        {
            foreach (var part in boardUnfoldParts)
                if (part.Panel == panel && part.Transform != null)
                    part.Transform.SetPositionAndRotation(pivot + rotation * (part.WorldPosition - originalPivot),
                        rotation * part.WorldRotation);
        }

        static float UnfoldEase(float time, float begin, float end)
        {
            float t = Mathf.Clamp01((time - begin) / (end - begin));
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        void RestoreBoardUnfoldParts()
        {
            foreach (var part in boardUnfoldParts)
                if (part.Transform != null) part.Transform.SetLocalPositionAndRotation(part.LocalPosition, part.LocalRotation);
            boardUnfoldParts.Clear();
            boardUnfoldPartsSettled = true;
        }

        void EndFormationBoardUnfold()
        {
            if (!FormationBoardUnfoldActive) return;
            boardUnfoldRun = null;
            RestoreBoardUnfoldParts();
            foreach (var entry in boardUnfoldReveals)
                if (entry.Renderer != null) entry.Renderer.forceRenderingOff = entry.WasHidden;
            boardUnfoldReveals.Clear();
            if (actorRoot != null) actorRoot.gameObject.SetActive(boardUnfoldActorsWereActive);
            if (boardUnfoldAudio != null) boardUnfoldAudio.Stop();
            if (battleCamera != null) ApplyFormationCamera(FormationCameraTarget);
        }

        readonly struct BoardUnfoldPart
        {
            public readonly Transform Transform;
            public readonly int Panel;
            public readonly Vector3 WorldPosition, LocalPosition;
            public readonly Quaternion WorldRotation, LocalRotation;

            public BoardUnfoldPart(Transform transform)
            {
                Transform = transform;
                WorldPosition = transform.position;
                WorldRotation = transform.rotation;
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
                float distance = Mathf.Abs(WorldPosition.x);
                // Widen the center relative to the animatic to avoid splitting the
                // single 2×1 basin mesh across a hinge at the edge of column 12.
                int side = WorldPosition.x < 0 ? -1 : 1;
                Panel = distance <= 3 ? 0 : side * (distance <= 6 ? 1 : distance <= 9 ? 2 : 3);
            }
        }

        readonly struct BoardUnfoldReveal
        {
            public readonly Renderer Renderer;
            public readonly bool WasHidden;
            public readonly float VisibleAt;

            public BoardUnfoldReveal(Renderer renderer, bool wasHidden, float visibleAt)
            {
                Renderer = renderer;
                WasHidden = wasHidden;
                VisibleAt = visibleAt;
            }
        }
    }
}
