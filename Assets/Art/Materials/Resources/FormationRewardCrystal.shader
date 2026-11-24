Shader "Yesterday Convoy/Formation Reward Crystal"
{
    Properties { _Boost ("Light", Float) = 1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Boost;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; fixed4 color : COLOR; };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex=UnityObjectToClipPos(input.vertex);
                output.normal=UnityObjectToWorldNormal(input.normal);
                output.view=WorldSpaceViewDir(input.vertex);
                output.color=input.color;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float3 normal=normalize(input.normal);
                float3 view=normalize(input.view);
                float3 light=normalize(float3(-.46,.86,-.22));
                float diffuse=saturate(dot(normal,light));
                float glow=pow(1-saturate(dot(normal,view)),3)*.20;
                float shine=pow(saturate(dot(normal,normalize(light+view))),18)*.32;
                return fixed4(saturate(input.color.rgb*(.52+.48*diffuse)*_Boost+glow*input.color.rgb+shine),1);
            }
            ENDCG
        }
    }
}
