Shader "Hidden/HiZ/CopyDepth"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"  "Queue" = "Geometry" "IgnoreProjector" = "True" }
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_FLOAT(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            float4 _CameraDepthTexture_TexelSize;

            //--------------------------------------
            // 顶点结构体
            struct Attributes
            {
                float3 positionOS   : POSITION;
                half2 texcoord      : TEXCOORD0;
            };

            //--------------------------------------
            // 片元结构体
            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                half2 texcoord          : TEXCOORD0;    
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.texcoord = input.texcoord;        
                return output;
            }

            float frag(Varyings input) : SV_Target
            {
                float2 offset = _CameraDepthTexture_TexelSize.xy * 0.5;
                float p0 = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + offset, 0).x;
                float p1 = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord - offset, 0).x;
                float p2 = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + float2(offset.x, -offset.y), 0).x;
                float p3 = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + float2(-offset.x, offset.y), 0).x;

                 // 离相机最近的深度值
                float4 depth = float4(p0, p1, p2, p3);
#if UNITY_REVERSED_Z
                depth.xy = min(depth.xy, depth.zw);
                depth.x = min(depth.x, depth.y);
#else
                depth.xy = max(depth.xy, depth.zw);
                depth.x = max(depth.x, depth.y);
#endif
                return depth.x;
            }
            ENDHLSL
        }
    }
}
