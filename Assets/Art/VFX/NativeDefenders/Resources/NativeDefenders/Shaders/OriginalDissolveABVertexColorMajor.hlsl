// Identified source family: Torappu/Particles/Dissolve/Dissolve AB (VertexColor Major)
// Source Shader PathID: 929643023465914681
#include "OriginalDissolveGlesCommon.cginc"
// Literal source program 1: ordinary stream
float4 _MainTex_ST;
float4 _DissolveTex_ST;
float4 _TintColor;
float _BorderWidth;
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

    o.position = SourceDissolveObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.vs_TEXCOORD0.zw = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    o.vs_COLOR0 = v.in_COLOR0;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float3 u_xlat0;
    float4 u_xlat16_0;
    float u_xlat16_1;
    float3 u_xlat2;

    u_xlat16_0.x = tex2D(_DissolveTex, i.vs_TEXCOORD0.zw).x;
    u_xlat16_1 = (-i.vs_COLOR0.w) + 1.0;
    u_xlat16_1 = u_xlat16_0.x + (-u_xlat16_1);
    u_xlat16_1 = i.vs_COLOR0.w * _BorderWidth + u_xlat16_1;
    u_xlat16_1 = u_xlat16_1 / _BorderWidth;
    u_xlat16_1 = clamp(u_xlat16_1, 0.0, 1.0);
    u_xlat16_0 = tex2D(_MainTex, i.vs_TEXCOORD0.xy);
    u_xlat16_1 = u_xlat16_0.w * u_xlat16_1;
    SV_Target0.w = u_xlat16_1 * _TintColor.w;
    SV_Target0.w = clamp(SV_Target0.w, 0.0, 1.0);
    u_xlat2.xyz = i.vs_COLOR0.xyz + i.vs_COLOR0.xyz;
    u_xlat0.xyz = u_xlat16_0.xyz * u_xlat2.xyz;
    SV_Target0.xyz = u_xlat0.xyz;
    return SV_Target0;
}
