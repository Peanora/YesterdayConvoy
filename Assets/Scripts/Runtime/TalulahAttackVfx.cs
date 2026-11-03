using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Private presentation helper: sampled only from existing shots and simulation time.
    internal sealed class TalulahAttackVfx:IDisposable
    {
        readonly Transform parent;
        readonly List<SpriteRenderer> sprites=new List<SpriteRenderer>();
        readonly List<LineRenderer> cores=new List<LineRenderer>();
        readonly Sprite[] flames=new Sprite[25];
        readonly Sprite smoke,glow,solid;
        readonly Texture2D glowTexture;
        readonly Material additive,alpha;
        int used,coreUsed,order;
        Camera camera;
        public TalulahAttackVfx(Transform root)
        {
            parent=root;additive=new Material(Resources.Load<Shader>("TalulahAttackAdditive"));alpha=new Material(Shader.Find("Sprites/Default"));
            var texture=Resources.Load<Texture2D>("FireRainEmbers");
            for(int i=0;i<25;i++)flames[i]=Sprite.Create(texture,new Rect(i%5*128,(4-i/5)*128,128,128),new Vector2(.5f,.5f),128);
            var smokeTexture=Resources.Load<Texture2D>("TalulahAttackSmoke");
            smoke=Sprite.Create(smokeTexture,new Rect(0,0,smokeTexture.width,smokeTexture.height),new Vector2(.5f,.5f),smokeTexture.width);
            glowTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Talulah local light",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {float d=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));}
            glowTexture.SetPixels(pixels);glowTexture.Apply(false,true);
            glow=Sprite.Create(glowTexture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);
            solid=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        }
        static float Random(int i)=>Mathf.Repeat(Mathf.Sin(i*97.713f+17.18f)*43758.5453f,1);
        static float Smooth(float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(x));
        public void Begin(){used=coreUsed=0;}
        void DrawSprite(string name,Sprite image,Vector3 point,Vector2 size,float angle,Color tint,bool haze=false)
        {
            if(image==null||tint.a<=0||size.x<=0||size.y<=0)return;
            if(used==sprites.Count)
            {
                var r=new GameObject("Talulah attack / "+name,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                r.transform.SetParent(parent,false);sprites.Add(r);
            }
            var renderer=sprites[used++];renderer.name="Talulah attack / "+name;renderer.enabled=true;renderer.sprite=image;
            renderer.sharedMaterial=haze?alpha:additive;renderer.sortingOrder=order+(haze?2:1);renderer.color=tint;
            renderer.transform.SetPositionAndRotation(point,camera.transform.rotation*Quaternion.Euler(0,0,angle));
            renderer.transform.localScale=new Vector3(size.x/image.bounds.size.x,size.y/image.bounds.size.y,1);
        }
        void Flame(int frame,Vector3 p,float width,float height,float angle,float opacity)
            =>DrawSprite("flowing flame",flames[(frame%25+25)%25],p,new Vector2(width,height),angle,new Color(1,1,1,opacity));
        void Core(Vector3 head,Vector3 axis,Vector3 up,float angle,float strength)
        {
            if(coreUsed==cores.Count)
            {
                var line=new GameObject("Talulah attack / hot edge",typeof(LineRenderer)).GetComponent<LineRenderer>();
                line.transform.SetParent(parent,false);line.sharedMaterial=additive;line.useWorldSpace=true;line.positionCount=20;
                line.widthMultiplier=.018f;line.numCapVertices=3;cores.Add(line);
            }
            var r=cores[coreUsed++];r.enabled=true;r.sortingOrder=order;
            r.startColor=new Color(1,.43f,.12f,strength*.2f);r.endColor=new Color(1,.91f,.63f,strength*.8f);
            Vector3 a=head-axis*1.25f+up*(-Mathf.Sin(2-angle*2)*.31f),b=head-axis*.675f-up*.2f,c=head-axis*.3375f+up*.0625f;
            for(int i=0;i<20;i++){float t=i/19f,s=1-t;r.SetPosition(i,a*s*s*s+3*b*s*s*t+3*c*s*t*t+head*t*t*t);}
        }
        public void Present(CombatShot shot,double clock,Camera cam)
        {
            float age=(float)(clock-shot.StartedAt),hit=(float)(clock-shot.ImpactAt);
            if(shot.Erased||age<0||hit>1.2f)return;
            camera=cam;order=DemoSpriteVisual.GetSortingOrder(cam,shot.Destination)+50;
            Vector3 origin=shot.Origin+cam.transform.up*.72f,end=shot.Destination+cam.transform.up*.75f;
            var projected=Vector3.ProjectOnPlane(end-origin,cam.transform.forward);
            Vector3 axis=projected.sqrMagnitude>.001f?projected.normalized:cam.transform.right,up=cam.transform.up;
            float angle=Mathf.Atan2(Vector3.Dot(axis,up),Vector3.Dot(axis,cam.transform.right))*Mathf.Rad2Deg;
            float travel=(float)(shot.ImpactAt-shot.StartedAt),relative=age*.65f/travel;
            if(relative>=.36f&&relative<.85f)
            {
                float p=Mathf.Clamp01((relative-.38f)/.28f),strength=Smooth((relative-.36f)/.08f)*Smooth((.85f-relative)/.17f);
                Vector3 head=Vector3.Lerp(origin,end,p);float length=Mathf.Min(1.91f,projected.magnitude*.85f);
                DrawSprite("warm reflection",glow,head-axis*.42f,new Vector2(1.75f,.45f),angle,new Color(1,.35f,.08f,strength*.09f));
                for(int i=0;i<20;i++)
                {
                    float tail=i/20f;var at=head-axis*(tail*length)-up*(Mathf.Sin(tail*2.9f-p*2)*tail*.56f+Mathf.Sin(relative*27+i)*.031f*tail);
                    Flame(Mathf.FloorToInt(relative*42)+i*7,at,.20f+Random(i+3)*.15f,(.29f+Random(i+6)*.31f)*(1-tail*.25f),angle-90-Mathf.Sin(tail*5-relative*11)*9,strength*(1-tail)*.70f);
                }
                Core(head,axis,up,p,strength);
            }
            if(hit<0||hit>=1.2f||!shot.Applied)return;
            float burst=Smooth(hit/.035f)*Smooth((.28f-hit)/.22f);
            DrawSprite("ground reflection",glow,shot.Destination,new Vector2(3.1f,.75f),0,new Color(1,.35f,.08f,burst*.20f));
            if(hit<.13f)DrawSprite("contact flash",glow,end,new Vector2(.07f,.85f+hit),angle+20,new Color(1,.95f,.8f,(1-hit/.13f)*.94f));
            for(int i=0;i<16;i++)
            {
                float life=.15f+Random(i+70)*.38f,t=(hit-Random(i+50)*.04f)/life;if(t<0||t>1)continue;
                float direction=Random(i+90)*Mathf.PI*2,distance=.15f+Random(i+110)*.525f;
                var at=end+axis*(Mathf.Cos(direction)*distance*t)+up*(-Mathf.Sin(direction)*distance*t*.6f+t*.2625f);
                Flame(Mathf.FloorToInt(hit*48)+i*3,at,(.225f+Random(i+150)*.275f)*(1-t*.3f),(.325f+Random(i+170)*.4f)*(1-t*.45f),angle-direction*Mathf.Rad2Deg-90,Mathf.Sin(Mathf.PI*t)*(.3f+Random(i+130)*.4f));
            }
            for(int i=0;i<28;i++)
            {
                float life=.24f+Random(i+210)*.63f,t=hit/life;if(t>1)continue;
                float vx=(Random(i+240)-.35f)*2.375f,vy=.44f+Random(i+260)*2.19f;
                var at=end+axis*(vx*hit)+up*(vy*hit-1.81f*hit*hit);
                float rotation=angle+Mathf.Atan2(vy-3.62f*hit,vx)*Mathf.Rad2Deg;
                DrawSprite("ballistic ember",solid,at,new Vector2(.035f+Mathf.Abs(vx)*.012f,.009f+Random(i+300)*.009f),rotation,new Color(1,i%4==0?.9f:.55f,.2f,(1-t)*(.4f+Random(i+280)*.55f)));
            }
            for(int i=0;i<5;i++)
            {
                float life=.58f+Random(i+340)*.53f,t=(hit-.06f)/life;if(t<0||t>1)continue;
                var at=end+axis*((Random(i+360)-.5f)*.94f*t)+up*(t*(.54f+Random(i+380)*.56f));
                DrawSprite("rising smoke",smoke,at,new Vector2(1.12f+t*.45f,.85f+t*.5f),(Random(i)-.5f)*74+t*14,new Color(.7f,.73f,.76f,Mathf.Sin(Mathf.PI*t)*.10f),true);
            }
        }
        public void End(){for(int i=used;i<sprites.Count;i++)sprites[i].enabled=false;for(int i=coreUsed;i<cores.Count;i++)cores[i].enabled=false;}
        public void Clear()
        {foreach(var r in sprites)UnityEngine.Object.Destroy(r.gameObject);foreach(var r in cores)UnityEngine.Object.Destroy(r.gameObject);sprites.Clear();cores.Clear();used=coreUsed=0;}
        public void Dispose()
        {Clear();foreach(var f in flames)UnityEngine.Object.Destroy(f);UnityEngine.Object.Destroy(glow);UnityEngine.Object.Destroy(glowTexture);UnityEngine.Object.Destroy(smoke);UnityEngine.Object.Destroy(solid);UnityEngine.Object.Destroy(additive);UnityEngine.Object.Destroy(alpha);}
    }
}
