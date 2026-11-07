using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class GuerrillaOrderView
    {
        public const float ChargeStart=1.97f,FinalStrikeAt=2.8f;
        int displayedStrikeCount;
        static readonly Vector2[][] blades={
            new[]{new Vector2(-250,1160),new Vector2(920,510),new Vector2(2180,-90)},
            new[]{new Vector2(-220,-120),new Vector2(1040,520),new Vector2(2180,1130)},
            new[]{new Vector2(-250,790),new Vector2(920,410),new Vector2(2180,240)},
            new[]{new Vector2(2160,1000),new Vector2(980,490),new Vector2(-230,-120)},
            new[]{new Vector2(-260,980),new Vector2(940,300),new Vector2(2180,180)}
        };
        static readonly Vector2[] crossBlade={new Vector2(-180,-80),new Vector2(900,590),new Vector2(2120,1150)};
        bool CanCharge(float time)=>volley!=null&&!volley.Cancelled&&!volley.Resolved&&
            owner!=null&&owner.Battle?.Phase==BattlePhase.Running&&time>=ChargeStart&&time<FinalStrikeAt;
        static Vector2 BladePoint(Vector2[] blade,float p)=>
            (1-p)*(1-p)*blade[0]+2*(1-p)*p*blade[1]+p*p*blade[2];
        static float StrikeRandom(int seed)
        {double value=System.Math.Sin(seed*127.1+32.7)*43758.5453;return (float)(value-System.Math.Floor(value));}

        void ChargeBackdrop(float time)
        {
            if(!CanCharge(time))return;
            float amount=S((time-ChargeStart)/.22f)*(.26f+.28f*ChargeProgress);
            behind.Rect(new Rect(0,0,1920,1080),Rgba(2,8,19,amount));
        }
        void FinalCharge(float time)
        {
            if(!CanCharge(time))return;
            float age=time-ChargeStart,p=ChargeProgress,envelope=S(age/.10f);
            Vector2 focus=BladePoint(blades[4],.55f);
            Glow(front,focus,210+p*190,Rgba(238,184,77,(.22f+.52f*p)*envelope));
            for(int i=0;i<62;i++)
            {
                float delay=StrikeRandom(i+187)*.34f,q=Mathf.Clamp01((age-delay)/(.33f+StrikeRandom(i+314)*.2f));
                if(q<=0||q>=1)continue;
                float a=StrikeRandom(i+721)*Mathf.PI*2,r=(1-Out(q))*(320+StrikeRandom(i+819)*720)+10;
                Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)*.58f),point=focus+d*r;
                Line(front,point+d*(12+38*(1-q)),point,Rgba(255,220,143,(.25f+.65f*q)*envelope),1+StrikeRandom(i+416)*1.6f,3);
            }
            for(int j=0;j<2;j++)
            {
                float q=Mathf.Clamp01((age-.07f*j)/.77f),r=360*(1-Out(q))+22;
                Ellipse(focus,r,r*.43f,-.28f+j*.5f,Rgba(255,224,161,(.14f+.33f*p)*(1-q*.35f)*envelope),1.2f,.2f+j*.4f,Mathf.PI*1.7f+j*.4f);
            }
            Ribbon(blades[4],.25f+.25f*p,.85f-.23f*p,1.2f+1.9f*p,Rgba(255,206,111,(.15f+.7f*p)*envelope));
            float radius=8+4*p;
            front.Fill(new[]{focus+new Vector2(-radius*2.8f,0),focus+new Vector2(0,-radius),focus+new Vector2(radius*2.8f,0),focus+new Vector2(0,radius)},Rgba(255,242,185,envelope));
            Line(front,focus-new Vector2(45+p*30,0),focus+new Vector2(45+p*30,0),Rgba(255,243,202,(.25f+.65f*p)*envelope),1.3f,5);
            Text("蓄  力",24,FontStyle.Bold,new Vector2(focus.x,613),Rgba(239,211,155,envelope));
            Text("终结一击",16,FontStyle.Normal,new Vector2(focus.x,647),Rgba(189,166,125,envelope));
        }
        void Ribbon(Vector2[] blade,float low,float high,float width,Color color)
        {
            if(high<=low||color.a<=.005f)return;
            Vector2 previousA=Vector2.zero,previousB=Vector2.zero;
            for(int k=0;k<=52;k++)
            {
                float p=Mathf.Lerp(low,high,k/52f);Vector2 point=BladePoint(blade,p);
                Vector2 tangent=BladePoint(blade,Mathf.Min(1,p+.002f))-BladePoint(blade,Mathf.Max(0,p-.002f));
                Vector2 n=new Vector2(-tangent.y,tangent.x).normalized*width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(k/52f*Mathf.PI)),.55f);
                Vector2 a=point+n,b=point-n;
                if(k>0)front.Fill(new[]{previousA,a,b,previousB},color);
                previousA=a;previousB=b;
            }
        }
        void Ellipse(Vector2 center,float rx,float ry,float rotation,Color tint,float width,float from=0,float to=Mathf.PI*2)
        {
            Vector2 previous=Vector2.zero;
            for(int i=0;i<=64;i++)
            {
                float a=Mathf.Lerp(from,to,i/64f);Vector2 point=center+Rotate(new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),rotation);
                if(i>0)Line(front,previous,point,tint,width,2);previous=point;
            }
        }
        void Slash(float time,GuerrillaOrderStrike strike)
        {
            float realAge=time-strike.At;bool fifth=strike.Index==4;
            if(realAge<0||realAge>(fifth?.72f:.48f))return;
            float age=fifth?Mathf.Max(0,realAge-.05f):realAge;
            Vector2[] blade=blades[strike.Index];float head=Out((age+.085f)/.115f),tail=Mathf.Clamp01((age-.015f)/.25f),alpha=1-S((age-.03f)/.28f);
            Color warm=fifth?Rgba(255,206,111):Rgba(72,169,255);
            warm.a=alpha*.18f;Ribbon(blade,tail,head,fifth?108:31,warm);
            warm.a=alpha*.57f;Ribbon(blade,tail,head,fifth?44:15,warm);
            Ribbon(blade,tail,head,fifth?10:3.8f,Rgba(247,255,255,alpha*.98f));
            if(fifth)
            {
                Ribbon(crossBlade,tail,head,18,Rgba(255,206,111,alpha*.16f));
                Ribbon(crossBlade,tail,head,2.1f,Rgba(247,255,255,alpha*.52f));
            }
            for(int i=0;i<(fifth?84:27);i++)
            {
                Vector2 point=BladePoint(blade,StrikeRandom(i+strike.Index*83));
                float life=.2f+StrikeRandom(i+711)*(fifth?.46f:.28f),u=age/life;if(u>1)continue;
                float a=StrikeRandom(i+381+strike.Index*57)*Mathf.PI*2,dist=(80+StrikeRandom(i+918)*(fifth?450:280))*Out(u);
                Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)*.65f),p=point+d*dist;
                Line(front,p,p+d*(fifth?26:14)*(1-u),fifth?Rgba(255,227,166,(1-u)*.9f):Rgba(170,225,255,(1-u)*.85f),1.2f+(1-u)*(fifth?3:1.8f),2);
            }
            if(age<.09f)front.Rect(new Rect(0,0,1920,1080),fifth?Rgba(255,245,218,(1-S(age/.09f))*.23f):Rgba(184,224,255,(1-S(age/.09f))*.09f));
            if(fifth)
            {
                float p=Out(age/.4f);Ellipse(new Vector2(1100,410),80+p*1400,35+p*600,-.14f,Rgba(204,234,255,(1-p)*.65f),2);
            }
        }
        void BattlefieldImpact(float time)
        {
            // This is only a consumer of committed model events. No render path deals damage.
            if(volley==null)return;
            FinalCharge(time);
            var completed=volley.Strikes.Where(s=>s.At<=time+.00001f).ToArray();
            if(completed.Length==0)return;
            displayedStrikeCount=completed.Length;
            foreach(var hit in volley.Hits)if(hit.Damage>0){impactTargetCount++;displayedDamage+=hit.Damage;}
            foreach(var strike in completed){Slash(time,strike);StrikeTargets(time,strike);}
            float alpha=S((time-completed[0].At)/.08f)*(1-S((time-(Duration-.28f))/.25f));
            if(alpha>0)
            {
                behind.Rect(new Rect(600,748,850,82),Rgba(6,20,32,alpha*.94f));
                bool fifth=displayedStrikeCount==5;Color tint=fifth?Rgba(255,226,163,alpha):Rgba(217,240,255,alpha);
                Text(fifth?"蓄力重斩":"全场追击",24,FontStyle.Bold,new Vector2(706,789),tint);
                Text(displayedStrikeCount.ToString("00")+" / 05",30,FontStyle.Bold,new Vector2(858,789),tint);
                for(int i=0;i<5;i++)front.Rect(new Rect(635+i*34,805,25,4),i<displayedStrikeCount?tint:Rgba(52,73,90,alpha));
                Text("累计伤害",19,FontStyle.Normal,new Vector2(1090,790),Rgba(156,185,204,alpha));
                Text(DamageNumber(displayedDamage),36,FontStyle.Bold,new Vector2(1330,795),tint);
            }
            if(captionFont!=null)caption.Texture=captionFont.material.mainTexture;
        }
        void StrikeTargets(float time,GuerrillaOrderStrike strike)
        {
            float age=time-strike.At;bool fifth=strike.Index==4;float life=fifth?.67f:.29f;
            if(age<0||age>life)return;var camera=Camera.main;if(camera==null)return;
            float alpha=1-S((age-.12f)/(life-.12f)),rise=Out(age/.35f)*30;
            var labels=new List<Rect>();
            foreach(var hit in strike.Hits.OrderBy(h=>h.TargetId,System.StringComparer.Ordinal))
            {
                if(hit.Damage<=0)continue;
                Vector3 projected=camera.WorldToScreenPoint(hit.WorldPosition+camera.transform.up*.90f);if(projected.z<=0)continue;
                Vector2 point=TacticalViewport.Current.FromScreen(projected)*1.5f;
                Glow(front,point,90,Rgba(159,229,255,Mathf.Exp(-age*16)*.8f));
                float radius=17+Out(age/.2f)*25;
                Ellipse(point+new Vector2(0,13),radius,radius*.36f,0,fifth?Rgba(255,223,160,alpha):Rgba(182,233,255,alpha),2);
                Line(front,point+new Vector2(-18,-25),point+new Vector2(18,10),Rgba(221,245,255,alpha),3,3);
                Line(front,point+new Vector2(15,-23),point+new Vector2(-14,12),Rgba(221,245,255,alpha),2,3);
                Vector2 label=point+new Vector2(point.x<960?-40:40,-70-rise);int size=fifth?55:38;
                for(int attempt=0;attempt<8;attempt++)
                {
                    label.x=Mathf.Clamp(label.x,110,1810);label.y=Mathf.Clamp(label.y,95,970);
                    var bounds=new Rect(label.x-100,label.y-size,200,size+8);
                    if(!labels.Any(r=>r.Overlaps(bounds))){labels.Add(bounds);break;}
                    label+=new Vector2(attempt%2==0?-70:70,-size-9);
                }
                string number="−"+DamageNumber(hit.Damage);
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)if(x!=0||y!=0)
                    Text(number,size,FontStyle.Bold,label+new Vector2(x*2,y*2),Rgba(16,35,50,alpha));
                Text(number,size,FontStyle.Bold,label,fifth?Rgba(255,230,170,alpha):Rgba(208,241,255,alpha));
            }
        }
    }
}
