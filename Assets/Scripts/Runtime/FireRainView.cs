using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>The approved orange fire waves. Reads battle state; never applies damage.</summary>
    public sealed class FireRainView : MonoBehaviour
    {
        const float WaveSpacing=.91f,CameraDepth=5;
        const int TonguesPerCast=30,SparksPerCast=100;
        sealed class Wave
        {
            internal SpriteRenderer Head,Tail;
            internal MeshRenderer Glow;
        }
        struct Cast
        {
            internal TalulahArtDefinition Art;
            internal float Age,Duration,Strength,Warmth;
        }
        readonly List<Wave> waves=new List<Wave>();
        readonly List<SpriteRenderer> tongues=new List<SpriteRenderer>(),sparks=new List<SpriteRenderer>();
        readonly List<Cast> casts=new List<Cast>();
        readonly Sprite[] emberFrames=new Sprite[25];
        ManualBattleController owner;
        Material material;
        Mesh glowMesh;
        Sprite spark;
        FireRainScreenEffect screenEffect;
        Rect groundScreenBounds;
        float groundCellPixels;
        int rowCount;
        public int VisibleStreams {get;private set;}
        public void Initialize(ManualBattleController value){owner=value;}

        void Awake()
        {
            var shader=Resources.Load<Shader>("FireRainAdditive");
            var texture=Resources.Load<Texture2D>("FireRainEmbers");
            if(shader==null||texture==null){enabled=false;return;}
            material=new Material(shader);glowMesh=MakeGlow();
            for(int i=0;i<emberFrames.Length;i++)
                emberFrames[i]=Sprite.Create(texture,new Rect(i%5*128,(4-i/5)*128,128,128),new Vector2(.5f,.5f),128);
            spark=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        }
        SpriteRenderer NewSprite(string label,int order)
        {
            var renderer=new GameObject(label,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform,false);renderer.sharedMaterial=material;
            renderer.sortingOrder=order;renderer.enabled=false;return renderer;
        }
        void EnsureCapacity(int waveCount,int castCount)
        {
            while(waves.Count<waveCount)
            {
                var glow=new GameObject("Fire reflected glow",typeof(MeshFilter),typeof(MeshRenderer));
                glow.transform.SetParent(transform,false);glow.GetComponent<MeshFilter>().sharedMesh=glowMesh;
                var renderer=glow.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.sortingOrder=22499;renderer.enabled=false;
                waves.Add(new Wave{Head=NewSprite("Horizontal fire stream",22500),Tail=NewSprite("Turbulent flame tail",22500),Glow=renderer});
            }
            while(tongues.Count<castCount*TonguesPerCast)tongues.Add(NewSprite("Airborne fire tongue",22510));
            while(sparks.Count<castCount*SparksPerCast)
            {var renderer=NewSprite("Fire ember",22511);renderer.sprite=spark;sparks.Add(renderer);}
        }
        static float Smooth(float value){value=Mathf.Clamp01(value);return value*value*(3-2*value);}
        static float Random01(int seed)=>Mathf.Repeat(Mathf.Sin(seed*91.319f+17.281f)*43758.5453f,1);
        static bool IsGround(string name)
        {
            // Authored tile tops only: never basin water, walls, scenery or generated effects.
            return name.StartsWith("Cell ",StringComparison.Ordinal)
                ||name=="Tile surface"||name.StartsWith("Tile surface (",StringComparison.Ordinal)
                ||name=="High ground inset"||name.StartsWith("High ground inset (",StringComparison.Ordinal)
                ||name.StartsWith("Platform cap ",StringComparison.Ordinal);
        }
        bool GroundProjection(Camera camera,out Rect area,out float cellWidth)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            float bottom=float.PositiveInfinity,top=float.NegativeInfinity;
            cellWidth=float.PositiveInfinity;int corners=0;
            // Re-read enabled authored geometry only while casting. Surface edits take effect
            // on the next presentation frame, including the outermost corners and high tiles.
            foreach(var renderer in FindObjectsByType<MeshRenderer>())
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy
                    ||renderer.gameObject.scene!=owner.gameObject.scene||!IsGround(renderer.name))continue;
                var bounds=renderer.bounds;if(bounds.size.x<.4f||bounds.size.z<.4f)continue;
                for(int x=0;x<2;x++)for(int z=0;z<2;z++)
                {
                    var p=camera.WorldToScreenPoint(new Vector3(x==0?bounds.min.x:bounds.max.x,bounds.max.y,z==0?bounds.min.z:bounds.max.z));
                    if(p.z<=camera.nearClipPlane)continue;
                    left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);bottom=Mathf.Min(bottom,p.y);top=Mathf.Max(top,p.y);corners++;
                }
                var a=camera.WorldToScreenPoint(new Vector3(bounds.min.x,bounds.max.y,bounds.center.z));
                var b=camera.WorldToScreenPoint(new Vector3(bounds.max.x,bounds.max.y,bounds.center.z));
                if(a.z>camera.nearClipPlane&&b.z>camera.nearClipPlane)
                {
                    float width=Vector2.Distance(new Vector2(a.x,a.y),new Vector2(b.x,b.y));
                    if(width>1)cellWidth=Mathf.Min(cellWidth,width);
                }
            }
            area=Rect.MinMaxRect(left,bottom,right,top);
            return corners>0&&area.width>1&&area.height>1&&!float.IsInfinity(cellWidth);
        }
        void LateUpdate()
        {
            VisibleStreams=0;HideRenderers();casts.Clear();
            var battle=owner?.Battle;var camera=Camera.main;
            if(battle==null||camera==null||material==null||battle.Phase!=BattlePhase.Running||!battle.EngagementEnabled)
            {Hide();return;}
            float screenStrength=0,screenWarmth=0,screenAge=0;
            foreach(var unit in battle.Units)
            {
                if(!battle.UnitCombat(unit).FireRain||!battle.IsSkillActionPlaying(unit)
                    ||double.IsInfinity(unit.FireRainStartedAt)||double.IsNaN(unit.FireRainStartedAt))continue;
                var art=owner.CardForUnit(unit).ArticulatedArt;
                if(art==null||art.FireFrames==null||art.FireFrames.Count==0)continue;
                float age=(float)(battle.Clock-unit.FireRainStartedAt),duration=battle.UnitCombat(unit).SkillInterval;
                if(age<0||age>=duration)continue;
                float strength=Smooth(age/.30f)*Smooth((duration-age)/.45f);
                float warmth=Smooth(age/.25f)*Smooth((duration-age)/.6f);
                casts.Add(new Cast{Art=art,Age=age,Duration=duration,Strength=strength,Warmth=warmth});
                if(strength>=screenStrength){screenStrength=strength;screenAge=age;}
                screenWarmth=Mathf.Max(screenWarmth,warmth);
            }
            // Idle play does not enumerate map geometry.
            if(casts.Count==0||!GroundProjection(camera,out Rect area,out float cellWidth)){Hide();return;}
            groundScreenBounds=area;groundCellPixels=cellWidth;
            if(screenEffect==null||screenEffect.gameObject!=camera.gameObject)
            {if(screenEffect!=null)Destroy(screenEffect);screenEffect=camera.gameObject.AddComponent<FireRainScreenEffect>();}
            screenEffect.Present(screenAge,screenStrength,screenWarmth);
            // Preserve the approved flame proportions per terrain tile. Include both map edges;
            // deeper maps gain rows instead of stretching a fixed six-row screen band.
            rowCount=Mathf.Max(2,Mathf.CeilToInt(area.height/(cellWidth*.85f))+1);int capacity=0;
            foreach(var cast in casts)capacity+=rowCount*Mathf.CeilToInt(cast.Duration/WaveSpacing);
            EnsureCapacity(capacity,casts.Count);
            int index=0;
            for(int castIndex=0;castIndex<casts.Count;castIndex++)
            {
                var cast=casts[castIndex];float age=cast.Age,strength=cast.Strength;
                int count=Mathf.CeilToInt(cast.Duration/WaveSpacing);
                // The first front must still cross an unusually wide edited map inside the
                // existing skill window; this adapts presentation speed, never damage timing.
                float travelSpeed=Mathf.Max(cellWidth*5.3f,(area.width+cellWidth*6)/Mathf.Max(.35f,cast.Duration-.375f-.45f));
                for(int row=0;row<rowCount;row++)for(int wave=0;wave<count;wave++)
                {
                    float rowFraction=row/(float)(rowCount-1);
                    // Retain the old .375s total stagger even when the map has more rows.
                    float elapsed=age-wave*WaveSpacing-rowFraction*.375f;
                    float x=area.xMin-cellWidth*2.5f+elapsed*travelSpeed;
                    if(strength<=0||elapsed<0||x>=area.xMax+cellWidth*3.5f)continue;
                    var item=waves[index++];int id=row*17+wave+20+castIndex*457;
                    float y=Mathf.Lerp(area.yMax,area.yMin,rowFraction);
                    float size=(.78f+rowFraction*.375f)*(.9f+Random01(id)*.22f);
                    float width=(2.65f+Random01(id+9)*.45f)*cellWidth*size,height=1.76f*cellWidth*size;
                    item.Head.sprite=cast.Art.FireFrames[Mathf.FloorToInt(age*28+id*5)%cast.Art.FireFrames.Count];
                    item.Head.color=new Color(1.25f,1.12f,1,strength);item.Head.enabled=item.Head.sprite!=null;
                    // CC0 source head points down; camera-local +90 makes it travel right.
                    Place(item.Head,camera,x-width*.28f,y,width,height,90);
                    item.Head.transform.position+=camera.transform.up*(Mathf.Sin(age*11+id)*cellWidth*.034f*WorldPerPixel(camera));
                    item.Tail.sprite=cast.Art.FireFrames[Mathf.FloorToInt(age*31.64f+id*11+9)%cast.Art.FireFrames.Count];
                    item.Tail.color=new Color(1.25f,.98f,.8f,strength*.56f);item.Tail.enabled=item.Tail.sprite!=null;
                    Place(item.Tail,camera,x-width*.66f-width*.38f*.28f,y-Mathf.Sin(age*3+id)*cellWidth*.09f,width*.38f,height*.47f,90);
                    item.Glow.enabled=true;item.Glow.transform.position=Point(camera,x-cellWidth*.34f,y-cellWidth*.18f);
                    item.Glow.transform.rotation=camera.transform.rotation;
                    item.Glow.transform.localScale=new Vector3(width*.73f,height*.9f,1)*WorldPerPixel(camera)*strength;
                    if(item.Head.enabled)VisibleStreams++;
                }
                for(int i=0;i<TonguesPerCast;i++)
                {
                    var renderer=tongues[castIndex*TonguesPerCast+i];renderer.enabled=strength>0;
                    float p=Mathf.Repeat(age*(.4f+Random01(i+90)*.2f)+Random01(i+30),1);
                    float x=Mathf.Lerp(area.xMin-cellWidth*.5f,area.xMax+cellWidth*.5f,p);
                    float y=Mathf.Lerp(area.yMin,area.yMax,Random01(i+40))+Mathf.Sin(p*Mathf.PI)*cellWidth*.25f;
                    renderer.sprite=emberFrames[Mathf.FloorToInt(Mathf.Repeat(age*.72f+Random01(i),1)*25)];
                    renderer.color=new Color(1,1,1,strength*.48f);float width=(.375f+Random01(i+12)*.27f)*cellWidth;
                    Place(renderer,camera,x,y,width,width*1.1f,0);
                }
                for(int i=0;i<SparksPerCast;i++)
                {
                    var renderer=sparks[castIndex*SparksPerCast+i];renderer.enabled=strength>0;
                    float p=Mathf.Repeat(age*(.21f+Random01(i+119)*.19f)+Random01(i+1),1);
                    float x=Mathf.Lerp(area.xMin-cellWidth*.5f,area.xMax+cellWidth*.5f,Mathf.Repeat(Random01(i+15)+age*.15f,1));
                    float y=Mathf.Lerp(area.yMin,area.yMax,Random01(i+28))+p*cellWidth*.625f;
                    float alpha=strength*Mathf.Sin(p*Mathf.PI)*(.3f+Random01(i+23)*.6f);
                    renderer.color=new Color(1,.65f+Random01(i)*.25f,.27f,alpha);
                    float size=(.008f+Random01(i+80)*.016f)*cellWidth;Place(renderer,camera,x,y,size*3.5f,size*1.5f,15);
                }
            }
        }
        static float WorldPerPixel(Camera camera)=>camera.orthographic?camera.orthographicSize*2/camera.pixelHeight:2*CameraDepth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/camera.pixelHeight;
        static Vector3 Point(Camera camera,float x,float y)=>camera.ScreenToWorldPoint(new Vector3(x,y,CameraDepth));
        static void Place(SpriteRenderer renderer,Camera camera,float x,float y,float width,float height,float roll)
        {
            if(renderer.sprite==null){renderer.enabled=false;return;}
            renderer.transform.position=Point(camera,x,y);renderer.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,roll);
            float unit=WorldPerPixel(camera);Vector2 size=renderer.sprite.bounds.size;
            renderer.transform.localScale=roll==90?new Vector3(height*unit/size.x,width*unit/size.y,1):new Vector3(width*unit/size.x,height*unit/size.y,1);
        }
        void HideRenderers()
        {
            foreach(var wave in waves){if(wave.Head!=null)wave.Head.enabled=false;if(wave.Tail!=null)wave.Tail.enabled=false;if(wave.Glow!=null)wave.Glow.enabled=false;}
            foreach(var renderer in tongues)if(renderer!=null)renderer.enabled=false;
            foreach(var renderer in sparks)if(renderer!=null)renderer.enabled=false;
        }
        void Hide(){HideRenderers();VisibleStreams=0;rowCount=0;if(screenEffect!=null)screenEffect.Present(0,0,0);}
        static Mesh MakeGlow()
        {
            var vertices=new Vector3[25];var colors=new Color[25];var indices=new int[72];colors[0]=new Color(1,.34f,.045f,.23f);
            for(int i=0;i<24;i++)
            {
                float angle=i*Mathf.PI/12;vertices[i+1]=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                colors[i+1]=new Color(.4f,.04f,0,0);indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=(i+1)%24+1;
            }
            var mesh=new Mesh{name="Soft fire illumination",vertices=vertices,colors=colors,triangles=indices};mesh.RecalculateBounds();return mesh;
        }
        void OnDisable()=>Hide();
        void OnDestroy()
        {
            Hide();if(screenEffect!=null)Destroy(screenEffect);if(material!=null)Destroy(material);if(glowMesh!=null)Destroy(glowMesh);
            foreach(var frame in emberFrames)if(frame!=null)Destroy(frame);if(spark!=null)Destroy(spark);
        }
    }
}
