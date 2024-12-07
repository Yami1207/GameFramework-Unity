Shader "Rendering/Character/Star Rail/Face"
{
    Properties
    {
        _AlbedoTex("Main Texture", 2D) = "white" {}
		[Linear]_Color("Color", Vector) = (1, 1, 1, 1)

		[NoScaleOffset]_FaceTex("Face Tex", 2D) = "white" {}
        [NoScaleOffset]_FaceExpressionTex("Face Expression Tex", 2D) = "white" {}

		[Header(Diffuse)]
        [Linear]_ShadowColor("Face Shadow Color", Vector) = (0.98225, 0.75294, 0.76052, 1)
        [Linear]_EyeShadowColor("Eye Shadow Color", Vector) = (0.8563, 0.72534, 0.7919, 1)

		//[Header(Nose Line)]
        //[Linear]_NoseLineColor("Color", Vector) = (1, 1, 1, 1)
        //_NoseLinePower("Power", Range(0, 8)) = 1

		[Header(Expression)]
		[Linear]_ExCheekColor("Cheek Color", Vector) = (0.95761, 0.43328, 0.43328, 1)
		[Linear]_ExShyColor("Shy Color", Vector) = (0.00246, 0.00553, 0.13024, 1)
		[Linear]_ExShadowColor("Shadow Color", Vector) = (0.00246, 0.00553, 0.13024, 1)
		[Linear]_ExEyeShadowColor("Eye Shadow Color", Vector) = (0.8563, 0.72534, 0.7919, 1)

		[Header(Emission)]
		[Toggle(_USE_EMISSION)]_UseEmission("开启自发光", Float) = 0
        [Linear]_EmissionColor("Color", Vector) = (1, 1, 1, 1)
        _EmissionThreshold("Threshold", Range(0, 1)) = 0.6
        _EmissionIntensity("Intensity", Float) = 0.77

		[Header(Outline)]
		[Linear]_OutlineColor("Color", Vector) = (0.5, 0.5, 0.5, 1)
        _OutlineWidth("Width", Range(0, 4)) = 1
        _OutlineZOffset("Z Offset(View Space)", Range(0, 1)) = 0.0001

		[Header(Rendering)]
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Float) = 2

		[Header(Expression Control Params)]
		_ExCheekIntensity("Expression Cheek", Range(0.0, 1.0)) = 0.5
		_ExShyIntensity("Expression Shy", Range(0.0, 1.0)) = 0.0
		_ExShadowIntensity("Expression Shadow", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
			Cull [_CullMode]
            ZWrite [_ZWrite]

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

			// -------------------------------------
            // 自定义keywords
			#pragma shader_feature_local _USE_EMISSION

			//--------------------------------------
            // 自定义宏
			#define CHARACTER_FACE_PASS

            #include "CharacterFacePass.hlsl"

			ENDHLSL
        }

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ZWrite On
            ZTest LEqual
			Cull [_CullMode]
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment
			#pragma target 3.0

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "../../Lib/Pass/ShadowCasterPass.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}
			Cull [_CullMode]
			ColorMask 0

			HLSLPROGRAM
			#pragma target 4.5

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			#include "../../Lib/Pass/DepthOnlyPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Outline"
			Tags{ "LightMode" = "Outline" }

			Blend One Zero
			ZWrite On
			Cull Front
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex OutlineVertex
			#pragma fragment OutlineFragment

			//--------------------------------------
            // 自定义宏
			#define CHARACTER_FACE_PASS

			#include "../Pass/OutlinePass.hlsl"
			ENDHLSL
		}
    }

	CustomEditor "StarRailCharacterFaceShaderGUI"
}
