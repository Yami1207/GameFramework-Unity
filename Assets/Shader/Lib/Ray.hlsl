#ifndef __RAY_HLSL__
#define __RAY_HLSL__

struct Ray
{
    float3 position;
    float3 direction;
};

// Ray-Sphere相交
inline bool RayIntersectSphereSolution(Ray ray, float4 sphere, inout float2 solutions)
{
    float3 pos = ray.position - sphere.xyz;
    float posSqr = dot(pos, pos);
    
    // 二次方程式参数
    float3 quadraticCoef;
    quadraticCoef.x = dot(ray.direction, ray.direction);
    quadraticCoef.y = 2 * dot(ray.direction, pos);
    quadraticCoef.z = posSqr - sphere.w * sphere.w;
    
    // 解二次方程式
    float discriminant = quadraticCoef.y * quadraticCoef.y - 4 * quadraticCoef.x * quadraticCoef.z;
    if (discriminant >= 0)
    {
        float sqrtDiscriminant = sqrt(discriminant);
        solutions = (-quadraticCoef.y + float2(-1, 1) * sqrtDiscriminant) / (2 * quadraticCoef.x);
        return true;
    }
    return false;
}

#endif
