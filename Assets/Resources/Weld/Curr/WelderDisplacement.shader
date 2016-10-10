Shader "Weld/WelderDisplacement" 
{

	   CGINCLUDE
	   #include "UnityCG.cginc"
        
	   uniform float2 _DrawCenter;
	   uniform float _Height;
	   uniform float _Range;
	   uniform float _WHRatio;

       struct VS_OUTPUT
       {
            float4 v4Position : SV_POSITION;
		    half2 v2Texcoord : TEXCOORD0;
       };
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.v4Position = mul (UNITY_MATRIX_MVP, input.vertex);
           output.v2Texcoord = float2(input.texcoord.y,input.texcoord.x);

           return output;
        }
		
		float4 MainPS(VS_OUTPUT input) : COLOR
	 	{
		    float dist = length((input.v2Texcoord - _DrawCenter) * float2(_WHRatio,1)) * _Range;
			float height = max(0,_Height * exp(-6 * pow(dist/1,6)));
		    return height;
		}
         ENDCG
	     SubShader 
	     {
		        Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "WelderDisplacement"
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
}
