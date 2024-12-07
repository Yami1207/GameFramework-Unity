#ifndef __INPUT_HLSL__
#define __INPUT_HLSL__

//////////////////////////////////////////////
// 材质表面输入参数
struct CustomSurfaceData
{
    half3 albedo;
    
    // 高光
    half specular;
    half3 specularColor;
    half specularThreshold;
    
    // 光泽度
    half shininess;
    
    // 金属度
    half metallic;
    
    // 平滑度
    half smoothness;
    
    // 法线
    half3 normalTS;
    
    // 自发光
    half emission;
    half3 emissionColor;
    
    // 遮蔽强度
    half occlusion;
    
    // 透明度
    half alpha;
    
    // 顶点颜色
    half4 vertexColor;
};

inline CustomSurfaceData GetDefaultSurfaceData()
{
    CustomSurfaceData surfaceData = (CustomSurfaceData) 0;
    return surfaceData;
}

//////////////////////////////////////////////
// 片元顶点参数
struct CustomInputData
{
    float2 uv;
    
    float3 positionWS;
    float4 positionCS;

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

inline CustomInputData GetDefaultInputData()
{
    CustomInputData inputData = (CustomInputData) 0;
    return inputData;
}

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

#endif
