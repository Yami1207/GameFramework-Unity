Shader "Rendering/Vegetation/Grass Cutout"
{
    Properties
    {
        [Linear]_BaseColor("颜色", Vector) = (1, 1, 1, 1)
		[Linear]_GrassTipColor("草尖颜色", Vector) = (1, 1, 1, 1)
        _BaseMap("Texture", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Linear]_GrassShadowColor("阴影色", Vector) = (0.7, 0.7, 0.7, 1.0)

		_Roughness("粗糙度", Range(0, 1.0)) = 1
		_ReflectionIntensity("反射强度", Range(0, 1.0)) = 0.5

        [Toggle(_USE_SWING)]_Swing("摆动开启", int) = 0
        _SwingFeq("最小摆动频率", float) = 0.1
        _SwingFeqMax("最大摆动频率", float) = 0.6
        _SwingScale("修正频率", float) = 1
        _SwingAmp("摆动幅度", float) = 1

		[Toggle(_USE_INTERACTIVE)]_Interactive("互动草", int) = 0
		_GrassPivotPointTex("描点图", 2D) = "black" {}
		_GrassPivotPointTexUnit("描点图单位", float) = 1
		_GrassPushStrength("推力强度", float) = 1

        [MaterialEnum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Int) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"  "Queue" = "AlphaTest" }
        Blend One Zero
		AlphaToMask On
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
            #pragma instancing_options assumeuniformscaling lodfade nolightprobe nolightmap
			#pragma instancing_options procedural:Setup

			// -------------------------------------
			// URP keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            // -------------------------------------
            // 自定义keywords
            #pragma shader_feature_local _USE_SWING

			//--------------------------------------
            // 自定义宏
			#define USE_ALPHA_CUTOFF	1

			#include "Grass.hlsl"

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

			// -------------------------------------
            // 自定义keywords
            #pragma shader_feature_local _USE_SWING

			//--------------------------------------
            // 自定义宏
			#define USE_ALPHA_CUTOFF	1

			#include "Grass.hlsl"

			ENDHLSL
		}

        Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ColorMask 0

			HLSLPROGRAM

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment
			#pragma target 3.0

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma instancing_options procedural:Setup

			// -------------------------------------
            // 自定义keywords
            #pragma shader_feature_local _USE_SWING

			//--------------------------------------
            // 自定义宏
			#define USE_ALPHA_CUTOFF	1

			#include "Grass.hlsl"

			ENDHLSL
		}
    }
}
