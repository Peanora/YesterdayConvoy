Shader "YesterdayConvoy/FormationNativeGate"
{
    Properties
    {
        _MainTex ("Original gate texture", 2D) = "white" {}
        _TintColor ("Approved gate tint", Color) = (1,1,1,1)
        _DstBlend ("Original destination blend", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha [_DstBlend]
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_ST, _TintColor;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Original additive/alpha programs multiply texture, vertex color and tint by two.
                fixed4 color = tex2D(_MainTex, i.uv) * i.color * _TintColor * 2;
                color.a = saturate(color.a);
                return color;
            }
            ENDCG
        }
    }
}
