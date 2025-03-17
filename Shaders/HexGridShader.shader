Shader "Custom/HexGridShader"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv;
        };

        fixed4 _MainColor;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _MainColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}