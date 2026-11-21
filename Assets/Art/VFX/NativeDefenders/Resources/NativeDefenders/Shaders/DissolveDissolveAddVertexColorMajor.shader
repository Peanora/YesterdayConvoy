// External faithful source-math draft; not GPU validated.
Shader "YesterdayConvoyProbe/OriginalDissolveAddVertexColorMajor"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)

_MainTex ("MainTex", 2D) = "white" { }

_DissolveTex ("DissolveTex", 2D) = "white" { }

_BorderWidth ("Border Width", Range(0.0001, 1)) = 0.10000000149011612

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
        #include "OriginalDissolveAddVertexColorMajor.hlsl"
        ENDCG
    }
}
Fallback Off
}
