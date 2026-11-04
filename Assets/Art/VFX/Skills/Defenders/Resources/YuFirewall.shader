Shader "YesterdayConvoy/YuFirewall"
{
    Properties
    {
        _MainTex ("Texture",2D)="white" {}
        _RedFlame ("Red flame",Float)=1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend",Float)=10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest LEqual Blend SrcAlpha [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;float _RedFlame;
            v2f vert(appdata v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                // Keep texture detail/alpha; concentrate warmth in crimson and scarlet, not yellow-white.
                float heat=dot(c.rgb,float3(.3,.5,.2));
                float3 red=lerp(float3(.38,.008,.018),float3(1,.12,.025),saturate(heat*1.3));
                red=lerp(red,float3(1,.46,.21),smoothstep(.48,.82,heat)*.8);
                c.rgb=lerp(c.rgb,red,_RedFlame);
                return c*i.color;
            }
            ENDCG
        }
    }
}
