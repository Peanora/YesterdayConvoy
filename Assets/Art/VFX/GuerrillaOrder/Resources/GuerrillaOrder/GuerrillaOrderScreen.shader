Shader "Hidden/YesterdayConvoy/GuerrillaOrderScreen"
{
    Properties { _MainTex("Live battlefield",2D)="white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Transform;
            float _Aspect;
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 p=(i.uv-.5-_Transform.xy)/max(1,_Transform.z);
                p.x*=_Aspect;
                float sine=sin(-_Transform.w),cosine=cos(-_Transform.w);
                p=float2(p.x*cosine-p.y*sine,p.x*sine+p.y*cosine);
                p.x/=_Aspect;
                return tex2D(_MainTex,saturate(p+.5));
            }
            ENDCG
        }
    }
}
