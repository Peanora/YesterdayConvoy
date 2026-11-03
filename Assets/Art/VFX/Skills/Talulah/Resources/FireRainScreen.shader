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
                float2 uv=.5+(i.uv-.5)*(1-_Fire.x*.016)+_Fire.zw;
                fixed4 c=tex2D(_MainTex,uv);
                float3 dark=c.rgb*float3(.99,.94,.92)*.88;
                float3 warm=float3(.75,.24,.10);
                float3 soft=(1-2*warm)*dark*dark+2*warm*dark;
                float3 tinted=lerp(dark,soft,.28);
                float vignette=saturate((length((i.uv-.5)*float2(1.2,1))-.25)*1.3);
                tinted*=1-vignette*.22;
                c.rgb=lerp(c.rgb,tinted,_Fire.x);
                c.rgb=1-(1-c.rgb)*(1-float3(1,.67,.31)*_Fire.y*.12);
                return c;
            }
            ENDCG
        }
    }
}
