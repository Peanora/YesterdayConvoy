// Identified source family: Torappu/Particles/Dissolve/Dissolve(CustomData)
// Source Shader PathID: 5417055218229175512
#include "OriginalDissolveGlesCommon.cginc"
#if !defined(_HGCUSTOMVERTEXSTREAM_ON)
// Literal source program 1: ordinary stream
float4 _MainTex_ST;
float _MainUSpeed;
float _MainVSpeed;
float4 _DissolveTex_ST;
float _DissolveUSpeed;
float _DissolveVSpeed;
float4 _MainColor;
float _MainTexChannel;
float _DissolvTexChannel;
float _Opacity;
float _UseDissolveTex;
float _DissolveIntensity;
float _BorderWidth;
float _PremultiplyAlpha;
sampler2D _MainTex;
sampler2D _DissolveTex;

struct source_appdata
{
    float4 in_POSITION0 : POSITION0;
    float4 in_TEXCOORD0 : TEXCOORD0;
    float4 in_COLOR0 : COLOR0;
};
struct source_v2f
{
    float4 position : SV_POSITION;
    float4 vs_TEXCOORD0 : TEXCOORD0;
    float4 vs_COLOR0 : COLOR0;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat1;
    float2 u_xlat16_2;

    o.position = SourceDissolveObjectToClipPos(v.in_POSITION0.xyz);
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    u_xlat0.xy = OriginalSourceTime().yy * SourceVec2(_MainUSpeed, _MainVSpeed) + u_xlat16_2.xy;
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    u_xlat0.zw = OriginalSourceTime().yy * SourceVec2(_DissolveUSpeed, _DissolveVSpeed) + u_xlat16_2.xy;
    o.vs_TEXCOORD0 = u_xlat0;
    o.vs_COLOR0.xyz = v.in_COLOR0.xyz * _MainColor.xyz;
    o.vs_COLOR0.w = v.in_COLOR0.w;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;
    bool4 u_xlatb1;
    float u_xlat16_2;
    bool3 u_xlatb3;
    float3 u_xlat16_4;
    float3 u_xlat16_7;

    u_xlat0 = tex2D(_MainTex, i.vs_TEXCOORD0.xy);
    u_xlatb1 = SourceEqual4(SourceVec4(_MainTexChannel), SourceVec4(0.0, 1.0, 2.0, 3.0));
    u_xlat16_2 = (u_xlatb1.w) ? u_xlat0.z : u_xlat0.w;
    u_xlat16_2 = (u_xlatb1.z) ? u_xlat0.y : u_xlat16_2;
    u_xlat16_2 = (u_xlatb1.y) ? u_xlat0.x : u_xlat16_2;
    u_xlat16_0 = (u_xlatb1.x) ? u_xlat0 : SourceVec4(u_xlat16_2);
    u_xlat1 = tex2D(_DissolveTex, i.vs_TEXCOORD0.zw);
    u_xlatb3.xyz = SourceEqual4(SourceVec4(SourceVec4(_DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel)), SourceVec4(0.0, 1.0, 2.0, 0.0)).xyz;
    u_xlat16_2 = (u_xlatb3.z) ? u_xlat1.z : u_xlat1.w;
    u_xlat16_2 = (u_xlatb3.y) ? u_xlat1.y : u_xlat16_2;
    u_xlat16_2 = (u_xlatb3.x) ? u_xlat1.x : u_xlat16_2;
    u_xlat16_2 = u_xlat16_2 + (-_DissolveIntensity);
    u_xlat16_7.x = _DissolveIntensity + 0.5;
    u_xlat16_7.x = SourceRoundEven(u_xlat16_7.x);
    u_xlat16_7.x = (-u_xlat16_7.x) + 1.0;
    u_xlat16_2 = _BorderWidth * u_xlat16_7.x + u_xlat16_2;
    u_xlat16_2 = u_xlat16_2 / _BorderWidth;
    u_xlat16_2 = clamp(u_xlat16_2, 0.0, 1.0);
    u_xlat16_7.x = u_xlat16_0.w * _MainColor.w;
    u_xlat16_7.x = u_xlat16_7.x * i.vs_COLOR0.w;
    u_xlat16_7.x = dot(u_xlat16_7.xx, SourceVec2(SourceVec2(_Opacity, _Opacity)));
    u_xlat16_2 = u_xlat16_2 * u_xlat16_7.x + (-u_xlat16_7.x);
    u_xlat16_2 = _UseDissolveTex * u_xlat16_2 + u_xlat16_7.x;
    u_xlat16_2 = clamp(u_xlat16_2, 0.0, 1.0);
    u_xlat16_7.xyz = u_xlat16_0.xyz * i.vs_COLOR0.xyz;
    u_xlat16_7.xyz = u_xlat16_7.xyz + u_xlat16_7.xyz;
    u_xlatb1.x = _PremultiplyAlpha<1.5;
    if(u_xlatb1.x){
        u_xlat16_4.xyz = SourceVec3(u_xlat16_2) * u_xlat16_7.xyz;
        SV_Target0.xyz = u_xlat16_4.xyz + u_xlat16_4.xyz;
        SV_Target0.w = u_xlat16_2;
        return SV_Target0;
    }
    SV_Target0.w = u_xlat16_2;
    SV_Target0.xyz = u_xlat16_7.xyz;
    return SV_Target0;
}
#else
// Literal source program 2: _HGCUSTOMVERTEXSTREAM_ON
float4 _MainTex_ST;
float _MainUSpeed;
float _MainVSpeed;
float4 _DissolveTex_ST;
float _DissolveUSpeed;
float _DissolveVSpeed;
float4 _MainColor;
float _MainTexChannel;
float _DissolvTexChannel;
float _Opacity;
float _UseDissolveTex;
float _DissolveIntensity;
float _BorderWidth;
float _PremultiplyAlpha;
sampler2D _MainTex;
sampler2D _DissolveTex;

