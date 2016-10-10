Shader "Weld/Fluid/AddForce" 
{
	   CGINCLUDE
	   #include "UnityCG.cginc"
        
	   uniform float2 _CenterUV;
	   uniform float _Radius;
	   uniform float _Smoothness;
       uniform float _Force;
	   uniform float _AspectRatio;//短(0.xxx)：长(1.0)
       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    float2 v2Texcoord : TEXCOORD0;

       };
	   
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.Position = float4(input.texcoord.x * 2 - 1,(1 - input.texcoord.y) * 2 - 1,0,1);
           output.v2Texcoord = input.texcoord.xy;

           return output;
        }

		float4 MainPS(VS_OUTPUT input) : COLOR
	 	{
		   float dist = length((input.v2Texcoord - _CenterUV) * float2(1.0,_AspectRatio));
		   return (1.0 - smoothstep(_Radius -_Smoothness ,_Radius +_Smoothness,dist)) * _Force;
		}
         ENDCG
	     SubShader 
	     {
		        Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "Fluid/AddForce"
					  cull off
					  ZWrite Off
			          AlphaTest Off
					  Blend One One

			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 3.0
           
                      ENDCG
		        }
	    }
}