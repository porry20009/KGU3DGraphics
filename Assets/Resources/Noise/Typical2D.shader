Shader "PerlinNoise/Typical2D" 
{

	 CGINCLUDE
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

	   // 伪随机数[-1,1]
	   float noise(float2 pos)
	   {
		    pos += (int2)_Seed;
		    return 2.0 * frac(sin(dot(pos*0.001, float2(24.12357, 36.789))) * 12345.123) - 1.0;
	   }

	    //五次样条线插值
		float fifthSplineInterpolate(float a, float b, float t)
		{
			float f = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
			return lerp(a,b,f);
		}

		// 网格点的噪音进行3x3 box平滑
		float smoothNoise(float2 pos)
		{
			float rightX = pos.x + 1;
			float leftX = pos.x - 1;
			float rightY = pos.y + 1;
			float leftY = pos.y - 1;
			float2 curr = pos;

			float corners = (noise(float2(rightX, rightY)) + noise(float2(leftX, rightX)) + noise(float2(rightX, leftX)) + noise(pos + float2(leftX, leftX))) / 16.0;
			float slides = (noise(float2(rightX, curr.y)) + noise(pos + float2(leftX, curr.y)) + noise(float2(curr.x, rightY)) + noise(pos + float2(curr.x, leftY))) / 8.0;
			float center = noise(curr) / 4.0;
			return corners + slides + center;
		}

		// 网格内点的噪音通过两次样条线插值得到
		float interpolateNoise(float2 pos,float frequency)
		{
			pos *= frequency;
			float2 integerXY = floor(pos);
			float2 fracXY = frac(pos);

			float v1 = smoothNoise(fmod(integerXY, frequency));
			float v2 = smoothNoise(float2(fmod(integerXY.x + 1.0, frequency), integerXY.y));
			float v3 = smoothNoise(float2(integerXY.x, fmod(integerXY.y + 1.0, frequency)));
			float v4 = smoothNoise(float2(fmod(integerXY.x + 1.0, frequency), fmod(integerXY.y + 1.0, frequency)));

			float i1 = fifthSplineInterpolate(v1, v2, fracXY.x);
			float i2 = fifthSplineInterpolate(v3, v4, fracXY.x);

			return fifthSplineInterpolate(i1, i2, fracXY.y);
		}

		//分型叠加
		float fractalAdditive(float2 pos,int octaves, float persistence, float lacunarity, float frequency, float amplitude)
		{
			float sum = 0.0f;
			for (int i = 0; i < octaves; i++)
			{
				float perlinNoise = interpolateNoise(pos,frequency) * amplitude;
				sum += perlinNoise;
				frequency *= lacunarity;
				amplitude *= persistence;
			}
			return sum;
		}

        float4 TypicalPS(VS_OUTPUT input) : COLOR 
        {   
		    float noise = fractalAdditive(input.v2Texcoord,_Octaves,_Persistence,_Lacunarity,_Frequency,_Amplitude);
			noise = 0.5 + noise * 0.5;
		    return float4(noise, noise, noise, noise);
         }
		
		uniform float _CloudShapness;
		uniform float _CloudCover;
	    //云纹理
		float4 CloudPS(VS_OUTPUT input) : COLOR
		{
			 float noise = fractalAdditive(input.v2Texcoord,_Octaves,_Persistence,_Lacunarity,_Frequency,_Amplitude);
		     noise = 0.5 + noise * 0.5;

			 float c = noise - _CloudCover;
		     float density = 1.0 - pow(_CloudShapness,c);
		     return density;
		 }
         ENDCG
	     SubShader 
	     {
		       Tags { "RenderType"="Opaque" }
		        LOD 200
		        Lighting Off
		        ZWrite Off
		        AlphaTest Off
		        Pass
		        {
			          Name "Typical"
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment TypicalPS
                      #pragma target 3.0
           
                      ENDCG
		        }
			    Pass
			    {
				       Name "Cloud"
				       CGPROGRAM

                       #pragma vertex MainVS
                       #pragma fragment CloudPS
                       #pragma target 3.0

				       ENDCG
			    }
	    }
}
