Shader "YesterdayConvoy/Stronghold Energy"
{
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent+15"}
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 kind:TEXCOORD1;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float kind:TEXCOORD1;fixed4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.kind=v.kind.x;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;float r=length(p);float alpha;
                if(i.kind<.5)alpha=exp(-abs(r-.72)*38)*.85+exp(-r*r*7)*.2;
                else if(i.kind<1.5)alpha=pow(saturate(1-abs(p.x)),3)*pow(saturate(1-i.uv.y),1.4);
                else alpha=pow(saturate(1-r),2);
                return fixed4(i.color.rgb,i.color.a*alpha);
            }
            ENDCG
        }
    }
}
