Shader "Hidden/DownsampleTexture"
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
                float2 resolution = _Resolution - float2(1, 1);
                float2 initialPixelPosition = i.uv * resolution;

                float minDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, initialPixelPosition / resolution);

                for (int x = 0; x < _NumSamples; x++)
                {
                    for (int y = 0; y < _NumSamples; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        float2 pixelPosition = initialPixelPosition + float2(x, y);
                        float2 uvPosition = pixelPosition / resolution;
                        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvPosition);

                        // Unconverted depth is inverted
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
