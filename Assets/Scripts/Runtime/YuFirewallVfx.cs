using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    // View-only layered quads, batched by texture/depth. The battle remains the sole owner of wall rules.
    internal sealed class YuFirewallVfx:IDisposable
    {
        sealed class Batch
        {
            public readonly MeshRenderer Renderer;
            readonly Mesh mesh;
            readonly List<Vector3> vertices=new List<Vector3>(256);
            readonly List<Vector2> uv=new List<Vector2>(256);
            readonly List<Color> colors=new List<Color>(256);
            readonly List<int> triangles=new List<int>(384);
            public Batch(Transform parent,string name,Material material)
            {
                var node=new GameObject("Yu firewall / "+name,typeof(MeshFilter),typeof(MeshRenderer));
                node.transform.SetParent(parent,true);Renderer=node.GetComponent<MeshRenderer>();Renderer.sharedMaterial=material;
                Renderer.shadowCastingMode=ShadowCastingMode.Off;Renderer.receiveShadows=false;
                mesh=new Mesh{name="Yu firewall / "+name};mesh.MarkDynamic();node.GetComponent<MeshFilter>().sharedMesh=mesh;
            }
            public void Reset(){vertices.Clear();uv.Clear();colors.Clear();triangles.Clear();}
            public void Quad(Vector3 p,Vector3 right,Vector3 up,Rect tex,Color color)
            {
                if(color.a<=.001f)return;
                int n=vertices.Count;vertices.Add(p-right-up);vertices.Add(p+right-up);vertices.Add(p+right+up);vertices.Add(p-right+up);
                uv.Add(new Vector2(tex.xMin,tex.yMin));uv.Add(new Vector2(tex.xMax,tex.yMin));uv.Add(new Vector2(tex.xMax,tex.yMax));uv.Add(new Vector2(tex.xMin,tex.yMax));
                for(int i=0;i<4;i++)colors.Add(color);
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
            }
            public void Apply(int order)
            {
                mesh.Clear();Renderer.enabled=vertices.Count>0;Renderer.sortingOrder=order;
                if(vertices.Count==0)return;
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            }
            public void Dispose(){UnityEngine.Object.Destroy(Renderer.gameObject);UnityEngine.Object.Destroy(mesh);}
        }
        sealed class Wall
        {
            public readonly Batch[] Batches;
            public double Start=double.NegativeInfinity,Stopped=double.PositiveInfinity;
            public bool Seen;
            public Wall(Transform root,Material fire,Material glow,Material smoke)
            {Batches=new[]{new Batch(root,"rear flame",fire),new Batch(root,"connected fire",fire),new Batch(root,"front flame",fire),new Batch(root,"embers",glow),new Batch(root,"smoke",smoke),new Batch(root,"ground heat",glow)};}
        }
        readonly Transform parent;
        readonly Dictionary<string,Wall> walls=new Dictionary<string,Wall>();
        readonly Material fire,glow,smoke;
        readonly Texture2D radial;
        static readonly Rect Full=new Rect(0,0,1,1);
        public YuFirewallVfx(Transform root)
        {
            parent=root;var shader=Resources.Load<Shader>("YuFirewall");
            fire=new Material(shader){mainTexture=Resources.Load<Texture2D>("FireRainEmbers")};
            glow=new Material(shader);glow.SetFloat("_RedFlame",0);glow.SetFloat("_DstBlend",(float)BlendMode.One);
            radial=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Yu soft heat",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {float d=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));}
            radial.SetPixels(pixels);radial.Apply(false,true);glow.mainTexture=radial;
            smoke=new Material(shader){mainTexture=Resources.Load<Texture2D>("TalulahAttackSmoke")};smoke.SetFloat("_RedFlame",0);
        }
        static float Noise(int n)=>Mathf.Repeat(Mathf.Sin(n*97.713f+17.18f)*43758.5453f,1);
        static float Smooth(float n)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(n));
        static Rect Frame(float age,int seed)
        {int n=(Mathf.FloorToInt(age*28)+seed*7)%25;if(n<0)n+=25;return new Rect(n%5*.2f,(4-n/5)*.2f,.2f,.2f);}
        public void Begin(){foreach(var wall in walls.Values){wall.Seen=false;foreach(var b in wall.Batches)b.Reset();}}
        public void Present(ManualBattle battle,DefenderState defender,Camera cam)
        {
            bool active=battle.DefenderWallActive(defender);
            if(!walls.TryGetValue(defender.EntityId,out var wall))
            {if(!active)return;wall=new Wall(parent,fire,glow,smoke);walls.Add(defender.EntityId,wall);}
            if(active&&wall.Start!=defender.SkillStartedAt){wall.Start=defender.SkillStartedAt;wall.Stopped=double.PositiveInfinity;}
            if(!active&&double.IsPositiveInfinity(wall.Stopped))wall.Stopped=Math.Min(battle.Clock,defender.SkillActiveUntil);
            float age=(float)(battle.Clock-wall.Start-1.6),tail=(float)(battle.Clock-wall.Stopped);
            if(age<0||tail>1.25f)return;
            wall.Seen=true;float release=float.IsNegativeInfinity(tail)?1:Smooth((.65f-tail)/.65f),fade=Smooth(age/.22f)*release;
            var center=battle.DefenderWallPoint(defender)+Vector3.up*.035f;
            var axis=Vector3.Cross(defender.WallNormal,Vector3.up).normalized;var up=cam.transform.up;var right=cam.transform.right;
            var back=wall.Batches[0];var body=wall.Batches[1];var front=wall.Batches[2];var sparks=wall.Batches[3];var haze=wall.Batches[4];var heat=wall.Batches[5];
            // Preserve the old +/-5.76 world-unit extent. Each tongue has an independent phase and height.
            for(int i=0;i<45;i++)
            {
                float x=-5.76f+i*(11.52f/44),phase=Noise(i+70)*6.28f;
                float ignition=Smooth((age-Mathf.Abs(x)*.018f)/.2f)*release;
                var basePoint=center+axis*x;
                float height=(.8f+Noise(i+100)*.65f+Mathf.Sin(age*5+phase)*.15f)*(.3f+.7f*release);
                float bend=Mathf.Sin(age*3+phase)*.12f;
                back.Quad(basePoint+up*height*.43f+right*bend,right*(.36f+Noise(i+120)*.15f),up*height*.55f,Frame(age,i),new Color(1,.82f,.8f,ignition*.68f));
                body.Quad(basePoint+up*.23f,right*.32f,up*.35f,Frame(age*1.17f,i+50),new Color(1,1,1,ignition*.88f));
                if(i%2==0)front.Quad(basePoint+up*.12f,right*.33f,up*.23f,Frame(age*.92f,i+90),new Color(1,.62f,.52f,ignition*.46f));
                heat.Quad(basePoint,right*.43f,up*.12f,Full,new Color(1,.04f,.008f,ignition*.32f));
                // Ground contact light lies on the floor; it is not an opaque upright strip.
                if(i%3==0)heat.Quad(basePoint+Vector3.up*.006f,axis*.7f,defender.WallNormal*.58f,Full,new Color(1,.035f,.008f,ignition*.16f));
            }
            for(int i=0;i<36;i++)
            {
                float life=.65f+Noise(i+230)*.9f,t=Mathf.Repeat(age+Noise(i+200)*life,life)/life;
                float opacity=fade;if(tail>=0)opacity=Smooth((1.1f-tail)/1.1f);
                var p=center+axis*((Noise(i+240)-.5f)*11.6f)+up*(.17f+t*(.75f+Noise(i+260)*1.0f))+right*Mathf.Sin(age*2+i)*t*.16f;
                sparks.Quad(p,right*(.012f+Noise(i+270)*.012f),up*(.025f+Noise(i+280)*.034f),Full,new Color(1,.09f,.015f,Mathf.Sin(t*Mathf.PI)*opacity*.8f));
            }
            for(int i=0;i<15;i++)
            {
                float life=1.5f+Noise(i+330),t=Mathf.Repeat(age+Noise(i+300)*life,life)/life;
                float opacity=tail<0?Smooth(age/.3f):Smooth((1.25f-tail)/1.25f);
                var p=center+axis*(-5.6f+i*.8f)+up*(.55f+t*1.2f)+right*(Mathf.Sin(age+i)*t*.23f);
                haze.Quad(p,right*(.44f+t*.26f),up*(.48f+t*.36f),Full,new Color(.34f,.25f,.26f,Mathf.Sin(t*Mathf.PI)*opacity*.12f));
            }
            int order=DemoSpriteVisual.GetSortingOrder(cam,battle.DefenderWallPoint(defender));
            back.Apply(order+28);body.Apply(order+29);front.Apply(order+31);sparks.Apply(order+32);haze.Apply(order+27);heat.Apply(order+26);
        }
        public void End(){foreach(var wall in walls.Values)if(!wall.Seen)foreach(var b in wall.Batches)b.Renderer.enabled=false;}
        public void Clear(){foreach(var wall in walls.Values)foreach(var b in wall.Batches)b.Dispose();walls.Clear();}
        public void Dispose(){Clear();UnityEngine.Object.Destroy(fire);UnityEngine.Object.Destroy(glow);UnityEngine.Object.Destroy(smoke);UnityEngine.Object.Destroy(radial);}
    }
}
