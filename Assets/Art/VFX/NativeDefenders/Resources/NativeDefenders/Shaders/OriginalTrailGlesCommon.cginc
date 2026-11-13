// Direct translation of source GLES3 program1 and program2 custom stream.
// Preserve source UV ST -> optional packed rotation -> frac(speed * Time.y).
#include "UnityCG.cginc"
float _HG_UV_ROTATION;
float4 _Rotation0, _Rotation1, _Rotation2, _Rotation3;
float4 _MainTex_ST, _DissolveTex_ST, _DisturbTex_ST;
float _MainUSpeed, _MainVSpeed, _DissolveUSpeed, _DissolveVSpeed, _DisturbUSpeed, _DisturbVSpeed;
float _MainTexChannel, _DisturbTexChannel, _DissolvTexChannel;
float4 _MainColor;
float _Opacity, _PremultiplyAlpha, _DisturbInfluenceDissolveUV;
float _Amount, _BorderWidth, _IntensityU, _IntensityV;
float4 _OriginalFxTime;
float _OriginalFxTimeEnabled;
sampler2D _MainTex, _DissolveTex, _DisturbTex;

struct source_appdata
{
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
    float4 color : COLOR;
    #if defined(_HGCUSTOMVERTEXSTREAM_ON)
        float4 customUVAndControls : TEXCOORD1;
        float2 customDisturbUV : TEXCOORD2;
    #endif
};
struct source_v2f
{
    float4 position : SV_POSITION;
    float4 mainDissolveUV : TEXCOORD0;
    float4 ramOrWeightDisturbUV : TEXCOORD1;
    float2 customData : TEXCOORD2;
    float4 color : COLOR0;
};
float2 SourceRotateUV(float2 uv, float4 packed)
{
    float2 centered = uv - 0.5;
    float2 rotated = float2(dot(centered, packed.xz), dot(centered, packed.yw)) + 0.5;
    return _HG_UV_ROTATION > 0.5 ? rotated : uv;
}
source_v2f SourceVertexBase(source_appdata v, float4 ramOrWeightST)
{
    source_v2f o;
    float4 sourceTime = _OriginalFxTimeEnabled > 0.5 ? _OriginalFxTime : _Time;
    // Source expands ObjectToWorld then MatrixVP, with implicit position.w=1.
    o.position = UnityObjectToClipPos(v.vertex.xyz);
    #if defined(_HGCUSTOMVERTEXSTREAM_ON)
        // Exact source program2 input mapping; custom offsets precede rotation.
        o.mainDissolveUV.xy = SourceRotateUV(v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw + v.uv.zw, _Rotation0)
            + frac(float2(_MainUSpeed, _MainVSpeed) * sourceTime.y);
        o.mainDissolveUV.zw = SourceRotateUV(v.uv.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw + v.customUVAndControls.xy, _Rotation1)
            + frac(float2(_DissolveUSpeed, _DissolveVSpeed) * sourceTime.y);
        o.ramOrWeightDisturbUV.zw = SourceRotateUV(v.uv.xy * _DisturbTex_ST.xy + _DisturbTex_ST.zw + v.customDisturbUV.xy, _Rotation3)
            + frac(float2(_DisturbUSpeed, _DisturbVSpeed) * sourceTime.y);
        o.customData = v.customUVAndControls.zw;
    #else
        // Keep the already-compared program1 expression/order unchanged.
        o.mainDissolveUV.xy = SourceRotateUV(v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw, _Rotation0)
            + frac(float2(_MainUSpeed, _MainVSpeed) * sourceTime.y);
        o.mainDissolveUV.zw = SourceRotateUV(v.uv.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw, _Rotation1)
            + frac(float2(_DissolveUSpeed, _DissolveVSpeed) * sourceTime.y);
        o.ramOrWeightDisturbUV.zw = SourceRotateUV(v.uv.xy * _DisturbTex_ST.xy + _DisturbTex_ST.zw, _Rotation3)
            + frac(float2(_DisturbUSpeed, _DisturbVSpeed) * sourceTime.y);
        o.customData = float2(0, 0);
    #endif
    o.ramOrWeightDisturbUV.xy = SourceRotateUV(v.uv.xy * ramOrWeightST.xy + ramOrWeightST.zw, _Rotation2);
    o.color = v.color;
    return o;
}
float4 SourceRGBAChannel(float4 c, float channel)
{
    // Source compares floating enum values exactly. Do not round the enum.
    float scalar = channel == 1 ? c.r : channel == 2 ? c.g : channel == 3 ? c.b : c.a;
    return channel == 0 ? c : scalar.xxxx;
}
float SourceScalarChannel(float4 c, float channel)
{
    return channel == 0 ? c.r : channel == 1 ? c.g : channel == 2 ? c.b : c.a;
}
float SourceRoundEven(float value)
{
    // GLSL roundEven, including the important Amount=0 and Amount=1 ties.
    // Explicit implementation avoids relying on a backend's round tie rule.
    float lo = floor(value), fraction = value - lo;
    return fraction < 0.5 ? lo : fraction > 0.5 ? lo + 1 : lo + fmod(abs(lo), 2.0);
}
float SourceDissolveAlpha(float4 composedColor, float dissolve, float amount)
{
    float edge = _BorderWidth * (1.0 - SourceRoundEven(amount + 0.5)) + dissolve - amount;
    float coverage = clamp(edge / _BorderWidth, 0.0, 1.0);
    // Literal source dot(composedColor.aa, float2(Opacity)): factor two belongs
    // to the original shader. It is not an added exposure/intensity adjustment.
    return clamp(coverage * (composedColor.a * _Opacity + composedColor.a * _Opacity), 0.0, 1.0);
}
float4 SourceFinalColor(float4 composedColor, float alpha)
{
    float3 rgb = composedColor.rgb * 2.0;
    if (_PremultiplyAlpha < 1.5)
    {
        float3 premultiplied = alpha * rgb;
        rgb = premultiplied + premultiplied;
    }
    return float4(rgb, alpha);
}
