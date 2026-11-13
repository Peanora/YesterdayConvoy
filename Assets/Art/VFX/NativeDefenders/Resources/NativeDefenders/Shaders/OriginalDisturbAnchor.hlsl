// Identified source family: Torappu/Particles/Disturb/Disturb Anchor
// Source Shader PathID: -3878390923660880592
#include "OriginalRemainingRootGlesCommon.cginc"
#if defined(_WEIGHT_ON)
// Literal source program 3: _WEIGHT_ON
float4 _DissolveTex_ST;
float4 _DisturTex_ST;
float4 _DisturTex_02_ST;
float4 _WeightTex_ST;
int _ToggleUseDisturb2;
int _ToggleUseDissolve;
float4 _MainTex_ST;
float4 _MainColor;
float _MainColorACtrl;
float _IntensityU;
float _IntensityU_02;
float _IntensityV;
float _IntensityV_02;
float4 _UVTween;
float _AnchorU;
float _AnchorU_02;
float _AnchorV;
float _AnchorV_02;
float _Amount;
float _BorderWidth;
sampler2D _DisturTex;
sampler2D _DisturTex_02;
sampler2D _WeightTex;
sampler2D _MainTex;
sampler2D _DissolveTex;
struct source_appdata
{
    float4 in_POSITION0 : POSITION0;
    float2 in_TEXCOORD0 : TEXCOORD0;
    float4 in_COLOR0 : COLOR0;
};
struct source_v2f
{
    float4 position : SV_POSITION;
    float2 vs_TEXCOORD0 : TEXCOORD0;
    float4 vs_TEXCOORD1 : TEXCOORD1;
    float4 vs_TEXCOORD2 : TEXCOORD2;
    float4 vs_COLOR0 : COLOR0;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy;
    u_xlat16_0.xy = v.in_TEXCOORD0.xy * _DisturTex_ST.xy + _DisturTex_ST.zw;
    u_xlat16_0.zw = v.in_TEXCOORD0.xy * _WeightTex_ST.xy + _WeightTex_ST.zw;
    o.vs_TEXCOORD1 = u_xlat16_0;
    u_xlat16_0.xy = v.in_TEXCOORD0.xy * _DisturTex_02_ST.xy + _DisturTex_02_ST.zw;
    u_xlat16_0.zw = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    o.vs_TEXCOORD2 = u_xlat16_0;
    o.vs_COLOR0 = v.in_COLOR0;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float2 u_xlat16_1;
    float2 u_xlat2;
    float u_xlat10_2;
    float2 u_xlat16_3;
    float u_xlat16_5;
    float2 u_xlat8;
    float2 u_xlat16_8;
    float u_xlat10_8;
    bool u_xlatb8;
    float2 u_xlat16_9;

    u_xlat0 = _UVTween * OriginalSourceTime().yyyy;
    u_xlat0 = frac(u_xlat0);
    u_xlat8.xy = u_xlat0.zw + i.vs_TEXCOORD1.xy;
    u_xlat16_8.x = tex2D(_DisturTex, u_xlat8.xy).x;
    u_xlat16_1.xy = u_xlat16_8.xx + (-SourceVec2(_AnchorU, _AnchorV));
    u_xlatb8 = 0<_ToggleUseDisturb2;
    if(u_xlatb8){
        u_xlat10_8 = tex2D(_DisturTex_02, i.vs_TEXCOORD2.xy).x;
        u_xlat16_9.xy = SourceVec2(u_xlat10_8) + (-SourceVec2(_AnchorU_02, _AnchorV_02));
    } else {
        u_xlat16_9.x = float(0.0);
        u_xlat16_9.y = float(0.0);
    }
    u_xlat16_8.xy = tex2D(_WeightTex, i.vs_TEXCOORD1.zw).xy;
    u_xlat2.xy = u_xlat16_8.xy * u_xlat16_1.xy;
    u_xlat8.xy = u_xlat16_8.xy * u_xlat16_9.xy;
    u_xlat16_1.xy = u_xlat2.xy * SourceVec2(_IntensityU, _IntensityV) + i.vs_TEXCOORD0.xy;
    u_xlat16_3.x = u_xlat8.x * _IntensityU_02;
    u_xlat16_3.y = u_xlat8.y * _IntensityV_02;
    u_xlat16_1.xy = u_xlat16_1.xy + u_xlat16_3.xy;
    u_xlat8.xy = u_xlat16_1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    u_xlat0.xy = u_xlat0.xy + u_xlat8.xy;
    u_xlat16_0 = tex2D(_MainTex, u_xlat0.xy);
    u_xlat16_0 = u_xlat16_0 * _MainColor;
    u_xlat16_0 = u_xlat16_0 * i.vs_COLOR0;
    u_xlat16_0 = u_xlat16_0 + u_xlat16_0;
    u_xlat16_9.x = _MainColor.w + -1.0;
    u_xlat16_9.x = _MainColorACtrl * u_xlat16_9.x + 1.0;
    u_xlat16_0 = u_xlat16_0.wxyz * u_xlat16_9.xxxx;
    if(_ToggleUseDissolve != 0) {
        u_xlat2.xy = u_xlat16_1.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
        u_xlat10_2 = tex2D(_DissolveTex, u_xlat2.xy).x;
        u_xlat16_1.x = u_xlat10_2 + (-_Amount);
        u_xlat16_5 = _Amount + 0.5;
        u_xlat16_5 = SourceRoundEven(u_xlat16_5);
        u_xlat16_5 = (-u_xlat16_5) + 1.0;
        u_xlat16_1.x = _BorderWidth * u_xlat16_5 + u_xlat16_1.x;
        u_xlat16_1.x = u_xlat16_1.x / _BorderWidth;
        u_xlat16_1.x = clamp(u_xlat16_1.x, 0.0, 1.0);
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_1.x;
        u_xlat16_0.x = clamp(u_xlat16_0.x, 0.0, 1.0);
    }
    SV_Target0 = u_xlat16_0.yzwx;
    return SV_Target0;
}
#else
// Literal source program 2: ordinary stream
float4 _DissolveTex_ST;
float4 _DisturTex_ST;
float4 _DisturTex_02_ST;
int _ToggleUseDisturb2;
int _ToggleUseDissolve;
float4 _MainTex_ST;
float4 _MainColor;
float _MainColorACtrl;
float _IntensityU;
float _IntensityU_02;
float _IntensityV;
float _IntensityV_02;
float4 _UVTween;
float _AnchorU;
float _AnchorU_02;
float _AnchorV;
float _AnchorV_02;
float _Amount;
float _BorderWidth;
sampler2D _DisturTex;
sampler2D _DisturTex_02;
sampler2D _MainTex;
sampler2D _DissolveTex;
struct source_appdata
{
    float4 in_POSITION0 : POSITION0;
    float2 in_TEXCOORD0 : TEXCOORD0;
    float4 in_COLOR0 : COLOR0;
};
struct source_v2f
{
    float4 position : SV_POSITION;
    float2 vs_TEXCOORD0 : TEXCOORD0;
    float4 vs_TEXCOORD1 : TEXCOORD1;
    float4 vs_TEXCOORD2 : TEXCOORD2;
    float4 vs_COLOR0 : COLOR0;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float4 u_xlat1;
    float2 u_xlat16_2;

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    o.vs_TEXCOORD0.xy = v.in_TEXCOORD0.xy;
    u_xlat16_2.xy = v.in_TEXCOORD0.xy * _DisturTex_ST.xy + _DisturTex_ST.zw;
    o.vs_TEXCOORD1.xy = u_xlat16_2.xy;
    o.vs_TEXCOORD1.zw = SourceVec2(0.0, 0.0);
    u_xlat16_0.xy = v.in_TEXCOORD0.xy * _DisturTex_02_ST.xy + _DisturTex_02_ST.zw;
    u_xlat16_0.zw = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    o.vs_TEXCOORD2 = u_xlat16_0;
    o.vs_COLOR0 = v.in_COLOR0;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    float2 u_xlat16_1;
    float2 u_xlat16_2;
    float2 u_xlat3;
    float u_xlat10_3;
    float u_xlat16_5;
    float2 u_xlat8;
    float u_xlat16_8;
    float u_xlat10_8;
    bool u_xlatb8;
    float2 u_xlat16_9;

