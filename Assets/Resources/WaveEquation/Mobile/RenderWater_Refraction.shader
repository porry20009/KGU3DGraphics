﻿Shader "Water/SmallScale/RenderWater_Refraction" 
{
    Properties 
	{
		_RefractTex ("Refract Tex", 2D) = "black" {}
		_WaterNormal ("Water Normal Tex", 2D) = "black" {}
		_WaterShapeTex ("Water Shape Tex", 2D) = "white" {}
	    _ReflectTex ("_Reflect Texture (RGB)", Cube) = "" { TexGen CubeReflect }
		_DistortLevel("Water Distort Level ", Float) = 0.01
	}
    SubShader 
    {
        Tags { "Queue" = "Transparent"}
		Lighting Off

        Pass 
        {
		    Blend SrcAlpha OneMinusSrcAlpha
        
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"
            
            uniform sampler2D _WaterNormal;
            uniform sampler2D _RefractTex;
            uniform sampler2D _WaterShapeTex;
            uniform samplerCUBE _ReflectTex;
        
            uniform float4x4 g_matForceViewProj;
            uniform half  _DistortLevel;

            uniform fixed4 _WaterColor;
			uniform half4 _SunDir;
			uniform half _FrenelBias;
        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
				float4 v4ProjMC: TEXCOORD0;
				float4 v4ProjSC : TEXCOORD1;
				half4  v4WPos : TEXCOORD2;
				half3  v2Texcoord : TEXCOORD3;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                
                output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
                output.v4ProjMC = ComputeScreenPos(output.Position);
                output.v4WPos = mul(_Object2World, input.vertex);
                
			    float4x4 matSCMVP= mul (g_matForceViewProj,_Object2World);
                output.v4ProjSC = mul(matSCMVP, input.vertex);
                
                output.v2Texcoord = half3(input.texcoord.xy,output.Position.w);
                return output;
            }
            
            half Fresnel(half NdotL,half frenelBias,half fresnelPow)
            {
              half facing = (1.0 - NdotL);
              return max(frenelBias + (1.0 - frenelBias) * pow(facing,fresnelPow), 0.0);
            }

            fixed4 MainPS(VS_OUTPUT input) : COLOR 
            {   
				half2 v2UV = 0.5 * input.v4ProjSC.xy / input.v4ProjSC.w + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
				v2UV.y = 1 - v2UV.y;
                #endif

                fixed4 normalmap = tex2D(_WaterNormal, v2UV);
				half3 normal = normalize((normalmap.rgb - 0.5f) * 2.0f);
                
				half3 incident = normalize(input.v4WPos.xyz - _WorldSpaceCameraPos.xyz);
                half3 v3ReflectDir = normalize(reflect(incident, normal));
                half3 v3ViewDir = -incident.xyz;
               
	            half waterShape = tex2D(_WaterShapeTex, input.v2Texcoord).r;
                fixed3 v3ReflectColor = texCUBE(_ReflectTex,v3ReflectDir).xyz;
                
		        half4 distortOffset = half4(normal.xy * _DistortLevel * normal.z, 0, 0);
	            fixed3 v3RefractColor = tex2Dproj(_RefractTex, UNITY_PROJ_COORD(input.v4ProjMC + distortOffset)).rgb;
	          
	            half NdotL  = max(dot(v3ViewDir.xyz,normal),0);
                half facing = 1.0 - NdotL;
                half fresnel = max(0,Fresnel(NdotL,_FrenelBias,5));

				half3 sunDir = normalize(_SunDir.xyz);
				float specular_factor = pow(max(0,dot(sunDir, v3ReflectDir)), _SunDir.w);
                
				half diffuse_factor = max(0,dot(sunDir, normal));
                fixed3 v3WaterColor = lerp(v3RefractColor,v3ReflectColor, fresnel);
				v3WaterColor += diffuse_factor * _WaterColor;
				v3WaterColor += specular_factor;

				//return fresnel;
				return fixed4(v3WaterColor, waterShape);
            }
            ENDCG
        }
    }
}
