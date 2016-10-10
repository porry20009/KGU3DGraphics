Shader "Weld/Fluid/HeightToNormal" 
{
    Properties 
	{
		_HeightCurrentTex ("HeightCurrentTex", 2D) = "black" {}
		_TextureSize("Texture size", Vector) = (256,256,0,0)
	}
    SubShader 
    {
		Tags { "RenderType"="Opaque" }

        Pass 
        {
			Lighting Off
			ZWrite Off
			AlphaTest Off
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"

            uniform sampler2D _HeightCurrentTex : register(s0);
            uniform float4 _TextureSize;
			uniform float _NormalScale;

			static const float2 g_offset[4] =
	        {
				float2(-1.0f, 0.0f),
		        float2( 1.0f, 0.0f),
		        float2( 0.0f,-1.0f),
		        float2( 0.0f, 1.0f),
	        };	
        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
				float2 v2Texcoord : TEXCOORD0;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                
                output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
                output.v2Texcoord = input.texcoord;

                return output;
            }

            float4 MainPS(VS_OUTPUT input) : COLOR 
            {   
	            float fHeightL = tex2D(_HeightCurrentTex, input.v2Texcoord + g_offset[0]*_TextureSize.xy).r;
	            float fHeightR = tex2D(_HeightCurrentTex, input.v2Texcoord + g_offset[1]*_TextureSize.xy).r;
	            float fHeightT = tex2D(_HeightCurrentTex, input.v2Texcoord + g_offset[2]*_TextureSize.xy).r;
	            float fHeightB = tex2D(_HeightCurrentTex, input.v2Texcoord + g_offset[3]*_TextureSize.xy).r;
	
	            float3 normal = float3(fHeightB - fHeightT, fHeightR - fHeightL, _NormalScale);
	            return float4(normal.rgb, 1.0f);
            }
            ENDCG
        }
    }
}
