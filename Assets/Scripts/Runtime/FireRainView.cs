using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved fire-wave presentation. Reads the battle clock; never applies damage.</summary>
    public sealed class FireRainView : MonoBehaviour
    {
        private const int Lanes=6, WavesPerLane=8;
        private readonly SpriteRenderer[] streams=new SpriteRenderer[Lanes*WavesPerLane];
        private readonly SpriteRenderer[] tails=new SpriteRenderer[Lanes*WavesPerLane];
        private readonly SpriteRenderer[] tongues=new SpriteRenderer[30], sparks=new SpriteRenderer[100];
        private readonly MeshRenderer[] glows=new MeshRenderer[Lanes*WavesPerLane];
        private readonly Sprite[] emberFrames=new Sprite[25];
        private ManualBattleController owner;
        private Material material;
        private Mesh glowMesh;
        private Sprite spark;
        private FireRainScreenEffect screenEffect;
        public int VisibleStreams { get; private set; }
        public void Initialize(ManualBattleController value) { owner=value; }

        private void Awake()
        {
            material=new Material(Resources.Load<Shader>("FireRainAdditive"));
            glowMesh=MakeGlow();
            var texture=Resources.Load<Texture2D>("FireRainEmbers");
            for(int i=0;i<25;i++) emberFrames[i]=Sprite.Create(texture,new Rect(i%5*128,(4-i/5)*128,128,128),new Vector2(.5f,.5f),128);
            spark=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            for(int i=0;i<streams.Length;i++)
            {
                streams[i]=NewSprite("Horizontal fire stream",22500);
                tails[i]=NewSprite("Turbulent flame tail",22500);
                var glow=new GameObject("Fire reflected glow",typeof(MeshFilter),typeof(MeshRenderer));
                glow.transform.SetParent(transform,false); glow.GetComponent<MeshFilter>().sharedMesh=glowMesh;
                glows[i]=glow.GetComponent<MeshRenderer>(); glows[i].sharedMaterial=material;
                glows[i].sortingOrder=22499; glows[i].enabled=false;
            }
            for(int i=0;i<tongues.Length;i++) tongues[i]=NewSprite("Airborne fire tongue",22510);
            for(int i=0;i<sparks.Length;i++) { sparks[i]=NewSprite("Fire ember",22511); sparks[i].sprite=spark; }
        }

        private SpriteRenderer NewSprite(string label,int order)
        {
            var renderer=new GameObject(label,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform,false); renderer.sharedMaterial=material;
            renderer.sortingOrder=order; renderer.enabled=false; return renderer;
        }
        private static float Smooth(float value) { value=Mathf.Clamp01(value); return value*value*(3-2*value); }
        private static float Random01(int seed) => Mathf.Repeat(Mathf.Sin(seed*91.319f+17.281f)*43758.5453f,1);

        private void LateUpdate()
        {
            var b=owner?.Battle; var art=owner?.Definition.Card.ArticulatedArt; DeployedUnitState caster=null;
            if(b!=null && b.EngagementEnabled && b.CombatSettings.FireRain && art!=null)
                foreach(var u in b.Units) if(b.IsSkillActionPlaying(u)) { caster=u; break; }
            Camera cam=Camera.main;
            if(cam!=null && (screenEffect==null || screenEffect.gameObject!=cam.gameObject))
            {
                if(screenEffect!=null) Destroy(screenEffect);
                screenEffect=cam.gameObject.AddComponent<FireRainScreenEffect>();
            }
            VisibleStreams=0;
            if(caster==null || cam==null) { Hide(); return; }
            float age=(float)(b.Clock-caster.FireRainStartedAt), duration=b.CombatSettings.SkillInterval;
            float strength=Smooth(age/.30f)*Smooth((duration-age)/.45f);
            float warmth=Smooth(age/.25f)*Smooth((duration-age)/.6f);
            screenEffect.Present(age,strength,warmth);
            for(int i=0;i<streams.Length;i++)
            {
                int row=i/WavesPerLane, wave=i%WavesPerLane, id=row*17+wave+20;
                float elapsed=age-wave*.91f-row*.075f;
                // Spawn from off-screen, never wrap an existing flame across the map.
                float x=-220+elapsed*465;
                bool visible=strength>0 && elapsed>=0 && x<1580;
                streams[i].enabled=tails[i].enabled=glows[i].enabled=visible;
                if(!visible) continue;
                float y=165+row*77, size=(.78f+row*.075f)*(.9f+Random01(id)*.22f);
                float width=(235+Random01(id+9)*40)*size, height=155*size;
                streams[i].sprite=art.FireFrames[Mathf.FloorToInt(age*28+id*5)%art.FireFrames.Count];
                streams[i].color=new Color(1.25f,1.12f,1,strength);
                // Source bright head points down; +90 degrees makes it travel right.
                Place(streams[i],cam,x-width*.28f,y,width,height,90);
                streams[i].transform.position+=cam.transform.up*(Mathf.Sin(age*11+id)*3/720*ViewHeight(cam));
                tails[i].sprite=art.FireFrames[Mathf.FloorToInt(age*31.64f+id*11+9)%art.FireFrames.Count];
                tails[i].color=new Color(1.25f,.98f,.8f,strength*.56f);
                Place(tails[i],cam,x-width*.66f-width*.38f*.28f,y-Mathf.Sin(age*3+id)*8,width*.38f,height*.47f,90);
                glows[i].transform.position=Point(cam,x-30,y+16);
                glows[i].transform.rotation=cam.transform.rotation;
                float unit=ViewHeight(cam)/720;
                glows[i].transform.localScale=new Vector3(width*.73f*unit,height*.9f*unit,1)*strength;
                VisibleStreams++;
            }
            for(int i=0;i<tongues.Length;i++)
            {
                var s=tongues[i]; s.enabled=strength>0;
                float p=Mathf.Repeat(age*(.4f+Random01(i+90)*.2f)+Random01(i+30),1);
                float x=15+p*1400, y=150+Random01(i+40)*480-Mathf.Sin(p*Mathf.PI)*22;
                s.sprite=emberFrames[Mathf.FloorToInt(Mathf.Repeat(age*.72f+Random01(i),1)*25)];
                s.color=new Color(1,1,1,strength*.48f);
                float width=33+Random01(i+12)*24; Place(s,cam,x,y,width,width*1.1f,0);
            }
            for(int i=0;i<sparks.Length;i++)
            {
                var s=sparks[i]; s.enabled=strength>0;
                float p=Mathf.Repeat(age*(.21f+Random01(i+119)*.19f)+Random01(i+1),1);
                float x=Mathf.Repeat(Random01(i+15)+age*.15f,1)*1400-60,y=125+Random01(i+28)*510-p*55;
                float alpha=strength*Mathf.Sin(p*Mathf.PI)*(.3f+Random01(i+23)*.6f);
                s.color=new Color(1,.65f+Random01(i)*.25f,.27f,alpha);
                float size=.7f+Random01(i+80)*1.4f; Place(s,cam,x,y,size*3.5f,size*1.5f,15);
            }
        }
        // Camera-facing plane keeps the approved pixel proportions at any game resolution.
        private static float ViewHeight(Camera camera) => camera.orthographic ? camera.orthographicSize*2 : 10*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
        private static Vector3 Point(Camera camera,float x,float y) => camera.ViewportToWorldPoint(new Vector3(x/1280,1-y/720,5));
        private static void Place(SpriteRenderer s,Camera camera,float x,float y,float width,float height,float roll)
        {
            s.transform.position=Point(camera,x,y); s.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,roll);
            float unit=ViewHeight(camera)/720; Vector2 size=s.sprite.bounds.size;
            s.transform.localScale=roll==90 ? new Vector3(height*unit/size.x,width*unit/size.y,1) : new Vector3(width*unit/size.x,height*unit/size.y,1);
        }
        private void Hide()
        {
            foreach(var s in streams) if(s!=null) s.enabled=false;
            foreach(var s in tails) if(s!=null) s.enabled=false;
            foreach(var s in tongues) if(s!=null) s.enabled=false;
            foreach(var s in sparks) if(s!=null) s.enabled=false;
            foreach(var g in glows) if(g!=null) g.enabled=false;
            if(screenEffect!=null) screenEffect.Present(0,0,0);
            VisibleStreams=0;
        }
        private static Mesh MakeGlow()
        {
            var vertices=new Vector3[25]; var colors=new Color[25]; var indices=new int[72];
            colors[0]=new Color(1,.34f,.045f,.23f);
            for(int i=0;i<24;i++)
            {
                float angle=i*Mathf.PI/12; vertices[i+1]=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                colors[i+1]=new Color(.4f,.04f,0,0); indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=(i+1)%24+1;
            }
            var mesh=new Mesh{name="Soft fire illumination",vertices=vertices,colors=colors,triangles=indices};mesh.RecalculateBounds();return mesh;
        }
        private void OnDisable() => Hide();
        private void OnDestroy()
        {
            Hide(); if(screenEffect!=null) Destroy(screenEffect);
            if(material!=null) Destroy(material); if(glowMesh!=null) Destroy(glowMesh);
            foreach(var frame in emberFrames) if(frame!=null) Destroy(frame);
            if(spark!=null) Destroy(spark);
        }
    }
}
