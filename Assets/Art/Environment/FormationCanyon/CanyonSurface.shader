Shader "YesterdayConvoy/Formation Canyon Surface"
{
    Properties
    {
        _MainTex ("Original color atlas", 2D) = "white" {}
        _Cutoff ("Alpha cutout", Range(0,1)) = 0
        _Fill ("Atlas fill", Range(0,1)) = .22
        _Tint ("Surface tint", Color) = (1,1,1,1)
        _TintMix ("Tint strength", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface Surface Lambert addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        float _Cutoff,_Fill,_TintMix;
        fixed4 _Tint;
        struct Input { float2 uv_MainTex; };
        void Surface(Input input,inout SurfaceOutput output)
        {
            fixed4 color=tex2D(_MainTex,input.uv_MainTex);
            clip(color.a-_Cutoff);
            fixed3 surface=lerp(color.rgb,_Tint.rgb,_TintMix);
            output.Albedo=surface;
            output.Emission=surface*_Fill;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
