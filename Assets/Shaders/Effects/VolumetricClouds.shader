// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// TODO:
// Snap ray start and end to cloud volume bounds
// Add lighting

Shader "Unlit/Clouds"
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
        _CloudDistance ("Max Cloud Distance", Range(0.0, 10000.0)) = 300.0
        _Density ("Density", Range(0.0, 20.0)) = 1.0

        _ShadowSteps ("Shadow Steps", Range(1, 300)) = 32
        _ShadowDistance ("Shadow Distance", Range(0.0, 100.0)) = 2.0
		_ShadowDensity ("Shadow Density", Range(0.0, 20.0)) = 1.0

        _CloudOffset ("Cloud Center", Vector) = (0, 0, 0, 1)
        _CloudScale ("Cloud Size", Vector) = (1, 1, 1, 1)
        _NoiseScale ("Noise Scale", Vector) = (10, 10, 10, 1)
        _NoiseOffset ("Noise Offset", Vector) = (0, 0, 0, 1)

        [Toggle(TILE_CLOUDS)]
        _TileClouds ("Tile Clouds", float) = 0.0

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
        ZTest Off Cull Back ZWrite Off
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

            #pragma shader_feature TILE_CLOUDS
			#pragma shader_feature USE_TEMPORAL_JITTER
            #pragma shader_feature USE_POWDER_EFFECT
            #pragma shader_feature USE_SCATTERING

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct vertOutput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)

				float3 viewDir : POSITION1;
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
            float _CloudDistance;
            float _Density;

            float _LightScale;
            float _AmbientScale;

            int _ShadowSteps;
            float _ShadowDistance;
			float _ShadowDensity;

            float4 _CloudOffset;
            float4 _CloudScale;

            float4 _NoiseScale;
            float4 _NoiseOffset;

            float _PowderFactor;

            float _ForwardScattering;
            float _BackwardScattering;

            // Loaded matrices

			float4x4 _FrustumCorners;
			float4x4 _InverseView;
			float4 _CameraPosition;

            vertOutput vert (appdata v)
            {
                vertOutput output;

				int cornerIndex = v.vertex.z;
				v.vertex.z = 0;

                output.position = v.vertex;
                output.uv = v.uv;

				output.viewDir = _FrustumCorners[cornerIndex];
                output.viewDir = mul(_InverseView, output.viewDir);

                UNITY_TRANSFER_FOG(output, output.vertex);

                return output;
            }


			// Begin program

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
            
            // Transform world position to cloud space
            inline float3 worldToCloudSpace(float3 coord)
            {
                coord -= _CloudOffset;
                coord /= _CloudScale;

                return coord;
            }

            inline float sampleNoiseCheap(float3 coord)
            {
                float3 localCoord = worldToCloudSpace(coord);

                localCoord += float4(0.5, 0.5, 0.5, 0);

                #ifdef TILE_CLOUDS
                if (localCoord.y > 0 && localCoord.y < 1)
                #else
                if (localCoord.x > 0 && localCoord.x < 1 && localCoord.y > 0 && localCoord.y < 1 && localCoord.z > 0 && localCoord.z < 1)
                #endif
                {
                    float density = tex2Dlod(_CoverageTex, float4(localCoord.xz, 0, 2)).r;
                    return density;
                }

                return 0;
            }

			inline float sampleNoise(float3 coord)
			{
                float3 localCoord = worldToCloudSpace(coord);
				float4 color = float4(0, 0, 0, 0);

                // Transform from -0.5, 0.5 in cloud space to 0, 1 in texture space
                localCoord += float4(0.5, 0.5, 0.5, 0);

                // Bound noise to cloud region
                #ifdef TILE_CLOUDS
                if (localCoord.y > 0 && localCoord.y < 1)
                #else
                if (localCoord.x > 0 && localCoord.x < 1 && localCoord.y > 0 && localCoord.y < 1 && localCoord.z > 0 && localCoord.z < 1)
                #endif
                {
    				float density = tex3Dlod(_NoiseTex, float4((coord - _NoiseOffset.xyz) / _NoiseScale.xyz , 0)).r;
                    float coverage = 1 - tex2Dlod(_CoverageTex, float4(localCoord.xz, 0, 1)).r;
                    float heightGrade = tex2Dlod(_HeightGradientTex, float4(0.5, localCoord.y, 0, 1)).r;
                    density = remap(density * heightGrade, coverage, 1.0, 0.0, 1.0);

                    return max(0.0, density);
                }

				return 0;
			}

			// Pseudorandom function from https://thebookofshaders.com/10
            // TODO: Possible replace with noise texture lookup for faster randoms
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
                // TODO: Snap ray distances to end of cloud volume?

                float stepSize = rayDistance / numSteps;
                float3 rayDir = _WorldSpaceLightPos0;
                float3 rayStep = rayDir * stepSize;

                #ifdef USE_TEMPORAL_JITTER
                rayStep += (abs(random(rayPos)) - 0.5) * rayStep;
                #endif

                float accumulatedDensity = 0.0;
                
                // Integrate density function along line towards the sunlight
                for (int i = 0; i < numSteps; i++)
                {
                    if (sampleNoiseCheap(rayPos) > 0.01)
                    {
                        float sampleDensity = sampleNoise(rayPos).r * _ShadowDensity;
                        accumulatedDensity += sampleDensity * stepSize;

                        // If little light can penetrate at this point, stop
                        if (accumulatedDensity > -log(0.01))
                        {
                            break;
                        }
                    }

                    rayPos += rayStep;
                }

                // Find light intensity using Beer's Law
                float3 directLight = _LightColor0 * exp(-accumulatedDensity) * powderTerm(particleDensity) * hgScatterTerm;
                float3 ambientTerm = unity_AmbientSky;
                return  directLight * _LightScale + ambientTerm * _AmbientScale;
            }

            inline float4 sampleCloudRay(float3 rayPos, float3 rayDir, int numSteps, float rayDistance)
            {
                float stepSize = rayDistance / numSteps;
                float3 rayStep = rayDir * stepSize;

                #ifdef USE_TEMPORAL_JITTER
				// Apply temporal jitter
				rayPos += (abs(random(rayPos)) - 0.5) * rayStep;
				#endif

                float4 accumulatedColor = float4(0, 0, 0, 0);

                float cosScatterAngle = dot(_WorldSpaceLightPos0, rayDir);
                float hgScatterTerm = henyeyGreensteinPhase(_ForwardScattering, cosScatterAngle) + henyeyGreensteinPhase(_BackwardScattering, cosScatterAngle);

                for (int i = 0; i < numSteps; i++)
                {
                    // Only sample if particle can contribute to color
                    if (sampleNoiseCheap(rayPos) > 0.01)
                    {
                        float particleDensity = sampleNoise(rayPos) * _Density * stepSize;
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
				// return fixed4(input.viewDir.xyz, 1.0);

				float3 rayPos = _CameraPosition;
				float3 rayDir = normalize(input.viewDir);

                float cloudBaseHeight = _CloudOffset.y - _CloudScale.y / 2.0;
                float cloudTopHeight = _CloudOffset.y + _CloudScale.y / 2.0;

                float marchDistance = _MarchDistance;

				
                // Snap ray to cloud base plane
                if (rayPos.y < cloudBaseHeight && rayDir.y > 0)
                {
                    rayPos = rayPlaneIntersection(rayPos, rayDir, float3(0, 1, 0), cloudBaseHeight);
                    marchDistance = min(rayPlaneDistance(rayPos, rayDir, float3(0, 1, 0), cloudTopHeight), marchDistance);
                }
                else if (rayPos.y > cloudTopHeight && rayDir.y < 0)
                {
                    rayPos = rayPlaneIntersection(rayPos, rayDir, float3(0, 1, 0), cloudTopHeight);
                    marchDistance = min(rayPlaneDistance(rayPos, rayDir, float3(0, 1, 0), cloudBaseHeight), marchDistance);
                }

                /* if (length(rayPos.xz - _WorldSpaceCameraPos.xz) > _CloudDistance)
                {
                    return half4(0, 0, 0, 0);
                }
				*/

                float viewStepSize = marchDistance / _NumSteps;

                // Snap to view planes
                // rayPos = snapToView(rayPos, rayDir, viewStepSize);

				// Calculate max depth
				float screenDepth = tex2D(_CameraDepthTexture, input.uv);
                screenDepth = LinearEyeDepth(screenDepth);

                float rayDepth = dot(rayPos, rayDir) - dot(_CameraPosition, rayDir);

                // fixed4 debugColor = fixed4(0, 0, 0, 1);
                // debugColor.rgb = min(rayDepth, screenDepth) / 10;
                // // debugColor.rgb = rayPos / 5;
                // return debugColor;

                if (rayDepth > screenDepth)
                {
                    return fixed4(0, 0, 0, 0);
                }

                marchDistance = min(marchDistance, screenDepth - rayDepth);

				// Limit steps by travel distance
				int numSteps = min(_NumSteps, screenDepth / viewStepSize);
				viewStepSize = marchDistance / numSteps;

				fixed4 color = saturate(sampleCloudRay(rayPos, rayDir, numSteps, marchDistance));
            
                // apply fog
                UNITY_APPLY_FOG(input.fogCoord, color);

                return color;
            }


            ENDCG
        }
    }
}
