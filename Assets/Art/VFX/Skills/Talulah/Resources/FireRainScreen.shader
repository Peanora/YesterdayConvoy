Shader "Hidden/YesterdayConvoy/FireRainScreen"
{
    Properties { _MainTex ("Scene",2D)="white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Fire;
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 uv=.5+(i.uv-.5)*(1-_Fire.x*.01)+_Fire.zw;
                fixed4 c=tex2D(_MainTex,uv);
                float edge=saturate(dot((i.uv-.5)*float2(1.88,1.57),(i.uv-.5)*float2(1.88,1.57)));
                float warmth=_Fire.x*(.032+.025*edge)+_Fire.y*.022;
                c.rgb=lerp(c.rgb,float3(.482,.122,.086),warmth);
                return c;
            }
            ENDCG
        }
    }
}
