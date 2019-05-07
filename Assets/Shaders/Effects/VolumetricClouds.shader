Shader "Unlit/Clouds 1"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 3D) = "white" {}
		_NumSteps ("Raymarching Steps", Range(1, 3000)) = 10
        _MarchDistance ("Raymarch Distance", Range(0.0, 1000.0)) = 1.0
        _Density ("Density", Range(0.0, 20.0)) = 1.0
        _Contrast ("Contrast", Range(0.0, 10.0)) = 1.0
        _Offset ("Cloud Origin", Vector) = (0, 0, 0, 1)
        _Scale ("Cloud Region", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        ZTest off Cull Back ZWrite Off
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

            sampler3D _NoiseTex;
            float4 _NoiseTex_ST;

            // Custom parameters
			int _NumSteps;
			float _MarchDistance;
            float _Density;
            float _Contrast;
            float4 _Offset;
            float4 _Scale;

            fragInput vert (appdata v)
            {
                fragInput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.localPos = v.vertex;
				o.viewDir = float4(normalize(o.worldPos.xyz - _WorldSpaceCameraPos), 1.0);

                return o;
            }


			// Custom code

            float AdjustContrast(float color, float contrast) {
                return saturate(lerp(0.5, color, contrast));
            }

			inline float4 sampleNoise(float3 coord)
			{
				float4 color = float4(0, 0, 0, 0);

                coord += float3(0.5, 0.5, 0.5);

                // Bound noise to cloud region
                if (coord.x > 0 && coord.y > 0 && coord.z > 0 && coord.x < 1 && coord.y < 1 && coord.z < 1)
                {
    				color = tex3Dlod(_NoiseTex, float4(coord.xyz, 0.0));
                }

				return color;
			}

            // Transform world position to cloud space
            inline float3 worldToCloudSpace(float3 coord)
            {
                coord -= _Offset.xyz;
                coord /= _Scale.xyz;

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

			fixed4 frag(fragInput input) : SV_Target
			{
                // TODO: Calculate view direction and world position from pixel position
                // Doing so in fragment shader will prevent distortion

                float stepSize = _MarchDistance / _NumSteps;
				float raymarchValue = 0;
                float debugValue = 1;

				float3 rayPos = input.worldPos;
				float3 rayDir = input.viewDir.xyz;
                rayPos = snapToView(rayPos, rayDir, stepSize);

                // Do opacity march through volume texture
				for (int i = 0; i < _NumSteps; i++)
				{
					float3 coord = worldToCloudSpace(rayPos);
					float4 colorSample = sampleNoise(coord);

					raymarchValue += AdjustContrast(colorSample.r, _Contrast) * stepSize;

					rayPos += input.viewDir * stepSize;
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
