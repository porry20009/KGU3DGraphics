Shader "SimpleShadow/ShadowPassTwo" 
{
    Properties 
    {
		_ShadowTex ("Base (RGB)", 2D) = "black" {}
	}
    SubShader 
    {
        Tags {"RenderType"="Opaque"}
        Pass 
        {
        	ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"
            
            uniform sampler2D _ShadowTex;
            uniform float4x4 g_matLightViewProj;
            uniform float4   g_v4LightDir;
        
            struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
                half4 UVShadow : TEXCOORD0;
                //half  BlankingRatio : TEXCOORD1;
            };

            VS_OUTPUT MainVS(appdata_base input)
            {
                VS_OUTPUT output = (VS_OUTPUT)0;
                output.Position = mul (UNITY_MATRIX_MVP, input.vertex);
			    float4x4 matLightWVP = mul (g_matLightViewProj,_Object2World);
			    output.UVShadow = mul (matLightWVP, input.vertex);
			    //half BlankingRatio = dot(g_v4LightDir.xyz,mul((float3x3)_Object2World,input.normal));
			    //output.BlankingRatio = BlankingRatio > 0 ? 1 : 0;
                return output;
            }

            float4 MainPS(VS_OUTPUT input) : COLOR 
            {
                half2 uv = 0.5 * input.UVShadow.xy/input.UVShadow.w + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1 - uv.y;
                #endif

                half alpha = tex2D(_ShadowTex, uv).r;

                if (uv.x < 0 || uv.x > 1.0f ||
                    uv.y < 0 || uv.y > 1.0f)
                {
                    alpha = 0.0;
                }
                
                return half4(0,0,0, alpha);
            }

            ENDCG
        }
    }
}
