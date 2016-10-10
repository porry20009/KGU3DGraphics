Shader "Weld/Welder" 
{
       Properties 
	   {
		  _MainTex ("Base Tex", 2D) = "white" {}
		  _FluidTex ("Fluid Tex", 2D) = "white" {}
		  _HeatFluxToColorTex ("HeatFlux To ColorTex", 2D) = "white" {}
	   }
	   CGINCLUDE
	   #include "UnityCG.cginc"

	   uniform sampler2D _MainTex;
	   uniform sampler2D _FluidTex;
	   uniform sampler2D _HeatFluxToColorTex;

	   uniform float4 _MainTex_ST;
	   uniform float4 _FluidTex_ST;

	   uniform sampler2D _HeatFluxMap;
	   uniform sampler2D _FluidNormalMap;
	   StructuredBuffer<float4> gPoints;
	   StructuredBuffer<float3> gNormals;
	   uniform int _VerticesCount;

       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    float2 v2Texcoord0 : TEXCOORD0;
		    float2 v2Texcoord1 : TEXCOORD1;
		    float2 v2Texcoord2 : TEXCOORD2;
			float3 v3WNormal : TEXCOORD3;
			float4 v4WPos : TEXCOORD4;
       };
            
       VS_OUTPUT MainVS(appdata_full input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
		   int vertexID = DecodeFloatRG(input.texcoord1.xy) * _VerticesCount;
		   float4 points = gPoints[vertexID];
           float3 normal = gNormals[vertexID];

		   //output.Position = mul(UNITY_MATRIX_VP, float4(points.xyz,1.0));
		   output.Position = mul(UNITY_MATRIX_MVP, input.vertex);

           output.v2Texcoord0 =  TRANSFORM_TEX(input.texcoord, _MainTex);
           output.v2Texcoord1 =  TRANSFORM_TEX(input.texcoord, _FluidTex);
           output.v2Texcoord2 =  input.texcoord;
		   output.v3WNormal = normal;
		   output.v4WPos = points;
           return output;
        }

		float DiffuseFactor(float3 lightDir,float3 normal)
		{
			return max(0,dot(normal,lightDir));
		}

		float SpecularFactor(float3 viewDir,float3 lightDir,float3 normal,float skinness)
		{
			float3 H = normalize(viewDir + lightDir);
			return pow(saturate(dot(H, normal)), skinness);
		}
	   
        float4 MainPS(VS_OUTPUT input) : COLOR 
        {   
			float3 fluidNormal = normalize(tex2D(_FluidNormalMap,input.v2Texcoord2).rbg); 
			//热流密度
			float heatFlux = tex2D(_HeatFluxMap,input.v2Texcoord2).r;
		    float4 baseTexColor = tex2D(_MainTex,input.v2Texcoord0);
		    float4 fluidTexColor = tex2D(_FluidTex,input.v2Texcoord1 + fluidNormal.xz * 0.1);
			float4 temperatureTexColor =  tex2D(_HeatFluxToColorTex, float2(saturate(heatFlux),0));

	        float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.v4WPos.xyz);
			//暂时视线的方向==光线的方向
			float3 lightDir = viewDir;
			float fluidDiffuseFactor = DiffuseFactor(lightDir,fluidNormal);
			float fluidSpecularFactor = SpecularFactor(viewDir,lightDir,fluidNormal,70);
			float3 fluidColor = fluidDiffuseFactor * fluidTexColor.rgb + fluidSpecularFactor + temperatureTexColor.rgb;

			float baseDiffuseFactor = DiffuseFactor(lightDir,input.v3WNormal);
			float baseSpecularFactor = SpecularFactor(viewDir,lightDir,input.v3WNormal,70);
			float3 baseColor = baseDiffuseFactor * baseTexColor.rgb + baseSpecularFactor;
		    
			float3 color = lerp(baseColor,fluidColor,temperatureTexColor.a);
			return float4(color,input.v4WPos.w);
        }

         ENDCG
	     SubShader 
	     {
               Tags { "Queue" = "Transparent+100"}

		        LOD 200
		        Pass
		        {
			          Name "Welder"
		            //  Blend SrcAlpha OneMinusSrcAlpha

			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 5.0
           
                      ENDCG
		        }
	    }
		Fallback Off
}
