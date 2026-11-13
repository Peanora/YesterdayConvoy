// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable

// External source-formula draft. No Unity/GPU acceptance claimed.
#ifndef ORIGINAL_REMAINING_GLES_COMMON_INCLUDED
#define ORIGINAL_REMAINING_GLES_COMMON_INCLUDED
#ifdef SOURCE_OFFLINE_FXC
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
// float3 _WorldSpaceCameraPos;
float4 _Time;
#else
#include "UnityCG.cginc"
#endif

float4 _OriginalFxTime;
float _OriginalFxTimeEnabled;
float4 OriginalFxTimeValue()
{
    return _OriginalFxTimeEnabled > 0.5 ? _OriginalFxTime : _Time;
}
// None of these three actual source mains evaluates _Time. Keep this common
// production clock entry without inventing time-driven UV or rim animation.

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _TintColor;
#if defined(SOURCE_ZDISTORT_ALPHA)
float _BaselineZ;
#else
float _RimPower;
#endif
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
float4 _HGSpriteSheetParam;
float4 _HGExternalCtrl;
#endif

struct source_appdata
{
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
    float4 color : COLOR0;
#if defined(SOURCE_RIM_ALPHA) || defined(SOURCE_RIM_ADDITIVE)
    float3 normal : NORMAL;
#endif
};
struct source_v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
    float4 packedFrameUV : TEXCOORD1;
#endif
};

source_v2f vert(source_appdata v)
{
    source_v2f o;
    float4 worldW1 = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
#if defined(SOURCE_ZDISTORT_ALPHA)
    // Source programs 2/3/4 separately keep POSITION.w for depth deformation,
    // while the ordinary screen XY/W projection uses the source's fixed w=1.
    float4 worldOriginal = mul(unity_ObjectToWorld, v.vertex);
    float2 cameraYZ = _WorldSpaceCameraPos.yz - worldOriginal.yz;
    float denominator = cameraYZ.x < 0.0
        ? min(cameraYZ.x, -0.00100000005)
        : max(cameraYZ.x,  0.00100000005);
    float dz = worldOriginal.z - _BaselineZ;
    float2 slope = dz * float2(1.73205101, -2.30940199);
    float adjustedY = dz * 1.73205101 + worldOriginal.y;
    adjustedY += min(slope.y, 0.0);
    float adjustedZ = min(slope.x / denominator * cameraYZ.y + worldOriginal.z, _BaselineZ);
    float4 depthClip = mul(unity_MatrixVP, float4(worldOriginal.x, adjustedY, adjustedZ, worldOriginal.w));
    float ratio = depthClip.z / depthClip.w;
    float4 ordinaryClip = mul(unity_MatrixVP, worldW1);
    o.pos = float4(ordinaryClip.xy, ordinaryClip.w * ratio, ordinaryClip.w);
    o.color = v.color * _TintColor;
#elif defined(SOURCE_RIM_ALPHA) || defined(SOURCE_RIM_ADDITIVE)
    o.pos = mul(unity_MatrixVP, worldW1);
    // Original Rim Texture computes rim PER VERTEX, then interpolates its color.
    // It is not the fragment-rim algorithm of AlphaBlend(ZWrite).
    float3 cameraLocal = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
    float3 toCamera = cameraLocal - v.vertex.xyz;
    toCamera *= rsqrt(dot(toCamera, toCamera));
    float3 normalLocal = v.normal * rsqrt(dot(v.normal, v.normal));
    float rimBase = 1.0 - clamp(dot(toCamera, normalLocal), 0.0, 1.0);
    float rim = exp2(log2(rimBase) * _RimPower);
    o.color = rim.xxxx * _TintColor;
    // Exact source only multiplies vertex RGB. Vertex alpha is NOT multiplied.
    o.color.rgb *= v.color.rgb;
#else
#error Select an actual remaining source family.
#endif
    o.uv = v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
    float2 frameRatio = _HGExternalCtrl.xy / _HGSpriteSheetParam.xx;
    float2 absoluteFraction = frac(abs(frameRatio));
    float2 signedFraction;
    signedFraction.x = frameRatio.x >= -frameRatio.x ? absoluteFraction.x : -absoluteFraction.x;
    signedFraction.y = frameRatio.y >= -frameRatio.y ? absoluteFraction.y : -absoluteFraction.y;
    float2 rows = floor(frameRatio);
    float2 xPair = (signedFraction * _HGSpriteSheetParam.xx + o.uv.xx) * _HGSpriteSheetParam.zz;
    float2 yPair = -(rows + o.uv.yy) * _HGSpriteSheetParam.yy + float2(1.0,1.0);
    o.packedFrameUV = float4(xPair.x,yPair.x,xPair.y,yPair.y);
#endif
    return o;
}

float4 SourceRemainingSample(source_v2f i)
{
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
    clip(_HGExternalCtrl.w - 0.100000001);
    float4 first = tex2D(_MainTex, i.packedFrameUV.xy);
#if defined(HG_SPRITE_SHEET_LERP)
    float4 second = tex2D(_MainTex, i.packedFrameUV.zw);
    return _HGExternalCtrl.zzzz * (second - first) + first;
#else
    return first;
#endif
#else
    return tex2D(_MainTex, i.uv);
#endif
}
float4 frag(source_v2f i) : SV_Target
{
    float4 colored = SourceRemainingSample(i) * (i.color + i.color);
    return float4(colored.rgb, clamp(colored.a,0.0,1.0));
}
#endif
