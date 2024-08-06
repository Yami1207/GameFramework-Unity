Shader "Rendering/Custom/Foliage"
{
    Properties
    {
        [NoScaleOffset]_BaseMap("Albedo", 2D) = "white" {}

        [Toggle(_USE_GRADIENT_COLOR)]_UseGradientColor("Use Gradient Color", Float) = 0
        [Linear]_BaseColor("颜色", Vector) = (1, 1, 1, 1)
        [Linear]_BaseBottomColor("底部颜色", Vector) = (1, 1, 1, 1)
        _ColorMaskHeight("Color Mask Height", Range(0.0, 2.0)) = 1.0

        // 透明通道裁剪(只在面板设置用)
		[Toggle(_USE_ALPHA_CUTOFF)]_UseAlphaCutoff("Use Alpha Off", Float) = 0
		_AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0.35

        [Toggle(_ENABLE_SSS_ON)]_EnableSubsurfaceScattering("次表面散射", Float) = 0
		_SubsurfaceRadius("Subsurface Radius", Range(0.0, 1.0)) = 1.0
		[Linear]_SubsurfaceColor("Subsurface Color", Vector) = (1, 1, 1, 1)
        _SubsurfaceColorIntensity("Subsurface Color Color", Float) = 1

        [MaterialEnum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Int) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"  "Queue" = "AlphaTest" }
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
            #pragma multi_compile __ _USE_WIND_OFF _USE_WIND_ON _USE_WIND_WAVE

            #pragma shader_feature_local _USE_GRADIENT_COLOR
            #pragma shader_feature_local _USE_ALPHA_CUTOFF
            #pragma shader_feature_local _ENABLE_SSS_ON

            //--------------------------------------
            // 自定义宏
            #define USE_HALF_LAMBERT    1

            #include "FoliagePass.hlsl"

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
            #pragma instancing_options assumeuniformscaling nolightprobe nolightmap
            #pragma instancing_options procedural:Setup

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			// -------------------------------------
            // 自定义keywords
            #pragma multi_compile __ _USE_WIND_OFF _USE_WIND_ON _USE_WIND_WAVE
            #pragma shader_feature_local _USE_ALPHA_CUTOFF

			#include "FoliagePass.hlsl"

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
			#pragma instancing_options assumeuniformscaling nolightprobe nolightmap
            #pragma instancing_options procedural:Setup

            // -------------------------------------
            // 自定义keywords
            #pragma multi_compile __ _USE_WIND_OFF _USE_WIND_ON _USE_WIND_WAVE

			#pragma shader_feature_local _USE_ALPHA_CUTOFF

			#include "FoliagePass.hlsl"

			ENDHLSL
		}
    }

    CustomEditor "FoliageShaderGUI"
}
