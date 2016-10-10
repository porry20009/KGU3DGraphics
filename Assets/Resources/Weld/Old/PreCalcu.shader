Shader "Weld/PreCalcu" 
{

	   CGINCLUDE
	   #include "UnityCG.cginc"
        
	   uniform float2 _DrawUV;
	   uniform float _Force;
	   uniform float _Range;
	   uniform float _Smoothness;

       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    half2 v2Texcoord : TEXCOORD0;
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
		    float dist = length(input.v2Texcoord -  _DrawUV) * 30;
			//float alpha = smoothstep(_Range-_Smoothness,_Range + _Smoothness,dist);
			float alpha = 2 * exp(-3 * pow(dist/1,8));
		    return alpha * _Force;
		}
         ENDCG
	     SubShader 
	     {
		        Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "PreCalcu"
					  cull off
					  //Blend one one
		             // Blend SrcAlpha OneMinusSrcAlpha

		              Blend SrcAlpha one
					 // BlendOp Max
					  //Blend SrcAlpha DstAlpha
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
}
