Shader "YesterdayConvoy/MarineTide"
{
    Properties
    {
        _MainTex ("Original Nethersea brand", 2D) = "white" {}
        _DetailTex ("Original secondary membrane", 2D) = "white" {}
        _VeinTex ("Original vein mask", 2D) = "white" {}
        _FlowTex ("Original distortion flow", 2D) = "gray" {}
        _MarineTime ("Simulation time", Float) = 0
        _Growth ("Real cell growth", Float) = 1
        _BornAge ("Simulation age", Float) = 0
        _Active ("Living sea creatures", Float) = 1
        _Highlight ("Range inspection", Float) = 0
        _Seed ("Contact root", Float) = 0
        _Edges ("Exposed cell edges", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_DetailTex,_VeinTex,_FlowTex;
            float _MarineTime,_Growth,_BornAge,_Active,_Highlight,_Seed;
            float4 _Edges;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float t=_MarineTime;
                float2 flow=tex2D(_FlowTex,i.uv+float2(t*.018,-t*.024)).rg-.5;
                float2 uv=saturate(i.uv+flow*float2(.022,.030)*_Active);
                fixed4 ground=tex2D(_MainTex,uv);
                fixed4 detail=tex2D(_DetailTex,saturate(uv+float2(0,sin(t*.7)*.005)));
                float vein=tex2D(_VeinTex,i.uv).r;
                float radial=length(i.uv-.5);
                float reveal=smoothstep(-.065,.065,_Growth*.89-radial+vein*.08-.06);
                float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                float boundary=max(max((1-smoothstep(.012,.060,i.uv.x))*_Edges.x,(1-smoothstep(.012,.060,1-i.uv.x))*_Edges.y),
                    max((1-smoothstep(.012,.060,i.uv.y))*_Edges.z,(1-smoothstep(.012,.060,1-i.uv.y))*_Edges.w));
                float pulse=.5+.5*sin(t*1.6+vein*4);
                float newborn=(1-smoothstep(.25,1.15,_BornAge))*_Active;
                float3 color=lerp(ground.rgb,detail.rgb,.23)*lerp(.58,1,_Active);
                color+=float3(.09,.31,.38)*vein*(.055+pulse*.065)*_Active;
                color+=float3(.22,.65,.70)*boundary*(_Highlight*.95+newborn*.35);
                float root=(1-smoothstep(.055,.075,abs(radial-.16)))*_Seed*newborn;
                color+=float3(.14,.52,.55)*root*.35;
                float alpha=max(ground.a,detail.a*.5)*reveal*lerp(.40,.88,_Active);
                alpha=max(alpha,boundary*_Highlight*.93*reveal);
                alpha*=smoothstep(0,.009,edge);
                return fixed4(color,alpha);
            }
            ENDCG
        }
    }
}
