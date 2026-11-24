// EXTERNAL DRAFT: source-formula port; not Unity imported or GPU verified.
Shader "YesterdayConvoyProbe/OriginalRimAdditive"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


_TintColor ("Tint Color", Color) = (1,0,0,1)

_MainTex ("Particle Texture", 2D) = "white" { }

_RimPower ("Rim Power", Range(0.5, 10)) = 3.0

[Enum(Off, 0, On, 4)] _ZTest ("ZTest", Float) = 4.0

}
SubShader
{
    Tags { "IGNOREPROJECTOR"="true" "QUEUE"="Transparent" "RenderType"="Transparent" }
    Pass
    {
        Lighting Off
        ZClip On
        ZTest [_ZTest]
        ZWrite Off
        Cull Back
        Offset 0, 0
        AlphaToMask Off
        Blend SrcAlpha One, Zero OneMinusSrcAlpha
        BlendOp Add, Add
        ColorMask RGBA
        Stencil { Ref 0 ReadMask 255 WriteMask 255 Comp Always Pass Keep Fail Keep ZFail Keep }
        CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #include "OriginalRimAdditive.hlsl"
        ENDCG
    }
}
Fallback Off
}
