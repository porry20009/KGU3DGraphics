Shader "Sky/SimplifiedSky" 
{
    SubShader 
    {
        Tags
        {
           "Queue"="Background"
           "IgnoreProjector"="True"
        }
        Fog{ Mode Off}
        Pass 
        {
        	ZWrite Off
			cull front
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"

			uniform fixed4 _DuskHorizonColor;
			uniform fixed4 _DuskSkyColor;
			uniform fixed4 _NoonHorizonColor;
			uniform fixed4 _NoonSkyColor;
			
			uniform fixed4 _SunColor;
			uniform half3 _SunPositon;
			uniform half _ExposeScale;
			uniform half _ExposeOffset;

            struct VS_OUTPUT
            {
                float4 v4Position : SV_POSITION;
				float3 v3VertexPos : TEXCOORD0;
            };

            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                output.v4Position = mul (UNITY_MATRIX_MVP, input.vertex);
				output.v3VertexPos = input.vertex;//天空球的半径是1，（0，0）是原点
                return output;
            }

			fixed4 MainPS(VS_OUTPUT input) : COLOR
            {
			   half verticalDist = saturate(input.v3VertexPos.y);
			   half3 sunPosition = _SunPositon.xyz;
			   sunPosition.y = max(0,sunPosition.y);
			   fixed3 sky_horizonColor = lerp(_DuskHorizonColor.rgb, _NoonHorizonColor.rgb,pow(sunPosition.y,0.3));
			   fixed3 sky_Color = lerp(_DuskSkyColor.rgb, _NoonSkyColor.rgb,pow(sunPosition.y,0.3));
			   // Sky gradient
			   fixed3 skyGradient = lerp(sky_horizonColor, sky_Color, pow(verticalDist,0.5));

			   half sunHaloFactor = length(input.v3VertexPos - sunPosition);
			   half sunHaloPoly = (1.0 - pow(sunHaloFactor, _ExposeOffset + (1.0 - sunPosition.y)*_ExposeScale)) * (1 - verticalDist);
			   fixed3 sunHalo = _SunColor * sunHaloPoly;

               return fixed4(skyGradient + sunHalo, 1.0);
            }

            ENDCG
        }
    }
}