struct source_appdata
{
    float4 in_POSITION0 : POSITION0;
    float4 in_TEXCOORD0 : TEXCOORD0;
    float4 in_COLOR0 : COLOR0;
    float4 in_TEXCOORD1 : TEXCOORD1;
};
struct source_v2f
{
    float4 position : SV_POSITION;
    float4 vs_TEXCOORD0 : TEXCOORD0;
    float4 vs_COLOR0 : COLOR0;
    float2 vs_TEXCOORD1 : TEXCOORD1;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat1;
    float2 u_xlat16_2;

    o.position = SourceDissolveObjectToClipPos(v.in_POSITION0.xyz);
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    u_xlat16_2.xy = u_xlat16_2.xy + v.in_TEXCOORD0.zw;
    u_xlat0.xy = OriginalSourceTime().yy * SourceVec2(_MainUSpeed, _MainVSpeed) + u_xlat16_2.xy;
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    u_xlat16_2.xy = u_xlat16_2.xy + v.in_TEXCOORD1.xy;
    u_xlat0.zw = OriginalSourceTime().yy * SourceVec2(_DissolveUSpeed, _DissolveVSpeed) + u_xlat16_2.xy;
    o.vs_TEXCOORD0 = u_xlat0;
    o.vs_COLOR0.xyz = v.in_COLOR0.xyz * _MainColor.xyz;
    o.vs_COLOR0.w = v.in_COLOR0.w;
    o.vs_TEXCOORD1.xy = v.in_TEXCOORD1.zw;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;
    bool4 u_xlatb1;
    float u_xlat16_2;
    bool3 u_xlatb3;
    float3 u_xlat16_4;
    float3 u_xlat16_7;
    float u_xlat16_12;

    u_xlat0 = tex2D(_MainTex, i.vs_TEXCOORD0.xy);
    u_xlatb1 = SourceEqual4(SourceVec4(_MainTexChannel), SourceVec4(0.0, 1.0, 2.0, 3.0));
    u_xlat16_2 = (u_xlatb1.w) ? u_xlat0.z : u_xlat0.w;
    u_xlat16_2 = (u_xlatb1.z) ? u_xlat0.y : u_xlat16_2;
    u_xlat16_2 = (u_xlatb1.y) ? u_xlat0.x : u_xlat16_2;
    u_xlat16_0 = (u_xlatb1.x) ? u_xlat0 : SourceVec4(u_xlat16_2);
    u_xlat1 = tex2D(_DissolveTex, i.vs_TEXCOORD0.zw);
    u_xlatb3.xyz = SourceEqual4(SourceVec4(SourceVec4(_DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel)), SourceVec4(0.0, 1.0, 2.0, 0.0)).xyz;
    u_xlat16_2 = (u_xlatb3.z) ? u_xlat1.z : u_xlat1.w;
    u_xlat16_2 = (u_xlatb3.y) ? u_xlat1.y : u_xlat16_2;
    u_xlat16_2 = (u_xlatb3.x) ? u_xlat1.x : u_xlat16_2;
    u_xlat16_7.xy = i.vs_TEXCOORD1.yx + SourceVec2(_BorderWidth, _DissolveIntensity);
    u_xlat16_7.x = u_xlat16_7.x;
    u_xlat16_7.x = clamp(u_xlat16_7.x, 0.0, 1.0);
    u_xlat16_2 = (-u_xlat16_7.y) + u_xlat16_2;
    u_xlat16_12 = u_xlat16_7.y + 0.5;
    u_xlat16_12 = SourceRoundEven(u_xlat16_12);
    u_xlat16_12 = (-u_xlat16_12) + 1.0;
    u_xlat16_2 = u_xlat16_7.x * u_xlat16_12 + u_xlat16_2;
    u_xlat16_2 = u_xlat16_2 / u_xlat16_7.x;
    u_xlat16_2 = clamp(u_xlat16_2, 0.0, 1.0);
    u_xlat16_7.x = u_xlat16_0.w * _MainColor.w;
    u_xlat16_7.x = u_xlat16_7.x * i.vs_COLOR0.w;
    u_xlat16_7.x = dot(u_xlat16_7.xx, SourceVec2(SourceVec2(_Opacity, _Opacity)));
    u_xlat16_2 = u_xlat16_2 * u_xlat16_7.x + (-u_xlat16_7.x);
    u_xlat16_2 = _UseDissolveTex * u_xlat16_2 + u_xlat16_7.x;
    u_xlat16_2 = clamp(u_xlat16_2, 0.0, 1.0);
    u_xlat16_7.xyz = u_xlat16_0.xyz * i.vs_COLOR0.xyz;
    u_xlat16_7.xyz = u_xlat16_7.xyz + u_xlat16_7.xyz;
    u_xlatb1.x = _PremultiplyAlpha<1.5;
    if(u_xlatb1.x){
        u_xlat16_4.xyz = SourceVec3(u_xlat16_2) * u_xlat16_7.xyz;
        SV_Target0.xyz = u_xlat16_4.xyz + u_xlat16_4.xyz;
        SV_Target0.w = u_xlat16_2;
        return SV_Target0;
    }
    SV_Target0.w = u_xlat16_2;
    SV_Target0.xyz = u_xlat16_7.xyz;
    return SV_Target0;
}
#endif
