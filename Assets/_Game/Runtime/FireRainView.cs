using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Decorative map-wide rain. Damage is exclusively the model's per-defender CombatShot.</summary>
    public sealed class FireRainView : MonoBehaviour
    {
        private ManualBattleController owner;
        private Material material;
        private Mesh flameMesh;
        private readonly MeshRenderer[] flames=new MeshRenderer[24];
        private readonly LineRenderer[] tails=new LineRenderer[24], cores=new LineRenderer[24], rings=new LineRenderer[24];
        public void Initialize(ManualBattleController value) { owner=value; }
        private LineRenderer NewLine(float width)
        {
            var line=new GameObject("Fire rain graphic",typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(transform,false); line.sharedMaterial=material;
            line.widthMultiplier=width; line.useWorldSpace=true; line.sortingOrder=22000;
            return line;
        }
        private void Awake()
        {
            material=new Material(Shader.Find("Sprites/Default"));
            flameMesh=CreateFlameMesh();
            for(int i=0;i<24;i++)
            { tails[i]=NewLine(.13f); cores[i]=NewLine(.055f); rings[i]=NewLine(.035f); cores[i].sortingOrder++;
              tails[i].positionCount=cores[i].positionCount=2; rings[i].positionCount=24; rings[i].loop=true;
              tails[i].widthCurve=AnimationCurve.Linear(0,.08f,1,1);
              var head=new GameObject("Comic flame head",typeof(MeshFilter),typeof(MeshRenderer));
              head.transform.SetParent(transform,false); head.GetComponent<MeshFilter>().sharedMesh=flameMesh;
              flames[i]=head.GetComponent<MeshRenderer>(); flames[i].sharedMaterial=material; flames[i].sortingOrder=22002; }
        }
        private void LateUpdate()
        {
            var b=owner?.Battle; DeployedUnitState caster=null;
            if(b!=null && b.EngagementEnabled && b.CombatSettings.FireRain)
                foreach(var u in b.Units) if(b.IsSkillActionPlaying(u)) { caster=u; break; }
            Camera cam=Camera.main;
            for(int i=0;i<24;i++)
            {
                tails[i].enabled=cores[i].enabled=rings[i].enabled=flames[i].enabled=caster!=null && cam!=null;
                if(caster==null || cam==null) continue;
                float age=(float)(b.Clock-caster.FireRainStartedAt);
                float cycle=age-i*.071f; if(cycle<0) { tails[i].enabled=cores[i].enabled=rings[i].enabled=flames[i].enabled=false; continue; }
                float t=Mathf.Repeat(cycle,1.2f)/1.2f;
                // Stable staggered landing positions on the existing ground plane, within the playable view.
                float x=.12f+(i%6)*.15f, y=.20f+(i/6)*.16f;
                Ray ray=cam.ViewportPointToRay(new Vector3(x,y,0));
                var plane=new Plane(Vector3.up,b.Entry); if(!plane.Raycast(ray,out float distance)) continue;
                Vector3 ground=ray.GetPoint(distance), point=ground+cam.transform.up*(4.2f*(1-t))+cam.transform.right*(1-t)*.8f;
                bool falling=t<.82f; tails[i].enabled=cores[i].enabled=falling;
                flames[i].enabled=falling; flames[i].transform.position=point;
                flames[i].transform.rotation=cam.transform.rotation;
                flames[i].transform.localScale=Vector3.one*(.68f+.10f*Mathf.Sin(age*19+i));
                if(falling) foreach(var line in new[]{tails[i],cores[i]})
                { line.SetPosition(0,point+cam.transform.up*.75f); line.SetPosition(1,point); }
                tails[i].startColor=new Color(1,.15f,.02f,0); tails[i].endColor=new Color(1,.45f,.04f,.7f);
                cores[i].startColor=new Color(1,.6f,.15f,0); cores[i].endColor=new Color(1,.97f,.65f,.6f);
                float radius=falling ? .20f+t*.22f : .45f+(t-.82f)*2;
                Color color=falling ? new Color(1,.28f,.04f,.3f+t*.3f) : new Color(1,.65f,.1f,(1-t)/.18f);
                rings[i].startColor=rings[i].endColor=color; rings[i].sortingOrder=0;
                for(int j=0;j<24;j++)
                { float a=j*Mathf.PI/12; rings[i].SetPosition(j,ground+Vector3.up*.04f+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius); }
            }
        }
        private static Mesh CreateFlameMesh()
        {
            // Three nested flat-color flame silhouettes. Geometry, not a borrowed game texture.
            Vector2[] outline={new Vector2(0,-.12f),new Vector2(-.20f,.02f),new Vector2(-.16f,.24f),
                new Vector2(-.08f,.14f),new Vector2(-.02f,.60f),new Vector2(.07f,.40f),
                new Vector2(.10f,.22f),new Vector2(.17f,.32f),new Vector2(.22f,.06f),new Vector2(.13f,-.09f)};
            var vertices=new Vector3[33]; var colors=new Color[33]; var triangles=new int[90];
            Color[] palette={new Color(.25f,.035f,.02f),new Color(1,.25f,.015f),new Color(1,.91f,.34f)};
            for(int layer=0;layer<3;layer++)
            {
                int offset=layer*11; float scale=layer==0 ? 1.12f : layer==1 ? 1 : .57f;
                vertices[offset]=new Vector3(0,.13f,-layer*.001f); colors[offset]=palette[layer];
                for(int j=0;j<10;j++)
                {
                    vertices[offset+j+1]=new Vector3(outline[j].x*scale,outline[j].y*scale,-layer*.001f);
                    colors[offset+j+1]=palette[layer];
                    int index=layer*30+j*3; triangles[index]=offset; triangles[index+1]=offset+j+1; triangles[index+2]=offset+(j+1)%10+1;
                }
            }
            var mesh=new Mesh { name="Three-color comic flame",vertices=vertices,colors=colors,triangles=triangles };
            mesh.RecalculateBounds(); return mesh;
        }
        private void OnDestroy() { if(material!=null) Destroy(material); if(flameMesh!=null) Destroy(flameMesh); }
    }
}
