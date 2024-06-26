#ifndef __FOG_HLSL__
#define __FOG_HLSL__

half3 MixFog(half3 fragColor, CustomInputData inputData, CustomSurfaceData surfaceData)
{
    return MixFog(fragColor, inputData.fogCoord);
}

#endif
