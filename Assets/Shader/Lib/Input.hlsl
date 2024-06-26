#ifndef __INPUT_HLSL__
#define __INPUT_HLSL__

struct CustomSurfaceData
{
    //
    half3 albedo;
    
    // 高光
    half3 specular;
    
    // 金属度
    half metallic;
    
    // 平滑度
    half smoothness;
    
    // 法线
    half3 normalTS;
    
    // 自发光
    half3 emission;
    
    // 遮蔽强度
    half occlusion;
    
    // 透明度
    half alpha;
};

struct CustomInputData
{
    float3 positionWS;
    
    float3 normalWS;
    
    half3 viewDirectionWS;
    
    float4 shadowCoord;
    
    half fogCoord;
    
    half3 vertexLighting;
    
    half3 bakedGI;
    
    float2 normalizedScreenSpaceUV;
    
    half4 shadowMask;
    
    half3x3 tangentToWorld;
};

////////////////////////////////////////////////
//// 功能结构体与函数
//struct VertexPositionInputs
//{
//	float3 positionWS; // World space position
//	float3 positionVS; // View space position
//	float4 positionCS; // Homogeneous clip space position
//};

//VertexPositionInputs GetVertexPositionInputs(float4 positionOS)
//{
//	VertexPositionInputs input;
//	input.positionWS = mul(unity_ObjectToWorld, positionOS).xyz;
//	input.positionVS = mul(UNITY_MATRIX_V, input.positionWS);
//	input.positionCS = mul(UNITY_MATRIX_VP, float4(input.positionWS, 1.0));
//	return input;
//}

//struct VertexNormalInputs
//{
//	float3 tangentWS;
//	float3 bitangentWS;
//	float3 normalWS;
//};

//VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
//{
//	VertexNormalInputs tbn;
//	tbn.normalWS = UnityObjectToWorldNormal(normalOS);
//	tbn.tangentWS = UnityObjectToWorldDir(tangentOS.xyz);

//	half sign = tangentOS.w * unity_WorldTransformParams.w;
//	tbn.bitangentWS = cross(tbn.normalWS, tbn.tangentWS) * sign;

//	return tbn;
//}

////////////////////////////////////////////////
//// 材质表面输入参数
//struct SurfaceData
//{
//	half3 albedo;
//	half alpha;

//	half specGloss;
//	half3 specularColor;

//	// 暗部阈值
//	half darkThreshold;
//};

//SurfaceData GetDefaultSurfaceData()
//{
//	SurfaceData data = (SurfaceData)0;
//	return data;
//}

////////////////////////////////////////////////
//// 片元顶点参数
//struct InputData
//{
//	float3  positionWS;
//	float4	positionSS;
//	float3  normalWS;
//	float3  viewDirectionWS;
//};

//InputData GetDefaultInputData()
//{
//	InputData data = (InputData)0;
//	return data;
//}

#endif
