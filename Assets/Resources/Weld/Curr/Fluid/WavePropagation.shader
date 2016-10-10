Shader "Weld/Fluid/WavePropagation" 
{
    Properties 
	{
		_HeightPrevTex ("HeightPrevTex", 2D) = "black" {}
		_HeightCurrentTex ("HeightCurrentTex", 2D) = "black" {}
		_ShapeTex ("Water Shape Tex", 2D) = "white" {}
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
            
            uniform sampler2D _HeightPrevTex : register(s0);
            uniform sampler2D _HeightCurrentTex : register(s1);
            uniform sampler2D _ShapeTex : register(s2);
            
			uniform float3 _K;
            uniform float4 _TextureSize;
            uniform float  _Damping;
        
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

            float4 MainPS(VS_OUTPUT input) : COLOR 
            {   
	            float2 offset[4] =
	            {
					float2(-1.0f, 0.0f),
					float2( 1.0f, 0.0f),
					float2( 0.0f,-1.0f),
					float2( 0.0f, 1.0f),
	            };	
	            float shape = tex2D(_ShapeTex, input.v2Texcoord).r;
	            //前一桢的高度
	            float fHeightPrev = tex2D(_HeightPrevTex, input.v2Texcoord).r;
				//当前帧高度
				float fHeightCurr = tex2D(_HeightCurrentTex, input.v2Texcoord).r;
				
				float fHeightAdjoin = tex2D(_HeightCurrentTex, input.v2Texcoord + offset[0] * _TextureSize.xy).r;
				fHeightAdjoin += tex2D(_HeightCurrentTex, input.v2Texcoord + offset[1] * _TextureSize.xy).r;
				fHeightAdjoin += tex2D(_HeightCurrentTex, input.v2Texcoord + offset[2] * _TextureSize.xy).r;
				fHeightAdjoin += tex2D(_HeightCurrentTex, input.v2Texcoord + offset[3] * _TextureSize.xy).r;

				//下一帧高度
	            float fNextHeight = _K.x * fHeightCurr + _K.y * fHeightPrev + _K.z * fHeightAdjoin;
				fNextHeight *= _Damping;
			
	            return fNextHeight * shape;
            }
            ENDCG
        }
    }
}
