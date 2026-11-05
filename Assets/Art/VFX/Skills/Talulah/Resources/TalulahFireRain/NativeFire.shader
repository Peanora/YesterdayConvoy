Shader "Yesterday Convoy/Talulah Native Fire"
{
    Properties { [PerRendererData] _MainTex("Baked native fire",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
        Cull Off ZWrite Off ZTest LEqual Lighting Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct In {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct Out {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float worldX:TEXCOORD1;};
            sampler2D _MainTex;fixed4 _Color;float4 _GroundBounds;
            Out vert(In i){Out o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color*_Color;o.worldX=mul(unity_ObjectToWorld,i.vertex).x;return o;}
            fixed4 frag(Out i):SV_Target
            {
                clip(i.worldX-_GroundBounds.x);clip(_GroundBounds.y-i.worldX);
                fixed4 c=tex2D(_MainTex,i.uv);return fixed4(c.rgb*i.color.rgb*i.color.a,c.a*i.color.a);
            }
            ENDCG
        }
    }
}
