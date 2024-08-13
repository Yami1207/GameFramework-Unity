Shader "Rendering/Custom/Water"
{
    Properties
    {
        _DepthDistance("Depth Distance", Range(0.01, 10)) = 0.5
		_TransparentDistance("Transparent Distance", Range(0.01, 10)) = 0.13

        _WaterDirection("Water Direction", Vector) = (0, -1, 0, 0)
		_WaterSpeed("Water Speed", Float) = 1

        _WaterPhase("Phase", Vector) = (0.28, 0.47, 0.0, 0.0)
        _WaterAmplitude("Amplitude", Vector) = (3.33, 0.28, 0.37, 0.0)
        _WaterFrequency("Frequency", Vector) = (0.00, 0.6, 0.3, 0.0)
        _WaterOffset("Offset", Vector) = (0.0, 0.0, 0.13, 0.0)

        [Header(Normal)]
        [NoScaleOffset]_BumpMap("法线贴图", 2D) = "bump" {}
        _NormalTiling("法线平铺", Vector) = (0.5, 0.5, 0.0, 0.0)
        _NormalSpeed("法线速度", Float) = 1
        _NormalSubTiling("法线平铺(Sub)", Float) = 0.5
		_NormalSubSpeed("法线速度(Sub)", Float) = -0.5

        [Header(Reflection)]
        [Toggle(_USE_REFLECTION)]_EnableReflection("开启反射", Float) = 1
        [Linear]_ReflectionColor("Reflection Color", Vector) = (1, 1, 1, 1)
        _ReflectionCubemap("反射球", Cube) = "_Skybox" { }
        _ReflectionDistort("反射扭曲", Range(0, 2)) = 0.44
		_ReflectionIntensity("反射强度",Range(0, 1)) = 0.436

        [Header(Refraction)]
        [Toggle(_USE_REFRACTION)]_EnableRefraction("开启折射", Float) = 1
        _RefractionFactor("Refraction Factor", Range(0, 5)) = 0.15

        [Header(Specular)]
        [Toggle(_USE_SPECULAR)]_EnableSpecular("开启高光", Float) = 1
		[Linear]_SpecularColor("Color", Vector) = (1.0, 1.0, 1.0, 1.0)
		_SpecularShinness("Shinness", Range(0, 1)) = 1
		_SpecularIntensity("Intensity", Range(0, 5)) = 1

        [Header(Surface Foam)]
        [Toggle(_USE_FOAM)]_EnableFoam("开启泡沫", Float) = 1
        [Linear]_FoamColor("Color", Vector) = (1, 1, 1, 1)
        [NoScaleOffset]_FoamMaskMap("Foam Mask", 2D) = "black" {}
        _FoamAmount("泡沫量", Range(0.0, 1)) = 0.5
        _FoamDistortion("泡沫扰动", Range(0.0, 3)) = 0.1
        _FoamTiling("泡沫平铺", Vector) = (0.1, 0.1, 0.0, 0.0)
        _FoamSpeed("泡沫速度", Float) = 0.1
        _FoamSubTiling("泡沫平铺(Sub)", Float) = 0.5
		_FoamSubSpeed("泡沫速度(Sub)", Float) = -0.25

        [Header(Intersection)]
        [Toggle(_USE_INTERSECTION)]_EnableIntersection("开启交界处泡沫", Float) = 1
        _IntersectionDistance("Distance", Range(0.01, 5)) = 0.4
        _IntersectionClipping("Cutoff", Range(0.01, 1)) = 0.5
        [Linear]_IntersectionColor("Color", Vector) = (1, 1, 1, 1)
        [NoScaleOffset]_IntersectionNoiseMap("扰动图", 2D) = "white" {}
        _IntersectionTiling("Tiling", float) = 0.2
        _IntersectionThreshold("Threshold", Range(0.01 , 1)) = 0.5
        _IntersectionSpeed("Speed", Range(-5, 5)) = 1
        _IntersectionDistortion("浪花扰动", Range(0.0, 1)) = 0.2
		_IntersectionRippleStrength("涟漪强弱", Range(0 , 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
			Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
            // -------------------------------------
			// Unity defined keywords
			#pragma multi_compile_fog

            //--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling lodfade nolightprobe nolightmap

            // -------------------------------------
			// URP keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

			// -------------------------------------
            // 自定义keywords
            #pragma shader_feature_local _USE_FOAM
            #pragma shader_feature_local _USE_INTERSECTION
            #pragma shader_feature_local _USE_REFLECTION
            #pragma shader_feature_local _USE_REFRACTION
            #pragma shader_feature_local _USE_SPECULAR

            #include "WaterPass.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "WaterShaderGUI"
}
