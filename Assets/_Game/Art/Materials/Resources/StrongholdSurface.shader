Shader "YesterdayConvoy/Stronghold Surface"
{
    Properties
    {
        _Color("Tint", Color)=(1,1,1,1)
        _MainTex("Surface", 2D)="white"{}
        _BumpMap("Normal", 2D)="bump"{}
        _BumpScale("Normal strength", Range(0,2))=0.65
        _Metallic("Metallic",Range(0,1))=0.45
        _Glossiness("Smoothness",Range(0,1))=0.25
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex,_BumpMap;
        fixed4 _Color;
        half _BumpScale,_Metallic,_Glossiness;
        struct Input {float2 uv_MainTex;float3 worldPos;};
        void surf(Input i,inout SurfaceOutputStandard o)
        {
            fixed3 color=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb;
            float grain=frac(sin(dot(floor(i.worldPos.xz*73),float2(12.9898,78.233)))*43758.5453);
            float patch=sin(i.worldPos.x*2.7+i.worldPos.z*3.1)*sin(i.worldPos.z*4.2);
            o.Albedo=color*(.96+grain*.06+patch*.035);
            o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_MainTex),_BumpScale);
            o.Metallic=_Metallic;o.Smoothness=_Glossiness*(.83+grain*.17);
            o.Occlusion=.94+grain*.06;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
