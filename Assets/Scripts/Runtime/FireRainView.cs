using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved native waves and terrain-bound burning. Read-only battle presentation.</summary>
    public sealed class FireRainView:MonoBehaviour
    {
        const int Rows=5,WaveCapacity=30;
        const float Speed=2.5f,WaveSpacing=2,Afterglow=1.6f;
        sealed class Cast { internal double Start;internal float Duration; }
        sealed class Floor { internal Vector3 Position;internal SpriteRenderer Flame;internal float Phase,Scale; }
        readonly List<Cast> casts=new List<Cast>();
        readonly Dictionary<string,double> seen=new Dictionary<string,double>();
        readonly List<Floor> floors=new List<Floor>();
        readonly SpriteRenderer[] streams=new SpriteRenderer[WaveCapacity],impacts=new SpriteRenderer[24];
        MaterialPropertyBlock heatProperties;
        ManualBattleController owner;
        ManualBattle sampled;
        int run;
        Material fire,heat;
        Mesh heatMesh;
        MeshRenderer heatRenderer;
        FireRainArt.Clip streamClip,groundClip,impactClip;
        FireRainScreenEffect screenEffect;
        Bounds bounds;
        public int VisibleStreams {get;private set;}
        void Awake(){heatProperties=new MaterialPropertyBlock();}
        public void Initialize(ManualBattleController value){owner=value;}

        void EnsureArt()
        {
            if(fire!=null)return;
            fire=new Material(Resources.Load<Shader>("TalulahFireRain/NativeFire"));
            heat=new Material(Resources.Load<Shader>("TalulahFireRain/GroundHeat"));
            streamClip=FireRainArt.Load("stream");groundClip=FireRainArt.Load("ground");impactClip=FireRainArt.Load("impact");
            for(int i=0;i<streams.Length;i++)streams[i]=NewSprite("Talulah native fire stream");
            for(int i=0;i<impacts.Length;i++)impacts[i]=NewSprite("Talulah native fire impact");
        }
        SpriteRenderer NewSprite(string label)
        {
            var s=new GameObject(label,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            s.transform.SetParent(transform,false);s.sharedMaterial=fire;s.enabled=false;return s;
        }
        void BuildFloor()
        {
            foreach(var f in floors)if(f.Flame!=null)Destroy(f.Flame.gameObject);floors.Clear();
            if(heatRenderer!=null)Destroy(heatRenderer.gameObject);if(heatMesh!=null)Destroy(heatMesh);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            bool first=true;
            foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string n=r.name;
                // Read authored surfaces, never the old preview's screen-space mask.
                if(!r.enabled||!(n.StartsWith("Cell ")||n=="Tile surface"||n=="High ground inset"||n.StartsWith("Platform cap ")))continue;
                Bounds b=r.bounds;if(b.size.x<.4f||b.size.z<.4f)continue;
                Vector3 p=new Vector3(b.center.x,b.max.y+.025f,b.center.z);
                if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                float x=b.extents.x*.985f,z=b.extents.z*.985f;int j=vertices.Count;
                vertices.Add(p+new Vector3(-x,0,-z));vertices.Add(p+new Vector3(-x,0,z));vertices.Add(p+new Vector3(x,0,z));vertices.Add(p+new Vector3(x,0,-z));
                uv.Add(Vector2.zero);uv.Add(Vector2.up);uv.Add(Vector2.one);uv.Add(Vector2.right);
                triangles.AddRange(new[]{j,j+1,j+2,j,j+2,j+3});
                int k=floors.Count;
                floors.Add(new Floor{Position=p,Flame=NewSprite("Talulah ground flame"),Phase=Mathf.Repeat(k*.618034f,1)*4.8f,Scale=1.1f+Mathf.Repeat(k*.381966f,1)*.35f});
            }
            if(first){bounds=new Bounds(owner.Battle.Entry,new Vector3(12,0,7));return;}
            bounds.Expand(new Vector3(.95f,0,.95f));
            fire.SetVector("_GroundBounds",new Vector4(bounds.min.x,bounds.max.x,0,0));
            heatMesh=new Mesh{name="Talulah authored-ground heat",vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};heatMesh.RecalculateBounds();
            var go=new GameObject("Talulah ground heat",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=heatMesh;heatRenderer=go.GetComponent<MeshRenderer>();heatRenderer.sharedMaterial=heat;heatRenderer.enabled=false;
        }
        static float Smooth(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}
        float TailEnd(Cast c)
        {
            // Six-second casts group six damage events into three sparse visual fronts.
            float last=Mathf.Max(0,Mathf.Ceil(c.Duration/WaveSpacing)-1)*WaveSpacing+.25f;
            return last+bounds.size.x/Speed+Afterglow;
        }
        void LateUpdate()
        {
            var b=owner?.Battle;var cam=Camera.main;VisibleStreams=0;
            if(b==null||cam==null){Hide();return;}
            if(sampled!=b||run!=b.RunId)
            {
                Hide();sampled=b;run=b.RunId;casts.Clear();seen.Clear();
                if(fire!=null)BuildFloor();
            }
            if(b.Phase!=BattlePhase.Running){Hide();return;}
            foreach(var u in b.Units)
            {
                if(!b.UnitCombat(u).FireRain||double.IsInfinity(u.FireRainStartedAt)||double.IsNaN(u.FireRainStartedAt))continue;
                if(seen.TryGetValue(u.EntityId,out double last)&&last==u.FireRainStartedAt)continue;
                EnsureArt();if(floors.Count==0)BuildFloor();
                seen[u.EntityId]=u.FireRainStartedAt;
                casts.Add(new Cast{Start=u.FireRainStartedAt,Duration=b.UnitCombat(u).SkillInterval});
            }
            if(casts.Count==0){Hide();return;}
            if(screenEffect==null||screenEffect.gameObject!=cam.gameObject)
            {if(screenEffect!=null)Destroy(screenEffect);screenEffect=cam.gameObject.AddComponent<FireRainScreenEffect>();}
            HideRenderers();Cast groundCast=null;float strength=0,screenAge=0;int index=0;
            for(int i=casts.Count-1;i>=0;i--)
            {
                var c=casts[i];float age=(float)(b.Clock-c.Start),end=TailEnd(c);
                if(age>=end){casts.RemoveAt(i);continue;}
                float level=Smooth(age/.3f)*Smooth((end-age)/Afterglow);
                if(level>=strength){groundCast=c;strength=level;screenAge=age;}
                int count=Mathf.CeilToInt(c.Duration/WaveSpacing);
                for(int wave=0;wave<count;wave++)
                {
                    float elapsed=age-.25f-wave*WaveSpacing;
                    if(elapsed<0||elapsed>=bounds.size.x/Speed)continue;
                    float x=bounds.min.x+elapsed*Speed;
                    for(int row=0;row<Rows&&index<streams.Length;row++)
                    {
                        var s=streams[index++];s.sprite=streamClip.At(elapsed);s.enabled=s.sprite!=null;s.flipX=true;
                        var p=new Vector3(x,.07f,Mathf.Lerp(bounds.min.z+.25f,bounds.max.z-.25f,row/(float)(Rows-1)));
                        Place(s,cam,p,1.05f,new Color(1,1,1,Smooth(elapsed/.16f)*Smooth((bounds.max.x-x)/.6f)));
                        if(s.enabled)VisibleStreams++;
                    }
                }
            }
            if(groundCast==null){Hide();return;}
            float groundAge=(float)(b.Clock-groundCast.Start);
            foreach(var f in floors)
            {
                float age=groundAge-.25f-(f.Position.x-bounds.min.x)/Speed;
                f.Flame.enabled=age>=0&&strength>0;
                if(!f.Flame.enabled)continue;
                f.Flame.sprite=groundClip.At(age+f.Phase,true);f.Flame.flipX=((int)(f.Phase*10)&1)!=0;
                float pulse=.83f+.17f*Mathf.Cos(age*Mathf.PI);
                Place(f.Flame,cam,f.Position,f.Scale,new Color(1,.68f,.4f,strength*Smooth(age/.55f)*.28f*pulse));
            }
            if(heatRenderer!=null)
            {
                heatRenderer.enabled=true;heatProperties.SetFloat("_Age",groundAge-.25f);heatProperties.SetFloat("_Strength",strength);
                heatProperties.SetFloat("_Left",bounds.min.x);heatProperties.SetFloat("_Speed",Speed);heatRenderer.SetPropertyBlock(heatProperties);
            }
            index=0;
            foreach(var shot in b.CombatShots)
            {
                double age=b.Clock-shot.ImpactAt;
                if(!shot.FireRain||!shot.Applied||age<0||age>=1.3||index>=impacts.Length)continue;
                var s=impacts[index++];s.sprite=impactClip.At(age);s.enabled=s.sprite!=null;
                Place(s,cam,shot.Destination,1.05f,new Color(1,1,1,Smooth((1.3f-(float)age)/.3f)));
            }
            screenEffect.Present(screenAge,strength,strength);
        }
        static void Place(SpriteRenderer s,Camera cam,Vector3 position,float scale,Color color)
        {
            s.transform.position=position+cam.transform.up*.14f;s.transform.rotation=cam.transform.rotation;s.transform.localScale=Vector3.one*scale;
            s.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,position)-8;s.color=color;
        }
        void HideRenderers()
        {
            foreach(var s in streams)if(s!=null)s.enabled=false;
            foreach(var s in impacts)if(s!=null)s.enabled=false;
            foreach(var f in floors)if(f.Flame!=null)f.Flame.enabled=false;
            if(heatRenderer!=null)heatRenderer.enabled=false;
        }
        void Hide(){HideRenderers();VisibleStreams=0;if(screenEffect!=null)screenEffect.Present(0,0,0);}
        void OnDisable(){Hide();}
        void OnDestroy(){Hide();if(screenEffect!=null)Destroy(screenEffect);if(fire!=null)Destroy(fire);if(heat!=null)Destroy(heat);if(heatMesh!=null)Destroy(heatMesh);}
    }
}
