Shader "Rendering/Terrian/Low"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        
        _HeightMap("Height Map", 2D) = "black" {}
		_VertexNormalMap("Vertex Normal Map", 2D) = "black" {}

        _TerrianParam("Block Param", Vector) = (1024.0, 1024.0, 0.0, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry-100" }
		Blend One Zero

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile_fog

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup

            #include "../Lib/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TerrianParam;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            TEXTURE2D(_VertexNormalMap);
            SAMPLER(sampler_VertexNormalMap);

            struct Attributes
            {
                float3 positionOS : POSITION;
	            UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half2 texcoord      : TEXCOORD0;
                half3 normalWS      : TEXCOORD1;
                half4 lightingFog   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct FragData
            {
                half4 color : SV_Target0;
                float4 normal : SV_Target1;
            };

            #include "../Lib/Instancing.hlsl"

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float2 texcoord = (positionWS.xz - _TerrianParam.zw) / _TerrianParam.xy;
                positionWS.y = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, texcoord, 0.0).r;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.texcoord = texcoord;

                // ·¨Ïß
                float3 normalWS = SAMPLE_TEXTURE2D_LOD(_VertexNormalMap, sampler_VertexNormalMap, texcoord, 0.0).rgb;
                output.normalWS = 2.0 * normalWS - 1.0;

                Light mainLight = GetMainLight();
                half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;

                half NoL = dot(mainLight.direction, output.normalWS);
                half3 diffuseColor = attenuatedLightColor * (NoL * 0.5 + 0.5);
                output.lightingFog = half4(diffuseColor, ComputeFogFactor(output.positionCS.z));

                return output;
            }

            FragData frag(Varyings input)
            {
                half3 color = G2L(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord.xy).rgb);
                color = MixFog(color, input.lightingFog.w);

                FragData output = (FragData) 0;
                output.color = half4(L2G(color), 1.0);
                output.normal = float4(input.normalWS * 0.5 + 0.5, 0.0);
                return output;
            }
            ENDHLSL
        }

        Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ColorMask 0

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 3.0

			#pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma instancing_options procedural:Setup

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			#include "Pass/DepthOnlyPass.hlsl"
			ENDHLSL
		}
    }
}
