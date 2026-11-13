// Identified source family: Torappu/Particles/Dissolve/Dissolve AB
// Source Shader PathID: 422379319589426989
#include "OriginalDissolveGlesCommon.cginc"
// Literal source program 1: ordinary stream
float4 _MainTex_ST;
float4 _DissolveTex_ST;
float4 _TintColor;
float _Amount;
float _BorderWidth;
sampler2D _DissolveTex;
sampler2D _MainTex;
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

    o.position = SourceDissolveObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.vs_TEXCOORD0.zw = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    o.vs_COLOR0 = v.in_COLOR0 * _TintColor;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float u_xlat16_0;
    float4 u_xlat1;
    float u_xlat16_1;
    float4 u_xlat16_2;
    float u_xlat16_3;

    u_xlat16_0 = _Amount + 0.5;
    u_xlat16_0 = SourceRoundEven(u_xlat16_0);
    u_xlat16_0 = (-u_xlat16_0) + 1.0;
    u_xlat16_1 = tex2D(_DissolveTex, i.vs_TEXCOORD0.zw).x;
    u_xlat16_3 = u_xlat16_1 + (-_Amount);
    u_xlat16_0 = _BorderWidth * u_xlat16_0 + u_xlat16_3;
    u_xlat16_0 = u_xlat16_0 / _BorderWidth;
    u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
    u_xlat1 = i.vs_COLOR0 + i.vs_COLOR0;
    u_xlat16_2 = tex2D(_MainTex, i.vs_TEXCOORD0.xy);
    u_xlat1 = u_xlat1 * u_xlat16_2;
    SV_Target0.w = u_xlat16_0 * u_xlat1.w;
    SV_Target0.w = clamp(SV_Target0.w, 0.0, 1.0);
    SV_Target0.xyz = u_xlat1.xyz;
    return SV_Target0;
}
