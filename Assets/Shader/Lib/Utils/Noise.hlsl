#ifndef __NOISE_HLSL__
#define __NOISE_HLSL__

// https://godotshaders.com/snippet/fractal-brownian-motion-fbm/
inline float RandomNoise(float2 seed)
{
    return 2.0 * frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453123) - 1;
}

inline float PerlinNoise(float2 uv)
{
    float2 uv_index = floor(uv);
    float2 uv_fract = frac(uv);

    // Four corners in 2D of a tile
    float a = RandomNoise(uv_index);
    float b = RandomNoise(uv_index + float2(1.0, 0.0));
    float c = RandomNoise(uv_index + float2(0.0, 1.0));
    float d = RandomNoise(uv_index + float2(1.0, 1.0));

    float2 blur = smoothstep(0.0, 1.0, uv_fract);
    return lerp(a, b, blur.x) + (c - a) * blur.y * (1.0 - blur.x) + (d - b) * blur.x * blur.y;
}

// 分形布朗运动
inline float fbm(float2 uv, uint count)
{
    float total = 0, amplitude = 1;
    for (uint i = 0; i < count; ++i)
    {
        total += amplitude * PerlinNoise(uv);
        uv *= 2;
        amplitude *= 0.5;
    }
    return total;
}

#endif
