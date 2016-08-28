Shader "BRDF/CookTorrance" 
{
    Properties 
	{
	    _ReflectTex ("_Reflect Texture (RGB)", Cube) = "" { TexGen CubeReflect }
		_LightPos("Light Pos", Vector) = (0,0,0,0)
		_MaterialDiffuse("Material Diffuse", Color) = (1,1,1,1)
		_MaterialSpecular("Material Specular", Color) = (1,1,1,1)
		_LightColor("Light Color", Color) = (1,1,1,1)
		_SkyColor("Sky Color", Color) = (1,1,1,1)
		_GroundColor("Ground Color", Color) = (1,1,1,1)

		_MaterialGlossiness("Glossiness ", Float) = 0.01

	}
	 CGINCLUDE
	 #include "UnityCG.cginc"
     uniform samplerCUBE _ReflectTex;


	 uniform float3 _LightPos;
	 uniform float3 _LightColor; 
	 uniform float3 _SkyColor;
     uniform float3 _GroundColor;
     uniform float3 _MaterialDiffuse;
     uniform float3 _MaterialSpecular;
     
	 uniform float _MaterialGlossiness;

        
     struct VS_OUTPUT
     {
			float4 Position : SV_POSITION;
			half2 v2Texcoord : TEXCOORD0;
			half3 N : TEXCOORD1;
			float3 v3WPos : TEXCOORD2;
     };
            
     VS_OUTPUT MainVS(appdata_base input)
     {
           VS_OUTPUT output = (VS_OUTPUT)0;
                
           output.Position = mul(UNITY_MATRIX_MVP, input.vertex);
		   
		   float4 P = mul(_Object2World,input.vertex );
		   float3 N = UnityObjectToWorldNormal(input.normal );
	       output.Position = mul( UNITY_MATRIX_VP,P );
           output.v2Texcoord = input.texcoord.xy;
		   output.N = N;
	       output.v3WPos = P.xyz;
           return output;
      }

	  
	  half cos2sin( half x ) { return sqrt(1-x*x); }

	  half cos2tan2( half x ) { return (1-x*x)/(x*x); }

	  half3 FresnelFull( half3 R, half c )
	  {
		  // convert reflectance R into (real) refractive index n
		  half3 n = (1+sqrt(R))/(1-sqrt(R));
		  // then use Fresnel eqns to get angular variance
		  half3 FS = (c-n*sqrt(1-pow(cos2sin(c)/n,2)))/(c+n*sqrt(1-pow(cos2sin(c)/n,2)));
		  half3 FP = (sqrt(1-pow(cos2sin(c)/n,2))-n*c)/(sqrt(1-pow(cos2sin(c)/n,2))+n*c);
		  return (FS*FS+FP*FP)/2;
	  }

	  half3 FresnelOptimized( half3 R, half c )
      {
          half3 F = lerp( R, saturate( 60 * R ), pow(1-c,4) ); 
          return F;
      }

	  half Fresnel(half NdotL,half frenelBias,half fresnelPow)
      {
          half facing = (1.0 - NdotL);
          return max(frenelBias + (1.0 - frenelBias) * pow(facing,fresnelPow), 0.0);
     }

	  half BeckmannDistribution( half dotNH, half SpecularExponent )
	  {
		   half invm2 = SpecularExponent / 2;
		   half D = exp( -cos2tan2( dotNH ) * invm2 ) / pow( dotNH, 4 ) * invm2;
	       return D;
	  }

	  half BlinnPhongDistribution( half dotNH, half SpecularExponent )
	  {
		   half D = pow( dotNH, SpecularExponent ) * ( SpecularExponent + 1 ) / 2;
		   return D;
	  }

      float4 MainPS(VS_OUTPUT input) : COLOR 
	  {   
		  // input vectors
		  half3 toLight =  _LightPos - input.v3WPos.xyz;
		  half3 N = normalize( input.N );
		  half3 V = normalize( _WorldSpaceCameraPos.xyz -input.v3WPos.xyz );
		  half3 L = normalize( toLight );
		  half3 H = normalize( L + V );
        
          // dot products    
	      half dotNL = saturate( dot( N, L ) );
	      half dotNH = saturate( dot( N, H ) );
          half dotNV = saturate( dot( N, V ) );
	      half dotLH = saturate( dot( L, H ) );

		   // inverse gamma conversion
          _SkyColor *= _SkyColor;
          _GroundColor *= _GroundColor;
          _MaterialSpecular *= _MaterialSpecular;
          _MaterialDiffuse *= _MaterialDiffuse;
	      _LightColor *= _LightColor;

		   // glossyness to distribution parameter
           half SpecularExponent = exp2( _MaterialGlossiness * 12 );    
 
           // distribution term
           half D = BeckmannDistribution( dotNH, SpecularExponent );
    
           // fresnel term
           half3 F = FresnelFull( _MaterialSpecular, dotLH );
          // half3 F = Fresnel(dotNL,0.0, _MaterialSpecular );

		   

           // geometric term
           half G = saturate( 2 * dotNH * min( dotNV, dotNL ) / dotLH );

           // specular reflectance distribution from values above
           half3 SpecBRDF = D * F * G / ( 4 * dotNV * dotNL );
    
	       // components
           half atten = 16 / dot( toLight, toLight );
           half3 LightIntens = atten * _LightColor;    
           half3 AmbIntens = ( _SkyColor + _GroundColor ) / 2 * ( _MaterialDiffuse + _MaterialSpecular ) / 2;
           half3 SpecIntens = SpecBRDF * dotNL * LightIntens;
           half3 DiffIntens = _MaterialDiffuse * dotNL * LightIntens;

           half3 v3ReflectDir = normalize(reflect(-V, N));
           fixed3 v3ReflectColor = texCUBE(_ReflectTex,v3ReflectDir).xyz;
     
	       // color sum
           half3 result = 0;    
           result += AmbIntens;
           result += SpecIntens;
           result += DiffIntens;

           // tone mapping
           result = 1 - exp( -result );
    
           // gamma conversion    
           result = sqrt( result );
          
		 // return D;
  	      return half4( F, 1 );
      }
      ENDCG
	  SubShader 
	  {
		   Tags { "RenderType"="Opaque" }
		   LOD 200
		   Pass
		   {
				Name "CookTorrance"
				CGPROGRAM

				#pragma vertex MainVS
				#pragma fragment MainPS
				#pragma target 3.0
           
				ENDCG
		   }
	  }
}
