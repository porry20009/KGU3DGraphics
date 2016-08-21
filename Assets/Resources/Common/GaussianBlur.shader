Shader "PostEffect/GaussianBlur"
 {
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BlurTexSizeInverse ("BlurTexSize Inverse", Vector) = (0,0,0,0)
	}
    CGINCLUDE
    #include "UnityCG.cginc"
            
    uniform sampler2D _MainTex;
    uniform half4 _BlurTexSizeInverse;
        
    struct VS_OUTPUT
    {
         float4 Position : SV_POSITION;
		 half2 vTexcoord : TEXCOORD0;
    };
            
    VS_OUTPUT MainVS(appdata_base input)
    {
          VS_OUTPUT output = (VS_OUTPUT)0;
                
          output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
          output.vTexcoord = input.texcoord;

          return output;
     }

     fixed4 HorizontalBlur3x3PS(VS_OUTPUT input) : COLOR 
     {
           half2 ScreenParams = _BlurTexSizeInverse.xy;
           half2 vBlurOffset[3] = 
           {
                float2(-1,0.27407),
                float2( 0,0.45186),
                float2( 1,0.27407)
           };
                
           fixed4 vColor = 0;
           vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[0].x * ScreenParams.x,0)) * vBlurOffset[0].y);
           vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[1].x * ScreenParams.x,0)) * vBlurOffset[1].y);
           vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[2].x * ScreenParams.x,0)) * vBlurOffset[2].y);
                
		   return vColor;
     }
     
     fixed4 VerticalBlur3x3PS(VS_OUTPUT input) : COLOR 
     {
           half2 ScreenParams = _BlurTexSizeInverse.xy;
           half2 vBlurOffset[3] = 
           {
               half2(-1,0.27407),
               half2( 0,0.45186),
               half2( 1,0.27407)
           };
             
           fixed4 vColor = 0;
               
           vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[0].x * ScreenParams.y)) * vBlurOffset[0].y);
           vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[1].x * ScreenParams.y)) * vBlurOffset[1].y);
           vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[2].x * ScreenParams.y)) * vBlurOffset[2].y);
              
           return vColor;
     }

	 fixed4 HorizontalBlur5x5PS(VS_OUTPUT input) : COLOR
	 {
		 half2 ScreenParams = _BlurTexSizeInverse.xy;
		 half2 vBlurOffset[5] =
		 {
			 float2(-2,0.2),
			 float2(-1,0.2),
			 float2(0,0.2),
			 float2(1,0.2),
			 float2(2,0.2)
		 };

		 fixed4 vColor = 0;
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[0].x * ScreenParams.x,0)) * vBlurOffset[0].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[1].x * ScreenParams.x,0)) * vBlurOffset[1].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[2].x * ScreenParams.x,0)) * vBlurOffset[2].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[3].x * ScreenParams.x, 0)) * vBlurOffset[3].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(vBlurOffset[4].x * ScreenParams.x, 0)) * vBlurOffset[4].y);

		 return vColor;
	 }
	 fixed4 VerticalBlur5x5PS(VS_OUTPUT input) : COLOR
	 {
		 half2 ScreenParams = _BlurTexSizeInverse.xy;
		 half2 vBlurOffset[5] =
		 {
			 float2(-2,0.2),
			 float2(-1,0.2),
			 float2(0,0.2),
			 float2(1,0.2),
			 float2(2,0.2)
		 };

		 fixed4 vColor = 0;

		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[0].x * ScreenParams.y)) * vBlurOffset[0].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[1].x * ScreenParams.y)) * vBlurOffset[1].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[2].x * ScreenParams.y)) * vBlurOffset[2].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[3].x * ScreenParams.y)) * vBlurOffset[3].y);
		 vColor += (tex2D(_MainTex, input.vTexcoord + half2(0,vBlurOffset[4].x * ScreenParams.y)) * vBlurOffset[4].y);

		 return vColor;
	 }
    ENDCG
    SubShader 
	{
        Tags{ "RenderType" = "Opaque" "Queue"="Geometry" }

		Pass
		{
        	Name "HorizontalBlur3x3"
        	Lighting Off
			ZWrite Off
			AlphaTest Off
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment HorizontalBlur3x3PS
            #pragma target 2.0
           
            ENDCG
		}

		Pass 
        {
        	Name "VerticalBlur3x3"
        	Lighting Off
			ZWrite Off
			AlphaTest Off
            CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment VerticalBlur3x3PS
            #pragma target 2.0
           
            ENDCG
        }
		Pass
		{
			Name "HorizontalBlur5x5"
			Lighting Off
			ZWrite Off
			AlphaTest Off
			CGPROGRAM

           #pragma vertex MainVS
           #pragma fragment HorizontalBlur5x5PS
           #pragma target 2.0

			ENDCG
		}

		Pass
		{
			Name "VerticalBlur5x5"
			Lighting Off
			ZWrite Off
			AlphaTest Off
			CGPROGRAM

            #pragma vertex MainVS
            #pragma fragment VerticalBlur5x5PS
            #pragma target 2.0

			ENDCG
		}
    }
}
