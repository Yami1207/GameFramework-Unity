#ifndef __GLOBE_VARIABLES_HLSL__
#define __GLOBE_VARIABLES_HLSL__

//////////////////////////////////////////////
// 全局变量
uniform half3 _G_ShadowColor;

//////////////////////////////////////////////
// 全局纹理
TEXTURE2D(_G_ReflectionTex);
SAMPLER(sampler_G_ReflectionTex);

#endif
