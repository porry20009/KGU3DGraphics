Shader "PerlinNoise/Gradient3D" 
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Pass
		{
			Lighting Off
			ZWrite Off
			AlphaTest Off
			CGPROGRAM

			#pragma vertex MainVS
			#pragma fragment  MainPS
			#pragma target 2.0

			#include "UnityCG.cginc"

		    uniform float _Octaves;
	        uniform float _Persistence;
		    uniform float _Lacunarity;
		    uniform float _Frequency;
		    uniform float _Amplitude;
			uniform float2 _Seed;
        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
				half2 v2Texcoord : TEXCOORD0;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                
                output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
                output.v2Texcoord = input.texcoord.xy;

                return output;
            }

			//梯度
			float3 hash(float3 p)
			{
				p = float3(dot(p, float3(127.1, 311.7, 74.7)),
					dot(p, float3(269.5, 183.3, 246.1)),
					dot(p, float3(113.5, 271.9, 124.6)));

				return -1.0 + 2.0*frac(sin(p)*43758.5453123);
			}

			float noise(float3 p, float frequency)
			{
				p *= frequency;
				float3 i = floor(p);
				float3 f = frac(p);

				//五次样条线插值
				float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

				return lerp(lerp(lerp(dot(hash(fmod(i + float3(0.0, 0.0, 0.0), frequency)), f - float3(0.0, 0.0, 0.0)),
					                  dot(hash(fmod(i + float3(1.0, 0.0, 0.0), frequency)), f - float3(1.0, 0.0, 0.0)), u.x),
					             lerp(dot(hash(fmod(i + float3(0.0, 1.0, 0.0), frequency)), f - float3(0.0, 1.0, 0.0)),
						              dot(hash(fmod(i + float3(1.0, 1.0, 0.0), frequency)), f - float3(1.0, 1.0, 0.0)), u.x), u.y),
					        lerp(lerp(dot(hash(fmod(i + float3(0.0, 0.0, 1.0), frequency)), f - float3(0.0, 0.0, 1.0)),
						              dot(hash(fmod(i + float3(1.0, 0.0, 1.0), frequency)), f - float3(1.0, 0.0, 1.0)), u.x),
						         lerp(dot(hash(fmod(i + float3(0.0, 1.0, 1.0), frequency)), f - float3(0.0, 1.0, 1.0)),
							          dot(hash(fmod(i + float3(1.0, 1.0, 1.0), frequency)), f - float3(1.0, 1.0, 1.0)), u.x), u.y), u.z);
			}

			//分型叠加
			float fractalAdditive(float2 pos,int octaves, float persistence, float lacunarity, float frequency, float amplitude)
			{
				float sum = 0.0f;
				for (int i = 0; i < octaves; i++)
				{
					float perlinNoise = noise(float3(pos,_Time.y * 0.1),frequency) * amplitude;
					sum += perlinNoise;
					frequency *= lacunarity;
					amplitude *= persistence;
				}
				return sum;
			}

            float4 MainPS(VS_OUTPUT input) : COLOR 
            {   
				float noise = fractalAdditive(input.v2Texcoord,_Octaves,_Persistence,_Lacunarity,_Frequency,_Amplitude);
			    noise = 0.5 + noise * 0.5;
			    return float4(noise, noise, noise, noise);
            }
            ENDCG
        }
    }
}
