Shader "Rendering/Vegetation/Grass"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Color)]
        [Linear]_Color1("Color 1", Vector) = (1, 1, 1, 1)
        [Linear]_Color2("Color 2", Vector) = (1, 1, 1, 1)
        _ColorVariation("Color Variation", Range(0, 1)) = 1.0

        _GrassShadowColor("阴影色", Color) = (0.7, 0.7, 0.7, 1.0)

        [Header(Noise)]
        [Toggle(_USE_NOISE_WORLD_SPACE_UVS)]_NoiseWorldSpaceUVs("UV是否是世界坐标", float) = 0
        _Noise("Noise", 2D) = "white" {}
		_NoiseTiling("Noise Tiling", float) = 1

        [Header(Specular)]
        _GrassShininess("光滑度", Range(0, 1)) = 0.25
        _GrassSpecularScale("高光强度", Range(0, 5)) = 1

        [Header(Wind)]
        [Toggle(_USE_SWING)]_Swing("摆动开启", int) = 0
        _SwingFeq("最小摆动频率", float) = 0.1
        _SwingFeqMax("最大摆动频率", float) = 0.6
        _SwingScale("修正频率", float) = 1
        _SwingAmp("摆动幅度", float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"  "Queue" = "AlphaTest" }
        Cull Off
		AlphaToMask On

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
		    #pragma shader_feature_local _USE_NOISE_WORLD_SPACE_UVS
            #pragma shader_feature_local _USE_SWING

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

            #define USE_ALPHA_CUTOFF 1

			#include "../Lib/Pass/ShadowCasterPass.hlsl"

			ENDHLSL
		}
    }
}
