Shader "Weld/Heat/AddHeat" 
{
	   CGINCLUDE
	   #include "UnityCG.cginc"
        
	   static const float PI =  3.1415926536;

	   uniform float4 _HeatSourcePos;
	   uniform float _GaussianDistributionSigma;
	   uniform float _EffectivePower;
	   uniform float _HeatRange;
       
	   struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    float2 v2Texcoord : TEXCOORD0;
		    float4 v4WPos : TEXCOORD1;

       };
	   
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.Position = float4(input.texcoord.x * 2 - 1,(1 - input.texcoord.y) * 2 - 1,0,1);
           output.v2Texcoord = input.texcoord.xy;
           output.v4WPos = mul(_Object2World, input.vertex);

           return output;
        }

		float GassianHeatSource(float Q,float sigma,float r)
		{
			return Q / (2.0 * PI * sigma * sigma) * exp(-r * r / (2.0 * sigma * sigma));
		}
		
		float4 MainPS(VS_OUTPUT input) : COLOR
	 	{
		   float dist = length(input.v4WPos.xyz - _HeatSourcePos.xyz) * _HeatRange;
		   return GassianHeatSource(_EffectivePower, _GaussianDistributionSigma,dist);
		}
         ENDCG
	     SubShader 
	     {
		        Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "AddHeat"
					  cull off
					  ZWrite Off
			          AlphaTest Off
					  Blend One One

			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
}