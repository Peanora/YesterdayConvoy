Shader "YesterdayConvoy/GuerrillaOrder"
{
    Properties
    {
        _MainTex ("Approved illustration", 2D) = "white" {}
        _MembersA ("Soldier sniper hound Patriot", Vector) = (0,0,0,0)
        _MembersB ("Sarkaz shield herald extension A", Vector) = (0,0,0,0)
        _Member8 ("Extension B", Float) = 0
        _Opacity ("Fade", Range(0,1)) = 0
        _ExtraA ("Extension A sprite", 2D) = "black" {}
        _ExtraB ("Extension B sprite", 2D) = "black" {}
        _ExtraUvA ("Extension A texture rectangle", Vector) = (0,0,1,1)
        _ExtraUvB ("Extension B texture rectangle", Vector) = (0,0,1,1)
        _ExtraScreenA ("Extension A screen rectangle", Vector) = (0,0,1,1)
        _ExtraScreenB ("Extension B screen rectangle", Vector) = (0,0,1,1)
        _ExtraPresentA ("Extension A portrait available", Float) = 0
        _ExtraPresentB ("Extension B portrait available", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        // Preserve straight RGB and the fade alpha in the intermediate image. IMGUI performs
        // the single screen blend when its foreground draw is processed with GUI.depth.
        Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _ExtraA, _ExtraB;
            float4 _MembersA, _MembersB, _ExtraUvA, _ExtraUvB, _ExtraScreenA, _ExtraScreenB;
            float _Member8, _Opacity, _ExtraPresentA, _ExtraPresentB;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Output vert(Input v) { Output o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            float Left(float y) { return .07 + .04 * (.5-y); }
            float Right(float y) { return .93 + .04 * (.5-y); }
            float NativeSlot(float2 p, out float edge)
            {
                float left = p.y < .365 ? lerp(.355,.326,p.y/.365) : .326 + (.421-.326)*(p.y-.365)/.44;
                float right = .748-.215*p.y;
                float l1 = .366+.354*p.x, l2 = .613+.4675*p.x;
                float r1 = 1.027-.647*p.x, r2 = 1.184-.579*p.x;
                edge = min(abs(p.x-left),abs(p.x-right));
                if (p.x < left) { edge=min(edge,min(abs(p.y-l1),abs(p.y-l2))); return p.y<l1 ? _MembersA.x : p.y<l2 ? _MembersA.y : _MembersA.z; }
                if (p.x > right) { edge=min(edge,min(abs(p.y-r1),abs(p.y-r2))); return p.y<r1 ? _MembersB.x : p.y<r2 ? _MembersB.y : _MembersB.z; }
                return _MembersA.w;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex,i.uv);
                float2 p = float2(i.uv.x,1-i.uv.y);
                float edge;
                float member = NativeSlot(p,edge);
                // Keep the shared title, not any unselected character beneath its caption band.
                float captionArea = step(.335,p.x)*step(p.x,.638)*step(.718,p.y)*step(p.y,.970);
                float low = min(original.r,min(original.g,original.b));
                float high = max(original.r,max(original.g,original.b));
                float title = captionArea*smoothstep(.66,.84,low)*(1-smoothstep(.15,.30,high-low));
                float grain = frac(sin(dot(floor(p*float2(1672,941)),float2(12.9898,78.233)))*43758.5453);
                float3 empty = float3(.022,.025,.030) + grain*.008;
                float seam = 1-smoothstep(.001,.003,edge);
                empty += float3(.66,.19,.025)*seam*.65;
                float3 color = lerp(empty,original.rgb,saturate(member+title));
                float leftDistance = p.x-Left(p.y), rightDistance = Right(p.y)-p.x;
                float side = min(leftDistance,rightDistance);
                if (side < .003)
                {
                    float trim = 1-smoothstep(.001,.003,abs(side));
                    color = empty*.75 + float3(.68,.22,.035)*trim;
                    float2 localA=(p-_ExtraScreenA.xy)/max(_ExtraScreenA.zw,.0001);
                    float2 localB=(p-_ExtraScreenB.xy)/max(_ExtraScreenB.zw,.0001);
                    float insideA=step(0,localA.x)*step(localA.x,1)*step(0,localA.y)*step(localA.y,1);
                    float insideB=step(0,localB.x)*step(localB.x,1)*step(0,localB.y)*step(localB.y,1);
                    fixed4 portraitA=tex2D(_ExtraA,_ExtraUvA.xy+float2(localA.x,1-localA.y)*_ExtraUvA.zw);
                    fixed4 portraitB=tex2D(_ExtraB,_ExtraUvB.xy+float2(localB.x,1-localB.y)*_ExtraUvB.zw);
                    color=lerp(color,portraitA.rgb,portraitA.a*insideA*_ExtraPresentA*_MembersB.w*step(p.x,Left(p.y)));
                    color=lerp(color,portraitB.rgb,portraitB.a*insideB*_ExtraPresentB*_Member8*step(Right(p.y),p.x));
                }
                return fixed4(color,_Opacity);
            }
            ENDCG
        }
    }
}
