Shader "Rendering/Custom/Lit"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        [Linear]_BaseColor("颜色", Vector) = (1, 1, 1, 1)

        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Scale", Float) = 1.0

        _Metallic("Metallic", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0

        _EmissionIntensity("Emission Intensity", Range(0, 1)) = 0
        [Linear]_EmissionColor("Emission Color", Vector) = (0, 0, 0, 1)

		[Toggle(_USE_PDO)]_PDO_ON("开启PDO", int) = 0
        _DepthDiffer("深度差", float) = 1.0

        [MaterialEnum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Int) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"  "Queue" = "Geometry" }
        Blend One Zero
        Cull[_Cull]

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
            //#pragma instancing_options assumeuniformscaling lodfade nolightprobe nolightmap
			#pragma instancing_options procedural:Setup

			// -------------------------------------
			// URP keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

			// -------------------------------------
            // 自定义keywords
            #pragma shader_feature_local _USE_PDO

            //--------------------------------------
            // 自定义宏
            #define USE_BUMP_MAP    1

            #include "LitPass.hlsl"

			ENDHLSL
        }

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment
			#pragma target 3.0

			#pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma instancing_options procedural:Setup

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "../Lib/Pass/ShadowCasterPass.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ColorMask 0

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma instancing_options procedural:Setup

			#include "../Lib/Pass/DepthOnlyPass.hlsl"
			ENDHLSL
		}
    }
}
