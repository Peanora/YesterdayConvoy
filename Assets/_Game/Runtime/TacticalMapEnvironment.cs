using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    /// <summary>World-space surfaces, terraces and props. Decorative geometry never writes battle state.</summary>
    public sealed class TacticalMapEnvironment : MonoBehaviour
    {
        readonly List<Material> owned=new List<Material>();
        Material ground,walk,side,trim,dark,water,accent,white,bark,leaves,red,blue;
        LineRenderer routePreview;
        Mesh rockMesh;
        bool forest;
        public static Vector3 Cell(float x,float y,float height=0) => new Vector3(x-8.5f,height,5-y);
        public void Build(DefenderTrialDefinition data)
        {
            forest=data.autumnRuins;
            ground=Mat(forest ? new Color(.63f,.64f,.44f) : new Color(.68f,.73f,.74f),data.floorTexture);
            walk=Mat(forest ? new Color(.76f,.73f,.51f) : new Color(.53f,.61f,.64f),data.floorTexture);
            side=Mat(forest ? new Color(.23f,.29f,.19f) : new Color(.23f,.31f,.34f));
            trim=Mat(forest ? new Color(.69f,.65f,.43f) : new Color(.77f,.80f,.77f));
            dark=Mat(new Color(.09f,.15f,.16f)); white=Mat(new Color(.75f,.79f,.75f));
            accent=Mat(forest ? new Color(.67f,.58f,.22f) : new Color(.76f,.57f,.21f));
            water=new Material(data.surfaces[4]);owned.Add(water);
            bark=Mat(new Color(.18f,.21f,.14f));
            red=Glow(new Color(1,.12f,.15f)); blue=Glow(new Color(.12f,.75f,1));
            if(data.foliageTexture!=null)
            {
                // Sprite shader keeps alpha blending in Player builds without a stripped runtime-only Standard variant.
                leaves=Glow(new Color(.72f,.70f,.57f));leaves.mainTexture=data.foliageTexture;
            }
            var path=new HashSet<Vector2Int>();
            foreach(var lane in data.lanes)
                for(int i=1;i<lane.points.Length;i++)
                {
                    Vector3 a=lane.points[i-1],b=lane.points[i]; int steps=Mathf.RoundToInt(Vector3.Distance(a,b));
                    for(int s=0;s<=steps;s++)
                    { var p=Vector3.Lerp(a,b,steps==0?0:s/(float)steps); path.Add(new Vector2Int(Mathf.RoundToInt(p.x+8.5f),Mathf.RoundToInt(5-p.z))); }
                }
            var raised=new HashSet<Vector2Int>(data.raised);
            Box("Water basin",new Vector3(0,-.65f,0),new Vector3(60,.12f,40),water);
            for(int y=0;y<11;y++) for(int x=0;x<18;x++)
            {
                var key=new Vector2Int(x,y); bool route=path.Contains(key),high=raised.Contains(key);
                // Service trenches / stream cuts occupy only non-route, non-platform cells.
                bool gap=!route && !high && (forest ? (x==0 || x==17 || (y==10 && x<5)) : ((x==0||x==17) && y>3 || x==12 && y>=8));
                if(gap) { if((x+y)%2==0) Box("Submerged footing",Cell(x,y,-.43f),new Vector3(.6f,.2f,.6f),side); continue; }
                float h=high?.43f:0;
                Box("Foundation "+x+","+y,Cell(x,y,(h-.3f)/2-.04f),new Vector3(.98f,.3f+h,.98f),side);
                Box(route?"Fixed route surface":"Terrace surface",Cell(x,y,h-.012f),new Vector3(.95f,.024f,.95f),route?walk:ground);
                if(high)
                {
                    Box("Raised lip",Cell(x,y+.46f,h),new Vector3(.99f,.065f,.08f),trim);
                    Box("Platform inset",Cell(x,y,h+.018f),new Vector3(.77f,.025f,.77f),ground);
                    if(forest)
                    {
                        Box("Carved stone band",Cell(x,y,h+.035f),new Vector3(.07f,.015f,.68f),trim);
                        Box("Carved stone band",Cell(x,y,h+.035f),new Vector3(.68f,.015f,.07f),trim);
                    }
                    else Box("Corner hazard paint",Cell(x+.32f,y+.35f,h+.025f),new Vector3(.16f,.01f,.035f),accent);
                }
                else if(!forest && (x+y)%5==0)
                {
                    for(int n=0;n<5;n++) Box("Drain slots",Cell(x-.24f+n*.12f,y+.36f,.015f),new Vector3(.045f,.012f,.19f),dark);
                }
                if(forest && !route && (x*7+y)%7==0)
                    Rock(Cell(x+.24f,y+.28f,.09f),new Vector3(.35f,.16f,.25f));
                if(!route && !high && x>0 && x<17 && y>0 && y<10 && (x*7+y*3)%5==0 && ClearOfActors(Cell(x,y),data))
                {
                    Box(forest?"Broken moss wall":"Service bulkhead",Cell(x,y,.19f),new Vector3(.82f,.38f,.8f),side);
                    Box(forest?"Weathered wall cap":"White maintenance cap",Cell(x,y,.4f),new Vector3(.88f,.06f,.87f),forest?ground:white);
                    if(forest) Rock(Cell(x+.22f,y-.22f,.48f),new Vector3(.35f,.16f,.32f));
                    else Box("Bulkhead amber marking",Cell(x,y+.42f,.3f),new Vector3(.38f,.035f,.02f),accent);
                }
            }
            if(forest) ForestProps(); else CityProps();
            var exits=new HashSet<string>();
            foreach(var lane in data.lanes)
            {
                Gate(lane.id,lane.points[0],red);
                if(exits.Add(lane.exitId)) Gate(lane.exitId,lane.points[lane.points.Length-1],blue);
            }
        }
        void CityProps()
        {
            for(int i=0;i<6;i++)
            {
                float x=-11+i*4.2f;
                Box("Rear building",new Vector3(x,1.5f,8.5f),new Vector3(3.4f,3.5f,3),ground);
                Box("Flat white roof",new Vector3(x,3.28f,8.5f),new Vector3(3.6f,.18f,3.2f),white);
                Box("Window recess",new Vector3(x,1.5f,6.98f),new Vector3(2.8f,1.3f,.035f),dark);
                for(int j=0;j<6;j++) Box("Facade mullion",new Vector3(x-1.15f+j*.46f,1.5f,6.94f),new Vector3(.04f,1.3f,.04f),trim);
            }
            for(int s=-1;s<=1;s+=2)
            {
                Box("Service embankment",new Vector3(s*10.7f,-.1f,0),new Vector3(1.8f,.8f,15),side);
                for(int i=0;i<5;i++)
                {
                    float z=5.2f-i*2.4f;
                    Cylinder("Pump tank",new Vector3(s*10.6f,.6f,z),new Vector3(.95f,.8f,.95f),white);
                    Cylinder("Pump collar",new Vector3(s*10.6f,1.35f,z),new Vector3(1.06f,.07f,1.06f),side);
                    Pipe(new Vector3(s*10.6f,.25f,z),new Vector3(s*9.3f,.25f,z),.13f,trim);
                }
                Pipe(new Vector3(s*10.6f,1.2f,-6),new Vector3(s*10.6f,1.2f,6),.09f,trim);
                for(int i=0;i<12;i++) Box("Service railing",new Vector3(s*9.6f,.45f,-6+i),new Vector3(.06f,.9f,.06f),trim);
            }
            // Equipment sits on authored platforms, clear of every fixed route.
            PumpCabinet(-2,4,1.2f); PumpCabinet(19,8,1.2f);
            for(int i=0;i<8;i++)
            {
                Box("Front fascia",new Vector3(-7.8f+i*2.2f,-.25f,-6.2f),new Vector3(2.1f,.35f,.48f),ground);
                Box("Front fascia vent",new Vector3(-7.8f+i*2.2f,-.23f,-6.46f),new Vector3(1.5f,.16f,.02f),dark);
            }
        }
        void PumpCabinet(float x,float y,float width)
        {
            var p=Cell(x,y,.43f);
            Box("Pump housing",p+Vector3.up*.35f,new Vector3(width,.7f,.68f),side);
            Box("Equipment roof",p+Vector3.up*.73f,new Vector3(width+.1f,.09f,.79f),white);
            for(int n=0;n<7;n++) Box("Cooling fin",p+new Vector3(-width*.4f+n*width*.13f,.33f,-.35f),new Vector3(.045f,.42f,.035f),trim);
        }
        void ForestProps()
        {
            for(int s=-1;s<=1;s+=2)
                for(int i=0;i<6;i++)
                {
                    float z=6.8f-i*2.6f; var p=new Vector3(s*(10.7f+(i%2)*.7f),0,z);
                    Rock(p+Vector3.down*.25f,new Vector3(2.5f,.8f,2.1f));
                    Pipe(p,p+new Vector3(-s*.4f,2.3f,.4f),.18f,bark);
                    Pipe(p+Vector3.up*1.2f,p+new Vector3(-s*1.1f,2.5f,.1f),.09f,bark);
                    Canopy(p+new Vector3(0,2.2f,0),3.2f,i*31);
                }
            for(int i=0;i<7;i++)
            {
                var p=new Vector3(-9+i*3.1f,0,7.4f);
                Rock(p,new Vector3(2.2f,.7f,1.8f)); Canopy(p+Vector3.up*2.2f,3.6f,i*47);
                Box("Broken rear wall",p+new Vector3(0,.9f,0),new Vector3(2,1.6f,.65f),side);
                Box("Coped stone cap",p+new Vector3(0,1.75f,0),new Vector3(2.15f,.17f,.82f),trim);
            }
            foreach(var p in new[]{Cell(6,4,.43f),Cell(8,4,.43f),Cell(14,4,.43f)})
            {
                Box("Ruined plinth",p+Vector3.up*.1f,new Vector3(.78f,.2f,.78f),trim);
                Box("Broken pillar",p+Vector3.up*.46f,new Vector3(.48f,.7f,.48f),ground);
                Box("Pillar capital",p+Vector3.up*.84f,new Vector3(.65f,.13f,.65f),trim);
            }
            // Low front silhouettes frame the map without overlapping actor feet.
            for(int i=0;i<10;i++) Rock(new Vector3(-10+i*2.2f,-.25f,-6.5f),new Vector3(1.7f,.5f,.9f));
        }
        void Canopy(Vector3 p,float size,float rotation)
        {
            if(leaves==null) return;
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name="Golden canopy"; go.transform.SetParent(transform,false);
            go.transform.localPosition=p; go.transform.rotation=Camera.main.transform.rotation*Quaternion.Euler(0,0,rotation);
            go.transform.localScale=Vector3.one*size; go.GetComponent<Renderer>().sharedMaterial=leaves; Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<Renderer>().receiveShadows=false;
        }
        void Rock(Vector3 p,Vector3 scale)
        {
            if(rockMesh==null)
            {
                float a=(1+Mathf.Sqrt(5))/2;
                var v=new[]{new Vector3(-1,a,0),new Vector3(1,a,0),new Vector3(-1,-a,0),new Vector3(1,-a,0),new Vector3(0,-1,a),new Vector3(0,1,a),new Vector3(0,-1,-a),new Vector3(0,1,-a),new Vector3(a,0,-1),new Vector3(a,0,1),new Vector3(-a,0,-1),new Vector3(-a,0,1)};
                int[] f={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
                var points=new Vector3[f.Length];var indices=new int[f.Length];
                for(int i=0;i<f.Length;i++){points[i]=v[f[i]].normalized*(.85f+f[i]%3*.075f);indices[i]=i;}
                rockMesh=new Mesh{name="Faceted weathered stone"};rockMesh.vertices=points;rockMesh.triangles=indices;rockMesh.RecalculateNormals();
            }
            var go=new GameObject("Weathered rock",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=rockMesh;go.GetComponent<MeshRenderer>().sharedMaterial=side;
            go.transform.localPosition=p;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(0,p.x*41,12);
        }
        static bool ClearOfActors(Vector3 p,DefenderTrialDefinition data)
        {
            foreach(var d in data.defenders){var v=p-d.position;v.y=0;if(v.sqrMagnitude<3)return false;}
            foreach(var l in data.lanes)foreach(var gate in new[]{l.points[0],l.points[l.points.Length-1]}){var v=p-gate;v.y=0;if(v.sqrMagnitude<2)return false;}
            return true;
        }
        public void ShowLane(DeploymentLane lane)
        {
            if(lane==null){if(routePreview!=null)routePreview.enabled=false;return;}
            if(routePreview==null)
            {
                routePreview=new GameObject("Hovered red gate route").AddComponent<LineRenderer>();routePreview.transform.SetParent(transform,false);
                routePreview.sharedMaterial=blue;routePreview.widthMultiplier=.035f;routePreview.startColor=routePreview.endColor=new Color(1,1,1,.55f);
                routePreview.shadowCastingMode=ShadowCastingMode.Off;routePreview.receiveShadows=false;
            }
            routePreview.enabled=true;routePreview.positionCount=lane.Points.Count;
            for(int i=0;i<lane.Points.Count;i++)routePreview.SetPosition(i,lane.Points[i]+Vector3.up*.035f);
        }
        void Gate(string id,Vector3 p,Material m)
        {
            var holder=new GameObject("Gate "+id).transform; holder.SetParent(transform,false);
            Vector3[] b={new Vector3(-.47f,.03f,-.47f),new Vector3(.47f,.03f,-.47f),new Vector3(.47f,.03f,.47f),new Vector3(-.47f,.03f,.47f)};
            for(int i=0;i<4;i++) { Line(holder,p+b[i],p+b[(i+1)%4],m); Line(holder,p+b[i]+Vector3.up*.95f,p+b[(i+1)%4]+Vector3.up*.95f,m); Line(holder,p+b[i],p+b[i]+Vector3.up*.95f,m); }
            Vector3 c=p+new Vector3(0,.5f,0);
            var cam=Camera.main.transform; Vector3 u=cam.up*.22f,r=cam.right*.24f;
            Line(holder,c+u,c-u-r,m); Line(holder,c-u-r,c-u+r,m); Line(holder,c-u+r,c+u,m);
        }
        void Line(Transform parent,Vector3 a,Vector3 b,Material m)
        { var line=new GameObject("Gate edge").AddComponent<LineRenderer>(); line.transform.SetParent(parent,false); line.sharedMaterial=m; line.positionCount=2; line.SetPositions(new[]{a,b}); line.widthMultiplier=.023f; line.shadowCastingMode=ShadowCastingMode.Off; line.receiveShadows=false; }
        Material Mat(Color color,Texture2D texture=null)
        { var m=new Material(Shader.Find("Standard")); m.color=color; m.mainTexture=texture; m.SetFloat("_Glossiness",.12f); owned.Add(m); return m; }
        Material Glow(Color color) { var m=new Material(Shader.Find("Sprites/Default")); m.color=color; owned.Add(m); return m; }
        void Box(string name,Vector3 p,Vector3 scale,Material m) => Primitive(name,PrimitiveType.Cube,p,scale,m);
        void Cylinder(string name,Vector3 p,Vector3 scale,Material m) => Primitive(name,PrimitiveType.Cylinder,p,scale,m);
        Transform Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material m)
        { var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(transform,false); go.transform.localPosition=p; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=m; Destroy(go.GetComponent<Collider>()); return go.transform; }
        void Pipe(Vector3 a,Vector3 b,float radius,Material m)
        { var t=Primitive("Service pipe",PrimitiveType.Cylinder,(a+b)/2,new Vector3(radius*2,(b-a).magnitude/2,radius*2),m); t.up=(b-a).normalized; }
        void OnDestroy() { foreach(var m in owned) if(m!=null) Destroy(m);if(rockMesh!=null)Destroy(rockMesh); }
    }
}
