Shader "Hidden/VolumetricCloud"
{
    Properties
    {
        [HideInInspector]_MainTex("Texture", 2D) = "white" {}

        [HideInInspector]_Height("Clound Height", float) = 200
        [HideInInspector]_Thickness("Clound Thickness", float) = 10
        [HideInInspector]_StepCount("Step Count", float) = 20
        [HideInInspector]_StepSize("Step Size", float) = 0.02
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Ray
            {
                float3 position;
                float3 direction;
            };

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 texcoord     : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            half _Height;
            half _Thickness;
            half _StepCount;

            inline float3 GetWorldPosition(float3 positionCS)
            {
                float2 uv = positionCS.xy / _ScaledScreenParams.xy;
#if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(uv);
#else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
#endif
                return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
            }

            // 获取射线到AABB距离,返回最近点距离和最近点与最远点距离
            inline float2 RayToAABBDistance(Ray ray, float3 minBounds, float3 maxBounds)
            {
                // 通过各坐标轴计算出射线离AABB最近点与最远点距离
                float3 invRayDir = 1.0 / ray.direction;
                float3 t0 = (minBounds - ray.position) * invRayDir;
                float3 t1 = (maxBounds - ray.position) * invRayDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                // 最近点距离
                float distance = max(0, max(tmin.x, max(tmin.y, tmin.z)));

                // 最近点与最远点距离
                float inside = max(0, min(tmax.x, min(tmax.y, tmax.z)) - distance);

                return float2(distance, inside);
            }

            inline half SampleDensity(float3 p)
            {
                return 0.02;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                return output;
            }

            // https://zhuanlan.zhihu.com/p/533853808?utm_id=0
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);

                float3 positionWS = GetWorldPosition(input.positionCS);

                Ray ray;
                ray.position = _WorldSpaceCameraPos.xyz;
                ray.direction = normalize(positionWS - ray.position);

                //float3 minBouns = float3(0, 0, 0);
                //float3 maxBouns = float3(5, 5, 5);
                float3 boundsSize = float3(2000, 0, 2000);
                float3 minBouns = float3(ray.position.x, _Height, ray.position.z) - boundsSize;
                float3 maxBouns = float3(ray.position.x, _Height + _Thickness, ray.position.z) + boundsSize;
                float2 result = RayToAABBDistance(ray, minBouns, maxBouns);

                // 遮挡物距离
                float toOpaque = length(positionWS - ray.position);
                float maxDist = result.y;//min(toOpaque - result.x, result.y);

                float density = 0;

                int stepCount = _StepCount;
                float3 pos = ray.position + result.x * ray.direction;
                float stepSize = result.y / stepCount;
                //float3 additive = ray.direction * stepSize;
                float additiveDist = 0;
                for (int i = 0; i < stepCount; ++i)
                {
                    if (additiveDist > maxDist)
                        break;

                    density += stepSize * 0.1;//SampleDensity(pos);
                    additiveDist += stepSize;
                    //pos += additive;
                }

                // //if (intersect.x < toOpaque)
                // {
                //     int stepCount = 16;
                //     float3 pos = ray.position + intersect.x * ray.direction;
                //     float length = intersect.y - intersect.x;//min(toOpaque, intersect.y) - intersect.x;
                //     float stepSize = length / stepCount;
                //     float3 forward = ray.direction * stepSize;
                //     float distance = 0, limit = min(toOpaque - intersect.x, length);
                //     for (int i = 0; i < stepCount; ++i)
                //     {
                //         if (distance > limit)
                //             break;

                //         distance += stepSize;
                //         density += SampleDensity(pos);
                //         pos += forward;
                //     }
                // }

                return half4(color.rgb + density, color.a);
            }
            ENDHLSL
        }
    }
}
