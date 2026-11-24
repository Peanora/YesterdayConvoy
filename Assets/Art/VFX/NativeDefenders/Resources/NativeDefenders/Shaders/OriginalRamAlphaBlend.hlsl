// Identified source family: Torappu/Particles/Ram/AlphaBlend
// Source Shader PathID: -507227593674636265
#include "OriginalRemainingRootGlesCommon.cginc"
#if defined(HG_SPRITE_SHEET_LERP)
// Literal source program 4: HG_SPRITE_SHEET_LERP
float4 _HGSpriteSheetParam;
float4 _HGExternalCtrl;
float4 _MainTex_ST;
float4 _RamTex_ST;
float4 _TintColor;
sampler2D _RamTex;
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
    float4 vs_TEXCOORD1 : TEXCOORD1;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    bool2 u_xlatb0;
    float4 u_xlat1;
    float2 u_xlat16_2;
    float2 u_xlat16_3;
    float2 u_xlat16_6;
    float2 u_xlat16_10;

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.zw = v.in_TEXCOORD0.xy * _RamTex_ST.xy + _RamTex_ST.zw;
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.vs_TEXCOORD0.xy = u_xlat16_2.xy;
    o.vs_COLOR0 = v.in_COLOR0 * _TintColor;
    u_xlat16_10.xy = _HGExternalCtrl.xy / _HGSpriteSheetParam.xx;
    u_xlatb0.xy = SourceGreaterEqual4(u_xlat16_10.xyxx, (-u_xlat16_10.xyxx)).xy;
    u_xlat16_3.xy = frac(abs(u_xlat16_10.xy));
    u_xlat16_10.xy = floor(u_xlat16_10.xy);
    u_xlat16_6.xy = u_xlat16_10.xy + u_xlat16_2.yy;
    o.vs_TEXCOORD1.yw = (-u_xlat16_6.xy) * _HGSpriteSheetParam.yy + SourceVec2(1.0, 1.0);
    u_xlat16_6.x = (u_xlatb0.x) ? u_xlat16_3.x : (-u_xlat16_3.x);
    u_xlat16_6.y = (u_xlatb0.y) ? u_xlat16_3.y : (-u_xlat16_3.y);
    u_xlat16_2.xy = u_xlat16_6.xy * _HGSpriteSheetParam.xx + u_xlat16_2.xx;
    u_xlat0.xy = u_xlat16_2.xy * _HGSpriteSheetParam.zz;
    o.vs_TEXCOORD1.xz = u_xlat0.xy;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;
    float4 u_xlat16_1;
    bool u_xlatb1;
    float4 u_xlat2;

    u_xlat16_0.x = _HGExternalCtrl.w + -0.100000001;
    u_xlatb1 = u_xlat16_0.x<0.0;
    if(u_xlatb1){discard;}
    u_xlat16_0 = tex2D(_MainTex, i.vs_TEXCOORD1.zw);
    u_xlat16_1 = tex2D(_MainTex, i.vs_TEXCOORD1.xy);
    u_xlat0 = u_xlat16_0 + (-u_xlat16_1);
    u_xlat0 = _HGExternalCtrl.zzzz * u_xlat0 + u_xlat16_1;
    u_xlat16_1 = tex2D(_RamTex, i.vs_TEXCOORD0.zw);
    u_xlat2 = i.vs_COLOR0 + i.vs_COLOR0;
    u_xlat1 = u_xlat16_1 * u_xlat2;
    u_xlat0 = u_xlat0 * u_xlat1;
    SV_Target0.w = u_xlat0.w;
    SV_Target0.w = clamp(SV_Target0.w, 0.0, 1.0);
    SV_Target0.xyz = u_xlat0.xyz;
    return SV_Target0;
}
#elif defined(HG_SPRITE_SHEET)
// Literal source program 3: HG_SPRITE_SHEET
float4 _HGSpriteSheetParam;
float4 _HGExternalCtrl;
float4 _MainTex_ST;
float4 _RamTex_ST;
float4 _TintColor;
sampler2D _RamTex;
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
    float4 vs_TEXCOORD1 : TEXCOORD1;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    bool2 u_xlatb0;
    float4 u_xlat1;
    float2 u_xlat16_2;
    float2 u_xlat16_3;
    float2 u_xlat16_6;
    float2 u_xlat16_10;

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.zw = v.in_TEXCOORD0.xy * _RamTex_ST.xy + _RamTex_ST.zw;
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.vs_TEXCOORD0.xy = u_xlat16_2.xy;
    o.vs_COLOR0 = v.in_COLOR0 * _TintColor;
    u_xlat16_10.xy = _HGExternalCtrl.xy / _HGSpriteSheetParam.xx;
    u_xlatb0.xy = SourceGreaterEqual4(u_xlat16_10.xyxx, (-u_xlat16_10.xyxx)).xy;
    u_xlat16_3.xy = frac(abs(u_xlat16_10.xy));
    u_xlat16_10.xy = floor(u_xlat16_10.xy);
    u_xlat16_6.xy = u_xlat16_10.xy + u_xlat16_2.yy;
    o.vs_TEXCOORD1.yw = (-u_xlat16_6.xy) * _HGSpriteSheetParam.yy + SourceVec2(1.0, 1.0);
    u_xlat16_6.x = (u_xlatb0.x) ? u_xlat16_3.x : (-u_xlat16_3.x);
    u_xlat16_6.y = (u_xlatb0.y) ? u_xlat16_3.y : (-u_xlat16_3.y);
    u_xlat16_2.xy = u_xlat16_6.xy * _HGSpriteSheetParam.xx + u_xlat16_2.xx;
    u_xlat0.xy = u_xlat16_2.xy * _HGSpriteSheetParam.zz;
    o.vs_TEXCOORD1.xz = u_xlat0.xy;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;
    float4 u_xlat16_1;
    bool u_xlatb1;

    u_xlat16_0.x = _HGExternalCtrl.w + -0.100000001;
    u_xlatb1 = u_xlat16_0.x<0.0;
    if(u_xlatb1){discard;}
    u_xlat16_0 = tex2D(_RamTex, i.vs_TEXCOORD0.zw);
    u_xlat1 = i.vs_COLOR0 + i.vs_COLOR0;
    u_xlat0 = u_xlat16_0 * u_xlat1;
    u_xlat16_1 = tex2D(_MainTex, i.vs_TEXCOORD1.xy);
    u_xlat0 = u_xlat0 * u_xlat16_1;
    SV_Target0.w = u_xlat0.w;
    SV_Target0.w = clamp(SV_Target0.w, 0.0, 1.0);
    SV_Target0.xyz = u_xlat0.xyz;
    return SV_Target0;
}
#else
// Literal source program 2: ordinary stream
float4 _MainTex_ST;
float4 _RamTex_ST;
float4 _TintColor;
sampler2D _RamTex;
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

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    o.vs_TEXCOORD0.zw = v.in_TEXCOORD0.xy * _RamTex_ST.xy + _RamTex_ST.zw;
    o.vs_COLOR0 = v.in_COLOR0 * _TintColor;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;
    float4 u_xlat16_1;

    u_xlat16_0 = tex2D(_RamTex, i.vs_TEXCOORD0.zw);
    u_xlat1 = i.vs_COLOR0 + i.vs_COLOR0;
    u_xlat0 = u_xlat16_0 * u_xlat1;
    u_xlat16_1 = tex2D(_MainTex, i.vs_TEXCOORD0.xy);
    u_xlat0 = u_xlat0 * u_xlat16_1;
    SV_Target0.w = u_xlat0.w;
    SV_Target0.w = clamp(SV_Target0.w, 0.0, 1.0);
    SV_Target0.xyz = u_xlat0.xyz;
    return SV_Target0;
}
#endif
