Shader "Water/SmallScale/AddFource" 
{
    Properties 
	{
		//_MainTex ("Base(RGB)", 2D) = "black" {}
		_Force(" Force ", Float) = 1.0
		_WaterPlaneY(" Water PlaneY ", Float) = 0.0
	}
    SubShader 
    {
		Tags { "RenderType"="Opaque" }
        Pass 
        {
			Lighting Off
			ZWrite Off
			AlphaTest Off
			cull off
			//Blend One One
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"


           // uniform sampler2D _MainTex : register(s0);
            uniform half _WaterPlaneY;
            uniform half _Force;
        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
                //half2 uv : TEXCOORD0;
                float4 v4WolrdPos : TEXCOORD0;
				float4 v4ProjPos : TEXCOORD1;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
				//output.uv = input.texcoord;
                output.v4WolrdPos = mul(_Object2World, input.vertex);
                output.v4ProjPos = output.Position;
                return output;
            } 

            fixed4 MainPS(VS_OUTPUT input) : COLOR 
            {   
                if ((_WaterPlaneY - input.v4WolrdPos.y) < 0.0)
                   return 0;
				float3 pos = 0.5 * input.v4ProjPos.xyz/ input.v4ProjPos.w + 0.5;
	            return fixed4(pos,_Force);
            }
            ENDCG
        }
    }
}
