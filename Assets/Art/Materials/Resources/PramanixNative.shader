Shader "YesterdayConvoy/Pramanix Native Ground Depth"
{
    Properties { [PerRendererData] _MainTex("Native frame",2D)="black"{} _HdrRange("HDR encoding range",Float)=16 _PramanixAnchor("Actual source ground anchor",Vector)=(0,0,0,0) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct output{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float3 world:TEXCOORD1;};
            sampler2D _MainTex;float _HdrRange;float4 _PramanixAnchor;
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            struct result{float4 color:SV_Target;float depth:SV_Depth;};
            result frag(output i)
            {
                result o;float4 c=tex2D(_MainTex,i.uv);
                o.color=float4(c.rgb*c.rgb*_HdrRange*i.color.rgb*i.color.a,c.a*i.color.a);
                #if defined(UNITY_COLORSPACE_GAMMA)
                    // The approved captures contain linear radiance, even though the
                    // game's target is Gamma. Match the preview's sRGB output transform.
                    float3 low=o.color.rgb*12.92;
                    float3 high=1.055*pow(max(o.color.rgb,0.0),1.0/2.4)-0.055;
                    o.color.rgb=lerp(low,high,step(0.0031308,o.color.rgb));
                #endif
                // Keep the approved native camera capture upright and undistorted, but
                // use its true anchor's ground plane when testing map occlusion.
                float3 ray=i.world-_WorldSpaceCameraPos;
                float denominator=abs(ray.y)>.0001?ray.y:-.0001;
                float3 ground=_WorldSpaceCameraPos+ray*((_PramanixAnchor.y-_WorldSpaceCameraPos.y)/denominator);
                float4 clip=UnityWorldToClipPos(ground);o.depth=clip.z/clip.w;
                #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                    o.depth=o.depth*.5+.5;
                #endif
                return o;
            }
            ENDCG
        }
    }
}
