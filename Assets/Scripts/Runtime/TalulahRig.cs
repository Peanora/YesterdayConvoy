using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Talulah-only visual articulation. Called by DemoSpriteVisual, never moves the entity root.</summary>
    public sealed class TalulahRig : MonoBehaviour
    {
        private readonly SpriteRenderer[] parts=new SpriteRenderer[13];
        private TalulahArtDefinition art;
        private SpriteRenderer castingFlame;
        private double previousClock;
        private float walkBlend;
        private bool hasClock;
        public Vector2 LeftFoot { get; private set; }
        public Vector2 RightFoot { get; private set; }
        public float WalkBlend => walkBlend;
        public Quaternion WeaponRotation => parts[8].transform.localRotation;

        public void Bind(TalulahArtDefinition assets,Material material)
        {
            art=assets;
            if(!art.Complete) throw new ArgumentException("Incomplete Talulah articulated art.");
            for(int i=0;i<13;i++)
            {
                parts[i]=new GameObject("Talulah part "+i,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                parts[i].transform.SetParent(transform,false); parts[i].sprite=art.Parts[i];
                parts[i].sharedMaterial=material;
                // The generated torso already includes a short skirt; keep the separate skirt as an unused source part to avoid a doubled belt.
                if(i==3) parts[i].enabled=false;
            }
            castingFlame=new GameObject("Talulah casting hand fire",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            castingFlame.transform.SetParent(transform,false); castingFlame.sharedMaterial=material; castingFlame.enabled=false;
        }

        public void Present(ManualBattle battle,DeployedUnitState unit,Color tint,int order)
        {
            float dt=hasClock ? (float)Math.Max(0,battle.Clock-previousClock) : 0; previousClock=battle.Clock; hasClock=true;
            if(battle.IsPaused) dt=0;
            walkBlend=Mathf.MoveTowards(walkBlend,unit.IsMoving ? 1 : 0,dt/.16f);
            float phase=(float)(unit.Motion.DistanceTravelled/.72);
            float angle=phase*Mathf.PI*2;
            bool casting=battle.IsSkillActionPlaying(unit);
            float age=(float)(battle.Clock-unit.LastAttackTime);
            float wind=age>=0 && age<.40f ? Smooth(age/.40f) : age>=.40f && age<.66f ? 1-Smooth((age-.40f)/.26f) : 0;
            float slash=age>=.40f && age<.66f ? Smooth((age-.40f)/.26f) : age>=.66f && age<1.08f ? 1 : 0;
            float recover=age>=.66f ? 1-Smooth((age-.66f)/.42f) : 1;
            if(casting) { wind=slash=0; recover=0; }
            float lean=(-wind*4+slash*8)*recover;
            float bob=-.012f*Mathf.Cos(angle*2)*walkBlend;
            float sway=.008f*Mathf.Sin(angle)*walkBlend;
            Vector2 hip=new Vector2(sway,.625f-.033f*walkBlend+bob);
            Vector2 l=FootCycle(phase,.08f),r=FootCycle(phase+.5f,-.08f);
            LeftFoot=Vector2.Lerp(new Vector2(.09f,.025f),l,walkBlend);
            RightFoot=Vector2.Lerp(new Vector2(-.09f,.025f),r,walkBlend);
            Leg(4,6,hip+new Vector2(.075f,0),LeftFoot,.27f,.35f);
            Leg(5,7,hip+new Vector2(-.075f,0),RightFoot,.27f,.35f);
            Pose(2,new Vector2(-.025f+sway,.99f+bob),-2+Mathf.Sin(angle-.6f)*2.3f*walkBlend-lean*.4f);
            Pose(3,new Vector2(sway,.65f+bob),-lean*.22f);
            Pose(1,new Vector2(sway,.55f+bob),-lean);
            Pose(0,new Vector2(sway+lean*.0015f,.98f+bob),-lean*.28f+Mathf.Sin(angle-.25f)*.65f*walkBlend);
            float castAge=(float)(battle.Clock-unit.FireRainStartedAt);
            float cast=casting ? Smooth(castAge/.65f)*Smooth((battle.CombatSettings.SkillInterval-castAge)/.35f) : 0;
            float leftUpper=Mathf.Sin(angle)*12*walkBlend+cast*72;
            float leftFore=-10+cast*35;
            float rightUpper=-6-Mathf.Sin(angle)*10*walkBlend;
            float rightFore=8;
            float sword=-Mathf.Sin(angle)*3*walkBlend;
            if(age>=0 && age<1.08f && !casting)
            {
                rightUpper+=(-78*wind+72*slash)*recover;
                rightFore+=(-35*wind+6*slash)*recover;
                sword=(-100*wind+105*slash)*recover;
                leftUpper-=20*(wind+slash)*recover;
            }
            rightUpper-=cast*125; rightFore-=cast*12; sword-=cast*132;
            Vector2 palm=Arm(9,11,new Vector2(.19f+sway,.94f+bob),leftUpper,leftFore);
            Vector2 hand=Arm(10,12,new Vector2(-.19f+sway,.94f+bob),rightUpper,rightFore);
            Pose(8,hand,sword);
            int[] layers={24,18,0,20,10,4,11,5,22,16,21,17,23};
            for(int i=0;i<13;i++) { parts[i].color=tint; parts[i].sortingOrder=order+layers[i]; }
            castingFlame.enabled=cast>0;
            if(castingFlame.enabled)
            {
                castingFlame.sprite=art.FireFrames[(int)(castAge*24)%art.FireFrames.Count];
                castingFlame.transform.localPosition=new Vector3(palm.x,palm.y+.18f,0);
                castingFlame.transform.localRotation=Quaternion.identity; castingFlame.transform.localScale=Vector3.one*.4f;
                castingFlame.color=new Color(1.3f,1,.7f,cast); castingFlame.sortingOrder=order+26;
            }
        }

        // Stance is linear backwards motion: the foot stays planted while the logical body advances.
        // Swing has a smooth horizontal return and a curved lift; the two legs are half a cycle apart.
        public static Vector2 FootCycle(float phase,float lane)
        {
            float t=Mathf.Repeat(phase,1),x,y;
            if(t<.62f) { x=Mathf.Lerp(.2232f,-.2232f,t/.62f); y=.025f; }
            else { float s=(t-.62f)/.38f; x=Mathf.Lerp(-.2232f,.2232f,Smooth(s)); y=.025f+.125f*Mathf.Sin(s*Mathf.PI); }
            return new Vector2(lane+x,y);
        }
        private void Leg(int upper,int lower,Vector2 hip,Vector2 foot,float a,float b)
        {
            Vector2 delta=foot-hip; float distance=Mathf.Clamp(delta.magnitude,.02f,a+b-.001f);
            float direction=Mathf.Atan2(delta.y,delta.x);
            float bend=Mathf.Acos(Mathf.Clamp((a*a+distance*distance-b*b)/(2*a*distance),-1,1));
            Vector2 knee=hip+new Vector2(Mathf.Cos(direction+bend),Mathf.Sin(direction+bend))*a;
            Pose(upper,hip,(direction+bend)*Mathf.Rad2Deg+90);
            Vector2 shin=foot-knee; Pose(lower,knee,Mathf.Atan2(shin.y,shin.x)*Mathf.Rad2Deg+90);
        }
        private Vector2 Arm(int upper,int fore,Vector2 shoulder,float angle,float relative)
        {
            Pose(upper,shoulder,angle);
            Vector2 elbow=shoulder+Down(angle)*.16f; Pose(fore,elbow,angle+relative);
            return elbow+Down(angle+relative)*.19f;
        }
        private static Vector2 Down(float angle) => new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),-Mathf.Cos(angle*Mathf.Deg2Rad));
        private static float Smooth(float t) { t=Mathf.Clamp01(t); return t*t*(3-2*t); }
        private void Pose(int index,Vector2 position,float degrees)
        {
            var t=parts[index].transform; t.localPosition=new Vector3(position.x,position.y,0);
            t.localRotation=Quaternion.Euler(0,0,degrees);
        }
    }
}
