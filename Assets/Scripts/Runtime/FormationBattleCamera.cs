using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        // Layer 31 belongs to the original recruitment RenderTexture stage.
        const int FormationLeftLayer = 29, FormationRightLayer = 30;
        static readonly Quaternion FormationCameraRotation = Quaternion.Euler(60, 0, 0);
        static readonly Vector3 FormationCameraForward = FormationCameraRotation * Vector3.forward;
        static readonly Vector3 FormationCameraUp = FormationCameraRotation * Vector3.up;
        static readonly Vector3 FormationCameraRight = FormationCameraRotation * Vector3.right;
        static readonly Quaternion FormationCameraInverse = Quaternion.Inverse(FormationCameraRotation);
        public static Rect FormationCameraSafeRect => new Rect(204, 250, 790, 244);
        public bool ViewingDefenders { get; private set; }
        public bool CameraTransitioning => false;
        public bool CanEditFormationView => Ready && Model != null && Model.Phase == FormationPhase.Preparation && !ViewingDefenders;
        bool formationCameraInitialized, formationCameraWasPreparing;
        int formationCameraRound, formationOriginalCameraMask;
        Vector3 formationLeftPose, formationRightPose;
        readonly List<Vector3> formationCameraPoints = new List<Vector3>();

        public bool FormationPointVisibleInView(Vector2 point) => ViewingDefenders == (point.x >= FormationBattleModel.DeploymentColumns + .5f);
        public bool FormationActorVisibleInView(FormationActor actor) => actor != null && FormationPointVisibleInView(actor.Position);

        internal void InitializeFormationCamera()
        {
            if (battleCamera == null || Model == null) return;
            formationCameraInitialized = true;
            formationOriginalCameraMask = battleCamera.cullingMask;
            formationCameraWasPreparing = Model.Phase == FormationPhase.Preparation;
            formationCameraRound = Model.Round;
            ViewingDefenders = !formationCameraWasPreparing;
            // Both poses depend only on authored map dimensions and maximum presentation size.
            // Moving, buying, animating or losing a unit never reframes either view.
            formationLeftPose = FixedFormationPose(false);
            formationRightPose = FixedFormationPose(true);
            ClassifyFormationEnvironment();
            ApplyFixedFormationView();
        }
        public void ToggleFormationView()
        {
            if (Model != null && Model.Phase == FormationPhase.Preparation) SetFormationView(!ViewingDefenders);
        }
        public void SetFormationView(bool defenders, bool immediate = false)
        {
            if (battleCamera == null || Model == null) return;
            if (!formationCameraInitialized) InitializeFormationCamera();
            ViewingDefenders = Model.Phase == FormationPhase.Preparation ? defenders : true;
            CancelFormationDrag();
            ApplyFixedFormationView();
            if (Ready) SyncActors();
        }
        internal void TickFormationCamera(float unscaledDt)
        {
            if (battleCamera == null || Model == null) return;
            if (!formationCameraInitialized) InitializeFormationCamera();
            bool preparing = Model.Phase == FormationPhase.Preparation;
            if (preparing != formationCameraWasPreparing || preparing && Model.Round != formationCameraRound)
                SetFormationView(!preparing);
            formationCameraWasPreparing = preparing;
            formationCameraRound = Model.Round;
            ApplyFixedFormationView();
        }
        void ApplyFixedFormationView()
        {
            int hiddenLayer = ViewingDefenders ? FormationLeftLayer : FormationRightLayer;
            battleCamera.cullingMask = formationOriginalCameraMask & ~(1 << hiddenLayer);
            ApplyFormationCamera(ViewingDefenders ? formationRightPose : formationLeftPose);
        }
        Vector3 FixedFormationPose(bool defenders)
        {
            formationCameraPoints.Clear();
            float first = defenders ? FormationBattleModel.DeploymentColumns + .5f : -.5f;
            float last = defenders ? FormationBattleModel.Width - .5f : FormationBattleModel.DeploymentColumns + .5f;
            foreach (float col in new[] { first, last })
                foreach (float row in new[] { -.5f, FormationBattleModel.Height - .5f })
                {
                    var foot = WorldPoint(new Vector2(col, row));
                    formationCameraPoints.Add(foot - FormationCameraUp * .65f);
                    formationCameraPoints.Add(foot);
                    // Reserve all existing large bodies and raised defender platforms once.
                    formationCameraPoints.Add(foot + FormationCameraUp * 2.65f);
                }
            return FitFormationCamera(WorldPoint(new Vector2((first + last) * .5f, (FormationBattleModel.Height - 1) * .5f)), 0);
        }
        void ClassifyFormationEnvironment()
        {
            float seam = WorldPoint(new Vector2(FormationBattleModel.DeploymentColumns + .5f, 0)).x;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root == gameObject || root.transform.IsChildOf(transform)) continue;
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var bounds = renderer.bounds;
                    // Shared long scenery can remain in both views; complete tiles, gates,
                    // terrain and local scenery belong to exactly one side.
                    if (bounds.min.x < seam - .05f && bounds.max.x > seam + .05f && bounds.size.x > 2) continue;
                    renderer.gameObject.layer = bounds.center.x < seam ? FormationLeftLayer : FormationRightLayer;
                }
            }
        }
        Vector3 FitFormationCamera(Vector3 focus, float minimumDistance)
        {
            float low = 8, high = 16;
            while (!TryFormationCameraFit(focus, high, out _) && high < 128) high *= 2;
            for (int pass = 0; pass < 24; pass++)
            {
                float distance = (low + high) * .5f;
                if (TryFormationCameraFit(focus, distance, out _)) high = distance;
                else low = distance;
            }
            float fittedDistance = Mathf.Max(high + .06f, minimumDistance);
            TryFormationCameraFit(focus, fittedDistance, out var offset);
            return focus - FormationCameraForward * fittedDistance + FormationCameraRight * offset.x + FormationCameraUp * offset.y;
        }

        bool TryFormationCameraFit(Vector3 focus, float distance, out Vector2 offset)
        {
            float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;
            float minY = float.NegativeInfinity, maxY = float.PositiveInfinity;
            float focal = 360 / Mathf.Tan(20 * Mathf.Deg2Rad);
            var rect = FormationCameraSafeRect;
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
