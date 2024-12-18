﻿#ifndef __INSTANCING_HLSL__
#define __INSTANCING_HLSL__

struct InstanceParam
{
    float4x4 objectToWorld;
    float4x4 worldToObject;
};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
uniform int _TransformBufferOffset;
StructuredBuffer<InstanceParam> _TransformBuffer;
#endif

void Setup()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED   
    InstanceParam instance = _TransformBuffer[_TransformBufferOffset + unity_InstanceID];
    unity_ObjectToWorld = instance.objectToWorld;
    unity_WorldToObject = instance.worldToObject;
#endif
}

#endif
