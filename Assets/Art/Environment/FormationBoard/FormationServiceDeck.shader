Shader "YesterdayConvoy/FormationServiceDeck"
{
    Properties
    {
        _MainTex ("Service deck background", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-100" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
            };

            Interpolators Vert(float4 vertex : POSITION)
            {
                Interpolators output;
                output.position = UnityObjectToClipPos(vertex);
                // The approved art already has the board camera's steep perspective.
                // Use that projection for color, with depth supplied by the saved deck mesh.
                output.screenPosition = ComputeScreenPos(output.position);
                return output;
            }

            fixed4 Frag(Interpolators input) : SV_Target
            {
                return tex2Dproj(_MainTex, UNITY_PROJ_COORD(input.screenPosition));
            }
            ENDCG
        }
    }
}
