Shader "Water/SmallScale/WaterSimulation" 
{
    Properties 
	{
		_HeightPrevTex ("HeightPrevTex", 2D) = "black" {}
		_HeightCurrentTex ("HeightCurrentTex", 2D) = "black" {}
		_WaterShapeTex ("Water Shape Tex", 2D) = "white" {}
		_TextureSize("Texture size", Vector) = (256,256,0,0)
		_Damping("Water Damping", Float) = 0.99
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
            #include "HeightCode.cginc"

            
            uniform sampler2D _HeightPrevTex : register(s0);
            uniform sampler2D _HeightCurrentTex : register(s1);
            uniform sampler2D _WaterShapeTex : register(s2);
            
            uniform half4 _TextureSize;
            uniform half  _Damping;
        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
				half2 v2Texcoord : TEXCOORD0;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                
                output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
                output.v2Texcoord = input.texcoord.xy;

                return output;
            }


            fixed4 MainPS(VS_OUTPUT input) : COLOR 
            {   
	            half3 offset[4] =
	            {
		           half3(-1.0f, 0.0f, 0.25f),
		           half3( 1.0f, 0.0f, 0.25f),
		           half3( 0.0f,-1.0f, 0.25f),
		           half3( 0.0f, 1.0f, 0.25f),
	            };	
	            half shape = tex2D(_WaterShapeTex, input.v2Texcoord);
	            half fHeightPrev = DecodeHeightmap(tex2D(_HeightPrevTex, input.v2Texcoord));
	
	            half fHeighCurrent = 0;
	            for ( int i=0; i<4; i++ )
	            {
	             	half2 texcoord = input.v2Texcoord + offset[i].xy * _TextureSize.xy;
	             	fHeighCurrent += (DecodeHeightmap(tex2D(_HeightCurrentTex, texcoord)) * offset[i].z);
	            }	
	
	            half fHeight = fHeighCurrent * 2.0f - fHeightPrev;
	            fHeight *= _Damping;
	            fixed4 color = EncodeHeightmap(fHeight);
			
	            return color * (shape > 0.1f ? 1.0f : 0.0f);
            }
            ENDCG
        }
    }
}
