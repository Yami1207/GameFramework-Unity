Shader "Rendering/Other/Object Trail"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Object Trails"
            Tags { "LightMode" = "ObjectTrails" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

            //--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling lodfade nolightprobe nolightmap

            #include "../Lib/Core.hlsl"

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
                float3 positionWS   : TEXCOORD0;
                float4 originWS     : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform float4 _G_ObjectTrailsTexHeight;

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float3 originWS = TransformObjectToWorld(float3(0, 0, 0));
                float r = length(positionWS - originWS);

                // 压成一个平面（如果有厚度,高度就不准确）
                positionWS.y = originWS.y;

                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.originWS = float4(originWS, r);
                return output;
            }

            half4 frag(Varyings input) : SV_Target0
            {
                float h = (input.positionWS.y - _G_ObjectTrailsTexHeight.x) / (_G_ObjectTrailsTexHeight.y - _G_ObjectTrailsTexHeight.x);

                // 转换成极坐标
                float2 offset = input.positionWS.xz - input.originWS.xz;
                float theta = atan2(offset.y, offset.x) * INV_TWO_PI + 0.5;
                float len = length(input.positionWS - input.originWS.xyz);
                return half4(theta, len, h, input.originWS.w);
            }
            ENDHLSL
        }
    }
}
