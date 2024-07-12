Shader "Hidden/Highlighting/Blur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off

        Pass
        {
            Name "BlurVertical"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_FLOAT(_HighlightsTex);
            SAMPLER(sampler_HighlightsTex);

            uniform half _BlurIterations;
            uniform half _BlurPixelOffset;

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
                float4 positionCS   : SV_POSITION;
                half2 texcoord      : TEXCOORD0;   
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.texcoord = input.texcoord;      
                return output;
            }

            half frag(Varyings input) : SV_Target
            {
                float intensity = 0;
			    [unroll(50)]
			    for (float index = 0; index < _BlurIterations; ++index)
                {
				    float2 uv = input.texcoord + float2(0, (index / (_BlurIterations - 1) - 0.5) * _BlurPixelOffset);
				    intensity += SAMPLE_TEXTURE2D(_HighlightsTex, sampler_HighlightsTex, uv).r;
			    }
			    intensity *= 0.1;

                return intensity;
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlurHorizontal"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_FLOAT(_BlurVHighlightsTex);
            SAMPLER(sampler_BlurVHighlightsTex);

            uniform half _BlurIterations;
            uniform half _BlurPixelOffset;
            uniform half _BlurIntensity;

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
                float4 positionCS   : SV_POSITION;
                half2 texcoord      : TEXCOORD0;   
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.texcoord = input.texcoord;      
                return output;
            }

            half frag(Varyings input) : SV_Target
            {
                float invAspect = _ScreenParams.y / _ScreenParams.x;
			    float intensity = 0;
			    [unroll(50)]
                for (float index = 0; index < _BlurIterations; ++index)
                {
				    float2 uv = input.texcoord + float2((index / (_BlurIterations - 1) - 0.5) * _BlurPixelOffset * invAspect, 0.0);
				    intensity += SAMPLE_TEXTURE2D(_BlurVHighlightsTex, sampler_BlurVHighlightsTex, uv).r;
			    }
			    return saturate(0.1 * intensity * _BlurIntensity);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Mix"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_FLOAT(_HighlightsTex);
            SAMPLER(sampler_HighlightsTex);

            TEXTURE2D_FLOAT(_BlurHighlightsTex);
            SAMPLER(sampler_BlurHighlightsTex);

            uniform half3 _HighlightColor;

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
                float4 positionCS   : SV_POSITION;
                half2 texcoord      : TEXCOORD0;   
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.texcoord = input.texcoord;      
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_HighlightsTex, sampler_HighlightsTex, input.texcoord).r;
                half intensity = SAMPLE_TEXTURE2D(_BlurHighlightsTex, sampler_BlurHighlightsTex, input.texcoord).r;
                return half4(_HighlightColor, saturate(intensity - mask));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Mix"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_FLOAT(_HighlightsTex);
            SAMPLER(sampler_HighlightsTex);

            TEXTURE2D_FLOAT(_BlurVHighlightsTex);
            SAMPLER(sampler_BlurVHighlightsTex);

            uniform half _BlurIterations;
            uniform half _BlurPixelOffset;
            uniform half _BlurIntensity;

            uniform half3 _HighlightColor;

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
                float4 positionCS   : SV_POSITION;
                half2 texcoord      : TEXCOORD0;   
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.texcoord = input.texcoord;      
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float invAspect = _ScreenParams.y / _ScreenParams.x;
			    float intensity = 0;
			    [unroll(50)]
                for (float index = 0; index < _BlurIterations; ++index)
                {
				    float2 uv = input.texcoord + float2((index / (_BlurIterations - 1) - 0.5) * _BlurPixelOffset * invAspect, 0.0);
				    intensity += SAMPLE_TEXTURE2D(_BlurVHighlightsTex, sampler_BlurVHighlightsTex, uv).r;
			    }
			    intensity = saturate(0.1 * intensity * _BlurIntensity);

                half mask = SAMPLE_TEXTURE2D(_HighlightsTex, sampler_HighlightsTex, input.texcoord).r;
                return half4(_HighlightColor, saturate(intensity - mask));
            }
            ENDHLSL
        }
    }
}
