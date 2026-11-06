Shader "YesterdayConvoy/GuerrillaOrder"
{
    Properties { _MainTex ("Layer texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        // Accumulate genuinely independent portrait/glass/shard meshes as premultiplied layers.
        Pass
        {
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 flags:TEXCOORD1; float4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 flags:TEXCOORD1; float4 color:COLOR; };
            Output vert(Input v)
            {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.flags=v.flags; o.color=v.color; return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 sampled=tex2D(_MainTex,i.uv);
                sampled=lerp(sampled,fixed4(1,1,1,sampled.a),saturate(i.flags.y));
                fixed4 color=i.color*lerp(fixed4(1,1,1,1),sampled,saturate(i.flags.x));
                return fixed4(color.rgb*color.a,color.a);
            }
            ENDCG
        }
        // IMGUI performs one normal screen blend. Convert the complete layer image to
        // straight alpha first so its dim edges are not multiplied by alpha a second time.
        Pass
        {
            Blend Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img i):SV_Target
            {
                fixed4 color=tex2D(_MainTex,i.uv);
                return color.a>.0001 ? fixed4(saturate(color.rgb/color.a),color.a) : fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
