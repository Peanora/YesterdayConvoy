using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Clock-driven Kroos pose, projectiles and impacts. Never applies damage or moves a logical entity.</summary>
    public sealed class EngagementView : MonoBehaviour
    {
        private sealed class ShotView { public LineRenderer Trail, Impact, Rays, Ink; public SpriteRenderer Face; }
        private sealed class Puff { public LineRenderer Ring; public Vector3 Position; public double Time; public bool Death; }
        private readonly List<ShotView> shots = new List<ShotView>();
        private readonly List<Puff> puffs = new List<Puff>();
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
        public SpriteRenderer DefenderVisual => kroos;

        public void Initialize(ManualBattleController controller, EngagementDefinition data, AudioClip uiSound, AudioClip hitSound)
        {
            owner = controller; definition = data; click = uiSound; impact = hitSound;
            ClearTransient(); runId=-1;
            foreach(var v in extraVisuals) if(v!=null) Destroy(v.gameObject);
            extraVisuals.Clear();
            if(material==null) material = new Material(Shader.Find("Sprites/Default"));
            if(kroos==null) kroos = new GameObject("Kroos combat sprite", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            kroos.transform.SetParent(transform, false); kroos.sprite = data.KroosSprite; kroos.sharedMaterial = material;
            if(range==null) range = CreateLine("Kroos range", 0.018f); range.loop = true; range.positionCount = 64; range.sortingOrder = 0;
            for(int i=0;i<data.ExtraDefenders.Count;i++)
            { var v=new GameObject("Kroos flank "+i,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
              v.transform.SetParent(transform,false); v.sprite=data.KroosSprite; v.sharedMaterial=material; extraVisuals.Add(v); }
            if(audioSource==null) audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
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
            float death = d.Alive ? 0 : Mathf.Clamp01((float)(time - d.DeathTime) / 0.45f);
            kroos.enabled = death < 1;
            kroos.transform.position = d.Position;
            if (d.Alive)
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
                Color color=time<other.LastHitTime+.14 ? new Color(1,.5f,.45f,fade) : new Color(1,1,1,fade); v.color=color;
            }
            var modelShots = b.CombatShots;
            for (int i = 0; i < modelShots.Count; i++)
            {
                if (shots.Count <= i)
                {
                    var view=new ShotView { Trail = CreateLine("Shot trail", 0.06f), Impact = CreateLine("Impact ring", 0.04f), Rays = CreateLine("Impact rays", 0.025f), Ink=CreateLine("Comic impact ink",.12f) };
                    var faces=owner.Definition.Card.ComicImpactFaces;
                    if(faces?.Count>0)
                    {
                        view.Face=new GameObject("Comic expression",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                        view.Face.transform.SetParent(transform,false); view.Face.sharedMaterial=material;
                        view.Face.sprite=faces[i%faces.Count]; view.Face.enabled=false;
                    }
                    shots.Add(view);
                    Play(impact, modelShots[i].FromDefender ? 0.23f : modelShots[i].Empowered ? 0.75f : 0.35f);
                }
                PresentShot(shots[i], modelShots[i], b, cam);
            }
            foreach (var unit in b.Units)
            {
                if (spawned.Add(unit.EntityId)) { AddPuff(unit.Motion.Position, time, false); Play(click, 0.4f); }
                if (unit.Resolution == UnitResolution.Dead && dead.Add(unit.EntityId)) AddPuff(unit.Motion.Position, time, true);
            }
            foreach (var puff in puffs)
            {
                float age = (float)(time - puff.Time), duration = puff.Death ? 0.5f : 0.35f;
                puff.Ring.enabled = age < duration;
                if (!puff.Ring.enabled) continue;
                Color color = puff.Death ? new Color(0.65f, 0.16f, 0.1f, 1 - age / duration) : new Color(0.25f, 0.9f, 0.8f, 1 - age / duration);
                Ring(puff.Ring, puff.Position + Vector3.up * 0.02f, 0.12f + age * 0.8f, color, null);
            }
            if ((int)b.Phase != lastPhase)
            { if (b.Phase == BattlePhase.Won || b.Phase == BattlePhase.Lost) Play(click, 0.7f); lastPhase = (int)b.Phase; }
            audioSource.mute = owner.Muted;
            audioSource.pitch = owner.SimulationSpeed;
            if (b.IsPaused) audioSource.Pause(); else audioSource.UnPause();
        }

        private void PresentShot(ShotView view, CombatShot shot, ManualBattle b, Camera cam)
        {
            view.Ink.enabled=false; if(view.Face!=null) view.Face.enabled=false;
            if(shot.FireRain) { PresentFireRain(view,shot,b,cam); return; }
            double time = b.Clock;
            float t = Mathf.Clamp01((float)((time - shot.StartedAt) / (shot.ImpactAt - shot.StartedAt)));
            var target = b.FindUnit(shot.TargetId);
            Vector3 destination = shot.FromDefender && target != null ? target.Motion.Position : shot.Destination;
            Vector3 origin = shot.Origin + cam.transform.up * 0.75f;
            Vector3 end = destination + cam.transform.up * 0.8f;
            float face = cam.transform.InverseTransformDirection(end - origin).x < 0 ? -1 : 1;
            origin += cam.transform.right * (face * 0.45f);
            Color color = shot.FromDefender ? new Color(1, 0.83f, 0.36f) : shot.Empowered ? new Color(1, 0.36f, 0.12f) : new Color(0.95f, 0.68f, 0.3f);
            bool flying = !shot.Resolved && time < shot.ImpactAt;
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
            if(!shot.FromDefender && owner.Definition.Card.HasFrameAnimation)
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

        private void PresentFireRain(ShotView v,CombatShot shot,ManualBattle b,Camera cam)
        {
            float t=Mathf.Clamp01((float)((b.Clock-shot.StartedAt)/(shot.ImpactAt-shot.StartedAt)));
            Vector3 end=shot.Destination+cam.transform.up*.55f;
            bool falling=b.Clock<shot.ImpactAt;
            v.Trail.enabled=falling; v.Ink.enabled=falling;
            v.Trail.positionCount=v.Ink.positionCount=2;
            Vector3 point=end+cam.transform.up*(4*(1-t))+cam.transform.right*(1-t);
            foreach(var line in new[]{v.Trail,v.Ink})
            { line.SetPosition(0,point+cam.transform.up*.9f); line.SetPosition(1,point); line.sortingOrder=23000; }
            v.Ink.widthMultiplier=.22f; v.Ink.startColor=v.Ink.endColor=new Color(.15f,.035f,.02f);
            v.Trail.sortingOrder=23001; v.Trail.widthMultiplier=.12f;
            v.Trail.startColor=new Color(1,.22f,.03f); v.Trail.endColor=new Color(1,.95f,.45f);
            float age=(float)(b.Clock-shot.ImpactAt);
            bool hit=age>=0 && age<.6f;
            v.Impact.enabled=falling || hit; v.Rays.enabled=hit;
            if(falling) Ring(v.Impact,shot.Destination+Vector3.up*.04f,.45f,new Color(1,.28f,.05f,.65f),null);
            else if(hit)
            {
                ComicBurst(v.Impact,end,.35f+age*1.4f,new Color(1,.4f,.06f,1-age/.6f),cam);
                ComicBurst(v.Rays,end,.22f+age*.8f,new Color(1,.9f,.4f,1-age/.6f),cam);
            }
            v.Impact.sortingOrder=23002; v.Rays.sortingOrder=23003;
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

        private void AddPuff(Vector3 position, double time, bool death)
        { puffs.Add(new Puff { Ring = CreateLine(death ? "Death dissolve" : "Deployment pulse", 0.035f), Position = position, Time = time, Death = death }); }

        private void ClearTransient()
        {
            foreach (var v in shots) { Destroy(v.Trail.gameObject); Destroy(v.Impact.gameObject); Destroy(v.Rays.gameObject); Destroy(v.Ink.gameObject); if(v.Face!=null) Destroy(v.Face.gameObject); }
            foreach (var p in puffs) Destroy(p.Ring.gameObject);
            shots.Clear(); puffs.Clear(); spawned.Clear(); dead.Clear();
        }
        private void OnDestroy() { ClearTransient(); if (material != null) Destroy(material); }
    }
}
