﻿#ifndef __OBJECT_TRAILS_HLSL__
#define __OBJECT_TRAILS_HLSL__

TEXTURE2D(_G_ObjectTrailsTex);
SAMPLER(sampler_G_ObjectTrailsTex);

uniform float4 _G_ObjectTrailsTexPos;
uniform float4 _G_ObjectTrailsTexHeight;

// 根据坐标返回是否有互动角色信息（xyz:世界坐标 w:半径）
inline float4 GetTrailObject(float3 positionWS)
{
    half2 uv = saturate((positionWS.xz - _G_ObjectTrailsTexPos.xy) / _G_ObjectTrailsTexPos.zz);
#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1 - uv.y;
#endif
    
    half4 obstacle = SAMPLE_TEXTURE2D_LOD(_G_ObjectTrailsTex, sampler_G_ObjectTrailsTex, uv, 0.0);
    half theta = ((2.0 * obstacle) - 1.0) * PI;
    float x = obstacle.y * cos(theta);
    float z = obstacle.y * sin(theta);
    float planeHeight = lerp(_G_ObjectTrailsTexHeight.x, _G_ObjectTrailsTexHeight.y, obstacle.z);
    float3 p = float3(positionWS.x + x, planeHeight, positionWS.z + z);
    
    float r = obstacle.w;
    /// 判断uv是否为边缘,如果是就无视
    half2 t = floor(uv + 0.99) * abs(floor(1 + uv) - 2);
    r *= t.x * t.y;
    
    return float4(p, r);
}

#endif
