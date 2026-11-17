Shader "YesterdayConvoy/Approved Painted Formation Floor"
{
 Properties { _MainTex("Approved courtyard",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)
 _ProjectRow0("Saved projector row 0",Vector)=(1,0,0,0) _ProjectRow1("Saved projector row 1",Vector)=(0,1,0,0) _ProjectRow3("Saved projector row 3",Vector)=(0,0,1,0) }
 SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry" }
 Pass { Cull Back ZWrite On
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex; float4 _ProjectRow0,_ProjectRow1,_ProjectRow3;
 struct Input {float4 vertex:POSITION;}; struct Output {float4 vertex:SV_POSITION;float3 paint:TEXCOORD0;};
 Output vert(Input v) {Output o;float4 world=mul(unity_ObjectToWorld,v.vertex);o.vertex=UnityObjectToClipPos(v.vertex);o.paint=float3(dot(_ProjectRow0,world),dot(_ProjectRow1,world),dot(_ProjectRow3,world));return o;}
 fixed4 frag(Output i):SV_Target {float2 uv=i.paint.xy/i.paint.z*.5+.5;return tex2D(_MainTex,uv);}
 ENDCG
 } }
 Fallback Off
}