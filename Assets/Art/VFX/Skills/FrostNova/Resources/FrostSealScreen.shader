Shader "Hidden/YesterdayConvoy/FrostSealScreen"
{
    Properties {_MainTex("Source",2D)="white"{}}
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Frost;
            fixed4 frag(v2f_img i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,saturate(i.uv+_Frost.zw));
                c.rgb=lerp(c.rgb,c.rgb*float3(.82,.95,1.08),_Frost.x*.45);
                c.rgb=lerp(c.rgb,float3(.82,.94,.99),_Frost.y*.12);
                return c;
            }
            ENDCG
        }
    }
}
