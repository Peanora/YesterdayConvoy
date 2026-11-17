using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float FormationCameraTravelSeconds = .42f;
        static readonly Quaternion FormationCameraRotation = Quaternion.Euler(60, 0, 0);
        static readonly Vector3 FormationCameraForward = FormationCameraRotation * Vector3.forward;
        static readonly Vector3 FormationCameraUp = FormationCameraRotation * Vector3.up;
        static readonly Vector3 FormationCameraRight = FormationCameraRotation * Vector3.right;
        static readonly Quaternion FormationCameraInverse = Quaternion.Inverse(FormationCameraRotation);

        // This stays inside the existing map pointer area, above the original A lineup and
        // left of its recruitment list. The camera continues to render the full virtual canvas.
        public static Rect FormationCameraSafeRect => new Rect(204, 72, 790, 400);
        public bool ViewingDefenders { get; private set; }
        public bool CameraTransitioning => formationCameraInitialized && formationCameraBlend < 1;
        public bool CanEditFormationView => Ready && Model != null && Model.Phase == FormationPhase.Preparation &&
            !ViewingDefenders && !CameraTransitioning;

        bool formationCameraInitialized, formationCameraWasPreparing;
        int formationCameraRound, formationCameraSignature;
        float formationCameraBlend = 1, formationCameraRefitTime;
        Vector3 formationCameraFrom, formationCameraTarget;
        readonly List<Vector3> formationCameraPoints = new List<Vector3>();

        internal void InitializeFormationCamera()
        {
            if (battleCamera == null || Model == null) return;
            formationCameraInitialized = true;
            formationCameraWasPreparing = Model.Phase == FormationPhase.Preparation;
            formationCameraRound = Model.Round;
            ViewingDefenders = !formationCameraWasPreparing;
            ReframeFormationCamera(true);
        }

        public void ToggleFormationView()
        {
            if (Model != null && Model.Phase == FormationPhase.Preparation)
                SetFormationView(!ViewingDefenders);
        }

        public void SetFormationView(bool defenders, bool immediate = false)
        {
            if (battleCamera == null || Model == null) return;
            if (!formationCameraInitialized) InitializeFormationCamera();
            // Scouting is a preparation choice; an active battle always shows the guard side.
            ViewingDefenders = Model.Phase == FormationPhase.Preparation ? defenders : true;
            CancelFormationDrag();
            ReframeFormationCamera(immediate);
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

            formationCameraRefitTime += Mathf.Max(0, unscaledDt);
            if (!pointerHeld && formationCameraRefitTime >= .15f)
            {
                formationCameraRefitTime = 0;
                int signature = FormationCameraRosterSignature();
                // Moving a prepared large unit may change its required headroom. During battle,
                // expand only when a visible body needs room, so idle frames do not pump the zoom.
                if (preparing && signature != formationCameraSignature)
                    ReframeFormationCamera(false);
                else if (!preparing && FormationCameraNeedsHeadroom())
                    ReframeFormationCamera(false, true);
            }
            formationCameraBlend = Mathf.Min(1, formationCameraBlend + Mathf.Max(0, unscaledDt) / FormationCameraTravelSeconds);
            float eased = Mathf.SmoothStep(0, 1, formationCameraBlend);
            ApplyFormationCamera(Vector3.Lerp(formationCameraFrom, formationCameraTarget, eased));
        }

        int FormationCameraRosterSignature()
        {
            unchecked
            {
                int hash = ViewingDefenders ? 17 : 31;
                foreach (var actor in Model.Units)
                    if (actor.Enabled && (!ViewingDefenders || actor.Position.x >= 6.5f))
                        hash = ((hash * 397 ^ actor.Id) * 397 ^ actor.Position.GetHashCode()) * 397 ^ Mathf.RoundToInt(ActorPresentationHeight(actor) * 100);
                if (ViewingDefenders)
                    foreach (var actor in Model.Defenders)
                        hash = ((hash * 397 ^ actor.Id) * 397 ^ actor.Position.GetHashCode()) * 397 ^ Mathf.RoundToInt(ActorPresentationHeight(actor) * 100);
                return hash;
            }
        }

        void ReframeFormationCamera(bool immediate, bool expandOnly = false)
        {
            CollectFormationCameraPoints();
            Vector3 focus = new Vector3(ViewingDefenders ? 3.5f : -3.5f, .12f, 0);
            float minimumDistance = expandOnly ? Vector3.Dot(focus - formationCameraTarget, FormationCameraForward) : 0;
            Vector3 target = FitFormationCamera(focus, minimumDistance);
            formationCameraSignature = FormationCameraRosterSignature();
            if (immediate)
            {
                formationCameraFrom = formationCameraTarget = target;
                formationCameraBlend = 1;
                ApplyFormationCamera(target);
            }
            else if (Vector3.Distance(target, formationCameraTarget) > .025f)
            {
                formationCameraFrom = battleCamera.transform.position;
                formationCameraTarget = target;
                formationCameraBlend = 0;
            }
        }

        void CollectFormationCameraPoints()
        {
            formationCameraPoints.Clear();
            float first = ViewingDefenders ? 6.5f : -.5f;
            float last = ViewingDefenders ? 13.5f : 6.5f;
            formationCameraPoints.Add(WorldPoint(new Vector2(first, -.5f)));
            formationCameraPoints.Add(WorldPoint(new Vector2(first, 7.5f)));
            formationCameraPoints.Add(WorldPoint(new Vector2(last, -.5f)));
            formationCameraPoints.Add(WorldPoint(new Vector2(last, 7.5f)));
            foreach (var actor in Model.Units)
                if (actor.Enabled && (!ViewingDefenders || actor.Alive && actor.Position.x >= 6.5f)) AddFormationActorToCamera(actor);
            if (ViewingDefenders)
                foreach (var actor in Model.Defenders) AddFormationActorToCamera(actor);
        }

        void AddFormationActorToCamera(FormationActor actor)
        {
            Vector3 foot = ActorWorldPoint(actor);
            float height = Mathf.Max(.25f, ActorPresentationHeight(actor));
            Vector3 head = foot + FormationCameraUp * (height + .18f);
            formationCameraPoints.Add(foot);
            formationCameraPoints.Add(head);
            if (bodies.TryGetValue(actor.Id, out var body) && body != null && body.sprite != null)
            {
                var bounds = body.sprite.bounds;
                // Use the displayed sprite's actual pivot, width and scale. Re-anchor it at the
                // latest model foot, since camera hooks may run immediately before SyncActors.
                foreach (float x in new[] { bounds.min.x, bounds.max.x })
                    foreach (float y in new[] { bounds.min.y, bounds.max.y })
                        formationCameraPoints.Add(foot + FormationCameraRotation * Vector3.Scale(new Vector3(body.flipX ? -x : x, body.flipY ? -y : y, 0), body.transform.lossyScale));
            }
            else
            {
                // First startup can precede presentation; still reserve a complete large body.
                float halfWidth = height * .6f;
                formationCameraPoints.Add(foot - FormationCameraRight * halfWidth);
                formationCameraPoints.Add(foot + FormationCameraRight * halfWidth);
                formationCameraPoints.Add(head - FormationCameraRight * halfWidth);
                formationCameraPoints.Add(head + FormationCameraRight * halfWidth);
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

        bool FormationCameraNeedsHeadroom()
        {
            if (CameraTransitioning) return false;
            CollectFormationCameraPoints();
            var rect = FormationCameraSafeRect;
            foreach (var point in formationCameraPoints)
            {
                Vector3 local = FormationCameraInverse * (point - formationCameraTarget);
                if (local.z <= .1f) return true;
                float focal = 360 / Mathf.Tan(20 * Mathf.Deg2Rad);
                var projected = new Vector2(640 + focal * local.x / local.z, 360 - focal * local.y / local.z);
                if (projected.x < rect.xMin - 4 || projected.x > rect.xMax + 4 || projected.y < rect.yMin - 4 || projected.y > rect.yMax + 4) return true;
            }
            return false;
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
