Shader "Weld/Heat/LoseHeat" 
{
	   CGINCLUDE
	   #include "UnityCG.cginc"
        
	   uniform sampler2D _HeatMap;
	   uniform float _HeatDamping;
	   uniform float _MaxHeat;

       struct VS_OUTPUT
       {
            float4 v4Position : SV_POSITION;
		    half2 v2Texcoord : TEXCOORD0;
       };
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.v4Position = mul (UNITY_MATRIX_MVP, input.vertex);
           output.v2Texcoord = input.texcoord.xy;

           return output;
        }
		
		float4 MainPS(VS_OUTPUT input) : COLOR
	 	{
		    float temperature = min(tex2D(_HeatMap,input.v2Texcoord).r,_MaxHeat);
		    return temperature * _HeatDamping;
		}
         ENDCG
	     SubShader 
	     {
		        Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "LoseHeat"
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
}