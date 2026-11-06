using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    /// <summary>Four bounded mesh batches read the existing weather clock. No control or damage writes.</summary>
    public sealed class SnowstormView:MonoBehaviour
    {
        sealed class Batch
        {
            internal Mesh Mesh;internal MeshRenderer Renderer;internal Material Material;
            internal readonly List<Vector3> Vertices=new List<Vector3>(3000);
            internal readonly List<Vector2> Uvs=new List<Vector2>(3000);
            internal readonly List<Color> Colors=new List<Color>(3000);
            internal readonly List<int> Triangles=new List<int>(4500);
            internal void Begin(){Vertices.Clear();Uvs.Clear();Colors.Clear();Triangles.Clear();}
            internal void Upload(){Mesh.Clear();Mesh.SetVertices(Vertices);Mesh.SetUVs(0,Uvs);Mesh.SetColors(Colors);Mesh.SetTriangles(Triangles,0);Mesh.RecalculateBounds();Renderer.enabled=Vertices.Count>0;}
        }
        ManualBattleController owner;ManualBattle observed;int run=-1,pulses;
        double started=double.NegativeInfinity,terminalAt=double.NegativeInfinity;
        float terminalStrength;readonly List<Batch> batches=new List<Batch>();
        AudioSource bed,gust;Rect area;Camera lastCamera;Matrix4x4 lastProjection,lastView;int screenWidth,screenHeight;
        public int VisibleLayers{get;private set;}
        public float Strength{get;private set;}
        public float WeatherAge{get;private set;}
        public Rect WeatherScreenBounds=>area;
        public void Initialize(ManualBattleController controller){owner=controller;ResetView();}
        static float Smooth(float v){v=Mathf.Clamp01(v);return v*v*(3-2*v);}
        static float Rand(int i){unchecked{uint x=(uint)i*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return(((x>>22)^x)&0xffffff)/16777216f;}}
        Batch Make(string name,Texture texture,int order)
        {
            var go=new GameObject("Snowstorm / "+name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            var mesh=new Mesh{name="Snowstorm "+name};mesh.MarkDynamic();
            var material=new Material(Shader.Find("Sprites/Default")){name="Snowstorm alpha / "+name,mainTexture=texture};
            material.SetColor("_Color",Color.white);material.SetColor("_RendererColor",Color.white);material.SetVector("_Flip",Vector4.one);
            var renderer=go.GetComponent<MeshRenderer>();go.GetComponent<MeshFilter>().sharedMesh=mesh;renderer.sharedMaterial=material;
            renderer.sortingOrder=order;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
            var batch=new Batch{Mesh=mesh,Renderer=renderer,Material=material};batches.Add(batch);return batch;
        }
        void EnsureResources()
        {
            if(batches.Count>0)return;SnowstormArt.Warm();
            Make("cold atmosphere",Texture2D.whiteTexture,22990);Make("mist",SnowstormArt.Fog,22991);
            Make("wind",SnowstormArt.Wind,22992);Make("snow",SnowstormArt.Snow,22993);
            bed=gameObject.AddComponent<AudioSource>();bed.playOnAwake=false;bed.loop=true;bed.spatialBlend=0;bed.clip=SnowstormArt.Bed;
            gust=gameObject.AddComponent<AudioSource>();gust.playOnAwake=false;gust.spatialBlend=0;gust.clip=SnowstormArt.Gust;
        }
        static bool Ground(string name)=>name.StartsWith("Cell ",StringComparison.Ordinal)||name=="Tile surface"||name.StartsWith("Tile surface (",StringComparison.Ordinal)||name=="High ground inset"||name.StartsWith("High ground inset (",StringComparison.Ordinal)||name.StartsWith("Platform cap ",StringComparison.Ordinal);
        void UpdateArea(Camera camera)
        {
            var vp=TacticalViewport.Current;Vector2 a=vp.ToScreen(new Vector2(184,54)),b=vp.ToScreen(new Vector2(1264,551));
            Rect safe=Rect.MinMaxRect(a.x,b.y,b.x,a.y);
            bool dirty=lastCamera!=camera||screenWidth!=Screen.width||screenHeight!=Screen.height||lastProjection!=camera.projectionMatrix||lastView!=camera.worldToCameraMatrix;
            if(!dirty)return;lastCamera=camera;screenWidth=Screen.width;screenHeight=Screen.height;lastProjection=camera.projectionMatrix;lastView=camera.worldToCameraMatrix;
            float loX=float.PositiveInfinity,loY=float.PositiveInfinity,hiX=float.NegativeInfinity,hiY=float.NegativeInfinity;
            foreach(var r in FindObjectsByType<MeshRenderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r.gameObject.scene!=owner.gameObject.scene||!Ground(r.name))continue;
                var bounds=r.bounds;if(bounds.size.x<.4f||bounds.size.z<.4f)continue;
                for(int x=0;x<2;x++)for(int z=0;z<2;z++)
                {var p=camera.WorldToScreenPoint(new Vector3(x==0?bounds.min.x:bounds.max.x,bounds.max.y,z==0?bounds.min.z:bounds.max.z));if(p.z<=camera.nearClipPlane)continue;loX=Mathf.Min(loX,p.x);hiX=Mathf.Max(hiX,p.x);loY=Mathf.Min(loY,p.y);hiY=Mathf.Max(hiY,p.y);}
            }
            area=float.IsInfinity(loX)?safe:Rect.MinMaxRect(Mathf.Max(safe.xMin,loX-80*vp.Scale),Mathf.Max(safe.yMin,loY-45*vp.Scale),Mathf.Min(safe.xMax,hiX+80*vp.Scale),Mathf.Min(safe.yMax,hiY+50*vp.Scale));
            if(area.width<=1||area.height<=1)area=safe;
        }
        float Edge(Vector2 p)=>Smooth((p.x-area.xMin)/(area.width*.09f))*Smooth((area.xMax-p.x)/(area.width*.08f))*Smooth((p.y-area.yMin)/(area.height*.08f))*Smooth((area.yMax-p.y)/(area.height*.1f));
        void Quad(Batch batch,Camera camera,Vector2 center,Vector2 size,float angle,Rect uv,Color color,bool feather=false)
        {
            int n=feather?4:2,start=batch.Vertices.Count;float sn=Mathf.Sin(angle),cs=Mathf.Cos(angle);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=feather?(x==0?0:x==1?.15f:x==2?.85f:1):x;
                float v=feather?(y==0?0:y==1?.15f:y==2?.85f:1):y;
                float dx=(u-.5f)*size.x,dy=(v-.5f)*size.y;
                Vector2 pixel=center+new Vector2(dx*cs-dy*sn,dx*sn+dy*cs);pixel.x=Mathf.Clamp(pixel.x,area.xMin,area.xMax);pixel.y=Mathf.Clamp(pixel.y,area.yMin,area.yMax);
                var tint=color;tint.a*=Edge(pixel);if(feather&&(x==0||x==n-1||y==0||y==n-1))tint.a=0;
                batch.Vertices.Add(transform.InverseTransformPoint(camera.ScreenToWorldPoint(new Vector3(pixel.x,pixel.y,camera.nearClipPlane+.2f))));
                batch.Uvs.Add(new Vector2(uv.x+u*uv.width,uv.y+v*uv.height));batch.Colors.Add(tint);
            }
            for(int y=0;y<n-1;y++)for(int x=0;x<n-1;x++)
            {int i=start+y*n+x;batch.Triangles.Add(i);batch.Triangles.Add(i+n);batch.Triangles.Add(i+1);batch.Triangles.Add(i+1);batch.Triangles.Add(i+n);batch.Triangles.Add(i+n+1);}
        }
        void Draw(Camera camera,float age)
        {
            UpdateArea(camera);foreach(var batch in batches)batch.Begin();float s=TacticalViewport.Current.Scale;
            Vector2 Point(float x,float y)=>new Vector2(area.xMin+x*area.width,area.yMax-y*area.height);
            float pulse=age<8?Mathf.Exp(-Mathf.Repeat(age,2)*5.5f):0;
            Quad(batches[0],camera,area.center,area.size,0,new Rect(0,0,1,1),new Color(.55f,.79f,.91f,.11f*Strength),true);
            for(int i=0;i<14;i++)
            {
                float f=Mathf.Repeat(age*(.12f+Rand(i+11)*.1f)+Rand(i+40),1);int cell=(int)(Rand(i+100)*4);float size=152+Rand(i+130)*136;
                Quad(batches[1],camera,Point(f,Rand(i+60)),new Vector2(size*2.7f,size*.65f)*s,-.12f,new Rect(cell%2*.5f,1-(cell/2+1)*.5f,.5f,.5f),new Color(1,1,1,(.085f+pulse*.035f)*Strength));
            }
            for(int i=0;i<7;i++)Quad(batches[2],camera,Point(Mathf.Repeat(Rand(i+290)+age*.19f,1),Rand(i+320)),new Vector2(576,144)*s,-.12f,new Rect(0,0,1,1),new Color(1,1,1,(.12f+.06f*pulse)*Strength),true);
            for(int i=0;i<620;i++)
            {
                bool near=i>=500;float velocity=(near?390:210)+Rand(i+430)*(near?310:190),life=Mathf.Repeat(Rand(i+480)+age*(.14f+Rand(i+600)*.18f),1);
                Vector2 point=Point(Mathf.Repeat(Rand(i+1)+age*velocity/1460,1),Mathf.Repeat(Rand(i+80)+age*(.08f+Rand(i+90)*.1f),1));
                int cell=(int)(Rand(i+125)*8);float size=((near?14:4)+Rand(i+920)*(near?14:6))*.8f*s;
                Quad(batches[3],camera,point,Vector2.one*size,age*.16f+Rand(i+890)*Mathf.PI*2,new Rect(cell%4*.25f,1-(cell/4+1)*.5f,.25f,.5f),new Color(1,1,1,(near?.4f:.6f)*Mathf.Sin(life*Mathf.PI)*Strength*(1+pulse*.55f)));
            }
            if(age<8&&Mathf.Repeat(age,2)<.75f)
            {
                float f=Mathf.Repeat(age,2)/.75f;
                for(int i=0;i<5;i++)Quad(batches[2],camera,Point(f-i*.055f,.2f+i*.14f),new Vector2(440,140)*s,-.12f,new Rect(0,0,1,1),new Color(1,1,1,.28f*(1-f)*Strength),true);
            }
            foreach(var batch in batches)batch.Upload();VisibleLayers=batches.Count;
        }
        void LateUpdate()
        {
            var b=owner?.Battle;var camera=Camera.main;
            if(observed!=b||run!=(b?.RunId??-1)){ResetView();observed=b;run=b?.RunId??-1;}
            if(b==null||camera==null||owner.TestSelectionOpen||owner.ArtLoading||b.SnowTier!=5||double.IsInfinity(b.SnowstormStartedAt)||b.Phase==BattlePhase.Ready){Hide();return;}
            if(started!=b.SnowstormStartedAt){started=b.SnowstormStartedAt;terminalAt=double.NegativeInfinity;lastCamera=null;}
            WeatherAge=(float)(owner.PresentationClock-started);
            Strength=Smooth(WeatherAge/.3f)*(1-Smooth((WeatherAge-ManualBattle.SnowstormDuration)/.65f));
            if(b.Phase==BattlePhase.Won||b.Phase==BattlePhase.Lost)
            {
                if(double.IsNegativeInfinity(terminalAt))
                {
                    // Combat.Clock is the authoritative result time, even when a long frame
                    // advances presentation before this component first observes the result.
                    terminalAt=b.Clock;float endAge=(float)(terminalAt-started);
                    terminalStrength=Smooth(endAge/.3f)*(1-Smooth((endAge-ManualBattle.SnowstormDuration)/.65f));
                }
                Strength=terminalStrength*(1-Smooth((float)(owner.PresentationClock-terminalAt)/.5f));
            }
            if(WeatherAge<0||Strength<=.001f){Hide();return;}
            EnsureResources();Draw(camera,WeatherAge);
            bed.mute=gust.mute=owner.Muted;bed.volume=.65f*Strength;bed.pitch=gust.pitch=Mathf.Clamp(owner.EffectiveSimulationSpeed,.03f,2);
            if(b.IsPaused){bed.Pause();gust.Pause();}
            else
            {
                bed.UnPause();gust.UnPause();if(!bed.isPlaying)bed.Play();
                while(pulses<b.SnowstormPulses.Count){var p=b.SnowstormPulses[pulses++];if(p.At>=started&&b.Clock-p.At<.25&&b.Phase==BattlePhase.Running)gust.PlayOneShot(SnowstormArt.Gust,.65f);}
            }
        }
        void Hide(){VisibleLayers=0;Strength=0;foreach(var b in batches)b.Renderer.enabled=false;if(bed!=null){bed.Stop();gust.Stop();}}
        void ResetView(){Hide();started=terminalAt=double.NegativeInfinity;WeatherAge=0;pulses=0;lastCamera=null;}
        void OnDisable()=>ResetView();
        void OnDestroy(){foreach(var b in batches){Destroy(b.Mesh);Destroy(b.Material);if(b.Renderer!=null)Destroy(b.Renderer.gameObject);}if(bed!=null){Destroy(bed);Destroy(gust);}}
    }
}
