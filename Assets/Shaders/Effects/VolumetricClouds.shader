// TODO:
// Snap ray start and end to cloud volume bounds
// Add lighting

Shader "Unlit/Clouds 1"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 3D) = "white" {}
        _CoverageTex ("Coverage Texture", 2D) = "white" {}
        _HeightGradientTex ("Height Gradient", 2D) = "white" {}

        _LightScale ("Light Scale", Range(0.0, 5.0)) = 1.0
        _AmbientScale ("Ambient Scale", Range(0.0, 5.0)) = 1.0

		_NumSteps ("Raymarching Steps", Range(1, 300)) = 64
        _MarchDistance ("Max Raymarch Distance", Range(0.0, 1000.0)) = 1.0
        _Density ("Density", Range(0.0, 20.0)) = 1.0

        _ShadowSteps ("Shadow Steps", Range(1, 300)) = 32
        _ShadowDistance ("Shadow Distance", Range(0.0, 100.0)) = 2.0
		_ShadowDensity ("Shadow Density", Range(0.0, 20.0)) = 1.0

        _AmbientSteps ("Ambient Steps", Range(1, 10)) = 4
        _AmbientDistance ("Ambient Distance", Range (0.0, 5.0)) = 0.1

        _Offset ("Cloud Center", Vector) = (0, 0, 0, 1)
        _Scale ("Cloud Size", Vector) = (1, 1, 1, 1)
        _NoiseScale ("Noise Scale", Vector) = (10, 10, 10, 1)
        _NoiseOffset ("Noise Offset", Vector) = (0, 0, 0, 1)

		[Toggle(USE_TEMPORAL_JITTER)]
		_UseJitter ("Use Temporal Jitter", float) = 0.0

        [Toggle(USE_POWDER_EFFECT)]
        _UsePowder ("Use Powder Effect", float) = 0.0
        _PowderFactor ("Powder Factor", Range(0.0, 30.0)) = 10.0

        [Toggle(USE_SCATTERING)]
        _UseScattering ("Use Forward Backward Scattering", float) = 0.0
        _ForwardScattering ("Forward Scattering", Range(0.0, 1.0)) = 0.1
        _BackwardScattering ("Backward Scattering", Range(-1.0, 0.0)) = -0.1
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
            #pragma shader_feature USE_SCATTERING

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
            sampler2D _CoverageTex;
            sampler2D _HeightGradientTex;
			sampler2D _CameraDepthTexture;
            float4 _NoiseTex_ST;

            //**********************
            // Custom parameters
            //**********************

			int _NumSteps;
			float _MarchDistance;
            float _Density;

            float _LightScale;
            float _AmbientScale;

            int _ShadowSteps;
            float _ShadowDistance;
			float _ShadowDensity;

            int _AmbientSteps;
            float _AmbientDistance;

            float4 _Offset;
            float4 _Scale;

            float4 _NoiseScale;
            float4 _NoiseOffset;

            float _PowderFactor;

            float _ForwardScattering;
            float _BackwardScattering;

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

            inline float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
            {
                value -= oldMin;
                value /= (oldMax - oldMin);
                // value = saturate(value);
                value *= (newMax - newMin);
                value += newMin;

                return value;
            }

			inline float4 sampleNoise(float3 coord, float3 worldCoord)
			{
				float4 color = float4(0, 0, 0, 0);

                // Transform from -0.5, 0.5 in cloud space to 0, 1 in texture space
                coord += float4(0.5, 0.5, 0.5, 0);

                // Bound noise to cloud region
                if (coord.y > 0 && coord.y < 1)
                {
    				float density = tex3Dlod(_NoiseTex, float4((worldCoord - _NoiseOffset.xyz) / _NoiseScale.xyz , 0)).r;
                    float coverage = 1 - tex2Dlod(_CoverageTex, float4(coord.xz, 0, 1)).r;
                    float heightGrade = tex2Dlod(_HeightGradientTex, float4(0.5, coord.y, 0, 1)).r;
                    density = remap(density * heightGrade, coverage, 1.0, 0.0, 1.0);

                    color.rgba = max(density, 0.0);
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
				return frac(sin(dot(st.xyz, float3(12.9898, 78.233, 63.5962))) *	43758.5453123);
			}

            inline float randomExtra(float3 st, float extra)
            {
                return frac(sin(dot(float4(st.xyz, extra), float4(12.9898, 78.233, 63.5962, extra))) * 43758.5453123);
            }

            inline float3 randomSpherePoint(float3 pos, float extra)
            {
                float3 sphere = float3(0, 0, 0);

                sphere.x = randomExtra(pos, 175.2235 * extra);
                sphere.y = abs(randomExtra(pos, 43.7853 * extra));
                sphere.z = randomExtra(pos, 4.5561 * extra);

                return normalize(sphere);
            }

            inline float rayPlaneDistance(float3 rayOrigin, float3 rayDir, float3 planeNormal, float planeOffset)
            {
                float projectedRayOrigin = dot(planeNormal, rayOrigin);
                float projectedRayStep = dot(planeNormal, rayDir);
                return (planeOffset - projectedRayOrigin) / projectedRayStep;
            }

            inline float3 rayPlaneIntersection(float3 rayOrigin, float3 rayDir, float3 planeNormal, float planeOffset)
            {
                return rayOrigin + rayDir * rayPlaneDistance(rayOrigin, rayDir, planeNormal, planeOffset);
            }

            // Used to align rays along view planes
            inline float3 snapToView(float3 position, float3 viewDir, float increments)
            {
                float currentDistance = dot(position, viewDir);
                float offsetDistance = fmod(currentDistance, increments);

                position -= viewDir * offsetDistance;
                return position;
            }

            inline float powderTerm(float particleDensity)
            {
                #ifdef USE_POWDER_EFFECT
                return 1.0 - exp(-particleDensity * _PowderFactor);
                #else
                return 1.0;
                #endif
            }

            inline float henyeyGreensteinPhase(float eccentricity, float cosTheta)
            {
                #ifdef USE_SCATTERING
                float eccentricitySquared = eccentricity * eccentricity;
                return (1.0 / 2.0) * (1.0 - eccentricitySquared) / pow(1.0 + eccentricitySquared - 2.0 * eccentricity * cosTheta, 3.0 / 2.0);
                #else
                return (1.0 / 2.0);
                #endif
            }

            inline float3 sampleCloudLight(float3 rayPos, int numSteps, float rayDistance, float particleDensity, float hgScatterTerm)
            {
                float stepSize = rayDistance / numSteps;
                float3 rayDir = _WorldSpaceLightPos0;
                float3 rayStep = rayDir * stepSize;

                #ifdef USE_TEMPORAL_JITTER
                rayStep -= rayDir * stepSize * random(rayPos) * 0.5;
                #endif

                float accumulatedDensity = 0.0;
                
                // Integrate density function along line towards the sunlight
                for (int i = 0; i < numSteps; i++)
                {
                    float sampleDensity = sampleNoise(worldToCloudSpace(rayPos), rayPos).r * _ShadowDensity;
                    accumulatedDensity += sampleDensity * stepSize;

                    // If little light can penetrate at this point, stop
                    if (accumulatedDensity > -log(0.01))
                    {
                        break;
                    }

                    rayPos += rayStep;
                }

                float ambientDensity = 0.0;
                float ambientStepDistance = _AmbientDistance / _AmbientSteps;

                for (int j = 0; j < _AmbientSteps; j++)
                {
                    float3 offset = rayPos + randomSpherePoint(rayPos, j) * ambientStepDistance * j;
                    ambientDensity += sampleNoise(worldToCloudSpace(offset), offset).r * _ShadowDensity;
                }

                // Find light intensity using Beer's Law
                float3 directLight = _LightColor0 * exp(-accumulatedDensity) * powderTerm(particleDensity) * hgScatterTerm;
                float3 ambientTerm = unity_AmbientSky * exp(-ambientDensity);
                return  directLight * _LightScale + ambientTerm * _AmbientScale;
            }

            inline float4 sampleCloudRay(float3 rayPos, float3 rayDir, int numSteps, float rayDistance)
            {
                float stepSize = rayDistance / numSteps;
                float3 rayStep = rayDir * stepSize;

                float4 accumulatedColor = float4(0, 0, 0, 0);

                float cosScatterAngle = dot(_WorldSpaceLightPos0, rayDir);
                float hgScatterTerm = henyeyGreensteinPhase(_ForwardScattering, cosScatterAngle) + henyeyGreensteinPhase(_BackwardScattering, cosScatterAngle);

                for (int i = 0; i < numSteps; i++)
                {
                    float particleDensity = sampleNoise(worldToCloudSpace(rayPos), rayPos).r * _Density * stepSize;

                    // Only sample if particle can contribute to color
                    if (particleDensity > 0.01)
                    {
                        float4 particleColor = float4(sampleCloudLight(rayPos, _ShadowSteps, _ShadowDistance, particleDensity, hgScatterTerm) * particleDensity, particleDensity);

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
				float3 viewRayPos = input.worldPos;
				float3 viewRayDir = normalize(input.viewDir);

                float cloudBaseHeight = _Offset.y - _Scale.y / 2.0;
                float cloudTopHeight = _Offset.y + _Scale.y / 2.0;

                float marchDistance = _MarchDistance;

                // Snap ray to cloud base plane
                if (viewRayPos.y < cloudBaseHeight)
                {
                    viewRayPos = rayPlaneIntersection(viewRayPos, viewRayDir, float3(0, 1, 0), cloudBaseHeight);
                    marchDistance = min(rayPlaneDistance(viewRayPos, viewRayDir, float3(0, 1, 0), cloudTopHeight), marchDistance);
                }
                else if (viewRayPos.y > cloudTopHeight)
                {
                    viewRayPos = rayPlaneIntersection(viewRayPos, viewRayDir, float3(0, 1, 0), cloudTopHeight);
                    marchDistance = min(rayPlaneDistance(viewRayPos, viewRayDir, float3(0, 1, 0), cloudBaseHeight), marchDistance);
                }

                float viewStepSize = marchDistance / _NumSteps;

				#ifdef USE_TEMPORAL_JITTER
				// Apply temporal jitter
				viewRayPos -= input.viewDir * random(viewRayPos) * viewStepSize * 0.5;
				#else
				// Snap to view planes
                viewRayPos = snapToView(viewRayPos, viewRayDir, viewStepSize);
				#endif

				// Calculate max depth
				float screenDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screenPos));
				screenDepth = LinearEyeDepth(screenDepth);

				float currentDepth = dot(viewRayDir, viewRayPos - _WorldSpaceCameraPos);
				float travelDepth = screenDepth - currentDepth;
                marchDistance = min(marchDistance, travelDepth);

				// Limit steps by travel distance
				int numSteps = min(_NumSteps, travelDepth / viewStepSize);
				viewStepSize = marchDistance / numSteps;

				fixed4 color = saturate(sampleCloudRay(viewRayPos, viewRayDir, numSteps, marchDistance));
            
                // apply fog
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }


            ENDCG
        }
    }
}
