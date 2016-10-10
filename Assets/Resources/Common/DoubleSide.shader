Shader "DoubleSide" 
{
    Properties 
	{
		_MainTex ("MainTex", 2D) = "black" {}
	}
	   CGINCLUDE
	   #include "UnityCG.cginc"
        

       uniform sampler2D _MainTex : register(s0);

       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    float2 uv : TEXCOORD0;
       };
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
           output.uv = input.texcoord;

           return output;
        }
	   
        float4 MainPS(VS_OUTPUT input) : COLOR 
        {   
		    return tex2D(_MainTex,input.uv);
        }

         ENDCG
	     SubShader 
	     {
		       Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {				
			          Name "KGLine"
					  cull off
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
		Fallback Off
}
