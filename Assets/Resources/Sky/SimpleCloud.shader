Shader "Sky/SimpleCloud" 
{
    SubShader
    {
        Tags
        {
			"Queue" = "Background+2"
            "IgnoreProjector"="True"
        }
		Fog{ Mode Off }

        Pass
        {
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"

            uniform half _CloudDensity;
            uniform half2 _CloudScale1;
            uniform half2 _CloudScale2;
            uniform half4 _CloudUV;

            uniform sampler2D _NoiseTexture1;
            uniform sampler2D _NoiseTexture2;

            struct v2f
			{
                float4 position : POSITION;
                half4 cloudUV  : TEXCOORD0;
                half  params   : TEXCOORD1;
            };

            v2f vert(appdata_base v) 
			{
                v2f o;
                // Vertex position and uv coordinates
                float3 vertnorm = normalize(v.vertex.xyz);
                half2 vertuv   = vertnorm.xz / pow(vertnorm.y + 0.1, 0.75);
                half  vertfade = saturate(100 * vertnorm.y * vertnorm.y);
                // Write results
                o.position   = mul(UNITY_MATRIX_MVP, v.vertex);
                o.cloudUV.xy = (vertuv + _CloudUV.xy) / _CloudScale1;
                o.cloudUV.zw = (vertuv + _CloudUV.zw) / _CloudScale2;
                o.params   = _CloudDensity * vertfade;
                return o;
            }
            
            fixed4 frag(v2f i) : COLOR
             {
				fixed4 color = fixed4(1,1,1,1);
                fixed noise1 = tex2D(_NoiseTexture1, i.cloudUV.xy).r;
                fixed noise2 = tex2D(_NoiseTexture2, i.cloudUV.zw).r;
				fixed sharp = noise1 * noise2 * i.params;
				fixed d = 0.05 * sharp * sharp;
				color.rgb -= d;
				color.a = saturate(sharp);
                return color;
            }

            ENDCG
        }
    }
}
