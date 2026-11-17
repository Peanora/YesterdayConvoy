Shader "YesterdayConvoy/BossRosterCannonStructure"
{
    Properties { _MainTex("Original source albedo",2D)="white"{} _Color("Structure tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back ZWrite On ZTest LEqual Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            sampler2D _MainTex;fixed4 _Color;
            Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(Output i):SV_Target{return fixed4(tex2D(_MainTex,i.uv).rgb*_Color.rgb,1);}
            ENDCG
        }
    }
}
