Shader "Hidden/DownsampleDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ SKIP_DOWNSAMPLE

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float2 _Resolution;
            int _NumSamples;

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef SKIP_DOWNSAMPLE
                return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                #endif

                int numSamples = _NumSamples;
                float2 numTexels = _Resolution - float2(1, 1);
                float2 texelSize = float2(1, 1) / numTexels;

                float2 texelStart = i.uv - (numSamples / 2.0) * texelSize;

                float minDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, texelStart);

                for (int x = 0; x < numSamples; x++)
                {
                    for (int y = 0; y < numSamples; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        float2 texel = texelStart + float2(x, y) * texelSize;
                        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, texel.xy);

                        // Unconverted depth is inverted
                        // Use min to get max
                        minDepth = min(minDepth, depth);
                    }
                }

                fixed4 col = EncodeFloatRGBA(minDepth);
                return col;
            }
            ENDCG
        }
    }
}
