Shader "Yesterday Convoy/Talulah Ground Heat"
{
    Properties { _Age("Simulation time",Float)=0 _Strength("Envelope",Float)=0 _Left("Left",Float)=0 _Speed("Speed",Float)=2.5 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct In {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct Out {float4 vertex:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;};
            float _Age,_Strength,_Left,_Speed;
            Out vert(In i){Out o;o.vertex=UnityObjectToClipPos(i.vertex);o.world=mul(unity_ObjectToWorld,i.vertex).xyz;o.uv=i.uv;return o;}
            fixed4 frag(Out i):SV_Target
            {
                float heat=saturate((_Age-(i.world.x-_Left)/_Speed)/.55)*_Strength;
                float flow=.65+.17*sin(i.world.x*2.7+i.world.z*3.9-_Age*4.8)+.13*sin(i.world.x*5.1-i.world.z*2.3+_Age*7.1);
                float rim=saturate(min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y))*18);
                return fixed4(1,.18+.12*flow,.025,heat*flow*.17*rim);
            }
            ENDCG
        }
    }
}
