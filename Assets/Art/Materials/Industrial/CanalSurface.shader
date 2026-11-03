Shader "YesterdayConvoy/Static Canal"
{
    Properties
    {
        _Deep("Deep water", Color) = (0.025,0.105,0.12,1)
        _Shallow("Water reflections", Color) = (0.075,0.29,0.27,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; };
            fixed4 _Deep, _Shallow;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xz; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // World-space, static currents: tiling and animation cannot drift against the board.
                float2 p=i.world;
                float n=noise(p*1.7)*.6+noise(p*4.3)*.27+noise(p*11.1)*.13;
                float swell=sin(p.x*8+p.y*17+noise(p*2.2)*9)*.5+.5;
                float vein=pow(swell,12)*smoothstep(.37,.72,n);
                float shade=smoothstep(.15,.86,n);
                float3 color=lerp(_Deep.rgb,_Shallow.rgb,shade);
                color+=vein*float3(.025,.065,.056);
                return float4(color,1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
