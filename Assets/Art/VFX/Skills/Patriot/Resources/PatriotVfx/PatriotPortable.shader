Shader "YesterdayConvoy/PatriotPortable"
{
    Properties
    {
        [PerRendererData] _MainTex("Native effect atlas",2D)="black"{}
        _Frame("Frame on page",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            sampler2D _MainTex;float _Frame;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float4 frag(v2f i):SV_Target
            {
                float2 tile=float2(fmod(_Frame,4),3-floor(_Frame/4));
                float4 c=tex2D(_MainTex,(tile+clamp(i.uv,.001953125,.998046875))/4);
                // Native additive pixels legitimately have RGB > 0 and alpha == 0.
                return float4(c.rgb*c.rgb*32*i.color.rgb*i.color.a,c.a*i.color.a);
            }
            ENDCG
        }
    }
}
