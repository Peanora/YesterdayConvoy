// Identified source family: Torappu/Particles/Dissolve/Dissolve Add Double
// Source Shader PathID: 5210227398647873839
#include "OriginalDissolveGlesCommon.cginc"
// Literal source program 1: ordinary stream
float4 _MainTex_ST;
float4 _DissolveTex_01_ST;
float4 _DissolveTex_02_ST;
float4 _TintColor;
float _Amount_01;
float _BorderWidth_01;
float _Amount_02;
float _BorderWidth_02;
sampler2D _DissolveTex_01;
sampler2D _DissolveTex_02;
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
    float2 vs_TEXCOORD1 : TEXCOORD1;
    float4 vs_COLOR0 : COLOR0;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat1;

    o.position = SourceDissolveObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.vs_TEXCOORD0.zw = v.in_TEXCOORD0.xy * _DissolveTex_01_ST.xy + _DissolveTex_01_ST.zw;
    o.vs_TEXCOORD1.xy = v.in_TEXCOORD0.xy * _DissolveTex_02_ST.xy + _DissolveTex_02_ST.zw;
    o.vs_COLOR0 = v.in_COLOR0 * _TintColor;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float u_xlat16_0;
    float2 u_xlat16_1;
    float4 u_xlat16_2;
    float2 u_xlat16_4;

    u_xlat16_0 = tex2D(_DissolveTex_01, i.vs_TEXCOORD0.zw).x;
    u_xlat16_1.x = u_xlat16_0 + (-_Amount_01);
    u_xlat16_4.xy = SourceVec2(_Amount_01, _Amount_02) + SourceVec2(0.5, 0.5);
    u_xlat16_4.xy = SourceRoundEven(u_xlat16_4.xy);
    u_xlat16_4.xy = (-u_xlat16_4.xy) + SourceVec2(1.0, 1.0);
    u_xlat16_1.x = _BorderWidth_01 * u_xlat16_4.x + u_xlat16_1.x;
    u_xlat16_0 = tex2D(_DissolveTex_02, i.vs_TEXCOORD1.xy).x;
    u_xlat16_4.x = u_xlat16_0 + (-_Amount_02);
    u_xlat16_1.y = _BorderWidth_02 * u_xlat16_4.y + u_xlat16_4.x;
    u_xlat16_1.xy = u_xlat16_1.xy / SourceVec2(_BorderWidth_01, _BorderWidth_02);
    u_xlat16_1.xy = clamp(u_xlat16_1.xy, 0.0, 1.0);
    u_xlat16_1.x = u_xlat16_1.y * u_xlat16_1.x;
    u_xlat0 = i.vs_COLOR0 + i.vs_COLOR0;
    u_xlat16_2 = tex2D(_MainTex, i.vs_TEXCOORD0.xy);
    u_xlat0 = u_xlat0 * u_xlat16_2;
    SV_Target0.w = u_xlat16_1.x * u_xlat0.w;
    SV_Target0.w = clamp(SV_Target0.w, 0.0, 1.0);
    SV_Target0.xyz = u_xlat0.xyz;
    return SV_Target0;
}
