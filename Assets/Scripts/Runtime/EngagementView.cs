using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Clock-driven Kroos pose, projectiles and impacts. Never applies damage or moves a logical entity.</summary>
    public sealed class EngagementView : MonoBehaviour
    {
        private sealed class ShotView { public LineRenderer Trail, Impact, Rays, Ink; public SpriteRenderer Face; }
        private sealed class Puff { public Vector3 Position; public double Time; public bool Death; public string CardId; }
        private TalulahAttackVfx talulahAttackVfx;
        private SilverWolfView wolfVfx;
        private bool wolfSlugTrial;
        public int WolfEffectCount => wolfVfx?.VisibleCount??0;
        private CommonCombatVfxDirector commonVfx;
        private AdvancedDefenderView advancedVfx;
        private InfectionBurstFeedback infectionFeedback;
        public double NativeEffectsUntil => System.Math.Max(wolfVfx?.PendingUntil??double.NegativeInfinity,System.Math.Max(System.Math.Max(CommonCombatVfxDirector.PendingUntil(owner?.Battle),advancedVfx?.PendingUntil??double.NegativeInfinity),infectionFeedback?.PendingUntil??double.NegativeInfinity));
        private readonly List<ShotView> shots = new List<ShotView>();
        private readonly List<Puff> puffs = new List<Puff>();
        private readonly List<LineRenderer> seedViews=new List<LineRenderer>();
        private readonly HashSet<string> spawned = new HashSet<string>(), dead = new HashSet<string>();
        private ManualBattleController owner;
        private EngagementDefinition definition;
        private Material material;
        [SerializeField] private SpriteRenderer kroos;
        private LineRenderer range;
        private readonly List<SpriteRenderer> extraVisuals=new List<SpriteRenderer>();
        private AudioSource audioSource;
        private AudioClip click, impact;
        private int runId, lastPhase = -1;
        private float readyIdleTime;
        private DefenderRosterView rosterView;
        public SpriteRenderer DefenderVisual => kroos;

        public void Initialize(ManualBattleController controller, EngagementDefinition data, AudioClip uiSound, AudioClip hitSound)
        {
            owner = controller; definition = data; click = uiSound; impact = hitSound;
            ClearTransient(); runId=-1;
            wolfSlugTrial=!controller.DefenderTrialActive && !controller.TacticalMapActive && controller.Definition.Card.CardId==SilverWolfCombat.CardId;
            if(commonVfx==null)commonVfx=new CommonCombatVfxDirector(transform);
            infectionFeedback=GetComponent<InfectionBurstFeedback>()??gameObject.AddComponent<InfectionBurstFeedback>();infectionFeedback.Bind(controller);
            if(rosterView!=null) { rosterView.gameObject.SetActive(false); Destroy(rosterView.gameObject); rosterView=null; }
            if(controller.DefenderTrialActive)
            { rosterView=new GameObject("Eight original defenders").AddComponent<DefenderRosterView>();
              rosterView.transform.SetParent(transform,false); rosterView.Initialize(controller); }
            var deathEffects=GetComponent<MonsterDeathEffectView>();
            if(deathEffects==null) deathEffects=gameObject.AddComponent<MonsterDeathEffectView>();
            deathEffects.Initialize(controller);
            var fuel=GetComponent<FlamerazerEffectView>()??gameObject.AddComponent<FlamerazerEffectView>();fuel.Bind(()=>controller.Battle,()=>controller.PresentationClock);
            var mandra=GetComponent<MandragoraEffectView>()??gameObject.AddComponent<MandragoraEffectView>();mandra.Bind(()=>controller.Battle,()=>controller.PresentationClock);
            var mephi=GetComponent<MephistoEffectView>()??gameObject.AddComponent<MephistoEffectView>();mephi.Bind(()=>controller.Battle);
            var neural=GetComponent<NeuralEffectView>()??gameObject.AddComponent<NeuralEffectView>();neural.Bind(()=>controller.Battle,()=>controller.PresentationClock);
            var marine=GetComponent<MarineTideView>()??gameObject.AddComponent<MarineTideView>();marine.Bind(()=>controller.Battle,()=>controller.PresentationClock);
            var monsterOriginal=GetComponent<MonsterOriginalEffectView>()??gameObject.AddComponent<MonsterOriginalEffectView>();monsterOriginal.Bind(()=>controller.Battle,()=>controller.PresentationClock);
            advancedVfx=GetComponent<AdvancedDefenderView>()??gameObject.AddComponent<AdvancedDefenderView>();advancedVfx.Bind(()=>controller.Battle,()=>controller.PresentationClock);
            foreach(var v in extraVisuals) if(v!=null) Destroy(v.gameObject);
            extraVisuals.Clear();
            if(material==null) material = new Material(Shader.Find("Sprites/Default"));
            if(kroos==null) kroos = new GameObject("Kroos combat sprite", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            kroos.transform.SetParent(transform, false); kroos.sprite = data.KroosSprite; kroos.sharedMaterial = material;
            if(range==null) range = CreateLine("Kroos range", 0.018f); range.loop = true; range.positionCount = 64; range.sortingOrder = 0;
            for(int i=0;i<data.ExtraDefenders.Count;i++)
            { var v=new GameObject("Kroos flank "+i,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
              v.transform.SetParent(transform,false); v.sprite=data.KroosSprite; v.sharedMaterial=material; extraVisuals.Add(v); }
            if(audioSource==null) audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0; audioSource.mute=owner.Muted;
        }

        private void LateUpdate()
        {
            if (owner == null || owner.Battle == null || !owner.Battle.EngagementEnabled) return;
            ManualBattle b = owner.Battle; var d = b.Defender; Camera cam = Camera.main;
            if (runId != b.RunId) { ClearTransient(); runId = b.RunId; lastPhase = -1; readyIdleTime = 0; }
            // Before combat there is no simulation clock yet. Carry that idle phase into combat
            // so starting does not snap the pose; thereafter pause/speed use only the shared clock.
            if (b.Phase == BattlePhase.Ready) readyIdleTime += Time.deltaTime;
            double time = b.Clock;
            double fxTime=owner.PresentationClock;
            commonVfx.Begin();
            commonVfx.PresentBattleBursts(b,cam,fxTime);
            commonVfx.PresentDefenderBuffs(b,cam,fxTime);
            for(int i=0;i<b.DeepSeeds.Count;i++)
            {
                if(i>=seedViews.Count)seedViews.Add(CreateLine("Deep seed marker",.07f));
                var seed=b.DeepSeeds[i];var line=seedViews[i];line.enabled=!seed.Spent;
                if(line.enabled)Ring(line,seed.Position+Vector3.up*.1f,.18f,new Color(1,.48f,.12f,.8f),null);
            }
            if(wolfSlugTrial)
            {
                SilverWolfView.PresentSlug(kroos,b,d,cam,fxTime,readyIdleTime);
                for(int i=0;i<extraVisuals.Count;i++)
                    SilverWolfView.PresentSlug(extraVisuals[i],b,b.Defenders[i+1],cam,fxTime,readyIdleTime);
                range.enabled=false;
            }
            else if(rosterView==null)
            {
            float death = d.Alive ? 0 : Mathf.Clamp01((float)(time - d.DeathTime) / 0.45f);
            kroos.enabled = death < 1;
            kroos.transform.position = d.Position;
            if (d.Alive && d.FrozenUntil<=time)
            {
                float lift = 0.02f * (1 - Mathf.Cos((float)((readyIdleTime + time) * (Mathf.PI * 2 / 2.8f))));
                kroos.transform.position += cam.transform.up * lift;
            }
            kroos.transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, death * -16);
            var target = b.FindUnit(d.TargetId);
            if (target != null) kroos.flipX = cam.transform.InverseTransformDirection(target.Motion.Position - d.Position).x > 0;
            float recoil = b.Clock - b.LastShotTime < 0.25 ? 0.045f * Mathf.Exp(-(float)(b.Clock - b.LastShotTime) / 0.06f) : 0;
            kroos.transform.position += cam.transform.right * recoil;
            kroos.sortingOrder = DemoSpriteVisual.GetSortingOrder(cam, d.Position) + 30;
            Color tint = time < d.LastHitTime + 0.14 ? new Color(1, 0.5f, 0.45f) : Color.white;
            if(time>=d.LastHitTime+.14 && (d.ColdUntil>time || d.FrozenUntil>time || d.FrostSlowUntil>time)) tint=new Color(.45f,.8f,1);
            if(d.NeuralUntil>time)tint=new Color(.92f,.83f,.35f);
            tint.a = 1 - death; kroos.color = tint;
            range.enabled = d.Alive;
            range.startColor = range.endColor = new Color(0.45f, 0.8f, 0.88f, 0.25f);
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64;
                range.SetPosition(i, new Vector3(d.Position.x, 0.055f, d.Position.z) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * b.CombatSettings.DefenderRange);
            }
            for(int i=0;i<extraVisuals.Count;i++)
            {
                var other=b.Defenders[i+1]; var v=extraVisuals[i];
                float fade=other.Alive ? 1 : 1-Mathf.Clamp01((float)(time-other.DeathTime)/.45f);
                v.enabled=fade>0; v.transform.position=other.Position; v.transform.rotation=cam.transform.rotation;
                v.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,other.Position)+30;
                Color color=time<other.LastHitTime+.14 ? new Color(1,.5f,.45f,fade) :
                    other.ColdUntil>time || other.FrozenUntil>time ? new Color(.45f,.8f,1,fade) : new Color(1,1,1,fade); v.color=color;
            }
            }
            else { kroos.enabled=range.enabled=false; foreach(var v in extraVisuals) v.enabled=false; }
            foreach(var unit in b.Units)
                if(unit.Active&&owner.CardForUnit(unit).CardId==OriginalTalulahArt.CardId)
                {if(talulahAttackVfx==null)talulahAttackVfx=new TalulahAttackVfx(transform);break;}
            talulahAttackVfx?.Begin();
            if(talulahAttackVfx!=null)foreach(var unit in b.Units)
            {
                if(!unit.Active||owner.CardForUnit(unit).CardId!=OriginalTalulahArt.CardId)continue;
                var target=b.FindDefender(unit.CombatTargetId);
                var direction=target!=null?target.Position-unit.Motion.Position:unit.Motion.Direction;
                talulahAttackVfx.PresentUnit(b,unit,cam,Vector3.Dot(direction,cam.transform.right)<0);
            }
            wolfVfx?.Begin();
            var modelShots = b.CombatShots;
            for (int i = 0; i < modelShots.Count; i++)
            {
                if (shots.Count <= i)
                {
                    var view=new ShotView { Trail = CreateLine("Shot trail", 0.06f), Impact = CreateLine("Impact ring", 0.04f), Rays = CreateLine("Impact rays", 0.025f), Ink=CreateLine("Comic impact ink",.12f) };
                    shots.Add(view);
                    Play(impact, modelShots[i].FromDefender ? 0.23f : modelShots[i].Empowered ? 0.75f : 0.35f);
                }
                PrepareImpactFace(shots[i],modelShots[i],b,i);
                PresentShot(shots[i], modelShots[i], b, cam);
            }
            wolfVfx?.End();
            talulahAttackVfx?.End();
            foreach (var unit in b.Units)
            {
                string cardId=owner.CardForUnit(unit).CardId;
                if (spawned.Add(unit.EntityId)) { AddPuff(unit.Motion.Position, unit.SpawnTime, false,cardId); Play(click, 0.4f); }
                if (unit.Resolution == UnitResolution.Dead && dead.Add(unit.EntityId)) AddPuff(unit.Motion.Position, unit.DeathTime, true,cardId);
            }
            foreach (var puff in puffs)
            {
                commonVfx.PresentPuff(puff.Position,fxTime-puff.Time,puff.Death,cam,puff.CardId);
            }
            commonVfx.End();
            if ((int)b.Phase != lastPhase)
            { if (b.Phase == BattlePhase.Won || b.Phase == BattlePhase.Lost) Play(click, 0.7f); lastPhase = (int)b.Phase; }
            audioSource.mute = owner.Muted;
            audioSource.pitch = owner.SimulationSpeed;
            if (b.IsPaused) audioSource.Pause(); else audioSource.UnPause();
        }

        private void PrepareImpactFace(ShotView view,CombatShot shot,ManualBattle battle,int index)
        {
            // Mixed decks cannot borrow the primary card's effects. Resolve the retained
            // caster even after death so already-applied impact tails keep their own art.
            if(view.Face!=null)view.Face.enabled=false;
            var source=shot.FromDefender?null:battle.FindUnit(shot.SourceId);
            if(source==null)return;
            var card=owner.CardForUnit(source);var faces=card.ComicImpactFaces;
            if((faces==null||faces.Count==0)&&card.ArticulatedArt==null)return;
            if(view.Face==null)
            {
                view.Face=new GameObject("Comic expression",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                view.Face.transform.SetParent(transform,false);view.Face.sharedMaterial=material;
            }
            view.Face.sprite=faces!=null&&faces.Count>0?faces[index%faces.Count]:null;
            view.Face.enabled=false;
        }

        private void PresentShot(ShotView view, CombatShot shot, ManualBattle b, Camera cam)
        {
            bool wolfShot=!double.IsNegativeInfinity(shot.WolfActionAt);
            if(wolfShot || wolfSlugTrial && shot.FromDefender)
            {
                if(wolfShot)
                {
                    if(wolfVfx==null)wolfVfx=new SilverWolfView(transform,material);
                    wolfVfx.Present(shot,b,cam,owner.PresentationClock);
                }
                view.Trail.enabled=view.Impact.enabled=view.Rays.enabled=view.Ink.enabled=false;
                if(view.Face!=null)view.Face.enabled=false;
                return;
            }
            if(commonVfx.PresentShot(shot,b,cam,owner.PresentationClock,owner.CardForUnit(b.FindUnit(shot.SourceId))))
            {view.Trail.enabled=view.Impact.enabled=view.Rays.enabled=view.Ink.enabled=false;if(view.Face!=null)view.Face.enabled=false;return;}
            if(!shot.FromDefender&&(b.UnitCombat(b.FindUnit(shot.SourceId)).Effects.Mudrock||b.UnitCombat(b.FindUnit(shot.SourceId)).Effects.Mandragora)){view.Trail.enabled=view.Impact.enabled=view.Rays.enabled=view.Ink.enabled=false;if(view.Face!=null)view.Face.enabled=false;return;}
            if(shot.Erased){view.Trail.enabled=view.Impact.enabled=view.Rays.enabled=view.Ink.enabled=false;if(view.Face!=null)view.Face.enabled=false;return;}
            var defenderSource=shot.FromDefender?b.FindDefender(shot.SourceId):null;
            if(defenderSource!=null&&(DefenderVfxArt.SupportsDefender(defenderSource)||DefenderVfxArt.Supports(defenderSource.Settings.Skill.Kind)))
            {
                // The textured defender director owns these shots; never stack the old diagnostic ring over it.
                view.Trail.enabled=view.Impact.enabled=view.Rays.enabled=view.Ink.enabled=false;if(view.Face!=null)view.Face.enabled=false;return;
            }
            var sourceUnit=b.FindUnit(shot.SourceId);
            if(!shot.FromDefender&&b.IsPatriot(sourceUnit))
            {
                // Patriot's original-atlas director owns every hit and spear, including cancelled casts.
                view.Trail.enabled=view.Impact.enabled=view.Rays.enabled=view.Ink.enabled=false;if(view.Face!=null)view.Face.enabled=false;return;
            }
            var sourceCombat=b.UnitCombat(sourceUnit);
            var sourceCard=owner.CardForUnit(sourceUnit);
            view.Ink.enabled=false; if(view.Face!=null) view.Face.enabled=false;
            if(!shot.FromDefender && sourceCombat.Melee) { PresentMelee(view,shot,b,cam); return; }
            if(shot.FireRain) { PresentFireRain(view,shot,b,cam); return; }
            if(!shot.FromDefender && sourceCard.ArticulatedArt!=null) { PresentTalulahSlash(view,shot,b,cam); return; }
            if(shot.Healing) { PresentHealing(view,shot,b,cam); return; }
            double time = b.Clock;
            float t = Mathf.Clamp01((float)((time - shot.StartedAt) / (shot.ImpactAt - shot.StartedAt)));
            var target = b.FindUnit(shot.TargetId);
            Vector3 destination = shot.FromDefender && target != null ? target.Motion.Position : shot.Destination;
            Vector3 origin = shot.Origin + cam.transform.up * 0.75f;
            Vector3 end = destination + cam.transform.up * 0.8f;
            float face = cam.transform.InverseTransformDirection(end - origin).x < 0 ? -1 : 1;
            origin += cam.transform.right * (face * 0.45f);
            Color color = shot.FromDefender ? (shot.DamageType==CombatDamageType.Arts ? new Color(.55f,.65f,1) : new Color(1, 0.83f, 0.36f)) : shot.Empowered ? new Color(1, 0.36f, 0.12f) : new Color(0.95f, 0.68f, 0.3f);
            if(!shot.FromDefender && sourceCombat.Effects.ArmorShred>0) color=new Color(.45f,1,.2f);
            else if(!shot.FromDefender && sourceCombat.Effects.DamageType==CombatDamageType.Arts) color=new Color(.75f,.35f,1);
            if(shot.FrozenBonusApplied) color=new Color(.45f,.95f,1);
            if(!shot.FromDefender && shot.ColdDuration>0) color=new Color(.2f,.85f,1);
            bool flying = !shot.Resolved && time >= shot.StartedAt && time < shot.ImpactAt;
            view.Trail.enabled = flying;
            if (flying)
            {
                Vector3 point = Vector3.Lerp(origin, end, t) + cam.transform.up * Mathf.Sin(t * Mathf.PI) * (shot.Empowered ? 0.45f : 0.12f);
                view.Trail.positionCount = 2;
                view.Trail.SetPosition(0, Vector3.Lerp(origin, point, 0.65f)); view.Trail.SetPosition(1, point);
                view.Trail.widthMultiplier = shot.Empowered ? 0.13f : 0.035f;
                view.Trail.startColor = color; view.Trail.endColor = Color.white;
                view.Trail.sortingOrder = DemoSpriteVisual.GetSortingOrder(cam, destination) + 50;
            }
            float age = (float)(time - shot.ImpactAt), duration = shot.Empowered ? 0.55f : 0.18f;
            bool hit = shot.Applied && age >= 0 && age < duration;
            view.Impact.enabled = view.Rays.enabled = hit;
            if (!hit) return;
            color.a = 1 - age / duration;
            float radius = (shot.Empowered ? 0.28f : 0.05f) + age * (shot.Empowered ? 1.3f : 0.8f);
            if(ViviCombat.SkillShell(sourceUnit,shot))radius=shot.SplashRadius*Mathf.Lerp(.55f,1,Mathf.Clamp01(age/duration));
            if(!shot.FromDefender && sourceCard.HasFrameAnimation && LocalMonsterArt.For(sourceCard.CardId)==null)
            {
                // Graphic, black-edged comic burst and source-expression stamp; no extra damage event.
                view.Rays.enabled=false; view.Ink.enabled=true;
                int order=DemoSpriteVisual.GetSortingOrder(cam,destination)+50;
                view.Ink.sortingOrder=order; view.Impact.sortingOrder=order+1;
                ComicBurst(view.Ink,end,radius,new Color(.035f,.025f,.025f,color.a),cam);
                ComicBurst(view.Impact,end,radius,new Color(1,.83f,.34f,color.a),cam);
                view.Ink.widthMultiplier=shot.Empowered ? .13f : .06f;
                view.Impact.widthMultiplier=shot.Empowered ? .075f : .025f;
                if(shot.Empowered && view.Face!=null)
                {
                    view.Face.enabled=true; view.Face.sortingOrder=order+2;
                    view.Face.transform.position=end; view.Face.transform.rotation=cam.transform.rotation;
                    view.Face.transform.localScale=Vector3.one*(.84f+age*.56f);
                    view.Face.color=new Color(1,1,1,color.a);
                }
                return;
            }
            Ring(view.Impact, end, radius, color, cam);
            view.Impact.widthMultiplier = shot.Empowered ? 0.10f * (1 - age / duration) : 0.025f;
            view.Rays.positionCount = 24;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                Vector3 axis = cam.transform.right * Mathf.Cos(a) + cam.transform.up * Mathf.Sin(a);
                view.Rays.SetPosition(i * 2, end + axis * radius * 0.6f);
                view.Rays.SetPosition(i * 2 + 1, end + axis * radius * 1.3f);
            }
            view.Rays.startColor = view.Rays.endColor = color;
        }

        private void PresentHealing(ShotView v,CombatShot shot,ManualBattle b,Camera cam)
        {
            v.Trail.enabled=v.Rays.enabled=false;
            float age=(float)(b.Clock-shot.ImpactAt);
            v.Impact.enabled=shot.Applied && age>=0 && age<.55f;
            if(v.Impact.enabled)
            {
                var ally=b.FindDefender(shot.TargetId);
                Ring(v.Impact,(ally?.Position ?? shot.Destination)+cam.transform.up*.6f,.18f+age*.45f,new Color(.3f,1,.65f,1-age/.55f),cam);
            }
        }

        private void PresentMelee(ShotView v,CombatShot shot,ManualBattle b,Camera cam)
        {
            v.Trail.enabled=v.Ink.enabled=v.Rays.enabled=false;
            float age=(float)(b.Clock-shot.ImpactAt);
            v.Impact.enabled=shot.Applied && age>=0 && age<.16f;
            if(v.Impact.enabled)
            {
                v.Impact.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,shot.Destination)+50;
                Color color=shot.FrozenBonusApplied ? new Color(.45f,.95f,1,1-age/.16f) : new Color(1,.76f,.28f,1-age/.16f);
                Ring(v.Impact,shot.Destination+cam.transform.up*.32f,.06f+age*.8f,color,cam);
            }
        }

        private void PresentFireRain(ShotView v,CombatShot shot,ManualBattle b,Camera cam)
        {
            v.Trail.enabled=v.Ink.enabled=v.Impact.enabled=v.Rays.enabled=false;
            var art=owner.CardForUnit(b.FindUnit(shot.SourceId)).ArticulatedArt;
            if(v.Face==null)return;
            float age=(float)(b.Clock-shot.ImpactAt);
            v.Face.enabled=art!=null&&age>=0&&age<.66f;
            if(!v.Face.enabled)return;
            v.Face.sprite=art.ImpactFrames[Mathf.Clamp((int)(age*24),0,art.ImpactFrames.Count-1)];
            v.Face.transform.position=shot.Destination+cam.transform.up*.65f;
            v.Face.transform.rotation=cam.transform.rotation;v.Face.transform.localScale=Vector3.one*1.05f;
            v.Face.sortingOrder=23003;v.Face.color=new Color(1,1,1,1-age/.66f);
        }

        private void PresentTalulahSlash(ShotView v,CombatShot shot,ManualBattle b,Camera cam)
        {
            v.Trail.enabled=v.Ink.enabled=v.Impact.enabled=v.Rays.enabled=false;
            if(v.Face!=null)v.Face.enabled=false;
            if(talulahAttackVfx==null)talulahAttackVfx=new TalulahAttackVfx(transform);
            var unit=b.FindUnit(shot.SourceId);
            var art=OriginalTalulahArt.For(owner.CardForUnit(unit));
            if(art==null)return;
            float travel=(float)(shot.ImpactAt-shot.StartedAt);
            float recovery=Mathf.Max(.001f,b.UnitCombat(unit).UnitInterval-travel);
            float sourceRecoveryScale=(art["attack"].Duration-art.AttackContact)/recovery;
            talulahAttackVfx.Present(shot,b.Clock,cam,sourceRecoveryScale);
        }

        private void Play(AudioClip clip, float volume)
        { if (clip != null && !owner.Muted && !owner.Battle.IsPaused) audioSource.PlayOneShot(clip, volume * 0.35f); }

        private LineRenderer CreateLine(string label, float width)
        {
            var line = new GameObject(label, typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(transform, false); line.sharedMaterial = material; line.widthMultiplier = width;
            line.useWorldSpace = true; line.sortingOrder = 19000; line.enabled = false;
            return line;
        }

        private static void Ring(LineRenderer line, Vector3 point, float radius, Color color, Camera camera)
        {
            line.loop = true; line.positionCount = 32; line.startColor = line.endColor = color;
            Vector3 right = camera != null ? camera.transform.right : Vector3.right;
            Vector3 up = camera != null ? camera.transform.up : Vector3.forward;
            for (int i = 0; i < 32; i++)
            { float a = i * Mathf.PI / 16; line.SetPosition(i, point + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius); }
        }

        private static void ComicBurst(LineRenderer line,Vector3 center,float radius,Color color,Camera camera)
        {
            line.loop=true; line.positionCount=16; line.startColor=line.endColor=color;
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI/8,r=radius*(i%2==0 ? 1.25f : .64f);
                line.SetPosition(i,center+(camera.transform.right*Mathf.Cos(angle)+camera.transform.up*Mathf.Sin(angle))*r);
            }
        }

        private void AddPuff(Vector3 position, double time, bool death,string cardId)
        { puffs.Add(new Puff { Position = position, Time = time, Death = death,CardId=cardId }); }

        private void ClearTransient()
        {
            wolfVfx?.Clear();
            talulahAttackVfx?.Clear();
            commonVfx?.Clear();
            foreach (var v in shots)
            {
                v.Trail.enabled=v.Impact.enabled=v.Rays.enabled=v.Ink.enabled=false;
                if(v.Face!=null){v.Face.enabled=false;Destroy(v.Face.gameObject);}
                Destroy(v.Trail.gameObject);Destroy(v.Impact.gameObject);Destroy(v.Rays.gameObject);Destroy(v.Ink.gameObject);
            }
            foreach(var ring in seedViews)Destroy(ring.gameObject);seedViews.Clear();
            shots.Clear(); puffs.Clear(); spawned.Clear(); dead.Clear();
        }
        private void OnDestroy() { ClearTransient(); commonVfx?.Dispose();talulahAttackVfx?.Dispose(); if (material != null) Destroy(material); }
    }
}
