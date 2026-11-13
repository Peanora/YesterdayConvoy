// EXTERNAL DRAFT: source-formula port; not Unity imported or GPU verified.
Shader "YesterdayConvoyProbe/OriginalAlphaBlendZWrite"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)

_MainTex ("Particle Texture", 2D) = "white" { }

[Toggle] _UseRim ("use rim", Range(0, 1)) = 0.0

_RimPower ("Rim Power", Range(0.5, 10)) = 3.0

_RimCol ("Rim Color", Color) = (1,1,1,1)

[Enum(Off, 0, On, 4)] _ZTest ("ZTest", Float) = 4.0

[Enum(Off, 0, On, 4)] _ZWrite ("ZWrite", Float) = 0.0

[Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Cull Mode", Float) = 0.0

}
SubShader
{
    Tags { "IGNOREPROJECTOR"="true" "QUEUE"="Transparent" "RenderType"="Transparent" }
    Pass
    {
        Lighting Off
        ZClip On
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Cull [_CullMode]
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
        #include "OriginalAlphaBlendZWrite.hlsl"
        ENDCG
    }
}
Fallback Off
}
