Shader "Sky/SimpleSun" 
{
	Properties 
	{
	
	}
    SubShader 
    {
        Tags
        {
			"Queue" = "Background+1"
            "IgnoreProjector"="True"
        }
        Pass 
        {
			Fog{ Mode Off }
        	ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"
            
            uniform fixed4 _SunColor;
            
            struct VS_OUTPUT
            {
                float4 v4Position : SV_POSITION;
                half2 v2Texcoord : TEXCOORD0;
            };

            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                output.v4Position = mul (UNITY_MATRIX_MVP, input.vertex);
				output.v2Texcoord = input.texcoord;
               
                return output;
            }

            fixed4 MainPS(VS_OUTPUT input) : COLOR 
            {
               fixed4 color = 0;
               half dist = distance(input.v2Texcoord,float2(0.5,0.5));
               if (dist > 0.5)
                  discard;
               if (dist < 0.05)
               {
                  color.a = 1.0;
               }
               else
               {
                  half t = (dist - 0.05)/0.45;
				  color.a = 1 - sqrt(1 - (t - 1 ) * (t - 1 ));
               }
			   color.rgb = _SunColor.rgb;
               return color;
            }
            ENDCG
        }
    }
}
