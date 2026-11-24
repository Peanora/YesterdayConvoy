// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable

// DRAFT: translated only from identified GLES3 program mains.
// No gamma conversion, HDR encode/decode, exposure adjustment or substitute art.
#ifndef ORIGINAL_BASIC_GLES_COMMON_INCLUDED
#define ORIGINAL_BASIC_GLES_COMMON_INCLUDED

// The offline branch is solely a syntax harness. Unity must use UnityCG matrices.
#ifdef SOURCE_OFFLINE_FXC
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
// float3 _WorldSpaceCameraPos;
float4 _Time;
#else
#include "UnityCG.cginc"
#endif

// Production can define Shader_Time to its paused simulation clock before include.
// These three source families have no evaluated time-driven UV formula. Sprite
// frames are externally driven by _HGExternalCtrl, not by this clock in shader.
#ifndef Shader_Time
#define Shader_Time _Time
#endif

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _TintColor;
#if defined(SOURCE_ALPHA_BLEND) || defined(SOURCE_ADDITIVE)
float _MainTexChannel;
#endif
#if defined(SOURCE_ALPHA_BLEND)
float _PremultiplyAlpha;
#endif
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
float4 _HGSpriteSheetParam;
float4 _HGExternalCtrl;
#endif
#if defined(SOURCE_ALPHA_ZWRITE)
float _RimPower;
float _UseRim;
float4 _RimCol;
#endif

struct source_appdata
{
    float4 vertex : POSITION;
    float4 uv : TEXCOORD0;
    float4 color : COLOR0;
#if defined(SOURCE_ALPHA_ZWRITE)
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
#if defined(SOURCE_ALPHA_ZWRITE)
    float4 localPos : TEXCOORD2;
    float3 localNormal : TEXCOORD3;
#endif
};

// Source: channel == 0 is RGBA; 1/2/3 selects R/G/B; all other values select A.
// No rounding or saturating of the selector: the source uses exact equality.
float4 SourceBasicRGBAChannel(float4 rgba, float selector)
{
    float singleChannel = selector == 3.0 ? rgba.b : rgba.a;
    singleChannel = selector == 2.0 ? rgba.g : singleChannel;
    singleChannel = selector == 1.0 ? rgba.r : singleChannel;
    return selector == 0.0 ? rgba : singleChannel.xxxx;
}

source_v2f vert(source_appdata v)
{
    source_v2f o;
    // GLES source explicitly adds ObjectToWorld column 3, independent of POSITION.w.
    float4 world = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
    o.pos = mul(unity_MatrixVP, world);
    o.uv = v.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.color = v.color * _TintColor;
#if defined(SOURCE_ALPHA_ZWRITE)
    o.localPos = v.vertex;
    o.localNormal = v.normal;
#endif
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
    // Literal signed-fract / floor packed atlas formula from source programs 3/4.
    // Keep the negative-frame behavior; frac(frame/columns) alone would differ.
    float2 frameRatio = _HGExternalCtrl.xy / _HGSpriteSheetParam.xx;
    float2 absoluteFraction = frac(abs(frameRatio));
    float2 signedFraction;
    signedFraction.x = frameRatio.x >= -frameRatio.x ? absoluteFraction.x : -absoluteFraction.x;
    signedFraction.y = frameRatio.y >= -frameRatio.y ? absoluteFraction.y : -absoluteFraction.y;
    float2 rows = floor(frameRatio);
    float2 xPair = (signedFraction * _HGSpriteSheetParam.xx + o.uv.xx) * _HGSpriteSheetParam.zz;
    float2 yPair = -(rows + o.uv.yy) * _HGSpriteSheetParam.yy + float2(1.0, 1.0);
    o.packedFrameUV = float4(xPair.x, yPair.x, xPair.y, yPair.y);
#endif
    return o;
}

float4 SourceBasicSample(source_v2f i)
{
#if defined(HG_SPRITE_SHEET) || defined(HG_SPRITE_SHEET_LERP)
    // Source discards iff externalCtrl.w - 0.100000001 < 0; exact boundary passes.
    clip(_HGExternalCtrl.w - 0.100000001);
    float4 firstFrame = tex2D(_MainTex, i.packedFrameUV.xy);
#if defined(HG_SPRITE_SHEET_LERP)
    float4 secondFrame = tex2D(_MainTex, i.packedFrameUV.zw);
    // The source does not clamp the interpolation factor.
    return _HGExternalCtrl.zzzz * (secondFrame - firstFrame) + firstFrame;
#else
    return firstFrame;
#endif
#else
    return tex2D(_MainTex, i.uv);
#endif
}

float4 frag(source_v2f i) : SV_Target
{
    float4 sampled = SourceBasicSample(i);
#if defined(SOURCE_ALPHA_BLEND) || defined(SOURCE_ADDITIVE)
    sampled = SourceBasicRGBAChannel(sampled, _MainTexChannel);
#endif
    float4 colored = sampled * (i.color + i.color);
    float alpha = clamp(colored.a, 0.0, 1.0);
#if defined(SOURCE_ALPHA_BLEND)
    if (_PremultiplyAlpha < 1.5)
    {
        float3 premultiplied = colored.rgb * alpha;
        return float4(premultiplied + premultiplied, alpha);
    }
    return float4(colored.rgb, alpha);
#elif defined(SOURCE_ALPHA_ZWRITE)
    // Source rim uses the camera and normal in OBJECT space, not world space.
    float3 cameraLocal = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
    float3 toCamera = cameraLocal - i.localPos.xyz;
    toCamera *= rsqrt(dot(toCamera, toCamera));
    float3 normalLocal = i.localNormal * rsqrt(dot(i.localNormal, i.localNormal));
    float rimBase = 1.0 - clamp(dot(toCamera, normalLocal), 0.0, 1.0);
    // Preserve source log2 -> multiply -> exp2, including zero edge behavior.
    float rimFactor = exp2(log2(rimBase) * _RimPower);
    float3 rimmed = _RimCol.rgb * rimFactor.xxx + colored.rgb;
    return float4(_UseRim > 0.0 ? rimmed : colored.rgb, alpha);
#elif defined(SOURCE_ADDITIVE)
    return float4(colored.rgb, alpha);
#else
#error Select an actual source shader family.
#endif
}
#endif
