using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Clock-driven battle frames; never modifies the logical unit.</summary>
    public sealed class DemoSpriteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        private ManualBattle manualBattle;
        private DeployedUnitState deployedUnit;
        private CardDefinition frameCard;
        private OriginalTalulahArt original;
        private OriginalSlugArt slug;
        private LocalMonsterArt local;
        private Func<double> presentationClock;
        private bool slugWasMoving;
        private double slugTransitionTime,slugWalkDistance;
        public TalulahRig Rig { get; private set; }
        public bool UsesFrameAnimation => Rig==null && frameCard!=null && frameCard.HasFrameAnimation;
        public string FrameState { get; private set; } = "Idle";
        public int FrameIndex { get; private set; }
        public SpriteRenderer Renderer => sprite;

        private void OnEnable()
        {
            transform.localPosition=Vector3.zero;
            if(sprite==null) return;
            if(UsesFrameAnimation) sprite.sprite=original != null ? original["idle"].Frames[0] : frameCard.BattleSprite;
            sprite.color=Color.white;
            FrameState="Idle"; FrameIndex=0;
        }

        public void Bind(ManualBattle battle,DeployedUnitState unit,CardDefinition frames,Func<double> presentationClock=null)
        {
            if(frames==null || !frames.HasFrameAnimation) throw new ArgumentException("Battle animation configuration is required.");
            if(sprite==null) sprite=GetComponent<SpriteRenderer>();
            manualBattle=battle; deployedUnit=unit; frameCard=frames;
            this.presentationClock=presentationClock; local=LocalMonsterArt.For(frames.CardId); slug=OriginalSlugArt.For(frames.CardId);
            slugWasMoving=false; slugTransitionTime=battle.Clock; slugWalkDistance=unit.Motion.DistanceTravelled;
            original=OriginalTalulahArt.For(frames);
            sprite.sprite=original != null ? original["idle"].Frames[0] : frames.BattleSprite; FrameState="Idle"; FrameIndex=0;
            if(original==null && frames.ArticulatedArt!=null)
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
            if(local!=null) { PresentLocal(); return; }
            if(slug!=null) { PresentSlug(); return; }
            if(manualBattle.IsPaused) return;
            if(original!=null) { PresentOriginal(); return; }
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

        private void PresentLocal()
        {
            string clip; double time; bool loop=false;
            if(deployedUnit.Resolution==UnitResolution.Dead || deployedUnit.IsReviving)
            { FrameState="Death"; clip="death"; time=(presentationClock?.Invoke() ?? manualBattle.Clock)-deployedUnit.DeathTime; sprite.color=Color.white; }
            else
            {
                double age=manualBattle.Clock-deployedUnit.LastAttackTime;
                if(manualBattle.CombatSettings.Effects.Mudrock && manualBattle.Clock<deployedUnit.BarrierRefreshStartedAt+1.5 && local.HasClip("extra_skill_1"))
                {
                    FrameState="BarrierRefresh";sprite.sprite=local["extra_skill_1"].At(manualBattle.Clock-deployedUnit.BarrierRefreshStartedAt,false,out int shieldIndex);
                    FrameIndex=shieldIndex;return;
                }
                if(manualBattle.CombatSettings.Effects.Healer && manualBattle.Clock<deployedUnit.HealingStartedAt+manualBattle.CombatSettings.AttackDuration)
                {
                    FrameState="Heal";sprite.sprite=local["attack"].At(manualBattle.Clock-deployedUnit.HealingStartedAt,false,out int healIndex);
                    FrameIndex=healIndex;return;
                }
                if(manualBattle.CombatSettings.Effects.FrostNova && manualBattle.Clock<deployedUnit.IceRingStartedAt+3 && local.HasClip("extra_skill_1"))
                {
                    FrameState="IceRing";
                    sprite.sprite=local["extra_skill_1"].At(manualBattle.Clock-deployedUnit.IceRingStartedAt,false,out int ringIndex);
                    FrameIndex=ringIndex;return;
                }
                string attackClip=manualBattle.CombatSettings.Effects.Mudrock && deployedUnit.OrdinaryAttacksFired>=3 && local.HasClip("extra_attack_2") ? "extra_attack_2" : "attack";
                if(manualBattle.CombatSettings.CanAttack && local.HasAttack && age>=0 && age<local[attackClip].Duration/deployedUnit.LastAttackRate)
                { FrameState="Attack"; clip=attackClip; time=age*deployedUnit.LastAttackRate; }
                else if(deployedUnit.IsMoving)
                { FrameState="Walk"; clip="walk"; time=deployedUnit.Motion.DistanceTravelled/local.ReferenceSpeed; loop=true; }
                else { FrameState="Idle"; clip="idle"; time=manualBattle.Clock-deployedUnit.SpawnTime; loop=true; }
            }
            sprite.sprite=local[clip].At(time,loop,out int index); FrameIndex=index;
        }

        private void PresentSlug()
        {
            string clip; double time;
            if(deployedUnit.Resolution==UnitResolution.Dead)
            {
                FrameState="Death"; clip="death";
                time=(presentationClock?.Invoke() ?? manualBattle.Clock)-deployedUnit.DeathTime;
                sprite.color=Color.white;
            }
            else
            {
                bool moving=deployedUnit.IsMoving;
                if(moving!=slugWasMoving)
                { slugWasMoving=moving; slugTransitionTime=manualBattle.Clock; slugWalkDistance=deployedUnit.Motion.DistanceTravelled; }
                double elapsed=manualBattle.Clock-slugTransitionTime;
                double attackAge=manualBattle.Clock-deployedUnit.LastAttackTime;
                if(!moving && deployedUnit.HasCombatTarget && attackAge>=0 && attackAge<slug["attack"].Duration)
                { FrameState="Attack"; clip="attack"; time=attackAge; }
                else if(moving)
                {
                    double travel=(deployedUnit.Motion.DistanceTravelled-slugWalkDistance)/slug.ReferenceSpeed;
                    if(travel<slug["start"].Duration) { FrameState="Start"; clip="start"; time=travel; }
                    else { FrameState="Walk"; clip="walk"; time=travel-slug["start"].Duration; }
                }
                else if(elapsed<slug["stop"].Duration && !deployedUnit.HasCombatTarget)
                { FrameState="Stop"; clip="stop"; time=elapsed; }
                else { FrameState="Idle"; clip="idle"; time=manualBattle.Clock-deployedUnit.SpawnTime; }
                // Presentation-only hit recoil, never modify the route/root/collider.
                float recoil=Mathf.Clamp01((float)(1-(manualBattle.Clock-deployedUnit.LastHitTime)/.2));
                transform.localPosition=transform.parent.InverseTransformVector(Camera.main.transform.right*(-.035f*recoil));
            }
            sprite.sprite=slug[clip].At(time,clip=="walk" || clip=="idle",out int index);FrameIndex=index;
        }

        private void PresentOriginal()
        {
            double age = manualBattle.Clock - deployedUnit.LastAttackTime;
            string clip;
            double sourceTime;
            bool loop;
            if (manualBattle.IsSkillActionPlaying(deployedUnit))
            {
                FrameState = "Skill"; clip = "cast"; loop = false;
                double elapsed = manualBattle.Clock - deployedUnit.FireRainStartedAt;
                double total = manualBattle.CombatSettings.SkillInterval;
                double raise = Math.Min(.8, total * .2), lower = Math.Min(.6, total * .2);
                double holdPose = 1.1666667;
                sourceTime = elapsed < raise ? elapsed / raise * holdPose :
                    elapsed < total - lower ? holdPose :
                    holdPose + (elapsed - (total - lower)) / lower * (original["cast"].Duration - holdPose);
            }
            else if (deployedUnit.IsMoving)
            {
                FrameState = "Walk"; clip = "walk"; loop = true;
                sourceTime = deployedUnit.Motion.DistanceTravelled / original.ReferenceSpeed;
            }
            else if (deployedUnit.HasCombatTarget && age >= 0 && age < manualBattle.CombatSettings.UnitInterval)
            {
                FrameState = "Attack"; clip = "attack"; loop = false;
                // Align the source OnAttack marker with this game's existing hit time. Never reschedule damage.
                double contact = Math.Min(manualBattle.CombatSettings.ProjectileTime, manualBattle.CombatSettings.UnitInterval * .8);
                sourceTime = age <= contact ? age / contact * original.AttackContact :
                    original.AttackContact + (age - contact) / (manualBattle.CombatSettings.UnitInterval - contact) *
                    (original["attack"].Duration - original.AttackContact);
            }
            else
            {
                FrameState = "Idle"; clip = "idle"; loop = true;
                sourceTime = manualBattle.Clock - deployedUnit.SpawnTime;
            }
            sprite.sprite = original[clip].At(sourceTime, loop, out int index);
            FrameIndex = index;
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
