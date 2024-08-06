Shader "Rendering/Other/Cosine Gradient"
{
    Properties
    {
        _Phase("Phase", Vector) = (0.28, 0.47, 0.0, 0.0)
        _Amplitude("Amplitude", Vector) = (3.33, 0.28, 0.37, 0.0)
        _Frequency("Frequency", Vector) = (0.00, 0.6, 0.3, 0.0)
        _Offset("Offset", Vector) = (0.0, 0.0, 0.13, 0.0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Cosine Gradient"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

            #include "../Lib/Core.hlsl"

            //--------------------------------------
            // 顶点结构体
            struct Attributes
            {
                float3 positionOS   : POSITION;
                half2 texcoord      : TEXCOORD0;
            };

            //--------------------------------------
            // 片元结构体
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 texcoord     : TEXCOORD0;
            };

            uniform half4 _Phase;
            uniform half4 _Amplitude;
            uniform half4 _Frequency;
            uniform half4 _Offset;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.texcoord = input.texcoord;
                return output;
            }

             half4 frag(Varyings input) : SV_Target0
            {
                half3 color = CosineGradient(input.texcoord.x, _Phase, _Amplitude, _Frequency, _Offset).rgb;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
