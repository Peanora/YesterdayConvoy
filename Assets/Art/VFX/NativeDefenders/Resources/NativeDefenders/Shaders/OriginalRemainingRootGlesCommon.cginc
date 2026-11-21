// External draft: shared language adapters, not a substitute effect formula.
#ifndef ORIGINAL_REMAINING_ROOT_GLES_COMMON_INCLUDED
#define ORIGINAL_REMAINING_ROOT_GLES_COMMON_INCLUDED
#ifdef SOURCE_OFFLINE_FXC
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Time;
float4 SourceRootObjectToClipPos(float3 p)
{
    return mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(p, 1.0)));
}
#else
#include "UnityCG.cginc"
float4 SourceRootObjectToClipPos(float3 p)
{
    return UnityObjectToClipPos(p);
}
#endif

// Production provides (t/20, t, 2t, 3t) from the shared simulation clock.
// Probe defaults to Unity time until explicitly enabled; no time warp/UV change.
float4 _OriginalFxTime;
float _OriginalFxTimeEnabled;
float4 OriginalSourceTime()
{
    return _OriginalFxTimeEnabled > 0.5 ? _OriginalFxTime : _Time;
}

// GLSL constructors allow scalar splats. Explicit overloads preserve that rule.
float2 SourceVec2(float x) { return x.xx; }
float2 SourceVec2(float2 x) { return x; }
float2 SourceVec2(float x, float y) { return float2(x, y); }
float3 SourceVec3(float x) { return x.xxx; }
float3 SourceVec3(float3 x) { return x; }
float3 SourceVec3(float x, float y, float z) { return float3(x, y, z); }
float4 SourceVec4(float x) { return x.xxxx; }
float4 SourceVec4(float4 x) { return x; }
float4 SourceVec4(float x, float y, float z, float w) { return float4(x, y, z, w); }
bool4 SourceEqual4(float4 a, float4 b) { return a == b; }
bool4 SourceGreaterEqual4(float4 a, float4 b) { return a >= b; }

// Explicit GLSL roundEven, including Amount=0/1 half ties. No epsilon change.
float SourceRoundEven(float x)
{
    float lo = floor(x), fraction = x - lo;
    return fraction < 0.5 ? lo : fraction > 0.5 ? lo + 1.0 : lo + fmod(abs(lo), 2.0);
}
float2 SourceRoundEven(float2 x)
{
    return float2(SourceRoundEven(x.x), SourceRoundEven(x.y));
}
#endif
