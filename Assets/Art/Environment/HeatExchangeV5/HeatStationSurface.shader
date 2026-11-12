Shader "YesterdayConvoy/Heat Station V5"
{
 Properties
 {
  _Color("Paint / cast surface",Color)=(.5,.5,.5,1)
  _MainTex("Neutral micro surface",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _RoughnessMap("Roughness (green)",2D)="white"{}
  _Detail("Albedo influence",Float)=.1
  _Mean("Albedo normalization",Float)=2.2
  _BumpScale("Normal strength",Float)=.04
  _Roughness("Source roughness",Float)=.95
  _Metallic("Metalness",Range(0,1))=0
  _Role("Top / paving / wall / metal",Vector)=(0,0,0,0)
 }
 SubShader
 {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap,_RoughnessMap;
  fixed4 _Color;
  half _Detail,_Mean,_BumpScale,_Roughness,_Metallic;
  float4 _Role;
  struct Input {float2 uv_MainTex;float2 panelUv;float4 shelter;float3 worldPos;};
  void vert(inout appdata_full v,out Input o)
  {UNITY_INITIALIZE_OUTPUT(Input,o);o.panelUv=v.texcoord1.xy;o.shelter=v.color;}
  void surf(Input i,inout SurfaceOutputStandard o)
  {
   float3 microSample=tex2D(_MainTex,i.uv_MainTex).rgb;
   #if defined(UNITY_COLORSPACE_GAMMA)
    microSample=GammaToLinearSpace(microSample);
   #endif
   float micro=dot(microSample,float3(.2126,.7152,.0722))*_Mean;
   float3 c=_Color.rgb*lerp(1,clamp(micro,.72,1.24),_Detail);
   float2 world=float2(i.worldPos.x,-i.worldPos.z);
   float broad=sin(world.x*.47+world.y*.23)*sin(world.y*.61-world.x*.13);
   c*=1+broad*.018;
   if(_Role.x+_Role.y>.5){float2 tile=floor(world+float2(7,5));float v=frac(sin(dot(tile,float2(127.1,311.7)))*43758.5453);c*=lerp(.955,1.045,v);}
   float4 d=float4(i.panelUv.x,1-i.panelUv.x,i.panelUv.y,1-i.panelUv.y);
   float4 g=i.shelter*exp(-d*20);
   c*=1-max(max(g.x,g.y),max(g.z,g.w))*.11;
   float edge=min(min(i.panelUv.x,1-i.panelUv.x),min(i.panelUv.y,1-i.panelUv.y));
   if(_Role.x>.5)c*=1+(1-smoothstep(.002,.010,edge))*.075;
   if(_Role.z>.5)c*=1-exp(-i.panelUv.y*10)*.075;
   if(_Role.w>.5){float worn=(1-smoothstep(.002,.015,edge))*smoothstep(.1,.85,sin(world.x*2.1+world.y*1.8)*.5+.5);c=lerp(c,float3(.13,.14,.12),worn*.12);}
   o.Albedo=c;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_MainTex),_BumpScale);
   half r=tex2D(_RoughnessMap,i.uv_MainTex).g*_Roughness;
   r=_Role.w>.5?.80+r*.15:.89+r*.09;
   o.Smoothness=1-r;o.Metallic=_Metallic;o.Occlusion=1;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
