Shader "YesterdayConvoy/Tactical Actor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
            struct actor_v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 world : TEXCOORD1;
                float plane : TEXCOORD2;
            };
            float3 GroundNormal()
            {
                return normalize(float3(UNITY_MATRIX_V[2].x,0,UNITY_MATRIX_V[2].z));
            }
            actor_v2f vert(appdata_t v)
            {
                v2f sprite=SpriteVert(v);
                actor_v2f o;o.vertex=sprite.vertex;o.uv=sprite.texcoord;o.color=sprite.color;
                o.world=mul(unity_ObjectToWorld,UnityFlipSprite(v.vertex.xyz,_Flip)).xyz;
                float3 foot=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
                o.plane=dot(foot-_WorldSpaceCameraPos,GroundNormal());
                return o;
            }
            struct actor_output { fixed4 color : SV_Target; float depth : SV_Depth; };
            actor_output frag(actor_v2f i)
            {
                actor_output o;
                o.color=SampleSpriteTexture(i.uv)*i.color;o.color.rgb*=o.color.a;
                // Draw an undistorted billboard, but test depth on the vertical plane at its feet.
                // This prevents a high tile slicing through the middle of a camera-facing card.
                float3 ray=i.world-_WorldSpaceCameraPos;
                float3 upright=_WorldSpaceCameraPos+ray*(i.plane/dot(ray,GroundNormal()));
                float4 clip=UnityWorldToClipPos(upright);o.depth=clip.z/clip.w;
                #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                    o.depth=o.depth*.5+.5;
                #endif
                return o;
            }
            ENDCG
        }
    }
}
