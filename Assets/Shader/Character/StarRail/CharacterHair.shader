Shader "Rendering/Character/Star Rail/Hair"
{
    Properties
    {
        _AlbedoTex("Main Texture", 2D) = "white" {}
		[Linear]_Color("Color", Vector) = (1, 1, 1, 1)
		[Linear]_BackColor("Back Color", Vector) = (0.13881, 0.2171, 0.38128, 1.0)

		[NoScaleOffset]_LightMap("Light Map", 2D) = "white" {}

        [RampTexture]_CoolRampTex("Cool", 2D) = "white" {}
        [RampTexture]_WarmRampTex("Warm", 2D) = "white" {}

		[Header(Specular)]
		[HideInInspector]_SpecularHeader("Specular", Float) = 0
		[Toggle(_USE_SPECULAR)]_UseSpecular("开启高光", Float) = 1
        [Linear]_SpecularColor("Color", Vector) = (1, 1, 1, 1)
		_SpecularIntensity("Intensity", Range(0, 1)) = 1
        _SpecularShininess("Shininess", Range(0.1, 100)) = 10
        _SpecularRoughness("Roughness", Range(0, 1)) = 0.1

		[Header(Rim Light)]
		[HideInInspector]_RimLightHeader("Rim Light", Float) = 0
		[Toggle(_USE_RIM_LIGHT)]_UseRimLight("开启边缘光", Float) = 1
		[Linear]_RimColor("Color", Vector) = (1, 1, 1, 1)
		_RimWidth("Width", Float) = 0.5
		_RimLightThreshold("Threshold", Range(0.01, 1)) = 0.07
		_RimLightEdgeSoftness("Edge Softness", Float) = 0.05

		[Header(Rim Shadow)]
		[HideInInspector]_RimShadowHeader("Rim Shadow", Float) = 0
		[Toggle(_USE_RIM_SHADOW)]_UseRimShadow("开启边缘阴影", Float) = 1
		[Linear]_RimShadowColor("Color", Vector) = (1, 1, 1, 1)
        _RimShadowIntensity("Intensity", Range(0, 1)) = 1
        _RimShadowWidth("Width", Float) = 1
        _RimShadowFeather("Feather", Range(0.01, 0.99)) = 0.01

		[Header(Emission)]
		[Toggle(_USE_EMISSION)]_UseEmission("开启自发光", Float) = 0
        [Linear]_EmissionColor("Color", Vector) = (1, 1, 1, 1)
        _EmissionThreshold("Threshold", Range(0, 1)) = 1
        _EmissionIntensity("Intensity", Float) = 0

		[Header(Outline)]
		[Linear]_OutlineColor("Color", Vector) = (0.5, 0.5, 0.5, 1)
        _OutlineWidth("Width", Range(0, 4)) = 1
        _OutlineZOffset("Z Offset(View Space)", Range(0, 1)) = 0.0001

		[Header(Rendering)]
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Float) = 2
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
			#pragma shader_feature_local _USE_SPECULAR
			#pragma shader_feature_local _USE_RIM_LIGHT
			#pragma shader_feature_local _USE_RIM_SHADOW
			#pragma shader_feature_local _USE_EMISSION

			//--------------------------------------
            // 自定义宏
			#define CHARACTER_HAIR_PASS
			#define _USE_MATERIAL_VALUES_PACK_LUT	0

            #include "CharacterHairPass.hlsl"

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
			Tags{ "LightMode" = "DepthOnly" }
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

			#include "../Pass/OutlinePass.hlsl"
			ENDHLSL
		}
    }

	CustomEditor "StarRailCharacterCommonShaderGUI"
}
