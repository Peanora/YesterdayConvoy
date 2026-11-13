// Identified source family: Torappu/Particles-L2D/Ram/Disturb(CustomData)
// Source Shader PathID: -363591244887235093
#include "OriginalRemainingRootGlesCommon.cginc"
#if defined(_HGCUSTOMVERTEXSTREAM_ON)
// Literal source program 2: _HGCUSTOMVERTEXSTREAM_ON
float _HG_UV_ROTATION;
float4 _Rotation0;
float4 _Rotation1;
float4 _Rotation2;
float4 _Rotation3;
float4 _MainTex_ST;
float4 _DissolveTex_ST;
float4 _DisturbTex_ST;
float4 _RamTex_ST;
float _MainUSpeed;
float _MainVSpeed;
float _DissolveUSpeed;
float _DissolveVSpeed;
float _DisturbUSpeed;
float _DisturbVSpeed;
float _MainTexChannel;
float _DisturbTexChannel;
float _DissolvTexChannel;
float _RamTexChannel;
float4 _MainColor;
float _Opacity;
float _PremultiplyAlpha;
float _DisturbInfluenceDissolveUV;
float _DisturbInfluenceMainUV;
float _Amount;
float _BorderWidth;
float _IntensityU;
float _IntensityV;
sampler2D _RamTex;
sampler2D _DisturbTex;
sampler2D _MainTex;
sampler2D _DissolveTex;
struct source_appdata
{
    float4 in_POSITION0 : POSITION0;
    float4 in_TEXCOORD0 : TEXCOORD0;
    float4 in_COLOR0 : COLOR0;
    float4 in_TEXCOORD1 : TEXCOORD1;
    float2 in_TEXCOORD2 : TEXCOORD2;
};
struct source_v2f
{
    float4 position : SV_POSITION;
    float4 vs_TEXCOORD0 : TEXCOORD0;
    float4 vs_COLOR0 : COLOR0;
    float4 vs_TEXCOORD1 : TEXCOORD1;
    float2 vs_TEXCOORD2 : TEXCOORD2;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat1;
    bool u_xlatb1;
    float2 u_xlat16_2;
    float2 u_xlat3;
    float2 u_xlat5;
    float2 u_xlat8;
    float2 u_xlat9;
    float2 u_xlat16_10;

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    u_xlat0.xy = SourceVec2(_MainUSpeed, _MainVSpeed) * OriginalSourceTime().yy;
    u_xlat0.xy = frac(u_xlat0.xy);
    u_xlat8.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    u_xlat16_2.xy = u_xlat8.xy + v.in_TEXCOORD0.zw;
    u_xlat16_10.xy = u_xlat16_2.xy + SourceVec2(-0.5, -0.5);
    u_xlat1.x = dot(u_xlat16_10.xy, _Rotation0.xz);
    u_xlat1.y = dot(u_xlat16_10.xy, _Rotation0.yw);
    u_xlat8.xy = u_xlat1.xy + SourceVec2(0.5, 0.5);
    u_xlatb1 = 0.5<_HG_UV_ROTATION;
    u_xlat16_2.xy = (bool(u_xlatb1)) ? u_xlat8.xy : u_xlat16_2.xy;
    u_xlat0.xy = u_xlat0.xy + u_xlat16_2.xy;
    u_xlat5.xy = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    u_xlat16_2.xy = u_xlat5.xy + v.in_TEXCOORD1.xy;
    u_xlat16_10.xy = u_xlat16_2.xy + SourceVec2(-0.5, -0.5);
    u_xlat9.x = dot(u_xlat16_10.xy, _Rotation1.xz);
    u_xlat9.y = dot(u_xlat16_10.xy, _Rotation1.yw);
    u_xlat5.xy = u_xlat9.xy + SourceVec2(0.5, 0.5);
    u_xlat16_2.xy = (bool(u_xlatb1)) ? u_xlat5.xy : u_xlat16_2.xy;
    u_xlat5.xy = SourceVec2(_DissolveUSpeed, _DissolveVSpeed) * OriginalSourceTime().yy;
    u_xlat5.xy = frac(u_xlat5.xy);
    u_xlat0.zw = u_xlat5.xy + u_xlat16_2.xy;
    o.vs_TEXCOORD0 = u_xlat0;
    o.vs_COLOR0 = v.in_COLOR0;
    u_xlat0.xy = v.in_TEXCOORD0.xy * _DisturbTex_ST.xy + _DisturbTex_ST.zw;
    u_xlat16_2.xy = u_xlat0.xy + v.in_TEXCOORD2.xy;
    u_xlat16_10.xy = u_xlat16_2.xy + SourceVec2(-0.5, -0.5);
    u_xlat8.x = dot(u_xlat16_10.xy, _Rotation3.xz);
    u_xlat8.y = dot(u_xlat16_10.xy, _Rotation3.yw);
    u_xlat0.xy = u_xlat8.xy + SourceVec2(0.5, 0.5);
    u_xlat16_2.xy = (bool(u_xlatb1)) ? u_xlat0.xy : u_xlat16_2.xy;
    u_xlat0.xy = SourceVec2(_DisturbUSpeed, _DisturbVSpeed) * OriginalSourceTime().yy;
    u_xlat0.xy = frac(u_xlat0.xy);
    u_xlat0.xy = u_xlat0.xy + u_xlat16_2.xy;
    o.vs_TEXCOORD1.zw = u_xlat0.xy;
    u_xlat0.xy = v.in_TEXCOORD0.xy * _RamTex_ST.xy + _RamTex_ST.zw;
    u_xlat16_2.xy = u_xlat0.xy + SourceVec2(-0.5, -0.5);
    u_xlat3.x = dot(u_xlat16_2.xy, _Rotation2.xz);
    u_xlat3.y = dot(u_xlat16_2.xy, _Rotation2.yw);
    u_xlat8.xy = u_xlat3.xy + SourceVec2(0.5, 0.5);
    o.vs_TEXCOORD1.xy = (bool(u_xlatb1)) ? u_xlat8.xy : u_xlat0.xy;
    o.vs_TEXCOORD2.xy = v.in_TEXCOORD1.zw;
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float u_xlat16_0;
    float4 u_xlat16_1;
    float4 u_xlat2;
    float4 u_xlat16_2;
    float4 u_xlat3;
    float4 u_xlat16_3;
    bool4 u_xlatb3;
    bool4 u_xlatb4;
    bool4 u_xlatb5;
    float3 u_xlat16_6;
    float u_xlat16_7;

