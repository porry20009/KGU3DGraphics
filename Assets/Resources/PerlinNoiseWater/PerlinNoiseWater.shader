Shader "PerlinNoiseWater/PerlinNoiseWater" 
{
    Properties 
	{
		_RefractTex ("Refract Tex", 2D) = "black" {}
	    _Heightmap("Height Tex", 2D) = "black" {}
		_WaterNormal ("Water Normal Tex", 2D) = "black" {}
	    _ReflectTex ("_Reflect Texture (RGB)", Cube) = "" { TexGen CubeReflect }
		_DistortLevel("Water Distort Level ", Float) = 0.01
	}
    SubShader 
    {
        // Draw ourselves after all opaque geometry
        Tags { "Queue" = "Transparent"}
        //Fog { Mode off }
		Lighting Off

        Pass 
        {
        
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 3.0
           
            #include "UnityCG.cginc"

			uniform sampler2D _Heightmap : register(s0);
            uniform sampler2D _WaterNormal : register(s1);
            uniform sampler2D _RefractTex : register(s2);
            uniform samplerCUBE _ReflectTex : register(s3);
            uniform half  _DistortLevel;
            
            uniform fixed4 _SunColor;
            uniform fixed4 _WaterColor;
			uniform half4 _SunDir;
			uniform half _HeightScale;
			uniform half2 _NoiseTile;

            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
				half2  v2Texcoord : TEXCOORD0;
				float4 v4Proj: TEXCOORD1;
				float4 v4WPos : TEXCOORD2;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
				float height = 2 * tex2Dlod(_Heightmap, float4(input.texcoord.xy, 0, 0)) - 1;
				height *= _HeightScale;
				float3 newPos = input.vertex.xyz + float3(0, height, 0);

				output.v2Texcoord = input.texcoord * _NoiseTile;
                output.Position = mul(UNITY_MATRIX_MVP, float4(newPos,1.0));
                output.v4Proj = ComputeScreenPos(output.Position);
                output.v4WPos = mul(_Object2World, newPos);
                return output;
            }
            
            half Fresnel(half NdotL,half frenelBias,half fresnelPow)
            {
              half facing = (1.0 - NdotL);
              return max(frenelBias + (1.0 - frenelBias) * pow(facing,fresnelPow), 0.0);
            }

            fixed4 MainPS(VS_OUTPUT input) : COLOR
            {   
				fixed4 normalmap = tex2D(_WaterNormal, input.v2Texcoord);
				half3 normal = normalize((normalmap.rgb - 0.5f) * 2.0f);

				half3 incident = normalize(input.v4WPos.xyz - _WorldSpaceCameraPos.xyz);
                half3 v3ReflectDir = normalize(reflect(incident, normal));
                half3 v3ViewDir = -incident.xyz;
               
                fixed3 v3ReflectColor = texCUBE(_ReflectTex,v3ReflectDir).xyz;
		        half4 distortOffset = half4(normal.xy * _DistortLevel * normal.z, 0, 0);
	            fixed3 v3RefractColor = tex2Dproj(_RefractTex, UNITY_PROJ_COORD(input.v4Proj + distortOffset)).rgb;
	          
	            half NdotL  = max(dot(v3ViewDir.xyz,normal),0);
                half facing = 1.0 - NdotL;
                half fresnel = max(0,Fresnel(NdotL,0.2,5));
				//return fresnel;


				half3 sunDir = normalize(_SunDir.xyz);
				float specular_factor = pow(max(0, dot(sunDir, v3ReflectDir)), _SunDir.w);
                
				half diffuse_factor = 0.2 + 0.2 * dot(sunDir, normal);
                fixed3 v3WaterColor = lerp(v3RefractColor, v3ReflectColor, fresnel);
				v3WaterColor += diffuse_factor * _WaterColor;
				v3WaterColor += specular_factor * _SunColor.rgb;

				//return diffuse_factor;
				return fixed4(v3WaterColor, 1.0);
	           // return fixed4(fresnel * v3ReflectColor,waterShape);
            }
            ENDCG
        }
    }
}
