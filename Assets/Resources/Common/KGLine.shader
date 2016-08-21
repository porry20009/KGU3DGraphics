Shader "KGLine" 
{

	   CGINCLUDE
	   #include "UnityCG.cginc"
        
       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    fixed4 color : TEXCOORD0;
       };
            
       VS_OUTPUT MainVS(appdata_full input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.Position = mul(UNITY_MATRIX_VP, input.vertex);
           output.color = input.color;

           return output;
        }
	   
        float4 MainPS(VS_OUTPUT input) : COLOR 
        {   
		    return input.color;
        }

         ENDCG
	     SubShader 
	     {
		       Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "KGLine"
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
		Fallback Off
}
