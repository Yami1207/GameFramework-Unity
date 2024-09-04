Shader "Hidden/Reflection/ScreenSpaceReflection"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "../../../Shader/Lib/Instancing.hlsl"

            //--------------------------------------
            // 顶点结构体
            struct Attributes
            {
                float3 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //--------------------------------------
            // 片元结构体
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);   
                output.normalWS = normalInput.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(0.5 * input.normalWS + 0.5, 1);
            }
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "ScreenSpaceReflection.hlsl"
            ENDHLSL
        }
    }
}
