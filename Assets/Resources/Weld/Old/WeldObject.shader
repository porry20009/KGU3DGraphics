Shader "Weld/WeldObject" 
{
      Properties 
	  {
		   _WeldHeightmap ("Weld Heightmap", 2D) = "black" {}
		   _WeldNormalmap ("Weld Normalmap", 2D) = "black" {}

	   }
	   CGINCLUDE
	   #include "UnityCG.cginc"

	   uniform sampler2D _WeldHeightmap;
	   uniform sampler2D _WeldNormalmap;

        
       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    float2 v2Texcoord : TEXCOORD0;
			float4 v4WPos : TEXCOORD1;
       };
            
       VS_OUTPUT MainVS(appdata_base input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
		   float height = tex2Dlod(_WeldHeightmap, float4(input.texcoord.xy, 0, 0)) * 0.2;
		   float3 newPos = input.vertex.xyz + float3(0,height,0);

           output.Position = mul(UNITY_MATRIX_MVP, float4(newPos,1.0));
           output.v2Texcoord = input.texcoord.xy;
           output.v4WPos = mul(_Object2World, float4(newPos,1.0));

           return output;
        }
		
		float4 MainPS(VS_OUTPUT input) : COLOR
	 	{
		    float4 weldHeightmap = tex2D(_WeldHeightmap,input.v2Texcoord);
			float3 weldNormalmap = normalize(2 * tex2D(_WeldNormalmap,input.v2Texcoord).rgb - 1);

			float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.v4WPos.xyz);
			float diffuseFactor = max(0,dot(weldNormalmap,viewDir));

			float3 H = normalize(viewDir + viewDir);
			float specularFactor = pow(saturate(dot(H, weldNormalmap.rgb)), 70.0);

		    return float4(diffuseFactor.xxx + specularFactor.xxx,weldHeightmap.r > 0.01 ? 1 : weldHeightmap.r);
		}
         ENDCG
	     SubShader 
	     {
               Tags { "Queue" = "Transparent"}

		        LOD 200
		        Pass
		        {
			          Name "WeldObject"
		              Blend SrcAlpha OneMinusSrcAlpha

			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 2.0
           
                      ENDCG
		        }
	    }
}
