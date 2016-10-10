Shader "Weld/Heat/ShowHeat" 
{

	   CGINCLUDE
	   #include "UnityCG.cginc"
        
	   uniform sampler2D _HeatMap;
	   uniform sampler2D _Normalmap;

       struct VS_OUTPUT
       {
            float4 v4Position : SV_POSITION;
		    half2 v2Texcoord : TEXCOORD0;
       };
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.v4Position = mul (UNITY_MATRIX_MVP, input.vertex);
           output.v2Texcoord = float2(input.texcoord.x,input.texcoord.y);

           return output;
        }
		
		float4 MainPS(VS_OUTPUT input) : COLOR
	 	{
		    float4 heatColor = tex2D(_HeatMap,input.v2Texcoord);
		    float4 normal = tex2D(_Normalmap,input.v2Texcoord);

		    return heatColor;
		}
         ENDCG
	     SubShader 
	     {
		        Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "Temperature"
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
}
