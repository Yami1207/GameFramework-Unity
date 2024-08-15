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

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //--------------------------------------
            // 顶点结构体
            struct Attributes
            {
                float3 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            //--------------------------------------
            // 片元结构体
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
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
