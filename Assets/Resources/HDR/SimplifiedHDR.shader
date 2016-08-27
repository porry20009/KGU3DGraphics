Shader "PostEffect/SimplifiedHDR"
{
	CGINCLUDE
	#include "UnityCG.cginc"
            
    uniform sampler2D _MainTex;
	uniform sampler2D _CurrAverLumTex;
	uniform sampler2D _PreAverLumTex;
	uniform sampler2D _AdaptedLumTex;
	uniform sampler2D _BloomTex;
	
	uniform half _MiddleGray;
	uniform half _AdaptiveSpeed;
	uniform half _ElapsedTime;
	uniform half2 _AverImageSizeInverse;

	uniform half _BrightOffset;
	uniform half _BrightScale;
	
	uniform half _Exposure;
	uniform half _Coeff0;

	static const float2 g_texOffset4x4[16] = 
	{
		float2(0,0), float2(0,1), float2(0,2), float2(0,3),
		float2(1,0), float2(1,1), float2(1,2), float2(1,3),
		float2(2,0), float2(2,1), float2(2,2), float2(2,3),
		float2(3,0), float2(3,1), float2(3,2), float2(3,3)
	};

	static const float2 g_texOffset2x2[4] = 
	{
		float2(0,0), float2(0,1),
		float2(1,0), float2(1,1)
	};

    struct VS_OUTPUT
    {
        float4 pos : SV_POSITION;
        half2 uv : TEXCOORD0;
    };
    
    VS_OUTPUT MainVS(appdata_base input)
    {
        VS_OUTPUT output = (VS_OUTPUT)0;

        output.pos = mul(UNITY_MATRIX_MVP, input.vertex);
        output.uv = input.texcoord;

        return output;
    }

	fixed4 EncodeLuminance(fixed v)
	{
		fixed h = v;
		fixed positive = v > 0 ? v : 0;
		fixed negative = v < 0 ? -v : 0;

		fixed4 color = 0;

		color.r = positive;
		color.g = negative;

		color.ba = frac(color.rg * 256);
		color.rg -= color.ba / 256.0f;
		return color;
	}

	fixed DecodeLuminance(fixed4 v)
	{
		fixed4 table = fixed4(1.0f, -1.0f, 1.0f / 256.0f, -1.0f / 256.0f);
		return dot(v, table);
	}

	fixed3 ToneMapping(fixed3 oriColor,fixed adaptedLum)
	{
		fixed multiplier =  pow(_MiddleGray / adaptedLum, 0.75);
		return oriColor * multiplier;
	}

    fixed3 ToneMappingHigh(fixed3 oriColor,fixed adaptedLum)
	{
		fixed3 HDRImage = oriColor;

	    fixed multiplier = _MiddleGray / adaptedLum;
	    fixed p = multiplier > 1 ? 0.5f : 0.85f;
	    multiplier = pow(multiplier, p);
	
	    HDRImage.rgb *= multiplier;
	
	    fixed3 table = fixed3(0.3, 0.59, 0.11);
	    fixed lum = dot(HDRImage.rgb, table.rgb);
	
	    fixed final = 1.0f - exp(-lum * _Exposure) * (adaptedLum * _Coeff0);
    	HDRImage *= final;
	    return HDRImage;
	}

	fixed4 LogLuminancePS(VS_OUTPUT input) : COLOR
	{
		fixed4 rgba = tex2D(_MainTex,input.uv);
		fixed luminance = dot(rgba.rgb, fixed3(0.3, 0.59, 0.11));
		fixed logLuminance = log(luminance + 0.0001);
		return EncodeLuminance(logLuminance);
	}

	fixed4 Average4x4SamplesPS(VS_OUTPUT input) : COLOR
	{
		fixed lum = 0;
		for (int i = 0; i < 16; i++)
		{
			float2 texcoord = input.uv + g_texOffset4x4[i].xy * _AverImageSizeInverse.xy;
			lum += DecodeLuminance(tex2D(_MainTex, texcoord));
		}
		lum *= 0.0625;
		return EncodeLuminance(lum);
    }

	fixed4 Average2x2SamplesPS(VS_OUTPUT input) : COLOR
	{
		fixed lum = 0;
		for (int i = 0; i < 4; i++)
		{
			float2 texcoord = input.uv + g_texOffset2x2[i].xy * _AverImageSizeInverse.xy;
			lum += exp(DecodeLuminance(tex2D(_MainTex, texcoord)));
		}
		lum *= 0.25;
		return EncodeLuminance(lum);
    }


	fixed4 AdaptiveLuminancePS(VS_OUTPUT input) : COLOR
	{
		fixed prevLuminance = DecodeLuminance(tex2D(_PreAverLumTex,  half2(0.5, 0.5)));
		fixed currentLuminance = DecodeLuminance(tex2D(_CurrAverLumTex, half2(0.5, 0.5)));
		fixed newLuminance = lerp(prevLuminance, currentLuminance, saturate(_AdaptiveSpeed * _ElapsedTime));
		return EncodeLuminance(newLuminance);
	}


	fixed4 AutoExposurePS(VS_OUTPUT input) : COLOR
	{
		fixed4 image = tex2D(_MainTex, input.uv);
		fixed adaptedLum = (DecodeLuminance(tex2D(_AdaptedLumTex, half2(0.5, 0.5))));
		fixed3 color = ToneMappingHigh(image.rgb,adaptedLum);
		return fixed4(color.rgb,1.0);

	}

	fixed4 AutoExposureBloomPS(VS_OUTPUT input) : COLOR
	{
		fixed4 image = tex2D(_MainTex, input.uv);
#if UNITY_UV_STARTS_AT_TOP
		fixed4 bloom = tex2D(_BloomTex, half2(input.uv.x,1 - input.uv.y));
#else
		fixed4 bloom = tex2D(_BloomTex, input.uv);

#endif
		fixed adaptedLum = (DecodeLuminance(tex2D(_AdaptedLumTex, half2(0.5, 0.5))));
		fixed3 color = ToneMappingHigh(image.rgb,adaptedLum);
		return fixed4(color.rgb + bloom.rgb,1.0);
	}


    fixed4 BrightnessPS(VS_OUTPUT input) : COLOR
    {
    	fixed4 rgba = tex2D(_MainTex,input.uv);
    	fixed  adaptedLum = tex2D( _CurrAverLumTex, float2(0.5f, 0.5f) );
    	rgba.rgb = ToneMappingHigh(rgba.rgb,adaptedLum);
    	rgba = max(rgba, 0.0f);
    	fixed4 old_intensity = dot(rgba.rgb, fixed3(0.3, 0.59, 0.11));
		fixed4 new_intensity = (old_intensity + _BrightOffset) * _BrightScale;
		return fixed4(rgba.rgb * new_intensity,1.0);
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
			Name "Luminance"
			CGPROGRAM

			#pragma vertex MainVS
			#pragma fragment LogLuminancePS
			#pragma target 2.0

			ENDCG
		}

		Pass
		{
			Name "Average4x4Samples"
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment Average4x4SamplesPS
            #pragma target 2.0
           
            ENDCG
		}

		Pass
		{
			Name "Average2x2amples"
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment Average2x2SamplesPS
            #pragma target 2.0
           
            ENDCG
		}


		Pass
		{
			Name "AdaptiveLuminance"
			CGPROGRAM

			#pragma vertex MainVS
			#pragma fragment AdaptiveLuminancePS
			#pragma target 2.0

			ENDCG
		}

		Pass
		{
			Name "AutoExposureWithoutBloom"
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment AutoExposurePS
            #pragma target 2.0

			ENDCG
		}

		Pass
		{
			Name "AutoExposureWithBloom"
			//Blend one one
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment AutoExposureBloomPS
            #pragma target 2.0

			ENDCG
		}

		Pass
		{
			Name "Brightness"
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment BrightnessPS
            #pragma target 2.0
           
            ENDCG
		}
	}
}
