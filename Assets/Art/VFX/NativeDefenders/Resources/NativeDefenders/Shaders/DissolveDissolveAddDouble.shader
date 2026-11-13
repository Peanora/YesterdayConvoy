// External faithful source-math draft; not GPU validated.
Shader "YesterdayConvoyProbe/OriginalDissolveAddDouble"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)

_MainTex ("MainTex", 2D) = "white" { }

_DissolveTex_01 ("DissolveTex", 2D) = "white" { }

_DissolveTex_02 ("DissolveTex", 2D) = "white" { }

_Amount_01 ("Amount_01", Range(0, 1)) = 0.5

_BorderWidth_01 ("Border Width_01", Range(0.0001, 1)) = 0.10000000149011612

_Amount_02 ("Amount_02", Range(0, 1)) = 0.5

_BorderWidth_02 ("Border Width_02", Range(0.0001, 1)) = 0.10000000149011612

[Enum(Off, 0, On, 4)] _ZTest ("ZTest", Float) = 4.0

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
        Cull Off
        Offset 0, 0
        AlphaToMask Off
        Blend SrcAlpha One, Zero OneMinusSrcAlpha
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
        #include "OriginalDissolveAddDouble.hlsl"
        ENDCG
    }
}
Fallback Off
}
