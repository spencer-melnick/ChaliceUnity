// TODO:
// Snap ray start and end to cloud volume bounds
// Add lighting

Shader "Unlit/Clouds 1"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 3D) = "white" {}

		_NumSteps ("Raymarching Steps", Range(1, 300)) = 64
        _MarchDistance ("Raymarch Distance", Range(0.0, 1000.0)) = 1.0
        _Density ("Density", Range(0.0, 20.0)) = 1.0

		_ShadowDensity ("Shadow Density", Range(0.0, 20.0)) = 1.0
        _ShadowSteps ("Shadow Steps", Range(1, 300)) = 32

        _Offset ("Cloud Center", Vector) = (0, 0, 0, 1)
        _Scale ("Cloud Size", Vector) = (1, 1, 1, 1)

		[Toggle(USE_TEMPORAL_JITTER)]
		_UseJitter ("Use Temporal Jitter", float) = 0.0

        [Toggle(USE_POWDER_EFFECT)]
        _UsePowder ("Use Powder Effect", float) = 0.0
    }
    SubShader
    {
        ZTest On Cull Back ZWrite Off
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
			#include "Lighting.cginc"

			#pragma shader_feature USE_TEMPORAL_JITTER
            #pragma shader_feature USE_POWDER_EFFECT

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct vertOutput
            {
                float4 position : POSITION;
                UNITY_FOG_COORDS(1)

				float4 worldPos : POSITION1;
				float4 localPos : POSITION2;
                float4 screenPos : POSITION3;
				float3 viewDir : POSITION4;
            };

            sampler3D _NoiseTex;
			sampler2D _CameraDepthTexture;
            float4 _NoiseTex_ST;

            //**********************
            // Custom parameters
            //**********************

			int _NumSteps;
            int _ShadowSteps;
			float _MarchDistance;
            float _Density;
			float _ShadowDensity;
            float4 _Offset;
            float4 _Scale;

            // Loaded matrices

            // float4x4 _MVP;
            // float4x4 _InverseMVP;

            vertOutput vert (appdata v)
            {
                vertOutput output;

                output.position = UnityObjectToClipPos(v.vertex);
				output.worldPos = mul(unity_ObjectToWorld, v.vertex);
				output.localPos = v.vertex;

				output.screenPos = ComputeScreenPos(output.position);
                output.viewDir = -UnityWorldSpaceViewDir(output.worldPos);

                UNITY_TRANSFER_FOG(output, output.vertex);

                return output;
            }


			// Custom code

            inline float AdjustContrast(float color, float contrast) {
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

			// Pseudorandom function from https://thebookofshaders.com/10
			inline float random(float3 st) {
				return frac(sin(dot(st.xy, float3(12.9898, 78.233, 63.5962))) *	43758.5453123);
			}

            // Used to align rays along view planes
            inline float3 snapToView(float3 position, float3 viewDir, float increments)
            {
                float currentDistance = dot(position, viewDir);
                float offsetDistance = fmod(currentDistance, increments);

                position -= viewDir * offsetDistance;
                return position;
            }

            inline float3 sampleCloudLight(float3 rayPos, int numSteps, float rayDistance)
            {
                float stepSize = rayDistance / numSteps;
                float3 rayDir = _WorldSpaceLightPos0;
                float3 rayStep = rayDir * stepSize;

                float accumulatedDensity = 0.0;
                
                // Integrate density function along line towards the sunlight
                for (int i = 0; i < numSteps; i++)
                {
                    float sampleDensity = sampleNoise(worldToCloudSpace(rayPos)).r * _ShadowDensity;
                    accumulatedDensity += sampleDensity * stepSize;

                    if (accumulatedDensity > 4)
                    {
                        break;
                    }

                    rayPos += rayStep;
                }

                // Find light intensity using Beer's Law
                return _LightColor0 * exp(-accumulatedDensity);
            }

            inline float4 sampleCloudRay(float3 rayPos, float3 rayDir, int numSteps, float rayDistance)
            {
                float stepSize = rayDistance / numSteps;
                float3 rayStep = rayDir * stepSize;

                float4 accumulatedColor = float4(0, 0, 0, 0);

                for (int i = 0; i < numSteps; i++)
                {
                    float sampleDensity = sampleNoise(worldToCloudSpace(rayPos)).r * _Density * stepSize;

                    // Only sample if particle can contribute to color
                    if (sampleDensity > 0.01)
                    {
                        float particleAlpha = sampleDensity;
                        float4 particleColor = float4(sampleCloudLight(rayPos, _ShadowSteps, _MarchDistance) * particleAlpha, particleAlpha);

                        // Alpha blend with particles in front of current particle
                        accumulatedColor += (1 - accumulatedColor.a) * particleColor;
                    }

                    rayPos += rayStep;

                    if (accumulatedColor.a > 0.99)
                    {
                        break;
                    }
                }

                return float4(accumulatedColor.rgb / accumulatedColor.a, accumulatedColor.a);
            }

			fixed4 frag(vertOutput input) : SV_Target
			{
                float viewStepSize = _MarchDistance / _NumSteps;
				float3 viewRayPos = input.worldPos;
				float3 viewRayDir = normalize(input.viewDir);

				#ifdef USE_TEMPORAL_JITTER
				// Apply temporal jitter
				viewRayPos -= input.viewDir * random(viewRayPos) * viewStepSize;
				#else
				// Snap to view planes
                viewRayPos = snapToView(viewRayPos, viewRayDir, viewStepSize);
				#endif

				// Calculate max depth
				float screenDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screenPos));
				screenDepth = LinearEyeDepth(screenDepth);

				float currentDepth = dot(viewRayDir, viewRayPos - _WorldSpaceCameraPos);
				float travelDepth = screenDepth - currentDepth;

				// Limit steps by travel distance
				int numSteps = min(_NumSteps, travelDepth / viewStepSize);
				viewStepSize = min(_MarchDistance, travelDepth) / numSteps;

				fixed4 color = sampleCloudRay(viewRayPos, viewRayDir, numSteps, min(_MarchDistance, travelDepth));
            
                // apply fog
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }


            ENDCG
        }
    }
}
