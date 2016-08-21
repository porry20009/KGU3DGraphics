Shader "PerlinNoise/Gradient2D" 
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
			float2 hash(float2 p)
			{
				p += (int2)_Seed;
				p = float2(dot(p, float2(127.1, 311.7)),
					dot(p, float2(269.5, 183.3)));

				return  2.0 * frac(sin(p)*43758.5453123) - 1.0;
			}

			//余弦插值
			float CosInterpolate(float a, float b, float t)
			{
				float ft = t * 3.14159268979;
				float f = (1 - cos(ft)) * 0.5f;
				return a * (1 - f) + b * f;
			}

			float noise(float2 p,float frequency)
			{
				p *= frequency;
				float2 i = floor(p);
				float2 f = frac(p);

				float dotGradient0 = dot(hash(fmod(i, frequency)), f);
				float dotGradient1 = dot(hash(fmod(i + float2(1.0,0.0), frequency)), f - float2(1.0, 0.0));
				float dotGradient2 = dot(hash(fmod(i + float2(0.0,1.0), frequency)), f - float2(0.0, 1.0));
				float dotGradient3 = dot(hash(fmod(i + float2(1.0,1.0), frequency)), f - float2(1.0, 1.0));

				float i1 = CosInterpolate(dotGradient0, dotGradient1, f.x);
				float i2 = CosInterpolate(dotGradient2, dotGradient3, f.x);

				return CosInterpolate(i1, i2, f.y);
			}

			//分型叠加
			float fractalAdditive(float2 pos,int octaves, float persistence, float lacunarity, float frequency, float amplitude)
			{
				float sum = 0.0f;
				for (int i = 0; i < octaves; i++)
				{
					float perlinNoise = noise(pos, frequency) * amplitude;
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
