Shader "Unlit/Clouds 1"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 2D) = "white" {}
		_NumSteps ("Raymarching Steps", Range(1, 3000)) = 10
        _MarchDistance ("Raymarch Distance", Range(0.0, 1000.0)) = 1.0
        _Density ("Density", Range(0.0, 1000.0)) = 1.0
        _Contrast ("Contrast", Range(0.0, 10.0)) = 1.0
    }
    SubShader
    {
		Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragInput
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;

				float4 worldPos : POSITION1;
				float4 localPos : POSITION2;
				float4 viewDir : POSITION3;
            };

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            // Custom parameters
			int _NumSteps;
			float _MarchDistance;
            float _Density;
            float _Contrast;

            fragInput vert (appdata v)
            {
                fragInput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.localPos = v.vertex;
				o.viewDir = normalize(o.worldPos - float4(_WorldSpaceCameraPos.xyz, 1));
                return o;
            }


			// Custom code

            float AdjustContrast(float color, float contrast) {
                return saturate(lerp(0.5, color, contrast));
            }

			inline float4 pseudoSample3D(float3 coord)
			{
				float4 color = float4(0, 0, 0, 0);

				coord -= float4(1, 1, 1, 0);
				coord /= 2;
				
				color = tex2Dlod(_NoiseTex, float4(coord.xz, 0.0, 0.0));

				return color;
			}

			fixed4 frag(fragInput input) : SV_Target
			{

				float raymarchValue = 0;
                float debugValue = 1;

				float3 rayPos = input.localPos.xyz;
				float3 rayDir = input.viewDir.xyz;

				float stepSize = _MarchDistance / _NumSteps;

				for (int i = 0; i < _NumSteps; i++)
				{
					float3 coord = rayPos;
					float4 colorSample = pseudoSample3D(coord);

					raymarchValue += AdjustContrast(colorSample.r, _Contrast) * stepSize;

					rayPos += input.viewDir * stepSize;
				}

				fixed4 color = fixed4(1, 1, 1, raymarchValue * _Density);

                // apply fog
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }


            ENDCG
        }
    }
}
