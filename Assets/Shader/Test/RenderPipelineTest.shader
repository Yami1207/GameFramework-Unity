Shader "Rendering/RenderPipelineTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="RuntimePipeline" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
			Name "RuntimeForwardBase"
			Tags { "LightMode"="RenderForward" }
			//Tags{ "LightMode" = "ForwardBase" }

			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
				return half4(1, 1, 1, 1);
            }
			ENDHLSL
        }
    }
}