    u_xlat16_0 = i.vs_TEXCOORD2.x + _Amount;
    u_xlat16_1 = i.vs_TEXCOORD2.yyyy + SourceVec4(_IntensityU, _IntensityV, _IntensityU, _IntensityV);
    u_xlat2 = tex2D(_RamTex, i.vs_TEXCOORD1.xy);
    u_xlatb3 = SourceEqual4(SourceVec4(SourceVec4(_RamTexChannel, _RamTexChannel, _RamTexChannel, _RamTexChannel)), SourceVec4(0.0, 1.0, 2.0, 3.0));
    u_xlat16_6.x = (u_xlatb3.w) ? u_xlat2.z : u_xlat2.w;
    u_xlat16_6.x = (u_xlatb3.z) ? u_xlat2.y : u_xlat16_6.x;
    u_xlat16_6.x = (u_xlatb3.y) ? u_xlat2.x : u_xlat16_6.x;
    u_xlat16_2 = (u_xlatb3.x) ? u_xlat2 : u_xlat16_6.xxxx;
    u_xlat3 = tex2D(_DisturbTex, i.vs_TEXCOORD1.zw);
    u_xlatb4 = SourceEqual4(SourceVec4(_DisturbTexChannel, _DisturbTexChannel, _DisturbTexChannel, _MainTexChannel), SourceVec4(0.0, 1.0, 2.0, 0.0));
    u_xlat16_6.x = (u_xlatb4.z) ? u_xlat3.z : u_xlat3.w;
    u_xlat16_6.x = (u_xlatb4.y) ? u_xlat3.y : u_xlat16_6.x;
    u_xlat16_6.x = (u_xlatb4.x) ? u_xlat3.x : u_xlat16_6.x;
    u_xlat16_1 = u_xlat16_1 * u_xlat16_6.xxxx;
    u_xlat16_1 = u_xlat16_1 * SourceVec4(_DisturbInfluenceMainUV, _DisturbInfluenceMainUV, _DisturbInfluenceDissolveUV, _DisturbInfluenceDissolveUV) + i.vs_TEXCOORD0;
    u_xlat3 = tex2D(_MainTex, u_xlat16_1.xy);
    u_xlatb5 = SourceEqual4(SourceVec4(_MainTexChannel, _MainTexChannel, _MainTexChannel, _DissolvTexChannel), SourceVec4(1.0, 2.0, 3.0, 0.0));
    u_xlat16_6.x = (u_xlatb5.z) ? u_xlat3.z : u_xlat3.w;
    u_xlat16_6.x = (u_xlatb5.y) ? u_xlat3.y : u_xlat16_6.x;
    u_xlat16_6.x = (u_xlatb5.x) ? u_xlat3.x : u_xlat16_6.x;
    u_xlat16_3 = (u_xlatb4.w) ? u_xlat3 : u_xlat16_6.xxxx;
    u_xlat16_3 = u_xlat16_3 * _MainColor;
    u_xlat16_3 = u_xlat16_3 * i.vs_COLOR0;
    u_xlat16_2 = u_xlat16_2 * u_xlat16_3;
    u_xlat16_6.xyz = u_xlat16_2.xyz * SourceVec3(2.0, 2.0, 2.0);
    u_xlat16_1.x = dot(u_xlat16_2.ww, SourceVec2(_Opacity));
    u_xlat2 = tex2D(_DissolveTex, u_xlat16_1.zw);
    u_xlatb4.xy = SourceEqual4(SourceVec4(SourceVec4(_DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel)), SourceVec4(1.0, 2.0, 0.0, 0.0)).xy;
    u_xlat16_7 = (u_xlatb4.y) ? u_xlat2.z : u_xlat2.w;
    u_xlat16_7 = (u_xlatb4.x) ? u_xlat2.y : u_xlat16_7;
    u_xlat16_7 = (u_xlatb5.w) ? u_xlat2.x : u_xlat16_7;
    u_xlat16_7 = (-u_xlat16_0) + u_xlat16_7;
    u_xlat16_0 = u_xlat16_0 + 0.5;
    u_xlat16_0 = SourceRoundEven(u_xlat16_0);
    u_xlat16_0 = (-u_xlat16_0) + 1.0;
    u_xlat16_0 = _BorderWidth * u_xlat16_0 + u_xlat16_7;
    u_xlat16_0 = u_xlat16_0 / _BorderWidth;
    u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
    u_xlat16_0 = u_xlat16_0 * u_xlat16_1.x;
    u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
    u_xlatb4.x = _PremultiplyAlpha<1.5;
    if(u_xlatb4.x){
        u_xlat16_1.xyz = SourceVec3(u_xlat16_0) * u_xlat16_6.xyz;
        SV_Target0.xyz = u_xlat16_1.xyz + u_xlat16_1.xyz;
        SV_Target0.w = u_xlat16_0;
        return SV_Target0;
    }
    SV_Target0.xyz = u_xlat16_6.xyz;
    SV_Target0.w = u_xlat16_0;
    return SV_Target0;
}
#else
// Literal source program 1: ordinary stream
float _HG_UV_ROTATION;
float4 _Rotation0;
float4 _Rotation1;
float4 _Rotation2;
float4 _Rotation3;
float4 _MainTex_ST;
float4 _DissolveTex_ST;
float4 _DisturbTex_ST;
float4 _RamTex_ST;
float _MainUSpeed;
float _MainVSpeed;
float _DissolveUSpeed;
float _DissolveVSpeed;
float _DisturbUSpeed;
float _DisturbVSpeed;
float _MainTexChannel;
float _DisturbTexChannel;
float _DissolvTexChannel;
float _RamTexChannel;
float4 _MainColor;
float _Opacity;
float _PremultiplyAlpha;
float _DisturbInfluenceDissolveUV;
float _DisturbInfluenceMainUV;
float _Amount;
float _BorderWidth;
float _IntensityU;
float _IntensityV;
sampler2D _RamTex;
sampler2D _DisturbTex;
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
    float4 vs_TEXCOORD1 : TEXCOORD1;
    float2 vs_TEXCOORD2 : TEXCOORD2;
};
source_v2f vert(source_appdata v)
{
    source_v2f o = (source_v2f)0;
    float4 u_xlat0;
    float4 u_xlat1;
    float2 u_xlat16_2;
    float2 u_xlat3;
    float2 u_xlat8;
    bool u_xlatb9;
    float2 u_xlat11;

    o.position = SourceRootObjectToClipPos(v.in_POSITION0.xyz);
    u_xlat0.xy = SourceVec2(_MainUSpeed, _MainVSpeed) * OriginalSourceTime().yy;
    u_xlat0.xy = frac(u_xlat0.xy);
    u_xlat8.xy = v.in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    u_xlat16_2.xy = u_xlat8.xy + SourceVec2(-0.5, -0.5);
    u_xlat1.x = dot(u_xlat16_2.xy, _Rotation0.xz);
    u_xlat1.y = dot(u_xlat16_2.xy, _Rotation0.yw);
    u_xlat1.xy = u_xlat1.xy + SourceVec2(0.5, 0.5);
    u_xlatb9 = 0.5<_HG_UV_ROTATION;
    u_xlat16_2.xy = (bool(u_xlatb9)) ? u_xlat1.xy : u_xlat8.xy;
    u_xlat0.xy = u_xlat0.xy + u_xlat16_2.xy;
    u_xlat1.xy = v.in_TEXCOORD0.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    u_xlat16_2.xy = u_xlat1.xy + SourceVec2(-0.5, -0.5);
    u_xlat11.x = dot(u_xlat16_2.xy, _Rotation1.xz);
    u_xlat11.y = dot(u_xlat16_2.xy, _Rotation1.yw);
    u_xlat3.xy = u_xlat11.xy + SourceVec2(0.5, 0.5);
    u_xlat16_2.xy = (bool(u_xlatb9)) ? u_xlat3.xy : u_xlat1.xy;
    u_xlat1.xy = SourceVec2(_DissolveUSpeed, _DissolveVSpeed) * OriginalSourceTime().yy;
    u_xlat1.xy = frac(u_xlat1.xy);
    u_xlat0.zw = u_xlat1.xy + u_xlat16_2.xy;
    o.vs_TEXCOORD0 = u_xlat0;
    o.vs_COLOR0 = v.in_COLOR0;
    u_xlat0.xy = v.in_TEXCOORD0.xy * _DisturbTex_ST.xy + _DisturbTex_ST.zw;
    u_xlat16_2.xy = u_xlat0.xy + SourceVec2(-0.5, -0.5);
    u_xlat8.x = dot(u_xlat16_2.xy, _Rotation3.xz);
    u_xlat8.y = dot(u_xlat16_2.xy, _Rotation3.yw);
    u_xlat8.xy = u_xlat8.xy + SourceVec2(0.5, 0.5);
    u_xlat16_2.xy = (bool(u_xlatb9)) ? u_xlat8.xy : u_xlat0.xy;
    u_xlat0.xy = SourceVec2(_DisturbUSpeed, _DisturbVSpeed) * OriginalSourceTime().yy;
    u_xlat0.xy = frac(u_xlat0.xy);
    u_xlat0.xy = u_xlat0.xy + u_xlat16_2.xy;
    o.vs_TEXCOORD1.zw = u_xlat0.xy;
    u_xlat0.xy = v.in_TEXCOORD0.xy * _RamTex_ST.xy + _RamTex_ST.zw;
    u_xlat16_2.xy = u_xlat0.xy + SourceVec2(-0.5, -0.5);
    u_xlat1.x = dot(u_xlat16_2.xy, _Rotation2.xz);
    u_xlat1.y = dot(u_xlat16_2.xy, _Rotation2.yw);
    u_xlat8.xy = u_xlat1.xy + SourceVec2(0.5, 0.5);
    o.vs_TEXCOORD1.xy = (bool(u_xlatb9)) ? u_xlat8.xy : u_xlat0.xy;
    o.vs_TEXCOORD2.xy = SourceVec2(0.0, 0.0);
    return o;
}
float4 frag(source_v2f i) : SV_Target
{
    float4 SV_Target0 = 0.0;
    float u_xlat16_0;
    float4 u_xlat16_1;
    float4 u_xlat2;
    float4 u_xlat16_2;
    float4 u_xlat3;
    float4 u_xlat16_3;
    bool4 u_xlatb3;
    bool4 u_xlatb4;
    bool4 u_xlatb5;
    float3 u_xlat16_6;
    float u_xlat16_7;

    u_xlat16_0 = i.vs_TEXCOORD2.x + _Amount;
    u_xlat16_1 = i.vs_TEXCOORD2.yyyy + SourceVec4(_IntensityU, _IntensityV, _IntensityU, _IntensityV);
    u_xlat2 = tex2D(_RamTex, i.vs_TEXCOORD1.xy);
    u_xlatb3 = SourceEqual4(SourceVec4(SourceVec4(_RamTexChannel, _RamTexChannel, _RamTexChannel, _RamTexChannel)), SourceVec4(0.0, 1.0, 2.0, 3.0));
    u_xlat16_6.x = (u_xlatb3.w) ? u_xlat2.z : u_xlat2.w;
    u_xlat16_6.x = (u_xlatb3.z) ? u_xlat2.y : u_xlat16_6.x;
    u_xlat16_6.x = (u_xlatb3.y) ? u_xlat2.x : u_xlat16_6.x;
    u_xlat16_2 = (u_xlatb3.x) ? u_xlat2 : u_xlat16_6.xxxx;
    u_xlat3 = tex2D(_DisturbTex, i.vs_TEXCOORD1.zw);
    u_xlatb4 = SourceEqual4(SourceVec4(_DisturbTexChannel, _DisturbTexChannel, _DisturbTexChannel, _MainTexChannel), SourceVec4(0.0, 1.0, 2.0, 0.0));
    u_xlat16_6.x = (u_xlatb4.z) ? u_xlat3.z : u_xlat3.w;
    u_xlat16_6.x = (u_xlatb4.y) ? u_xlat3.y : u_xlat16_6.x;
    u_xlat16_6.x = (u_xlatb4.x) ? u_xlat3.x : u_xlat16_6.x;
    u_xlat16_1 = u_xlat16_1 * u_xlat16_6.xxxx;
    u_xlat16_1 = u_xlat16_1 * SourceVec4(_DisturbInfluenceMainUV, _DisturbInfluenceMainUV, _DisturbInfluenceDissolveUV, _DisturbInfluenceDissolveUV) + i.vs_TEXCOORD0;
    u_xlat3 = tex2D(_MainTex, u_xlat16_1.xy);
    u_xlatb5 = SourceEqual4(SourceVec4(_MainTexChannel, _MainTexChannel, _MainTexChannel, _DissolvTexChannel), SourceVec4(1.0, 2.0, 3.0, 0.0));
    u_xlat16_6.x = (u_xlatb5.z) ? u_xlat3.z : u_xlat3.w;
    u_xlat16_6.x = (u_xlatb5.y) ? u_xlat3.y : u_xlat16_6.x;
    u_xlat16_6.x = (u_xlatb5.x) ? u_xlat3.x : u_xlat16_6.x;
    u_xlat16_3 = (u_xlatb4.w) ? u_xlat3 : u_xlat16_6.xxxx;
    u_xlat16_3 = u_xlat16_3 * _MainColor;
    u_xlat16_3 = u_xlat16_3 * i.vs_COLOR0;
    u_xlat16_2 = u_xlat16_2 * u_xlat16_3;
    u_xlat16_6.xyz = u_xlat16_2.xyz * SourceVec3(2.0, 2.0, 2.0);
    u_xlat16_1.x = dot(u_xlat16_2.ww, SourceVec2(_Opacity));
    u_xlat2 = tex2D(_DissolveTex, u_xlat16_1.zw);
    u_xlatb4.xy = SourceEqual4(SourceVec4(SourceVec4(_DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel, _DissolvTexChannel)), SourceVec4(1.0, 2.0, 0.0, 0.0)).xy;
    u_xlat16_7 = (u_xlatb4.y) ? u_xlat2.z : u_xlat2.w;
    u_xlat16_7 = (u_xlatb4.x) ? u_xlat2.y : u_xlat16_7;
    u_xlat16_7 = (u_xlatb5.w) ? u_xlat2.x : u_xlat16_7;
    u_xlat16_7 = (-u_xlat16_0) + u_xlat16_7;
    u_xlat16_0 = u_xlat16_0 + 0.5;
    u_xlat16_0 = SourceRoundEven(u_xlat16_0);
    u_xlat16_0 = (-u_xlat16_0) + 1.0;
    u_xlat16_0 = _BorderWidth * u_xlat16_0 + u_xlat16_7;
    u_xlat16_0 = u_xlat16_0 / _BorderWidth;
    u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
    u_xlat16_0 = u_xlat16_0 * u_xlat16_1.x;
    u_xlat16_0 = clamp(u_xlat16_0, 0.0, 1.0);
    u_xlatb4.x = _PremultiplyAlpha<1.5;
    if(u_xlatb4.x){
        u_xlat16_1.xyz = SourceVec3(u_xlat16_0) * u_xlat16_6.xyz;
        SV_Target0.xyz = u_xlat16_1.xyz + u_xlat16_1.xyz;
        SV_Target0.w = u_xlat16_0;
        return SV_Target0;
    }
    SV_Target0.xyz = u_xlat16_6.xyz;
    SV_Target0.w = u_xlat16_0;
    return SV_Target0;
}
#endif
