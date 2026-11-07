Shader "YesterdayConvoy/Herd Spirit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BodyHeight ("Body height", Float) = 1.25
        _Grounded ("Upright depth", Float) = 1
        _SpiritTime ("Simulation age", Float) = 0
        _SpiritOpacity ("Formation / dissolve", Float) = 1
        _SpriteUVRect ("Isolated atlas frame", Vector) = (0,0,1,1)
        _WispOnly ("Procedural ribbon", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize,_SpriteUVRect;
            float _BodyHeight,_Grounded,_SpiritTime,_SpiritOpacity,_WispOnly;
            struct spirit_v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float3 world:TEXCOORD1;float plane:TEXCOORD2;float height:TEXCOORD3;};
            float3 GroundNormal(){return normalize(float3(UNITY_MATRIX_V[2].x,0,UNITY_MATRIX_V[2].z));}
            spirit_v2f vert(appdata_t v)
            {
                v2f s=SpriteVert(v);spirit_v2f o;o.vertex=s.vertex;o.uv=s.texcoord;o.color=s.color;
                o.world=mul(unity_ObjectToWorld,UnityFlipSprite(v.vertex.xyz,_Flip)).xyz;
                float3 foot=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
                o.plane=dot(foot-_WorldSpaceCameraPos,GroundNormal());o.height=saturate(v.vertex.y/max(.1,_BodyHeight));return o;
            }
            float AlphaAt(float2 uv)
            {
                float2 inset=_MainTex_TexelSize.xy*.5;
                return SampleSpriteTexture(clamp(uv,_SpriteUVRect.xy+inset,_SpriteUVRect.zw-inset)).a;
            }
            struct output {fixed4 color:SV_Target;float depth:SV_Depth;};
            output frag(spirit_v2f i)
            {
                output o;
                if(_WispOnly>.5){o.color=i.color;o.color.rgb*=o.color.a;}
                else
                {
                    fixed4 source=SampleSpriteTexture(i.uv);
                    float2 step=_MainTex_TexelSize.xy*1.2;
                    float a=AlphaAt(i.uv+float2(step.x,0)),b=AlphaAt(i.uv-float2(step.x,0)),c=AlphaAt(i.uv+float2(0,step.y)),d=AlphaAt(i.uv-float2(0,step.y));
                    float outer=saturate(max(max(a,b),max(c,d))-source.a);
                    float inner=source.a*(1-min(min(a,b),min(c,d)));
                    float value=dot(source.rgb,float3(.3,.5,.2));
                    float3 blue=lerp(float3(.045,.17,.26),float3(.49,.81,.95),saturate(value*1.1));
                    float pulse=1+.025*sin(_SpiritTime*3.2);
                    float opacity=lerp(.25,.58,smoothstep(0,.65,i.height))*pulse;
                    float alpha=saturate(source.a*opacity+outer*.48+inner*.38)*_SpiritOpacity*i.color.a;
                    float rim=saturate(outer+inner*.7);
                    blue=lerp(blue,float3(.35,.92,1),rim);
                    // Preserve a restrained hit flash while keeping the cyan state unmistakable.
                    blue=lerp(blue,float3(.65,.95,1),saturate(i.color.r-i.color.g)*.3);
                    o.color=float4(blue*alpha,alpha);
                }
                float4 clip=UnityWorldToClipPos(i.world);o.depth=clip.z/clip.w;
                if(_Grounded>.5)
                {
                    float3 ray=i.world-_WorldSpaceCameraPos;
                    float3 upright=_WorldSpaceCameraPos+ray*(i.plane/dot(ray,GroundNormal()));
                    clip=UnityWorldToClipPos(upright);o.depth=clip.z/clip.w;
                }
                #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                    o.depth=o.depth*.5+.5;
                #endif
                return o;
            }
            ENDCG
        }
    }
}
