// TODO:
// Snap ray start and end to cloud volume bounds
// Add lighting

Shader "Unlit/Clouds 1"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 3D) = "white" {}

		_NumSteps ("Raymarching Steps", Range(1, 3000)) = 10
        _MarchDistance ("Raymarch Distance", Range(0.0, 1000.0)) = 1.0

        _Density ("Density", Range(0.0, 20.0)) = 1.0
        _Contrast ("Contrast", Range(0.0, 20.0)) = 1.0

        _Offset ("Cloud Center", Vector) = (0, 0, 0, 1)
        _Scale ("Cloud Size", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        ZTest on Cull Back ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct vertOutput
            {
                float4 screenPos : POSITION;
                UNITY_FOG_COORDS(1)

				float4 worldPos : POSITION1;
				float4 localPos : POSITION2;
                float3 viewPos : POSITION3;
				float3 viewDir : POSITION4;
            };

            sampler3D _NoiseTex;
            float4 _NoiseTex_ST;

            //**********************
            // Custom parameters
            //**********************

			int _NumSteps;
			float _MarchDistance;
            float _Density;
            float _Contrast;
            float4 _Offset;
            float4 _Scale;

            // Loaded matrices

            float4x4 _MVP;
            float4x4 _InverseMVP;

            vertOutput vert (appdata v)
            {
                vertOutput output;

                output.screenPos = UnityObjectToClipPos(v.vertex);
				output.worldPos = mul(unity_ObjectToWorld, v.vertex);
				output.localPos = v.vertex;

                output.viewPos = output.screenPos.xyz;
                output.viewDir = -UnityWorldSpaceViewDir(output.worldPos);

                UNITY_TRANSFER_FOG(output, output.vertex);

                return output;
            }


			// Custom code

            float AdjustContrast(float color, float contrast) {
                //return saturate(lerp(0.5, color, contrast));
				return color;
            }

			inline float4 sampleNoise(float3 coord)
			{
				float4 color = float4(0, 0, 0, 0);

                // Transform from -0.5, 0.5 in cloud space to 0, 1 in texture space
                coord += float4(0.5, 0.5, 0.5, 0);

                // Bound noise to cloud region
                if (coord.x > 0 && coord.y > 0 && coord.z > 0 && coord.x < 1 && coord.y < 1 && coord.z < 1)
                {
    				color = tex3Dlod(_NoiseTex, float4(coord, 1));
                }

				return color;
			}

            // Transform world position to cloud space
            inline float3 worldToCloudSpace(float3 coord)
            {
                coord -= _Offset;
                coord /= _Scale;

                return coord;
            }

            // Used to align rays along view planes
            inline float3 snapToView(float3 position, float3 viewDir, float increments)
            {
                float currentDistance = dot(position, viewDir);
                float offsetDistance = fmod(currentDistance, increments);

                position -= viewDir * offsetDistance;
                return position;
            }

			fixed4 frag(vertOutput input) : SV_Target
			{
                float stepSize = _MarchDistance / _NumSteps;
				float raymarchValue = 0;
                float debugValue = 1;

				float3 rayPos = input.worldPos;
				float3 rayDir = normalize(input.viewDir);
                rayPos = snapToView(rayPos, rayDir, stepSize);

                // Do opacity march through volume texture
				for (int i = 0; i < _NumSteps; i++)
				{
					float3 coord = worldToCloudSpace(rayPos);
					float4 colorSample = sampleNoise(coord);

					raymarchValue += AdjustContrast(colorSample.r, _Contrast) * stepSize;

					rayPos += rayDir * stepSize;
				}

                // Modified Beer's law for alpha
                float alpha = 1 - exp(raymarchValue * -_Density);
                float lightness = 1;

				fixed4 color = fixed4(lightness, lightness, lightness, alpha);
                color.a = clamp(color.a, 0.0, 1.0);
            
                // apply fog
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }


            ENDCG
        }
    }
}
