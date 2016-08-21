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
	            //前一桢的弹簧长度
	            half fHeightPrev = DecodeHeightmap(tex2D(_HeightPrevTex, input.v2Texcoord));
	
	            //当前帧弹簧的长度，它取的是邻近像素的平均值，而不是单一像素的值
	            half fNeighCurrent = 0;
	            for ( int i=0; i<4; i++ )
	            {
	             	half2 texcoord = input.v2Texcoord + offset[i].xy * _TextureSize.xy;
	             	fNeighCurrent += (DecodeHeightmap(tex2D(_HeightCurrentTex, texcoord)) * offset[i].z);
	            }	
	
	            // 预测下一帧弹簧的长度
	            half fHeight = fNeighCurrent * 2.0f - fHeightPrev;
	            // 减弱弹簧的长度，让它慢慢停止
	            fHeight *= _Damping;
	            fixed4 color = EncodeHeightmap(fHeight);
			
	            return color * (shape > 0.1f ? 1.0f : 0.0f);
            }
            ENDCG
        }
    }
}
