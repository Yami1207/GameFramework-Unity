Shader "Rendering/Terrian/Standard"
{
    Properties
    {
		_Control("Control", 2D) = "black" {}

		_HeightMap("Height Map", 2D) = "black" {}
		_VertexNormalMap("Vertex Normal Map", 2D) = "black" {}

		_TerrianParam("Block Param", Vector) = (1024.0, 1024.0, 0.0, 0.0)

		[Header(Layer 0)]
		_Splat0("Splat", 2D) = "black" {}
		_Normal0("Normal", 2D) = "bump" {}
		_NormalScale0("Normal Scale", float) = 1.0
		_Metallic0("Metallic", Range(0, 1)) = 0
		_Smoothness0("Smoothness", Range(0, 1)) = 0

		[Header(Layer 1)]
		_Splat1("Splat", 2D) = "black" {}
		_Normal1("Normal", 2D) = "bump" {}
		_NormalScale1("Normal Scale", float) = 1.0
		_Metallic1("Metallic", Range(0, 1)) = 0
		_Smoothness1("Smoothness", Range(0, 1)) = 0

		[Header(Layer 2)]
		_Splat2("Splat", 2D) = "black" {}
		_Normal2("Normal", 2D) = "bump" {}
		_NormalScale2("Normal Scale", float) = 1.0
		_Metallic2("Metallic", Range(0, 1)) = 0
		_Smoothness2("Smoothness", Range(0, 1)) = 0

		[Header(Layer 3)]
		_Splat3("Splat", 2D) = "black" {}
		_Normal3("Normal", 2D) = "bump" {}
		_NormalScale3("Normal Scale", float) = 1.0
		_Metallic3("Metallic", Range(0, 1)) = 0
		_Smoothness3("Smoothness", Range(0, 1)) = 0
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

			// -------------------------------------
			// URP keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

			// -------------------------------------
            // 自定义keywords
			#pragma multi_compile _ _RENDER_PIXEL_DEPTH_OFFSET

			#include "TerrainPass.hlsl"

			ENDHLSL
        }

		// Pass
		// {
		// 	Name "ShadowCaster"
		// 	Tags { "LightMode" = "ShadowCaster" }
		// 	ZWrite On
  //           ColorMask 0

		// 	HLSLPROGRAM
		// 	#pragma vertex ShadowPassVertex
		// 	#pragma fragment ShadowPassFragment
		// 	#pragma target 3.0

		// 	#pragma multi_compile_instancing
  //           #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

		// 	// This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
  //           #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

		// 	#define SHADOW_CASTER_PASS

		// 	#include "TerrainPass.hlsl"

		// 	ENDHLSL
		// }

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ColorMask 0

			HLSLPROGRAM
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
