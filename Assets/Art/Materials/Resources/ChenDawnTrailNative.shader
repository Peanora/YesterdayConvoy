Shader "YesterdayConvoy/Chen Dawn Native Trail Composite"
{
    Properties { _MainTex("Original six-layer trail frame",2D)="black"{} _HdrRange("Native HDR range",Float)=32 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;float _HdrRange;
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            float4 frag(output i):SV_Target
            {
                float4 c=tex2D(_MainTex,i.uv);c.rgb=c.rgb*c.rgb*_HdrRange;
                #if defined(UNITY_COLORSPACE_GAMMA)
                    float3 low=c.rgb*12.92,high=1.055*pow(max(c.rgb,0),1.0/2.4)-0.055;
                    c.rgb=lerp(low,high,step(0.0031308,c.rgb));
                #endif
                return c;
            }
            ENDCG
        }
    }
}
