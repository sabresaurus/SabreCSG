Shader "SabreCSG/ShapeEditorLine"
{
	Properties
	{
		_CutoffY ("Cutoff Y", Float) = 0.0
	}

	SubShader
	{
		Pass
		{
			ZWrite Off
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "UnityShaderVariables.cginc"

				// uniforms

				// vertex shader input data
				struct appdata
				{
					float3 pos : POSITION;
					half4 color : COLOR;
				};

				// vertex-to-fragment interpolators
				struct v2f
				{
					fixed4 color : COLOR0;
					float4 pos : SV_POSITION;
				};

				float _CutoffY;
                float _Height;

				// vertex shader
				v2f vert(appdata IN)
				{
					v2f o;
					half4 color = IN.color;
					half3 viewDir = 0.0;
					o.color = saturate(color);
					// compute texture coordinates
					// transform position
					o.pos = mul(UNITY_MATRIX_MVP, float4(IN.pos,1));
					return o;
				}

				// fragment shader
				fixed4 frag(v2f IN) : SV_Target
				{
					fixed4 col;
					col = IN.color;
#if UNITY_UV_STARTS_AT_TOP
                    if (IN.pos.y < _CutoffY)
                        discard;
#else
                    if (IN.pos.y > _Height)
                        discard;
#endif

					return col;
				}
			ENDCG
		}
	}
}