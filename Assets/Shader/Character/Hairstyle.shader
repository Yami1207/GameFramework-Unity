Shader "Character/Hairstyle"
{
    Properties
    {
        [NoScaleOffset]_AlbedoTex("基础帖图(RGB:颜色 A:不透明度)", 2D) = "white" {}

		[NoScaleOffset]_MaskTex("通道贴图(R:光泽度(高光形状) G:阴影 B:高光亮度)", 2D) = "white" {}

		[Linear]_SpecularColor("高光颜色", Vector) = (0.04, 0.04, 0.04, 0)
        [Linear]_EmissionColor("自发光颜色", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

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

            // -------------------------------------
			// URP keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

			//--------------------------------------
            // 自定义宏
			#define USING_HAIRSTYLE_PASS

            #include "CharacterPass.hlsl"

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
			#pragma target 4.5

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			#include "../Lib/Pass/DepthOnlyPass.hlsl"
			ENDHLSL
		}
    }
}
