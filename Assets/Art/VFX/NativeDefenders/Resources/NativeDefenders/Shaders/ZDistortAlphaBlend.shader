// EXTERNAL DRAFT: source-formula port; not Unity imported or GPU verified.
Shader "YesterdayConvoyProbe/OriginalZDistortAlphaBlend"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)

_MainTex ("Particle Texture", 2D) = "white" { }

[Enum(Off, 0, On, 4)] _ZTest ("ZTest", Float) = 4.0

_BaselineZ ("Baseline of Z axis", Float) = 0.0

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
        Cull Off
        Offset 0, 0
        AlphaToMask Off
        Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
        BlendOp Add, Add
        ColorMask RGBA
        Stencil { Ref 0 ReadMask 255 WriteMask 255 Comp Always Pass Keep Fail Keep ZFail Keep }
        CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile __ HG_SPRITE_SHEET HG_SPRITE_SHEET_LERP
        #include "OriginalZDistortAlphaBlend.hlsl"
        ENDCG
    }
}
Fallback Off
}
