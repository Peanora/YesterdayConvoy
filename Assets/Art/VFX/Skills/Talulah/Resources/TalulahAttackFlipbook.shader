Shader "YesterdayConvoy/TalulahAttackFlipbook"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fire sheet",2D)="white" {}
        _Frame ("Painted frame",Float)=0
        _Opacity ("Opacity",Range(0,1))=1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Frame,_Opacity;
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float WarmAt(float2 uv){float4 n=tex2D(_MainTex,uv);return (n.r-n.b)/max(n.r,.001)*smoothstep(.05,.20,n.a);}
            float4 SampleFrame(float index,float2 localUv)
            {
                float2 tile=float2(fmod(index,4),2-floor(index/4));
                float2 uv=(tile+clamp(localUv,.002,.998))/float2(4,3);
                float4 c=tex2D(_MainTex,uv);
                float warmth=(c.r-c.b)/max(c.r,.001);
                float2 d=_MainTex_TexelSize.xy*5;
                float neighbours=max(max(WarmAt(uv+float2(d.x,0)),WarmAt(uv-float2(d.x,0))),
                    max(WarmAt(uv+float2(0,d.y)),WarmAt(uv-float2(0,d.y))));
                float whiteHot=smoothstep(.90,.98,min(c.r,c.g))*smoothstep(.5,.8,c.a)*smoothstep(.2,.5,neighbours);
                c.a*=smoothstep(.035,.17,c.a)*max(smoothstep(.15,.48,warmth),whiteHot);
                return c;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float index=clamp(_Frame,0,11),low=floor(index),blend=frac(index);
                float4 a=SampleFrame(low,i.uv),b=SampleFrame(min(11,low+1),i.uv);
                float alpha=lerp(a.a,b.a,blend);
                float3 color=(a.rgb*a.a*(1-blend)+b.rgb*b.a*blend)/max(.001,alpha);
                return float4(color*i.color.rgb,alpha*_Opacity*i.color.a);
            }
            ENDCG
        }
    }
}
