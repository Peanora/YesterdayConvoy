// External faithful source-math draft; not GPU validated.
Shader "YesterdayConvoyProbe/OriginalDisturbAnchor"
{
Properties {
[HideInInspector] _OriginalFxTime ("Simulation Time", Vector) = (0,0,0,0)
[HideInInspector] _OriginalFxTimeEnabled ("Use Simulation Time", Float) = 0


_MainTex ("Main Texture", 2D) = "white" { }

[Header(UVTween)] _UVTween ("UV Tween Main(xy), DisturTex(zw)", Vector) = (0,0,0,0)

_MainColor ("Main Color", Color) = (0.5,0.5,0.5,0.5)

_MainColorACtrl ("Main A atten Ctrl", Range(0, 1)) = 1.0

[Toggle] _Weight ("Use Weight Tex", Float) = 0.0

_WeightTex ("Weight Texture", 2D) = "white" { }

[Header(Disturb1)] _DisturTex ("Disturb Texture", 2D) = "black" { }

_IntensityU ("Disturb Intensity U", Range(0, 2)) = 0.0

_AnchorU ("Disturb U center", Range(0, 1)) = 0.5

_IntensityV ("Disturb intensity V", Range(0, 2)) = 0.0

_AnchorV ("Disturb V center", Range(0, 1)) = 0.5

[Header(Disturb2)] [Toggle] _ToggleUseDisturb2 ("Use disturb 2", Float) = 0.0

_DisturTex_02 ("Disturb Texture_02", 2D) = "black" { }

_IntensityU_02 ("Disturb Intensity U", Range(0, 2)) = 0.0

_AnchorU_02 ("Disturb U center", Range(0, 1)) = 0.5

_IntensityV_02 ("Disturb intensity V", Range(0, 2)) = 0.0

_AnchorV_02 ("Disturb V center", Range(0, 1)) = 0.5

[Header(Dissolve)] [Toggle] _ToggleUseDissolve ("Use Dissolve", Float) = 0.0

_DissolveTex ("DissolveTex", 2D) = "white" { }

_Amount ("Amount", Range(0, 1)) = 0.5

_BorderWidth ("Border Width", Range(0.0001, 1)) = 0.10000000149011612

[Enum(One, 1, SrcAlpha, 5)] _SrcBlend ("SrcBlend", Float) = 1.0

[Enum(One, 1, OneMinusSrcAlpha, 10)] _DstBlend ("DstBlend", Float) = 1.0

[Enum(Off, 0, LEqual, 4)] _ZTest ("ZTest", Float) = 4.0

[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("CullMode", Float) = 2.0

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
        Cull [_CullMode]
        Offset 0, 0
        AlphaToMask Off
        Blend [_SrcBlend] [_DstBlend], Zero OneMinusSrcAlpha
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
        #pragma multi_compile_local __ _WEIGHT_ON
        #include "OriginalDisturbAnchor.hlsl"
        ENDCG
    }
}
Fallback Off
}
