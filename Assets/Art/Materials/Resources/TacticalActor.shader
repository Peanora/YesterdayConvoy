Shader "YesterdayConvoy/Tactical Actor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _PoseTex ("Adjacent pose", 2D) = "black" {}
        [PerRendererData] _PoseWeight ("Adjacent pose weight", Float) = 0
        [PerRendererData] _PoseUv ("Adjacent pose UV transform", Vector) = (0,0,0,0)
        [PerRendererData] _PoseRect ("Adjacent pose UV bounds", Vector) = (0,0,0,0)
        [PerRendererData] _ContactPulse ("Contact tint and weight", Vector) = (0,0,0,0)
        _Color ("Tint", Color) = (1,1,1,1)
        _EnvironmentTint ("Local ambient tint", Color) = (1,1,1,1)
        _BodyHeight ("Body height", Float) = 1.25
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
            fixed4 _EnvironmentTint;
            float _BodyHeight;
            sampler2D _PoseTex;
            float _PoseWeight;
            float4 _PoseUv, _PoseRect, _ContactPulse;
            struct actor_v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 world : TEXCOORD1;
                float plane : TEXCOORD2;
                float bodyHeight : TEXCOORD3;
                float2 poseUv : TEXCOORD4;
            };
            float3 GroundNormal()
            {
                return normalize(float3(UNITY_MATRIX_V[2].x,0,UNITY_MATRIX_V[2].z));
            }
            actor_v2f vert(appdata_t v)
            {
                v2f sprite=SpriteVert(v);
                actor_v2f o;o.vertex=sprite.vertex;o.uv=sprite.texcoord;o.color=sprite.color;
                o.bodyHeight=saturate(v.vertex.y/max(.1,_BodyHeight));
                float4 local=UnityFlipSprite(v.vertex.xyz,_Flip);
                o.world=mul(unity_ObjectToWorld,local).xyz;
                o.poseUv=local.xy*_PoseUv.xy+_PoseUv.zw;
                float3 foot=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
                o.plane=dot(foot-_WorldSpaceCameraPos,GroundNormal());
                return o;
            }
            struct actor_output { fixed4 color : SV_Target; float depth : SV_Depth; };
            actor_output frag(actor_v2f i)
            {
                actor_output o;
                fixed4 shade=i.color;
                shade.rgb*=lerp(_EnvironmentTint.rgb,fixed3(1,1,1),i.bodyHeight);
                fixed4 pixel=SampleSpriteTexture(i.uv);
                pixel.rgb*=pixel.a;
                if(_PoseWeight>0)
                {
                    fixed4 pose=tex2D(_PoseTex,clamp(i.poseUv,_PoseRect.xy,_PoseRect.zw));
                    fixed2 inside=step(_PoseRect.xy,i.poseUv)*step(i.poseUv,_PoseRect.zw);
                    pose.a*=inside.x*inside.y;
                    pose.rgb*=pose.a;
                    pixel=lerp(pixel,pose,_PoseWeight);
                }
                fixed3 bodyColor=pixel.rgb*shade.rgb;
                bodyColor=lerp(bodyColor,max(bodyColor,_ContactPulse.rgb*pixel.a),_ContactPulse.a);
                o.color=fixed4(bodyColor*shade.a,pixel.a*shade.a);
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
