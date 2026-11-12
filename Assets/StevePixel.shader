Shader "Steve/Pixel Skin"
{
    Properties
    {
        _MainTex ("Skin", 2D) = "white" {}
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

            sampler2D _MainTex;
            struct VertexInput
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half shade : TEXCOORD1;
            };
            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                half3 normal = UnityObjectToWorldNormal(input.normal);
                output.shade = 0.86h + 0.14h * abs(normal.z)
                               + 0.14h * max(normal.y, 0.0h)
                               - 0.14h * max(-normal.y, 0.0h);
                return output;
            }
            half4 frag(VertexOutput input) : SV_Target
            {
                return half4(tex2D(_MainTex, input.uv).rgb * input.shade, 1.0h);
            }
            ENDCG
        }
    }
}
