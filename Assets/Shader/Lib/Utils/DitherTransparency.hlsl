#ifndef __DITHER_TRANSPARENCY_HLSL__
#define __DITHER_TRANSPARENCY_HLSL__

//uniform half _DitherTransparency;

static const half g_DitherTransparencyThresholdValue[16] =
{
    1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
	13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
	4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
	16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
};

inline void ApplyDitherTransparencyCutoff(float4 positionSS, half transparency)
{
    float2 pixel = positionSS.xy / positionSS.w;
    pixel *= _ScreenParams.xy;
    
    pixel = fmod(pixel, 4);
    int index = pixel.x * 4 + pixel.y;
    clip(transparency - g_DitherTransparencyThresholdValue[index]);
}

#endif
