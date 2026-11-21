Shader "YesterdayConvoy/Formation Canyon Surface"
{
    Properties
    {
        _MainTex ("Original color atlas", 2D) = "white" {}
        _Cutoff ("Alpha cutout", Range(0,1)) = 0
        _Fill ("Atlas fill", Range(0,1)) = .22
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface Surface Lambert addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        float _Cutoff,_Fill;
        struct Input { float2 uv_MainTex; };
        void Surface(Input input,inout SurfaceOutput output)
        {
            fixed4 color=tex2D(_MainTex,input.uv_MainTex);
            clip(color.a-_Cutoff);
            output.Albedo=color.rgb;
            output.Emission=color.rgb*_Fill;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
