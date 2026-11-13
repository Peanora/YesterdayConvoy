// Private external compatibility probe. Literal source GLES3 program1 math.
// No source artwork repaint, bloom, exposure, HDR codec or gamma adjustment.
// Original no-custom program1 and exact custom-stream program2 are supported.
Shader "YesterdayConvoyProbe/OriginalRamDisturb"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


[Toggle] _HGCustomVertexStream ("Use Custom Data", Float) = 0.0

_MainColor ("MainCol", Color) = (0.5,0.5,0.5,0.5)

_Opacity ("Opacity", Float) = 1.0

[Enum(Default RGBA,0,R,1,G,2,B,3,A,4)] _MainTexChannel ("MainTex Channel", Float) = 0.0

_MainTex ("Maintex", 2D) = "white" { }

_MainUSpeed ("Main U Speed", Float) = 0.0

_MainVSpeed ("Main V Speed", Float) = 0.0

[Enum(Off, 0, On, 4)] _ZTest ("ZTest", Float) = 4.0

[Enum(Add, 1, AlphaBlend, 10)] _DstBlend ("Alpha Blend Mode", Float) = 1.0

[Enum(Off,5,On,1)] _PremultiplyAlpha ("Premultiply Alpha", Float) = 5.0

[Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Cull Mode", Float) = 0.0

[Header(Dissolve)] _BorderWidth ("Border Width", Range(0.0001, 1)) = 0.10000000149011612

[Enum(Default R,0,G,1,B,2,A,3)] _DissolvTexChannel ("DissolveTex Channel", Float) = 0.0

_DissolveTex ("DissolveTex", 2D) = "white" { }

_Amount ("Amount", Range(0, 1)) = 0.5

_DissolveUSpeed ("Dissolve U Speed", Float) = 0.0

_DissolveVSpeed ("Dissolve V Speed", Float) = 0.0

[Header(Ram)] [Enum(Default RGBA,0,R,1,G,2,B,3,A,4)] _RamTexChannel ("RamTex Channel", Float) = 0.0

_RamTex ("Ram Texture", 2D) = "white" { }

[Header(Disturb)] [Toggle] _DisturbInfluenceDissolveUV ("Disturb influence Dissolve(UV)", Float) = 0.0

_DisturbInfluenceMainUV ("Disturb influence Main(UV)", Range(0, 1)) = 1.0

[Enum(Default R,0,G,1,B,2,A,3)] _DisturbTexChannel ("DisturbTex Channel", Float) = 0.0

_DisturbTex ("Disturb Texture", 2D) = "black" { }

_IntensityU ("Disturb Intensity U", Range(0, 2)) = 0.0

_IntensityV ("Disturb intensity V", Range(0, 2)) = 0.0

_DisturbUSpeed ("Disturb U Speed", Float) = 0.0

_DisturbVSpeed ("Disturb V Speed", Float) = 0.0

    [HideInInspector] _OriginalFxTime ("Explicit simulation time", Vector) = (0,0,0,0)
    [HideInInspector] _OriginalFxTimeEnabled ("Use explicit simulation time", Float) = 0
}
SubShader
{
    Tags { "IgnoreProjector"="true" "Queue"="Transparent" "RenderType"="Transparent" }
    Pass
    {
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
        Cull [_CullMode]
        // Parsed original render state, not the lossy .txt export's ZTest Off.
        Blend [_PremultiplyAlpha] [_DstBlend], Zero OneMinusSrcAlpha
        BlendOp Add
        ColorMask RGBA
        CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_local __ _HGCUSTOMVERTEXSTREAM_ON
        #include "OriginalTrailGlesCommon.cginc"
float4 _RamTex_ST;
sampler2D _RamTex;
float _RamTexChannel, _DisturbInfluenceMainUV;
source_v2f vert(source_appdata v) { return SourceVertexBase(v, _RamTex_ST); }
float4 frag(source_v2f i) : SV_Target
{
    float amount = i.customData.x + _Amount;
    float2 intensity = i.customData.yy + float2(_IntensityU, _IntensityV);
    float4 ram = SourceRGBAChannel(tex2D(_RamTex, i.ramOrWeightDisturbUV.xy), _RamTexChannel);
    float disturbance = SourceScalarChannel(tex2D(_DisturbTex, i.ramOrWeightDisturbUV.zw), _DisturbTexChannel);
    float2 offset = intensity * disturbance;
    float2 mainUV = i.mainDissolveUV.xy + offset * _DisturbInfluenceMainUV;
    float2 dissolveUV = i.mainDissolveUV.zw + offset * _DisturbInfluenceDissolveUV;
    float4 main = SourceRGBAChannel(tex2D(_MainTex, mainUV), _MainTexChannel);
    float4 composed = ram * (main * _MainColor * i.color);
    float dissolve = SourceScalarChannel(tex2D(_DissolveTex, dissolveUV), _DissolvTexChannel);
    return SourceFinalColor(composed, SourceDissolveAlpha(composed, dissolve, amount));
}

        ENDCG
    }
}
Fallback Off
}
