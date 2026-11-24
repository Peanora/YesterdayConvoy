using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float FormationOpeningPanStart = .18f;
        const float FormationOpeningPanDuration = 1.38f;
        const float FormationOpeningPanEnd = FormationOpeningPanStart + FormationOpeningPanDuration;
        internal const float FormationBattleOpeningDuration = 2.40f;
        static readonly Color FormationOpeningMatte = new Color32(36, 38, 34, 255);
        static readonly Color FormationOpeningInk = new Color32(239, 235, 225, 255);

        FormationBattleModel formationOpeningBattle;
        float formationOpeningElapsed, formationOpeningReturnSeconds;
        Vector3 formationOpeningOrigin;

        internal bool FormationBattleOpeningActive =>
            formationOpeningBattle != null && formationOpeningBattle == Model && Model.Phase == FormationPhase.Fighting;

        float FormationOpeningTime => formationOpeningElapsed - formationOpeningReturnSeconds;
        bool FormationOpeningReachedDefenders => FormationOpeningTime >= FormationOpeningPanEnd;
        float FormationOpeningPanProgress =>
            FormationOpeningEase(Mathf.Clamp01((FormationOpeningTime - FormationOpeningPanStart) / FormationOpeningPanDuration));

        static float FormationOpeningEase(float progress) => .5f - .5f * Mathf.Cos(Mathf.PI * progress);
        static float FormationOpeningFade(float start, float end, float time) =>
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, time));

        void BeginFormationBattleOpening()
        {
            formationOpeningBattle = Model;
            formationOpeningElapsed = 0;
            formationOpeningOrigin = battleCamera.transform.position;
            formationOpeningReturnSeconds = (formationOpeningOrigin - formationLeftPreparationPose).sqrMagnitude > .001f
                ? FormationPanDuration : 0;
            formationPanElapsed = FormationPanDuration;
            ViewingDefenders = true;
        }

        void TickFormationBattleOpening(float unscaledDeltaTime)
        {
            if (!FormationBattleOpeningActive || Model.Paused) return;
            formationOpeningElapsed += unscaledDeltaTime;
            if (FormationOpeningTime >= FormationBattleOpeningDuration) CancelFormationBattleOpening();
        }

        void CancelFormationBattleOpening() => formationOpeningBattle = null;

        Vector3 FormationBattleOpeningCameraPose()
        {
            if (FormationOpeningTime < 0)
            {
                float returning = FormationOpeningEase(formationOpeningElapsed / formationOpeningReturnSeconds);
                return Vector3.Lerp(formationOpeningOrigin, formationLeftPreparationPose, returning);
            }
            return Vector3.Lerp(formationLeftPreparationPose, formationRightBattlePose, FormationOpeningPanProgress);
        }

        void DrawFormationBattleOpening()
        {
            if (Event.current.type != EventType.Repaint) return;
            float time = FormationOpeningTime;
            float matte = FormationOpeningFade(0, .23f, formationOpeningElapsed) *
                (1 - FormationOpeningFade(FormationOpeningPanEnd, 1.91f, time));
            float pan = FormationOpeningPanProgress;
            float top = Mathf.Lerp(73, 132, pan) * matte;
            float bottom = Mathf.Lerp(115, 107, pan) * matte;
            if (matte > 0)
            {
                Fill(new Rect(0, 0, 1280, top), FormationOpeningMatte);
                Fill(new Rect(0, 720 - bottom, 1280, bottom), FormationOpeningMatte);
            }
            if (top > 28 && !FormationOpeningReachedDefenders)
            {
                string board = FormationBattleMaps.DisplayName(maps != null ? maps.ActiveBoard : FormationBattleMaps.Board.Canyon);
                string node = Campaign?.IsStandaloneBossTrial == true ? "首领试玩" : Campaign?.CurrentNode.Id ?? Model.Round.ToString();
                var ink = FormationOpeningInk;
                ink.a = matte;
                GUI.Label(new Rect(35, 0, 520, top), node + "  " + board,
                    approvedShop.LoopText(20, TextAnchor.MiddleLeft, ink, 600));
            }

            float title = FormationOpeningFade(FormationOpeningPanEnd, 1.79f, time) *
                (1 - FormationOpeningFade(2.12f, FormationBattleOpeningDuration, time));
            if (title <= 0) return;
            float y = 110 + 6 * (1 - FormationOpeningFade(FormationOpeningPanEnd, 1.88f, time));
            var plate = new Rect(527, y, 226, 66);
            var surface = FormationOpeningMatte;
            surface.a = .88f * title;
            Fill(plate, surface);
            Fill(new Rect(plate.x, plate.yMax - 1, plate.width, 1), new Color(.63f, .61f, .55f, .64f * title));
            var titleInk = FormationOpeningInk;
            titleInk.a = title;
            GUI.Label(plate, "作战开始", approvedShop.LoopText(39, TextAnchor.MiddleCenter, titleInk, 700));
        }
    }
}
