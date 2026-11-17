Shader "YesterdayConvoy/FormationNativeSurface"
{
    Properties
    {
        _MainTex ("Original albedo", 2D) = "white" {}
        _Color ("Original albedo tint", Color) = (1,1,1,1)
        _CheckerTint ("Board cell tint", Color) = (1,1,1,1)
        _BumpMap ("Original normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _MetallicGlossMap ("Original metallic / smoothness", 2D) = "black" {}
        _GlossMapScale ("Smoothness scale", Float) = 1
        _EmissionMap ("Original emission", 2D) = "black" {}
        _EmissionColor ("Original emission color", Color) = (0,0,0,1)
        _EmissionEnabled ("Emission enabled", Float) = 0
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _AlphaClip ("Alpha clip enabled", Float) = 0
        _NativeAmbient ("Original RGBM ambient atlas", 2D) = "white" {}
        _NativeReflection ("Original RGBM reflection", Cube) = "black" {}
        _AmbientGain ("Ambient RGBM scale", Float) = 5
        _ReflectionGain ("Reflection probe strength", Float) = 0.93
        _ReflectionDecode ("Reflection RGBM scale", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex, _BumpMap, _MetallicGlossMap, _EmissionMap, _NativeAmbient;
            samplerCUBE _NativeReflection;
            float4 _MainTex_ST;
            float4 _Color, _CheckerTint, _EmissionColor;
            float _BumpScale, _GlossMapScale, _EmissionEnabled, _Cutoff, _AlphaClip;
            float _AmbientGain, _ReflectionGain, _ReflectionDecode;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 ambientUv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float3 worldTangent : TEXCOORD4;
                float3 worldBitangent : TEXCOORD5;
                SHADOW_COORDS(6)
                UNITY_FOG_COORDS(7)
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // These source UV2 coordinates are already in atlas space. No renderer ST.
                o.ambientUv = v.uv2;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangent.w * unity_WorldTransformParams.w;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 texel = tex2D(_MainTex, i.uv);
                if (_AlphaClip > 0.5) clip(texel.a - _Cutoff);
                float3 albedo = texel.rgb * _Color.rgb;
                float3 tangentNormal = UnpackNormal(tex2D(_BumpMap, i.uv));
                tangentNormal.xy *= _BumpScale;
                tangentNormal.z = sqrt(saturate(1.0 - dot(tangentNormal.xy, tangentNormal.xy)));
                float3 n = normalize(i.worldTangent * tangentNormal.x + i.worldBitangent * tangentNormal.y + i.worldNormal * tangentNormal.z);
                float3 view = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 light = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float4 ambient = tex2D(_NativeAmbient, i.ambientUv);
                float3 irradiance = ambient.rgb * ambient.a * _AmbientGain;
                float4 packed = tex2D(_MetallicGlossMap, i.uv);
                float metallic = saturate(packed.r);
                float smoothness = saturate(packed.a * _GlossMapScale);
                float perceptualRoughness = 1.0 - smoothness;
                float roughness = max(0.002, perceptualRoughness * perceptualRoughness);
                float roughness2 = roughness * roughness;
                float3 halfDir = normalize(light + view);
                float nh = saturate(dot(n, halfDir));
                float lh = saturate(dot(light, halfDir));
                float d = nh * nh * (roughness2 - 1.0) + 1.00001;
                float spec = roughness2 / (d*d * max(0.1, lh*lh) * (roughness + 0.5) * 4.0);
                // The approved preview uses the Gamma-compatible BRDF2 approximation.
                spec = sqrt(max(0.0001, spec));
                float3 specColor = lerp(float3(0.220916301,0.220916301,0.220916301), albedo, metallic);
                float3 diffuse = albedo * 0.779083699 * (1.0 - metallic);
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
                // Scene owner supplies source.color.gamma and intensity 3. No second gamma conversion.
                float3 sun = _LightColor0.rgb * saturate(dot(n, light)) * attenuation;
                float3 r = reflect(-view, n);
                // Source map used XY ground / -Z up. Project uses XZ ground / +Y up.
                float3 sourceReflection = float3(r.x, r.z, -r.y);
                float4 cubeRough = texCUBElod(_NativeReflection, float4(sourceReflection, 5.0));
                float4 cubeSmooth = texCUBElod(_NativeReflection, float4(sourceReflection, 3.0));
                float4 cube = lerp(cubeRough, cubeSmooth, smoothstep(0.2, 0.7, smoothness));
                float3 reflectedLight = cube.rgb * cube.a * _ReflectionDecode * _ReflectionGain;
                float nv = saturate(dot(n, view));
                float grazing = saturate(smoothness + 0.220916301);
                float3 fresnel = lerp(specColor, float3(grazing,grazing,grazing), pow(1.0-nv, 4.0));
                float reduction = 1.0 - 0.28 * roughness * perceptualRoughness;
                float3 emission = tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb * _EmissionEnabled;
                // Tint the complete lighting result once so rough metallic-grey cells stay readable.
                float4 color = float4(saturate((diffuse*irradiance + (diffuse+specColor*spec)*sun + reflectedLight*fresnel*reduction + emission) * _CheckerTint.rgb), 1.0);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff, _AlphaClip;
            struct v2f { V2F_SHADOW_CASTER; float2 uv:TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                if (_AlphaClip > 0.5) clip(tex2D(_MainTex, i.uv).a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