    u_xlat0 = _UVTween * OriginalSourceTime().yyyy;
    u_xlat0 = frac(u_xlat0);
    u_xlat8.xy = u_xlat0.zw + i.vs_TEXCOORD1.xy;
    u_xlat16_8 = tex2D(_DisturTex, u_xlat8.xy).x;
    u_xlat16_1.xy = SourceVec2(u_xlat16_8) + (-SourceVec2(_AnchorU, _AnchorV));
    u_xlatb8 = 0<_ToggleUseDisturb2;
    if(u_xlatb8){
        u_xlat10_8 = tex2D(_DisturTex_02, i.vs_TEXCOORD2.xy).x;
        u_xlat16_9.xy = SourceVec2(u_xlat10_8) + (-SourceVec2(_AnchorU_02, _AnchorV_02));
    } else {
        u_xlat16_9.x = float(0.0);
        u_xlat16_9.y = float(0.0);
    }
    u_xlat16_1.xy = u_xlat16_1.xy * SourceVec2(_IntensityU, _IntensityV) + i.vs_TEXCOORD0.xy;
    u_xlat16_2.x = u_xlat16_9.x * _IntensityU_02;
    u_xlat16_2.y = u_xlat16_9.y * _IntensityV_02;
    u_xlat16_1.xy = u_xlat16_1.xy + u_xlat16_2.xy;
    u_xlat8.xy = u_xlat16_1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    u_xlat0.xy = u_xlat0.xy + u_xlat8.xy;
    u_xlat16_0 = tex2D(_MainTex, u_xlat0.xy);
    u_xlat16_0 = u_xlat16_0 * _MainColor;
    u_xlat16_0 = u_xlat16_0 * i.vs_COLOR0;
    u_xlat16_0 = u_xlat16_0 + u_xlat16_0;
    u_xlat16_9.x = _MainColor.w + -1.0;
    u_xlat16_9.x = _MainColorACtrl * u_xlat16_9.x + 1.0;
    u_xlat16_0 = u_xlat16_0.wxyz * u_xlat16_9.xxxx;
    if(_ToggleUseDissolve != 0) {
        u_xlat3.xy = u_xlat16_1.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
        u_xlat10_3 = tex2D(_DissolveTex, u_xlat3.xy).x;
        u_xlat16_1.x = u_xlat10_3 + (-_Amount);
        u_xlat16_5 = _Amount + 0.5;
        u_xlat16_5 = SourceRoundEven(u_xlat16_5);
        u_xlat16_5 = (-u_xlat16_5) + 1.0;
        u_xlat16_1.x = _BorderWidth * u_xlat16_5 + u_xlat16_1.x;
        u_xlat16_1.x = u_xlat16_1.x / _BorderWidth;
        u_xlat16_1.x = clamp(u_xlat16_1.x, 0.0, 1.0);
        u_xlat16_0.x = u_xlat16_0.x * u_xlat16_1.x;
        u_xlat16_0.x = clamp(u_xlat16_0.x, 0.0, 1.0);
    }
    SV_Target0 = u_xlat16_0.yzwx;
    return SV_Target0;
}
#endif
