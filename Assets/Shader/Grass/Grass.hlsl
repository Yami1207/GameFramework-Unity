#ifndef __GRASS_HLSL__
#define __GRASS_HLSL__

// ========================= 开关定义 =========================
#define USING_SWING (_USE_SWING)

#define USING_NOISE_WORLD_SPACE_UVS (_USE_NOISE_WORLD_SPACE_UVS)

#include "../Lib/Core.hlsl"
#include "../Lib/Wind.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
    half3 _Color1;
    half3 _Color2;
    half3 _GrassShadowColor;
    half _ColorVariation;
    half _Cutoff;
    half _NormalScale;
    half _NoiseTiling;
    half _GrassShininess;
    half _GrassSpecularScale;
    half _SwingFeq;
    half _SwingFeqMax;
    half _SwingScale;
    half _SwingAmp;
CBUFFER_END

//--------------------------------------
// 贴图
//TEXTURE2D(_MainTex);
//SAMPLER(sampler_MainTex);

TEXTURE2D(_Noise);
SAMPLER(sampler_Noise);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS   : POSITION;
    half2 texcoord      : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    half2 texcoord          : TEXCOORD0;
    half3 normalWS          : TEXCOORD1;
    float4 positionWSAndFog : TEXCOORD2;
    float4 shadowCoord      : TEXCOORD3;

    UNITY_VERTEX_OUTPUT_STEREO
};

//--------------------------------------
// 片元输出结构体
struct FragData
{
    half4 color : SV_Target0;
    float4 normal : SV_Target1;
};

#include "../Lib/Instancing.hlsl"

inline float3 GrassWindOffset(float3 positionOS, float2 mask)
{
#if USING_SWING
    half stiffnessAtten = mask.y * mask.y;
    float phase = UNITY_MATRIX_M._m10 - mask.x;
    float feq = lerp(_SwingFeq, _SwingFeqMax, GetWindIntensity() * _SwingScale);
    positionOS = SimpleSwingPosOS(positionOS, feq, _SwingAmp, stiffnessAtten, phase);
#endif

    return positionOS;
}

Varyings vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float3 positionOS = GrassWindOffset(input.positionOS.xyz, input.texcoord);
    float3 positionWS = TransformObjectToWorld(positionOS);

    Varyings output;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.texcoord = input.texcoord;
    output.normalWS = TransformObjectToWorldNormal(normalize(positionOS));
    output.positionWSAndFog = float4(positionWS, ComputeFogFactor(output.positionCS.z));
    output.shadowCoord = GetShadowCoord(positionWS, output.positionCS);
    
    return output;
}

FragData frag(Varyings input)
{
    half4 mainColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord);
    clip(mainColor.a - _Cutoff);
    
    float3 positionWS = input.positionWSAndFog.xyz;
#if USING_NOISE_WORLD_SPACE_UVS
    half mask = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, positionWS.xz * _NoiseTiling).r;
#else
    half mask = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.texcoord).r;
#endif
    half3 albedo = mainColor.rgb * lerp(_Color1, _Color2, mask * _ColorVariation);

    // 阴影值
    float4 shadowCoord = GetShadowCoordInFragment(positionWS, input.shadowCoord);
    half shadowAtten = MainLightRealtimeShadow(shadowCoord);
    half3 shadow = lerp(_GrassShadowColor, 1, shadowAtten);
    
    Light mainLight = GetMainLight();
    float3 viewDirectionWS = GetCameraPositionWS() - positionWS;
    
    // Lambert
    half NoL = dot(input.normalWS, mainLight.direction);
    half diffuse = NoL * 0.5 + 0.5;
    half3 directDiffuse = diffuse * mainLight.color;
    half3 diffuseColor = shadow * directDiffuse;
    
    //half smoothness = _GrassShininess;
    //half roughness = 1 - smoothness;
    //half perceptualRoughness = roughness * roughness;
    //half roughness2 = perceptualRoughness * perceptualRoughness;
    //half roughness2MinusOne = roughness2 - 1;
    //half normalizationTerm = roughness * 4 + 2;

    //// 高光
    //half H = SafeNormalize(viewDirectionWS + mainLight.direction);
    //half NoH = saturate(dot(input.normalWS, H));
    //half LoH = saturate(dot(mainLight.direction, H));
    //half d = roughness2MinusOne * NoH * NoH + 1.0;
    //half ggxTerm = roughness2 / (d * d * max(0.1, LoH * LoH) * normalizationTerm);
    //half3 indirectSpecular = shadow * ggxTerm * mainLight.color;
    ////half ggxTerm = min(roughness2 / max(d * d, 0.0001) * INV_PI, 64.0);
    ////half LoV = dot(mainLight.direction, viewDirectionWS);
    ////half smoothLoV = smoothstep(-1, -0.9962, LoV);
    ////half3 indirectSpecular = shadow * ggxTerm * mainLight.color * smoothLoV * 4;
    //half3 sepcularColor = indirectSpecular * _GrassSpecularScale;
    
    half3 color = albedo * diffuseColor;

    // 与雾混合
    color = MixFog(color, input.positionWSAndFog.a);

    FragData output = (FragData) 0;
    output.color = half4(color, 1);
    output.normal = float4(input.normalWS * 0.5 + 0.5, 0.0);
    return output;
}

#endif
