Shader "SimpleShadow/ShadowPassOne"
 {
	SubShader
	{
		Tags {"RenderType"="Opaque"}
		LOD 200
	    Pass 
        {
        	ZWrite Off
        	cull front
            
            CGPROGRAM
            #pragma vertex MainVS
            #pragma fragment  MainPS
            #pragma target 2.0
           
            #include "UnityCG.cginc"
            
            uniform float4x4 g_matLightViewProj;
            uniform half _ShadowDensity;
           
             struct VS_OUTPUT
            {
                float4 Position : SV_POSITION;
            };
            
            VS_OUTPUT MainVS(appdata_base input)
            {
               VS_OUTPUT output = (VS_OUTPUT)0;
               
			   float4x4 matLightWVP= mul (g_matLightViewProj,_Object2World);
               output.Position = mul(matLightWVP,input.vertex);
               return output;
            }

            fixed4 MainPS(VS_OUTPUT input) : COLOR 
            {
                return _ShadowDensity;
            }
            ENDCG
        }
   }
}
