// External faithful source-math draft; not GPU validated.
Shader "YesterdayConvoyProbe/OriginalDissolveCustomData"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


[Toggle] _HGCustomVertexStream ("Use Custom Data", Float) = 0.0

[Enum(Default RGBA,0,R,1,G,2,B,3,A,4)] _MainTexChannel ("MainTex Channel", Float) = 0.0

_MainColor ("Main Color", Color) = (1,1,1,1)

_Opacity ("Opacity", Float) = 1.0

_MainTex ("Particle Texture", 2D) = "white" { }

_MainUSpeed ("Main U Speed", Float) = 0.0

_MainVSpeed ("Main V Speed", Float) = 0.0

[Enum(Off, 0, On, 4)] _ZTest ("ZTest", Float) = 4.0

[Enum(Add, 1, AlphaBlend, 10)] _DstBlend ("Alpha Blend Mode", Float) = 1.0

[Enum(Off,5,On,1)] _PremultiplyAlpha ("Premultiply Alpha", Float) = 5.0

[Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Cull Mode", Float) = 0.0

[Toggle] _UseDissolveTex ("Use Dissolve Texture", Float) = 0.0

[Enum(Default R,0,G,1,B,2,A,3)] _DissolvTexChannel ("DissolveTex Channel", Float) = 0.0

_DissolveTex ("DissolveTex", 2D) = "black" { }

_DissolveIntensity ("Dissolve Intensity", Range(0, 1)) = 0.0

_BorderWidth ("Border Width", Range(0.0001, 1)) = 0.10000000149011612

_DissolveUSpeed ("Dissolve U Speed", Float) = 0.0

_DissolveVSpeed ("Dissolve V Speed", Float) = 0.0

}
SubShader
{
    Tags { "IgnoreProjector"="true" "Queue"="Transparent" "RenderType"="Transparent" }
    Pass
    {
        Lighting Off
        ZClip On
        ZTest [_ZTest]
        ZWrite Off
        Cull [_CullMode]
        Offset 0, 0
        AlphaToMask Off
        Blend [_PremultiplyAlpha] [_DstBlend], Zero OneMinusSrcAlpha
        BlendOp Add, Add
        ColorMask RGBA
        Stencil
        {
            Ref 0
            ReadMask 255
            WriteMask 255
            Comp Always
            Pass Keep
            Fail Keep
            ZFail Keep
        }
        CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_local __ _HGCUSTOMVERTEXSTREAM_ON
        #include "OriginalDissolveCustomData.hlsl"
        ENDCG
    }
}
Fallback Off
}
