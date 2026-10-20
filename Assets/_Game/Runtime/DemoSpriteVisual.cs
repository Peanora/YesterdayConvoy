using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Only the current comic frames; never modifies the logical unit.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        private ManualBattle manualBattle;
        private DeployedUnitState deployedUnit;
        private CardDefinition frameCard;
        public TalulahRig Rig { get; private set; }
        public bool UsesFrameAnimation => Rig==null && frameCard!=null && frameCard.HasFrameAnimation;
        public string FrameState { get; private set; } = "Idle";
        public int FrameIndex { get; private set; }
        public SpriteRenderer Renderer => sprite;

        private void OnEnable()
        {
            transform.localPosition=Vector3.zero;
            if(sprite==null) return;
            if(UsesFrameAnimation) sprite.sprite=frameCard.BattleSprite;
            sprite.color=Color.white;
            FrameState="Idle"; FrameIndex=0;
        }

        public void Bind(ManualBattle battle,DeployedUnitState unit,CardDefinition frames)
        {
            if(frames==null || !frames.HasFrameAnimation) throw new ArgumentException("Comic frame configuration is required.");
            manualBattle=battle; deployedUnit=unit; frameCard=frames;
            sprite.sprite=frames.BattleSprite; FrameState="Idle"; FrameIndex=0;
            if(frames.ArticulatedArt!=null)
            { Rig=gameObject.AddComponent<TalulahRig>(); Rig.Bind(frames.ArticulatedArt,sprite.sharedMaterial); }
        }

        private void LateUpdate()
        {
            if(deployedUnit==null) { FaceCamera(); return; }
            Color tint=manualBattle.Clock<deployedUnit.LastHitTime+.18 ? new Color(1,.48f,.48f) : Color.white;
            PresentFrames(tint);
        }

        // Exact source frames, no cutout deformation or directional turning. Only the simulation clock is read.
        private void PresentFrames(Color tint)
        {
            if(!FaceCamera()) return;
            if(Rig!=null)
            {
                sprite.enabled=false; transform.localPosition=Vector3.zero; transform.localScale=Vector3.one;
                FrameState=manualBattle.IsSkillActionPlaying(deployedUnit) ? "Skill" : deployedUnit.IsMoving ? "Walk" : deployedUnit.HasCombatTarget ? "Attack" : "Idle";
                Rig.Present(manualBattle,deployedUnit,tint,GetSortingOrder(Camera.main,transform.parent.position));
                return;
            }
            sprite.enabled=true; sprite.flipX=false; sprite.color=tint;
            transform.localPosition=Vector3.zero; transform.localScale=Vector3.one;
            if(manualBattle.IsPaused) return;
            double age=manualBattle.Clock-deployedUnit.LastAttackTime;
            double skillDuration=manualBattle.CombatSettings.SkillInterval;
            // A fired skill gesture owns its full window, independently of target lifetime or resumed movement.
            if(manualBattle.IsSkillActionPlaying(deployedUnit))
            {
                FrameState="Skill";
                FrameIndex=manualBattle.CombatSettings.FireRain ?
                    (int)((manualBattle.Clock-deployedUnit.FireRainStartedAt)*4)%frameCard.SkillFrames.Count :
                    Mathf.Clamp((int)(age/skillDuration*frameCard.SkillFrames.Count),0,frameCard.SkillFrames.Count-1);
                sprite.sprite=frameCard.SkillFrames[FrameIndex]; return;
            }
            if(deployedUnit.IsMoving)
            {
                FrameState="Walk";
                // At the authored 0.75 move speed, retain the GIF's 0.95-second cycle; distance freezes on stop/pause.
                FrameIndex=manualBattle.CombatSettings.FireRain ?
                    (int)(deployedUnit.Motion.DistanceTravelled/.9*frameCard.WalkFrames.Count)%frameCard.WalkFrames.Count :
                    (int)(deployedUnit.Motion.DistanceTravelled/.75*20)%frameCard.WalkFrames.Count;
                sprite.sprite=frameCard.WalkFrames[FrameIndex]; return;
            }
            if(!deployedUnit.HasCombatTarget)
            { FrameState="Idle"; FrameIndex=0; sprite.sprite=frameCard.BattleSprite; return; }
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

        private bool FaceCamera()
        {
            Camera camera = Camera.main;
            if (camera == null || sprite == null) return false;
            transform.rotation = camera.transform.rotation;
            sprite.sortingOrder = GetSortingOrder(camera, transform.parent.position);
            return true;
        }

        // Ground markers use order 0. Reserve positive, resolution-independent bands for actors,
        // with enough room for health/skill feedback inside each band. Depth testing remains enabled.
        public static int GetSortingOrder(Camera camera, Vector3 groundPosition)
        {
            float viewportY = Mathf.Clamp01(camera.WorldToViewportPoint(groundPosition).y);
            return 1024 + Mathf.RoundToInt((1 - viewportY) * 512) * 32;
        }

    }
}
