using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Reference-style horizontal fire streams. Decoration never applies combat damage.</summary>
    public sealed class FireRainView : MonoBehaviour
    {
        private ManualBattleController owner;
        private Material material;
        private Mesh glowMesh;
        private Light fireLight;
        private readonly MeshRenderer[] glows=new MeshRenderer[48];
        private readonly SpriteRenderer[] streams=new SpriteRenderer[48],burns=new SpriteRenderer[18];
        private float envelope;
        public int VisibleStreams { get; private set; }
        public void Initialize(ManualBattleController value) { owner=value; }
        private void Awake()
        {
            material=new Material(Shader.Find("Sprites/Default"));
            glowMesh=MakeGlow();
            for(int i=0;i<streams.Length;i++)
            {
                streams[i]=NewSprite("Horizontal fire stream");
                var glow=new GameObject("Fire stream glow",typeof(MeshFilter),typeof(MeshRenderer));
                glow.transform.SetParent(transform,false); glow.GetComponent<MeshFilter>().sharedMesh=glowMesh;
                glows[i]=glow.GetComponent<MeshRenderer>(); glows[i].sharedMaterial=material; glows[i].sortingOrder=22499;
            }
            for(int i=0;i<burns.Length;i++) burns[i]=NewSprite("Ground fire residue");
            fireLight=new GameObject("Fire rain map light",typeof(Light)).GetComponent<Light>();
            fireLight.transform.SetParent(transform,false); fireLight.transform.position=new Vector3(0,5,0);
            fireLight.type=LightType.Point; fireLight.range=20; fireLight.color=new Color(1,.33f,.07f); fireLight.shadows=LightShadows.None; fireLight.enabled=false;
        }
        private SpriteRenderer NewSprite(string label)
        {
            var sprite=new GameObject(label,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sprite.transform.SetParent(transform,false); sprite.sharedMaterial=material; sprite.enabled=false;
            return sprite;
        }
        private void LateUpdate()
        {
            var b=owner?.Battle; var art=owner?.Definition.Card.ArticulatedArt; DeployedUnitState caster=null;
            if(b!=null && b.EngagementEnabled && b.CombatSettings.FireRain && art!=null)
                foreach(var u in b.Units) if(b.IsSkillActionPlaying(u)) { caster=u; break; }
            Camera cam=Camera.main; VisibleStreams=0; envelope=0;
            float age=caster!=null ? (float)(b.Clock-caster.FireRainStartedAt) : 0;
            if(caster!=null) envelope=Mathf.Min(Mathf.Clamp01(age/.45f),Mathf.Clamp01((b.CombatSettings.SkillInterval-age)/.35f));
            fireLight.enabled=envelope>0; fireLight.intensity=envelope*(3.2f+.2f*Mathf.Sin(age*8));
            for(int i=0;i<streams.Length;i++)
            {
                var s=streams[i]; s.enabled=glows[i].enabled=caster!=null && cam!=null && envelope>0;
                if(!s.enabled) continue;
                int row=i/8,col=i%8;
                float t=Mathf.Repeat((age-.35f)/1.65f+col/8f+row*.027f,1);
                float x=1.10f-t*1.20f,y=.29f+row*.105f+Mathf.Sin(age*3+col)*.006f;
                if(!Ground(cam,b.Entry,x,y,out Vector3 ground)) { s.enabled=false; continue; }
                s.sprite=art.FireFrames[(int)(age*24+i*3)%art.FireFrames.Count];
                s.transform.position=ground+cam.transform.up*(.32f+.10f*Mathf.Sin(i));
                s.transform.rotation=cam.transform.rotation*Quaternion.Euler(0,0,-90);
                float scale=1.6f+(i%3)*.20f;
                s.transform.localScale=Vector3.one*scale;
                s.sortingOrder=22500+row; s.color=new Color(1.45f,1.12f,.90f,envelope); VisibleStreams++;
                glows[i].transform.position=s.transform.position-cam.transform.right*(.40f*scale);
                glows[i].transform.rotation=cam.transform.rotation;
                glows[i].transform.localScale=Vector3.one*(.32f*scale*envelope);
            }
            for(int i=0;i<burns.Length;i++)
            {
                var s=burns[i]; s.enabled=caster!=null && cam!=null && envelope>0;
                if(!s.enabled) continue;
                float x=.18f+(i%3)*.30f,y=.29f+(i/3)*.105f;
                if(!Ground(cam,b.Entry,x,y,out Vector3 ground)) { s.enabled=false; continue; }
                s.sprite=art.FireFrames[(int)(age*22+i*5)%art.FireFrames.Count];
                s.transform.position=ground; s.transform.rotation=cam.transform.rotation;
                s.transform.localScale=Vector3.one*(.85f+.14f*Mathf.Sin(age*5+i));
                s.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,ground)-2;
                s.color=new Color(1.2f,.92f,.62f,envelope*.85f);
            }
        }
        private static bool Ground(Camera camera,Vector3 entry,float x,float y,out Vector3 point)
        {
            Ray ray=camera.ViewportPointToRay(new Vector3(x,y,0)); var plane=new Plane(Vector3.up,entry);
            if(plane.Raycast(ray,out float distance)) { point=ray.GetPoint(distance); return true; }
            point=Vector3.zero; return false;
        }
        private void OnGUI()
        {
            if(envelope<=0) return;
            int depth=GUI.depth; GUI.depth=100; Color old=GUI.color; GUI.color=new Color(1,.25f,.035f,.065f*envelope);
            GUI.DrawTexture(new Rect(0,Screen.height*66f/720,Screen.width,Screen.height*486f/720),Texture2D.whiteTexture);
            GUI.color=old; GUI.depth=depth;
        }
        private static Mesh MakeGlow()
        {
            var vertices=new Vector3[25]; var colors=new Color[25]; var indices=new int[72];
            colors[0]=new Color(1,.58f,.12f,.6f);
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12; vertices[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                colors[i+1]=new Color(1,.12f,0,0); indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=(i+1)%24+1;
            }
            var mesh=new Mesh{name="Soft fire glow",vertices=vertices,colors=colors,triangles=indices};mesh.RecalculateBounds();return mesh;
        }
        private void OnDestroy() { if(material!=null) Destroy(material); if(glowMesh!=null) Destroy(glowMesh); }
    }
}
