using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float FormationPanDuration = .48f;
        static readonly Quaternion FormationCameraRotation = Quaternion.Euler(60, 0, 0);
        static readonly Vector3 FormationCameraForward = FormationCameraRotation * Vector3.forward;
        static readonly Vector3 FormationCameraUp = FormationCameraRotation * Vector3.up;
        static readonly Vector3 FormationCameraRight = FormationCameraRotation * Vector3.right;
        static readonly Quaternion FormationCameraInverse = Quaternion.Inverse(FormationCameraRotation);
        public static Rect PreparationCameraSafeRect => new Rect(188, 64, 816, 472);
        public static Rect BattleCameraSafeRect => new Rect(12, 64, 1256, 644);
        public static Rect FormationCameraSafeRect => PreparationCameraSafeRect;
        public Rect CurrentFormationCameraSafeRect => UseFormationBattleCamera ? BattleCameraSafeRect : PreparationCameraSafeRect;
        public bool ViewingDefenders { get; private set; }
        public bool CameraTransitioning => formationPanElapsed < FormationPanDuration;
        public bool CanEditFormationView => Ready && Model != null && Model.Phase == FormationPhase.Preparation &&
            !ViewingDefenders && !CameraTransitioning;
        bool formationCameraInitialized, formationCameraWasPreparing;
        bool formationCameraHasSetPiece;
        int formationCameraRound;
        float formationPanElapsed = FormationPanDuration;
        Vector3 formationPanOrigin;
        Vector3 formationLeftPreparationPose, formationRightPreparationPose, formationRightBattlePose;
        readonly List<Vector3> formationCameraPoints = new List<Vector3>();
        bool UseFormationBattleCamera => Model != null && (Model.Phase == FormationPhase.Fighting || Model.Phase == FormationPhase.Exiting);

        public bool FormationPointVisibleInView(Vector2 point)
        {
            if (battleCamera == null) return false;
            var projected = battleCamera.WorldToViewportPoint(WorldPoint(point));
            // Keep bodies and effects whose feet are just outside the viewport.
            return projected.z > 0 && projected.x >= -.2f && projected.x <= 1.2f;
        }
        public bool FormationActorVisibleInView(FormationActor actor) => actor != null && FormationPointVisibleInView(actor.Position);

        internal void InitializeFormationCamera()
        {
            if (battleCamera == null || Model == null) return;
            formationCameraInitialized = true;
            formationCameraWasPreparing = Model.Phase == FormationPhase.Preparation;
            formationCameraRound = Model.Round;
            ViewingDefenders = !formationCameraWasPreparing;
            // These poses depend only on map dimensions and fixed visible-body envelopes.
            // Buying, moving or opening the shop never reframes a preparation view.
            RebuildFormationCameraPoses();
            formationPanElapsed = FormationPanDuration;
            ApplyFormationCamera(FormationCameraTarget);
        }
        public void ToggleFormationView()
        {
            if (Model == null || Model.Phase != FormationPhase.Preparation || openingGroundBall != null) return;
            SetMarketVisible(false);
            CloseFormationTraitDetails();
            CloseFormationRouteDetails();
            campaignSeaPainting = false;
            SetFormationView(!ViewingDefenders);
        }
        public void SetFormationView(bool defenders, bool immediate = false)
        {
            if (battleCamera == null || Model == null) return;
            if (!formationCameraInitialized) InitializeFormationCamera();
            bool preparing = Model.Phase == FormationPhase.Preparation;
            bool nextView = preparing ? defenders : !HasGroundRewards || UseFormationBattleCamera;
            bool changed = ViewingDefenders != nextView;
            if (changed)
            {
                CloseFormationInspection();
                CancelFormationEquipmentInteraction();
                CloseFormationEquipmentDetails();
                CancelFormationDrag();
            }
            ViewingDefenders = nextView;
            if (immediate || !preparing)
            {
                formationPanElapsed = FormationPanDuration;
                ApplyFormationCamera(FormationCameraTarget);
            }
            else if (changed)
            {
                formationPanOrigin = battleCamera.transform.position;
                formationPanElapsed = 0;
            }
        }
        internal void TickFormationCamera(float unscaledDt)
        {
            if (battleCamera == null || Model == null) return;
            if (!formationCameraInitialized) InitializeFormationCamera();
            bool setPiece=HasFormationSetPiece();
            if(setPiece!=formationCameraHasSetPiece)RebuildFormationCameraPoses();
            bool preparing = Model.Phase == FormationPhase.Preparation;
            if (preparing != formationCameraWasPreparing || preparing && Model.Round != formationCameraRound)
                SetFormationView(!preparing, true);
            if (!preparing && !UseFormationBattleCamera && HasGroundRewards && ViewingDefenders)
                SetFormationView(false);
            formationCameraWasPreparing = preparing;
            formationCameraRound = Model.Round;
            if (CameraTransitioning)
            {
                formationPanElapsed = Mathf.Min(formationPanElapsed + unscaledDt, FormationPanDuration);
                float progress = Mathf.SmoothStep(0, 1, formationPanElapsed / FormationPanDuration);
                ApplyFormationCamera(Vector3.Lerp(formationPanOrigin, FormationCameraTarget, progress));
            }
            else ApplyFormationCamera(FormationCameraTarget);
        }
        Vector3 FormationCameraTarget => UseFormationBattleCamera ? formationRightBattlePose :
            ViewingDefenders ? formationRightPreparationPose : formationLeftPreparationPose;
        void RebuildFormationCameraPoses()
        {
            formationCameraHasSetPiece=HasFormationSetPiece();
            formationLeftPreparationPose=FixedFormationPose(false,false);
            formationRightPreparationPose=FixedFormationPose(true,false);
            formationRightBattlePose=FixedFormationPose(true,true);
        }
        bool HasFormationSetPiece()=>Model.Units.Any(u=>u.Enabled&&u.ArtId=="B08"&&u.NativePhase>0);
        Vector3 FixedFormationPose(bool defenders, bool combat)
        {
            formationCameraPoints.Clear();
            float first = defenders ? FormationBattleModel.DeploymentColumns + .5f : -.5f;
            float last = defenders ? FormationBattleModel.Width - .5f : FormationBattleModel.DeploymentColumns + .5f;
            float horizontalMargin = formationCameraHasSetPiece ? 5.1f : combat ? 2.1f : 1.5f;
            float topMargin = formationCameraHasSetPiece ? 6.2f : 3.4f;
            foreach (float col in new[] { first, last })
                foreach (float row in new[] { -.5f, FormationBattleModel.Height - .5f })
                {
                    var foot = WorldPoint(new Vector2(col, row));
                    // Fit playable tiles and the usual visible bodies, ignoring the
                    // transparent padding of their atlas cells.
                    foreach (float side in new[] { -horizontalMargin, horizontalMargin })
                    {
                        var edge = foot + FormationCameraRight * side;
                        formationCameraPoints.Add(edge - FormationCameraUp * .2f);
                        formationCameraPoints.Add(edge);
                        formationCameraPoints.Add(edge + FormationCameraUp * topMargin);
                    }
                }
            return FitFormationCamera(WorldPoint(new Vector2((first + last) * .5f, (FormationBattleModel.Height - 1) * .5f)),
                combat ? BattleCameraSafeRect : PreparationCameraSafeRect);
        }
        Vector3 FitFormationCamera(Vector3 focus, Rect safeRect)
        {
            float low = 8, high = 16;
            while (!TryFormationCameraFit(focus, high, safeRect, out _) && high < 128) high *= 2;
            for (int pass = 0; pass < 24; pass++)
            {
                float distance = (low + high) * .5f;
                if (TryFormationCameraFit(focus, distance, safeRect, out _)) high = distance;
                else low = distance;
            }
            float fittedDistance = high + .06f;
            TryFormationCameraFit(focus, fittedDistance, safeRect, out var offset);
            return focus - FormationCameraForward * fittedDistance + FormationCameraRight * offset.x + FormationCameraUp * offset.y;
        }

        bool TryFormationCameraFit(Vector3 focus, float distance, Rect rect, out Vector2 offset)
        {
            float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;
            float minY = float.NegativeInfinity, maxY = float.PositiveInfinity;
            float focal = 360 / Mathf.Tan(20 * Mathf.Deg2Rad);
            foreach (var point in formationCameraPoints)
            {
                Vector3 local = FormationCameraInverse * (point - focus);
                float depth = local.z + distance;
                if (depth <= .1f) { offset = default; return false; }
                minX = Mathf.Max(minX, local.x - (rect.xMax - 640) * depth / focal);
                maxX = Mathf.Min(maxX, local.x - (rect.xMin - 640) * depth / focal);
                minY = Mathf.Max(minY, local.y + (rect.yMin - 360) * depth / focal);
                maxY = Mathf.Min(maxY, local.y + (rect.yMax - 360) * depth / focal);
            }
            offset = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
            return minX <= maxX && minY <= maxY;
        }

        void ApplyFormationCamera(Vector3 position)
        {
            battleCamera.orthographic = false;
            battleCamera.usePhysicalProperties = false;
            battleCamera.fieldOfView = 40;
            battleCamera.lensShift = Vector2.zero;
            battleCamera.transform.SetPositionAndRotation(position, FormationCameraRotation);
        }
    }
}
