using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Camera-facing presentation only; it never changes logical unit position or HP.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private InitialDemo movingDemo;
        [SerializeField] private Sprite rearPose;
        [SerializeField] private Sprite sidePose;
        [SerializeField] private Sprite rearQuarterPose;
        private Sprite idlePose;
        private int previousShots;
        private float flashUntil;
        private float previousTime;
        private DemoWalkPuppet puppet;
        private RouteMotion previousMotion;
        private ManualBattle manualBattle;
        private DeployedUnitState deployedUnit;
        private Sprite attackSprite;
        private SpriteRenderer attackPose;
        private CardDefinition frameCard;
        public bool UsesFrameAnimation => frameCard!=null && frameCard.HasFrameAnimation;
        public string FrameState { get; private set; } = "Idle";
        public int FrameIndex { get; private set; }

        public SpriteRenderer Renderer => sprite;
        public DemoWalkPuppet Puppet => puppet;

        private void OnEnable()
        {
            previousShots = 0;
            flashUntil = -1;
            previousTime = 0;
            previousMotion = null;
            puppet?.Reset();
            transform.localPosition = Vector3.zero;
            if (sprite != null)
            {
                if (idlePose == null) idlePose = sprite.sprite;
                sprite.sprite = UsesFrameAnimation ? frameCard.BattleSprite : idlePose;
                sprite.color = Color.white;
            }
        }

        private void LateUpdate()
        {
            if (deployedUnit != null)
            {
                bool combat = manualBattle.Phase == BattlePhase.Running;
                float clock = (float)(manualBattle.Clock - deployedUnit.SpawnTime);
                bool attacking = manualBattle.EngagementEnabled && deployedUnit.HasCombatTarget;
                Vector3 direction = attacking ? manualBattle.Defender.Position - deployedUnit.Motion.Position : deployedUnit.Motion.Direction;
                Color tint = combat && manualBattle.Clock < deployedUnit.LastHitTime + 0.18 ? new Color(1,0.48f,0.48f) : Color.white;
                if(UsesFrameAnimation) { PresentFrames(tint); return; }
                PresentMotion((float)deployedUnit.Motion.DistanceTravelled, clock, direction,
                    combat && !manualBattle.IsPaused && deployedUnit.Active && deployedUnit.IsMoving, tint);
                PresentAttack(attacking, direction, tint);
                return;
            }
            if (movingDemo == null) { FaceCamera(); return; }
            float time = movingDemo.SimulationTime;
            if (movingDemo.Motion != previousMotion || time < previousTime)
            {
                previousShots = 0;
                flashUntil = -1;
                puppet?.Reset();
                previousMotion = movingDemo.Motion;
            }
            previousTime = time;
            if (movingDemo.ShotsFired > previousShots) flashUntil = time + 0.18f;
            previousShots = movingDemo.ShotsFired;
            bool moving = movingDemo.Outcome == DemoOutcome.Running;
            PresentMotion((float)movingDemo.Motion.DistanceTravelled, time, movingDemo.Motion.Direction, moving,
                time < flashUntil ? new Color(1, 0.48f, 0.48f) : Color.white);
        }

        public void Bind(ManualBattle battle, DeployedUnitState unit, Sprite attackingSprite = null, CardDefinition frames = null)
        {
            manualBattle = battle; deployedUnit = unit; movingDemo = null;
            attackSprite = attackingSprite;
            frameCard = frames;
            if (idlePose == null) idlePose = sprite.sprite;
            if(UsesFrameAnimation) { sprite.sprite=frameCard.BattleSprite; FrameState="Idle"; FrameIndex=0; }
        }

        // Exact source frames, no cutout deformation or directional turning. Only the simulation clock is read.
        private void PresentFrames(Color tint)
        {
            if(!FaceCamera()) return;
            puppet?.Hide(); if(attackPose!=null) attackPose.enabled=false;
            sprite.enabled=true; sprite.flipX=false; sprite.color=tint;
            transform.localPosition=Vector3.zero; transform.localScale=Vector3.one;
            if(manualBattle.IsPaused) return;
            if(deployedUnit.IsMoving)
            {
                FrameState="Walk";
                // At the authored 0.75 move speed, retain the GIF's 0.95-second cycle; distance freezes on stop/pause.
                FrameIndex=(int)(deployedUnit.Motion.DistanceTravelled/.75*20)%frameCard.WalkFrames.Count;
                sprite.sprite=frameCard.WalkFrames[FrameIndex]; return;
            }
            if(!deployedUnit.HasCombatTarget)
            { FrameState="Idle"; FrameIndex=0; sprite.sprite=frameCard.BattleSprite; return; }
            double age=manualBattle.Clock-deployedUnit.LastAttackTime;
            bool skill=deployedUnit.SkillAmmo>0 || deployedUnit.LastAttackEmpowered && age<manualBattle.CombatSettings.SkillInterval;
            var sequence=skill ? frameCard.SkillFrames : frameCard.AttackFrames;
            FrameState=skill ? "Skill" : "Attack";
            if(skill && !deployedUnit.LastAttackEmpowered) age=0;
            if(double.IsInfinity(age)) age=0;
            // Fit the full five-second skill gesture into the existing attack interval without delaying damage.
            double duration=skill ? manualBattle.CombatSettings.SkillInterval : sequence.Count/20.0;
            FrameIndex=Mathf.Clamp((int)(age/duration*sequence.Count),0,sequence.Count-1);
            sprite.sprite=sequence[FrameIndex];
        }

        private void PresentAttack(bool attacking, Vector3 direction, Color tint)
        {
            if (attackSprite == null) return;
            if (attackPose == null)
            {
                attackPose = new GameObject("Wisadel aiming pose", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                attackPose.transform.SetParent(transform, false);
                attackPose.sprite = attackSprite;
            }
            attackPose.enabled = attacking;
            if (!attacking) return;
            puppet?.Hide(); sprite.enabled = false;
            float face = Camera.main.transform.InverseTransformDirection(direction).x < 0 ? -1 : 1;
            attackPose.flipX = face < 0;
            float age = (float)(manualBattle.Clock - deployedUnit.LastAttackTime);
            float recoil = age >= 0 && age < 0.3f ? 0.065f * Mathf.Exp(-age / 0.07f) : 0;
            attackPose.transform.localPosition = new Vector3(-face * recoil, 0, 0);
            attackPose.sortingOrder = sprite.sortingOrder + 30;
            attackPose.color = tint;
        }

        // Read-only movement input. Also used by the isolated art preview; never writes the logical root.
        public void PresentMotion(float distance, float simulationTime, Vector3 direction, bool moving, Color tint)
        {
            if (!FaceCamera()) return;
            sprite.color = tint;
            if (rearPose != null)
            {
                if (puppet == null) puppet = new DemoWalkPuppet(transform, idlePose, rearPose, sidePose != null ? sidePose : idlePose, rearQuarterPose != null ? rearQuarterPose : rearPose);
                sprite.enabled = false;
                transform.localPosition = Vector3.zero;
                puppet.Pose(distance, simulationTime, direction, moving, tint, sprite.sortingOrder);
                return;
            }
            puppet?.Hide();
            sprite.enabled = true;
            transform.localScale = Vector3.one;
        }

        private bool FaceCamera()
        {
            Camera camera = Camera.main;
            if (camera == null || sprite == null) return false;
            transform.rotation = camera.transform.rotation;
            sprite.sortingOrder = GetSortingOrder(camera, transform.parent.position);
            return true;
        }

        // Ground markers use order 0. Reserve positive, resolution-independent bands for actors,
        // with enough room for the cutout parts inside each band. Depth testing remains enabled.
        public static int GetSortingOrder(Camera camera, Vector3 groundPosition)
        {
            float viewportY = Mathf.Clamp01(camera.WorldToViewportPoint(groundPosition).y);
            return 1024 + Mathf.RoundToInt((1 - viewportY) * 512) * 32;
        }

        private void OnDestroy() => puppet?.Dispose();
    }
}
