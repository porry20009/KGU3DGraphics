Shader "Water/SmallScale/HeightToNormal" 
{
    Properties 
	{
		_HeightCurrentTex ("HeightCurrentTex", 2D) = "black" {}
		_TextureSize("Texture size", Vector) = (256,256,0,0)
	}
    SubShader 
    {
        // Draw ourselves after all opaque geometry
		Tags { "RenderType"="Opaque" }

        // Render the object with the texture generated above, and invert it's colors
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
            #include "HeightCode.cginc"

            
            uniform sampler2D _HeightCurrentTex : register(s0);
            uniform half4 _TextureSize;

        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
				half2 v2Texcoord : TEXCOORD0;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                
                output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
                output.v2Texcoord = input.texcoord;

                return output;
            }

            fixed4 MainPS(VS_OUTPUT input) : COLOR 
            {   
	            half2 offset[4] =
	            {
		           half2(-1.0f, 0.0f),
		           half2( 1.0f, 0.0f),
		           half2( 0.0f,-1.0f),
		           half2( 0.0f, 1.0f),
	            };	

	            half fHeightL = DecodeHeightmap(tex2D(_HeightCurrentTex, input.v2Texcoord + offset[0]*_TextureSize.xy));
	            half fHeightR = DecodeHeightmap(tex2D(_HeightCurrentTex, input.v2Texcoord + offset[1]*_TextureSize.xy));
	            half fHeightT = DecodeHeightmap(tex2D(_HeightCurrentTex, input.v2Texcoord + offset[2]*_TextureSize.xy));
	            half fHeightB = DecodeHeightmap(tex2D(_HeightCurrentTex, input.v2Texcoord + offset[3]*_TextureSize.xy));
	
	            half3 n = half3(fHeightB - fHeightT, fHeightR - fHeightL, 0.4);
	            half3 normal = (n + 1.0f) * 0.5f;
	
	            return fixed4(normal.rgb, 1.0f);
            }
            ENDCG
        }
    }
}
