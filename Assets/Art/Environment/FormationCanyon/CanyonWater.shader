Shader "YesterdayConvoy/Formation Canyon Water"
{
    Properties
    {
        _MainTex ("Original riverbed atlas", 2D) = "white" {}
        _BumpMap ("Original riverbed normal", 2D) = "bump" {}
        _FlowTime ("Unpaused map time", Float) = 0
        _Tint ("Water color", Color) = (.055,.33,.30,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface Surface Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap;
        float _FlowTime;
        fixed4 _Tint;
        struct Input { float3 worldPos; };
        void Surface(Input input,inout SurfaceOutputStandard output)
        {
            float2 coord=input.worldPos.xz;
            float2 flow=_FlowTime*float2(.10,.07);
            float3 bed=tex2D(_MainTex,coord*.27).rgb;
            float3 wave=UnpackNormal(tex2D(_BumpMap,coord*.54 + flow));
            float3 detail=UnpackNormal(tex2D(_BumpMap,coord*.91 - flow*.59));
            float glint=pow(saturate(.54+wave.x*.58+detail.y*.37),6);
            float3 color=lerp(bed,_Tint.rgb,.65);
            output.Albedo=color;
            output.Normal=normalize(float3((wave.xy+detail.xy*.45)*.32,1));
            output.Smoothness=.41;
            output.Emission=color*.23 + glint*float3(.11,.22,.19);
        }
        ENDCG
    }
    Fallback "Diffuse"
}
