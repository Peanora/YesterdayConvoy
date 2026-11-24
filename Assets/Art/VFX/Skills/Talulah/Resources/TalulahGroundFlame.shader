Shader "YesterdayConvoy/TalulahGroundFlame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Flame atlas", 2D) = "black" {}
        _HdrRange ("HDR encoding range", Float) = 32
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct input { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _HdrRange;

            output vert(input v)
            {
                output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float3 linearRadiance(float2 uv)
            {
                float3 encoded = tex2D(_MainTex, uv).rgb;
                return encoded * encoded;
            }

            float4 frag(output i) : SV_Target
            {
                float4 encoded = tex2D(_MainTex, i.uv);
                float3 center = encoded.rgb * encoded.rgb;
                float2 dx = float2(_MainTex_TexelSize.x, 0);
                float2 dy = float2(0, _MainTex_TexelSize.y);
                float3 localAverage = (linearRadiance(i.uv + dx) + linearRadiance(i.uv - dx) +
                    linearRadiance(i.uv + dy) + linearRadiance(i.uv - dy)) * .25;
                // Local contrast retains the shape and detail of the atlas flame.
                // Alpha and transparent additive color retain the source blend.
                float3 flare = max(0, center + (center - localAverage) * .72);
                return float4(flare * _HdrRange * i.color.rgb * i.color.a, encoded.a * i.color.a);
            }
            ENDCG
        }
    }
}
