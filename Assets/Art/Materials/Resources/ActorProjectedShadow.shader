Shader "YesterdayConvoy/Actor Projected Source Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex("Source sprite",2D)="white"{}
        [PerRendererData] _GroundAnchor("Actual foot ground",Vector)=(0,0,0,0)
        [PerRendererData] _FrameRect("Source frame UV bounds",Vector)=(0,0,1,1)
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize,_GroundAnchor,_FrameRect;
            struct output{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            output vert(appdata_t v)
            {
                output o;UNITY_SETUP_INSTANCE_ID(v);float3 world=mul(unity_ObjectToWorld,UnityFlipSprite(v.vertex.xyz,_Flip)).xyz;
                float3 foot=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
                float3 normal=normalize(float3(UNITY_MATRIX_V[2].x,0,UNITY_MATRIX_V[2].z));
                float3 ray=world-_WorldSpaceCameraPos;float denominator=dot(ray,normal);
                float3 upright=_WorldSpaceCameraPos+ray*(dot(foot-_WorldSpaceCameraPos,normal)/denominator);
                float3 sun=normalize(float3(-7,16,10));
                float3 shadow=upright-sun*((upright.y-_GroundAnchor.y)*.60/sun.y);shadow.y=_GroundAnchor.y;
                o.vertex=UnityWorldToClipPos(shadow);o.uv=v.texcoord;return o;
            }
            float alpha(float2 uv){return tex2D(_MainTex,clamp(uv,_FrameRect.xy+_MainTex_TexelSize.xy*.5,_FrameRect.zw-_MainTex_TexelSize.xy*.5)).a;}
            fixed4 frag(output i):SV_Target
            {
                float2 d=_MainTex_TexelSize.xy*2.5;
                float a=alpha(i.uv)*.28+
                    (alpha(i.uv+float2(d.x,0))+alpha(i.uv-float2(d.x,0))+
                     alpha(i.uv+float2(0,d.y))+alpha(i.uv-float2(0,d.y)))*.18;
                return fixed4(0,0,0,a*.18);
            }
            ENDCG
        }
    }
}
