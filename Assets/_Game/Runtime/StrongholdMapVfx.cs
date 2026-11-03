using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    // One transparent mesh batches all auras, sparks and portal columns. No combat objects.
    public sealed class StrongholdMapVfx : MonoBehaviour
    {
        readonly List<Vector3> vertices=new List<Vector3>(2048);
        readonly List<Color> colors=new List<Color>(2048);
        readonly List<Vector2> uvs=new List<Vector2>(2048),kinds=new List<Vector2>(2048);
        readonly List<int> triangles=new List<int>(3072);
        readonly Dictionary<string,bool> previousTransit=new Dictionary<string,bool>();
        readonly HashSet<string> breached=new HashSet<string>();
        readonly List<Pulse> pulses=new List<Pulse>();
        sealed class Pulse {public Vector3 Position;public double At;public Color Color;}
        ManualBattleController owner;
        ManualBattle previousBattle;
        Mesh mesh;
        Material material;
        double readyClock;
        int previousTrips;
        public double VisualClock {get;private set;}
        public int PortalPulseCount {get;private set;}
        public int TransientPulseCount=>pulses.Count;
        public int VertexCount=>mesh==null?0:mesh.vertexCount;
        public void Initialize(StrongholdMapArt art)
        {
            owner=FindAnyObjectByType<ManualBattleController>();
            mesh=new Mesh{name="Batched stronghold map energy"};mesh.MarkDynamic();
            material=new Material(art.energyShader);
            var go=new GameObject("Batched map effects",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        void LateUpdate()=>Present();
        public void Present()
        {
            if(owner==null||owner.Battle==null||mesh==null)return;var battle=owner.Battle;
            if(previousBattle!=battle){previousBattle=battle;previousTransit.Clear();breached.Clear();pulses.Clear();readyClock=0;previousTrips=PortalPulseCount=0;}
            if(battle.Phase==BattlePhase.Ready)readyClock+=Time.unscaledDeltaTime;
            VisualClock=readyClock+owner.PresentationClock;
            vertices.Clear();colors.Clear();uvs.Clear();kinds.Clear();triangles.Clear();
            Color amber=new Color(1,.48f,.07f),cyan=new Color(.18f,.83f,1),blue=new Color(.15f,.55f,1);
            int index=0;
            foreach(var p in StrongholdMapLayout.InfectionTiles)
            {
                float wave=.5f+.5f*Mathf.Sin((float)VisualClock*2.8f+index);
                Ground(p+Vector3.up*.04f,1.12f,WithAlpha(amber,.35f+wave*.3f));
                for(int n=0;n<4;n++)Spark(p,n,index,amber,.62f);
                index++;
            }
            foreach(int col in new[]{3,17})
            {
                var p=StrongholdMapLayout.Cell(col,1,.085f);float phase=Mathf.Repeat((float)VisualClock*.55f+col*.1f,1);
                Ground(p,.92f,WithAlpha(cyan,.8f));Ground(p,.75f+phase*.5f,WithAlpha(cyan,(1-phase)*.42f));
            }
            foreach(var lane in battle.Lanes)Ground(lane.Points[lane.Points.Count-1]+Vector3.up*.035f,1.1f,WithAlpha(blue,.35f));
            if(battle.StrongholdPortalTrips>previousTrips){PulseAt(StrongholdMapLayout.Cell(10,5,.09f),cyan);PortalPulseCount++;previousTrips=battle.StrongholdPortalTrips;}
            foreach(var u in battle.Units)
            {
                bool transit=battle.StrongholdInTransit(u.EntityId);
                if(transit)
                {
                    var p=u.Motion.Position;
                    if(!previousTransit.TryGetValue(u.EntityId,out bool was)||!was){PulseAt(p,cyan);PortalPulseCount++;}
                    float progress=1-battle.StrongholdTransitRemaining(u.EntityId)/3;
                    Ground(p+Vector3.up*.08f,.9f+progress*.25f,WithAlpha(cyan,.9f));
                    Quad(p+Vector3.up*.72f,Camera.main.transform.right*.55f,Vector3.up*.75f,WithAlpha(cyan,.45f),1);
                    for(int n=0;n<6;n++)Spark(p,n,3,cyan,1.2f);
                }
                previousTransit[u.EntityId]=transit;
                if(u.Resolution==UnitResolution.Breached&&breached.Add(u.EntityId))PulseAt(u.Motion.Position,blue);
                if(u.Active&&!transit&&battle.StrongholdInfected(u.EntityId))Infected(u.Motion.Position,u.EntityId.GetHashCode(),amber);
            }
            foreach(var d in battle.Defenders)if(d.Alive&&battle.StrongholdInfected(d.EntityId))Infected(d.Position,d.EntityId.GetHashCode(),amber);
            for(int i=pulses.Count-1;i>=0;i--)
            {
                float age=(float)(VisualClock-pulses[i].At);if(age>1){pulses.RemoveAt(i);continue;}
                Ground(pulses[i].Position+Vector3.up*.08f,.5f+age*1.8f,WithAlpha(pulses[i].Color,(1-age)*.9f));
                Quad(pulses[i].Position+Vector3.up*.6f,Camera.main.transform.right*(.25f+age*.2f),Vector3.up*.65f,WithAlpha(pulses[i].Color,(1-age)*.38f),1);
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uvs);mesh.SetUVs(1,kinds);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        }
        void PulseAt(Vector3 position,Color color){if(pulses.Count>=24)pulses.RemoveAt(0);pulses.Add(new Pulse{Position=position,At=VisualClock,Color=color});}
        void Infected(Vector3 p,int seed,Color color)
        {
            float beat=.4f+.4f*Mathf.Pow(Mathf.Max(0,Mathf.Cos((float)VisualClock*Mathf.PI*2)),5);
            Ground(p+Vector3.up*.045f,.65f,WithAlpha(color,beat));
            for(int n=0;n<2;n++)Spark(p,n,Mathf.Abs(seed%7),color,.7f);
        }
        void Spark(Vector3 p,int index,int seed,Color color,float height)
        {
            float age=Mathf.Repeat((float)VisualClock*.6f+index*.23f+seed*.11f,1);
            var position=p+new Vector3(Mathf.Sin(index*5.3f+seed)*.26f,age*height+.08f,Mathf.Cos(index*4.7f+seed)*.26f);
            float size=.022f+(1-age)*.015f;
            Quad(position,Camera.main.transform.right*size,Camera.main.transform.up*(size*2.7f),WithAlpha(color,Mathf.Sin(age*Mathf.PI)*.75f),2);
        }
        static Color WithAlpha(Color c,float a){c.a=a;return c;}
        void Ground(Vector3 p,float size,Color color)=>Quad(p,Vector3.right*size*.5f,Vector3.forward*size*.5f,color,0);
        void Quad(Vector3 p,Vector3 right,Vector3 up,Color color,int kind)
        {
            int start=vertices.Count;vertices.Add(p-right-up);vertices.Add(p+right-up);vertices.Add(p+right+up);vertices.Add(p-right+up);
            uvs.Add(Vector2.zero);uvs.Add(Vector2.right);uvs.Add(Vector2.one);uvs.Add(Vector2.up);
            for(int i=0;i<4;i++){colors.Add(color);kinds.Add(new Vector2(kind,0));}
            triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
