Shader "YesterdayConvoy/TalulahNative"
{
    Properties { [PerRendererData] _MainTex("Native frame",2D)="black"{} _HdrRange("HDR encoding range",Float)=32 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct output{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            sampler2D _MainTex;float _HdrRange;
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float4 frag(output i):SV_Target
            {
                float4 c=tex2D(_MainTex,i.uv);
                // Preserve additive RGB even when alpha is zero. Textures are imported linear.
                return float4(c.rgb*c.rgb*_HdrRange*i.color.rgb*i.color.a,c.a*i.color.a);
            }
            ENDCG
        }
    }
}
