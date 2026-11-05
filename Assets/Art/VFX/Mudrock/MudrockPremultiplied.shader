Shader "Yesterday Convoy/Mudrock Original Baked"
{
    Properties { [PerRendererData] _MainTex("Original effect",2D)="white"{} _Color("Color",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            sampler2D _MainTex;fixed4 _Color;
            Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(Output i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv);return fixed4(c.rgb*i.color.rgb*i.color.a,c.a*i.color.a);}
            ENDCG
        }
    }
}
