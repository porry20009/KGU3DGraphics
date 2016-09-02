Shader "Water/SmallScale/OutlineToHeight" 
{
    Properties 
	{
		_OutlineCurrTex ("Outline Curr Tex", 2D) = "black" {}
		_OutlinePreTex ("Outline Pre Tex", 2D) = "black" {}
	}
    SubShader 
    {
		Tags { "RenderType"="Opaque" }

        Pass 
        {
			Lighting Off
			ZWrite Off
			AlphaTest Off
			Blend One One

            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"
            #include "HeightCode.cginc"

            
            uniform sampler2D _OutlineCurrTex : register(s0);
            uniform sampler2D _OutlinePreTex : register(s1);
        
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
	            fixed4 OutlineCurrColor = tex2D(_OutlineCurrTex, input.v2Texcoord);
	            fixed4 OutlinePreColor = tex2D(_OutlinePreTex, input.v2Texcoord);
				fixed force = OutlineCurrColor.a;
				fixed3 vec = OutlineCurrColor.rgb - OutlinePreColor.rgb;
				fixed sdist = length(vec);

				return EncodeHeightmap(force * sdist);
            }
            ENDCG
        }
    }
}
