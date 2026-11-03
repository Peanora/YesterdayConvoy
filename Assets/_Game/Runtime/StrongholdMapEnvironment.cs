using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    public sealed class StrongholdMapEnvironment : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<LineRenderer> preview=new List<LineRenderer>();
        Material floor,floorAlternate,side,steel,dark,amber,red,blue,cyan,concrete,crystal;
        StrongholdMapArt art;
        Mesh crystalMesh;
        bool challenge;
        public bool RouteVisible {get;private set;}
        public int NormalMappedSurfaceCount {get;private set;}
        public void Build(DefenderTrialDefinition data)
        {
            challenge=data.mapId==StrongholdChallengeLayout.Id;
            art=Resources.Load<StrongholdMapArt>("StrongholdMapArt");
            if(art==null||art.surfaceShader==null||art.energyShader==null)throw new System.InvalidOperationException("Stronghold map art library missing.");
            floor=Surface(new Color(.82f,.87f,.87f),art.concrete,null,new Rect(0,0,1,1),0,.14f);
            floorAlternate=Surface(new Color(.72f,.78f,.79f),art.concrete,null,new Rect(.23f,.17f,1,1),0,.12f);
            side=Surface(new Color(.64f,.74f,.79f),art.metal02,art.normal02,new Rect(.01f,.4f,.98f,.58f),.58f,.28f);
            steel=Surface(new Color(1.12f,1.16f,1.16f),art.metal01,art.normal01,new Rect(.01f,.05f,.98f,.44f),.65f,.32f);
            concrete=Surface(new Color(.8f,.84f,.81f),art.concrete,null,new Rect(0,0,2,1),0,.12f);
            crystal=Surface(new Color(.12f,.14f,.16f),art.metal01,art.normal01,new Rect(.1f,.13f,.6f,.3f),.75f,.6f);
            dark=Mat(new Color(.055f,.085f,.1f));
            amber=Glow(new Color(1,.54f,.05f));red=Glow(new Color(1,.12f,.16f));blue=Glow(new Color(.1f,.75f,1));cyan=Glow(new Color(.23f,.95f,.85f));
            Box("Surrounding service basin",new Vector3(0,-.9f,0),new Vector3(40,.2f,22),dark);
            for(int row=challenge?-1:1;row<=(challenge?7:5);row++)for(int col=2;col<=18;col++)
            {
                char tile=challenge?StrongholdChallengeLayout.Tile(col,row):StrongholdMapLayout.Tile(col,row);Vector3 p=StrongholdMapLayout.Cell(col,row,0);
                if(tile=='#')
                {
                    Box("Closed bulkhead "+col+","+row,p+Vector3.up*.35f,new Vector3(.99f,.9f,.99f),side);
                    Box("Bulkhead lid",p+Vector3.up*.82f,new Vector3(.97f,.05f,.97f),steel);
                    Box("Bulkhead seam",p+new Vector3(0,.852f,.22f),new Vector3(.75f,.012f,.035f),dark);
                    for(int n=0;n<5;n++)Box("Bulkhead cooling slot",p+new Vector3(-.3f+n*.15f,.34f,-.504f),new Vector3(.055f,.27f,.025f),dark);
                    foreach(float x in new[]{-.38f,.38f})foreach(float z in new[]{-.38f,.38f})Box("Lid recessed fastener",p+new Vector3(x,.856f,z),new Vector3(.055f,.015f,.055f),dark);
                    Box("Bulkhead lower contact",p+new Vector3(0,-.01f,-.51f),new Vector3(1.04f,.025f,.12f),dark);continue;
                }
                if(tile=='H'){
                    Box("Raised defender platform",p+Vector3.up*.35f,new Vector3(.98f,.72f,.98f),side);
                    Box("High ground inset",p+Vector3.up*.73f,new Vector3(.95f,.05f,.95f),steel);
                    for(int n=0;n<4;n++)Box("High ground warning",p+new Vector3(-.32f+n*.21f,.765f,-.4f),new Vector3(.1f,.02f,.1f),cyan);
                    continue;
                }
                Box("Floor "+col+","+row,p-Vector3.up*.17f,new Vector3(.98f,.34f,.98f),side);
                Box("Tile surface",p,new Vector3(.95f,.035f,.95f),tile=='b'?side:(col+row)%3==0?floorAlternate:floor);
                foreach(float x in new[]{-.44f,.44f})Box("Steel seam",p+new Vector3(x,.022f,0),new Vector3(.013f,.012f,.89f),dark);
                if(tile=='a')
                {
                    Box("Active originium warning tile",p+Vector3.up*.03f,new Vector3(.89f,.025f,.89f),dark);
                    for(int n=0;n<5;n++)Box("Amber hazard stripe",p+new Vector3(-.36f+n*.18f,.048f,-.35f),new Vector3(.08f,.015f,.15f),amber);
                    for(int n=0;n<3;n++)
                    {
                        var shard=Crystal(p+new Vector3(-.2f+n*.2f,.2f,.16f-n*.12f),new Vector3(.16f,.4f+n*.08f,.16f));
                        shard.localRotation=Quaternion.Euler(12+n*10,30+n*31,15-n*12);
                        Box("Crystal energy seam",p+new Vector3(-.2f+n*.2f,.18f,.09f-n*.12f),new Vector3(.022f,.26f,.024f),amber);
                    }
                }
                if(tile=='I')Gate(p,cyan,false);
                if(tile=='E')Gate(p,blue,true);
                if(tile=='x')for(int n=0;n<5;n++)Box("Drain channel",p+new Vector3(-.3f+n*.15f,.026f,.15f),new Vector3(.045f,.018f,.45f),dark);
            }
            // Authored original barricade locations are shown as fixed routing structures.
            foreach(int col in new[]{3,4,8,12,16,17})
            {
                Vector3 p=StrongholdMapLayout.Cell(col,5,0);
                Box("Routing barricade",p+Vector3.up*.22f,new Vector3(.72f,.44f,.38f),steel);
                Box("Barricade warning",p+new Vector3(0,.25f,-.2f),new Vector3(.6f,.12f,.025f),amber);
                for(int n=0;n<4;n++)Box("Barricade black chevron",p+new Vector3(-.22f+n*.15f,.25f,-.217f),new Vector3(.07f,.13f,.012f),dark).localRotation=Quaternion.Euler(0,0,-25);
            }
            foreach(var lane in data.lanes)Gate(lane.points[0],red,true);
            for(int n=0;n<8;n++)
            {
                float x=-9+n*2.6f;
                Box("Rear industrial wall",new Vector3(x,.5f,challenge?6.5f:4.5f),new Vector3(2.5f,1.7f,1.8f),concrete);
                Box("Rear vent recess",new Vector3(x,.9f,(challenge?5.575f:3.575f)),new Vector3(1.8f,.55f,.05f),dark);
                for(int fin=0;fin<9;fin++)Box("Rear ventilation fin",new Vector3(x-.75f+fin*.19f,.9f,(challenge?5.535f:3.535f)),new Vector3(.055f,.48f,.045f),steel);
                Box("Wall service panel",new Vector3(x+.92f,.65f,(challenge?5.54f:3.54f)),new Vector3(.35f,1.2f,.07f),side);
                Box("Rear light rail",new Vector3(x,.45f,(challenge?5.58f:3.58f)),new Vector3(2.15f,.06f,.04f),cyan);
                Box("Service fascia",new Vector3(x,-.4f,challenge?-5:-3),new Vector3(2.45f,.45f,.5f),steel);
                for(int vent=0;vent<7;vent++)Box("Front fascia grille",new Vector3(x-.8f+vent*.25f,-.38f,challenge?-5.26f:-3.26f),new Vector3(.12f,.22f,.02f),dark);
            }
            for(int sideIndex=-1;sideIndex<=1;sideIndex+=2)
            {
                Box("Maintenance side pier",new Vector3(sideIndex*9.5f,-.1f,0),new Vector3(1.3f,.9f,challenge?11:7),side);
                for(int row=0;row<4;row++)
                {
                    var p=new Vector3(sideIndex*9.5f,.65f,-2+row*1.6f);
                    Box("Side equipment",p,new Vector3(.8f,1,.8f),steel);
                    Box("Side control recess",p+new Vector3(0,.05f,-.41f),new Vector3(.54f,.52f,.03f),dark);
                    Box("Equipment status light",p+new Vector3(.16f,.1f,-.43f),new Vector3(.1f,.08f,.025f),cyan);
                    Box("Equipment footing",p+new Vector3(0,-.57f,0),new Vector3(1,.12f,.97f),dark);
                }
                Box("Long service pipe",new Vector3(sideIndex*10,.24f,0),new Vector3(.15f,.15f,6.5f),side);
                for(int row=0;row<7;row++)Box("Pipe support collar",new Vector3(sideIndex*10,.24f,-3+row),new Vector3(.23f,.23f,.09f),steel);
            }
            gameObject.AddComponent<StrongholdMapVfx>().Initialize(art);
        }
        Material Surface(Color color,Texture2D texture,Texture2D normal,Rect uv,float metallic,float smoothness)
        {
            var m=new Material(art.surfaceShader){color=color,mainTexture=texture,enableInstancing=true};
            m.mainTextureScale=new Vector2(uv.width,uv.height);m.mainTextureOffset=new Vector2(uv.x,uv.y);
            if(normal!=null){m.SetTexture("_BumpMap",normal);NormalMappedSurfaceCount++;}
            m.SetFloat("_BumpScale",.6f);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smoothness);materials.Add(m);return m;
        }
        Transform Crystal(Vector3 position,Vector3 scale)
        {
            if(crystalMesh==null)
            {
                var v=new List<Vector3>();var t=new List<int>();
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5,b=(i+1)*Mathf.PI*2/5;var left=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var right=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                    v.Add(left);v.Add(Vector3.up);v.Add(right);v.Add(left);v.Add(right);v.Add(Vector3.down*.7f);
                }
                for(int i=0;i<v.Count;i++)t.Add(i);crystalMesh=new Mesh{name="Faceted dark originium"};crystalMesh.SetVertices(v);crystalMesh.SetTriangles(t,0);crystalMesh.RecalculateNormals();
            }
            var go=new GameObject("Faceted originium shard",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=crystalMesh;go.GetComponent<MeshRenderer>().sharedMaterial=crystal;return go.transform;
        }
        public void ShowLane(DeploymentLane lane)
        {
            foreach(var line in preview)line.enabled=false;RouteVisible=lane!=null;if(lane==null)return;
            int portal=StrongholdChallengeLayout.PortalSource(lane.Points);
            // The portal connector is deliberately omitted: it is not a walkable bridge.
            for(int i=1;i<lane.Points.Count;i++)
            {
                if(i==portal+1)continue;
                while(preview.Count<i)preview.Add(NewLine(transform,cyan,.045f));
                var line=preview[i-1];line.enabled=true;line.sharedMaterial=i<=portal?amber:cyan;line.startColor=line.endColor=Color.white;line.SetPositions(new[]{lane.Points[i-1]+Vector3.up*.08f,lane.Points[i]+Vector3.up*.08f});
            }
        }
        void Gate(Vector3 p,Material color,bool tall)
        {
            float height=tall?.85f:.12f;
            Vector3[] corners={new Vector3(-.43f,.04f,-.43f),new Vector3(.43f,.04f,-.43f),new Vector3(.43f,.04f,.43f),new Vector3(-.43f,.04f,.43f)};
            for(int i=0;i<4;i++)
            {
                var a=p+corners[i];var b=p+corners[(i+1)%4];var line=NewLine(transform,color,.025f);line.SetPositions(new[]{a,b});
                line=NewLine(transform,color,.025f);line.SetPositions(new[]{a+Vector3.up*height,b+Vector3.up*height});
                if(tall){line=NewLine(transform,color,.025f);line.SetPositions(new[]{a,a+Vector3.up*height});}
            }
        }
        static LineRenderer NewLine(Transform parent,Material material,float width)
        {
            var line=new GameObject("Map line").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.sharedMaterial=material;
            line.positionCount=2;line.widthMultiplier=width;line.shadowCastingMode=ShadowCastingMode.Off;return line;
        }
        Material Mat(Color color,Texture texture=null){var m=new Material(Shader.Find("Standard")){color=color,mainTexture=texture};m.SetFloat("_Glossiness",.15f);materials.Add(m);return m;}
        Material Glow(Color color){var m=new Material(Shader.Find("Sprites/Default")){color=color};materials.Add(m);return m;}
        Transform Box(string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go.transform;
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);if(crystalMesh!=null)Destroy(crystalMesh);}
    }
}
