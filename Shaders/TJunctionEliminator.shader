Shader "SabreCSG/TJunctionEliminator"
{
	Properties
	{
		_MSAA ("MSAA", Int) = 0
		_ScreenDetectionAggressiveness ("ScreenDetectionAggressiveness", float) = 0.03
		_DebugMode ("DebugMode", Int) = 0
		_MainTex ("Texture", 2D) = "white" { }
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;
			float4 _CameraDepthTexture_TexelSize;
			int _MSAA;
			float _ScreenDetectionAggressiveness;
			int _DebugMode;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float intensity(float3 color)
			{
				return (color.r + color.g + color.b) / 3.0f;
			}

			int isShimmeringDepthPixel(float2 uv : TEXCOORD) : COLOR
			{
				float xmin = _CameraDepthTexture_TexelSize.x;
				float ymin = _CameraDepthTexture_TexelSize.y;
				float xoff = uv.x / 1.0f;
				float yoff = uv.y / 1.0f;
	
				float4 s01 = tex2D(_CameraDepthTexture, float2(xoff - xmin, yoff       ));
				float4 s10 = tex2D(_CameraDepthTexture, float2(xoff       , yoff - ymin));
				float4 s11 = tex2D(_CameraDepthTexture, float2(xoff       , yoff       ));
				float4 s12 = tex2D(_CameraDepthTexture, float2(xoff       , yoff + ymin));
				float4 s21 = tex2D(_CameraDepthTexture, float2(xoff + xmin, yoff       ));

				float surmin = min(s01.r, min(s10.r, min(s12.r, s21.r)));
				float surmax = max(s01.r, max(s10.r, max(s12.r, s21.r)));

				if (s11.r <= surmax + 0.00001f && s11.r >= surmin - 0.00001f)
					return 0;
				return 1;
			}

			int isShimmeringScreenPixel(float2 uv : TEXCOORD) : COLOR
			{
				float xmin = _CameraDepthTexture_TexelSize.x;
				float ymin = _CameraDepthTexture_TexelSize.y;
				float xoff = uv.x / 1.0f;
				float yoff = uv.y / 1.0f;
	
				float4 s01 = tex2D(_MainTex, float2(xoff - xmin, yoff       ));
				float4 s10 = tex2D(_MainTex, float2(xoff       , yoff - ymin));
				float4 s11 = tex2D(_MainTex, float2(xoff       , yoff       ));
				float4 s12 = tex2D(_MainTex, float2(xoff       , yoff + ymin));
				float4 s21 = tex2D(_MainTex, float2(xoff + xmin, yoff       ));

				float surmin = min(intensity(s01.rgb), min(intensity(s10.rgb), min(intensity(s12.rgb), intensity(s21.rgb))));
				float surmax = max(intensity(s01.rgb), max(intensity(s10.rgb), max(intensity(s12.rgb), intensity(s21.rgb))));

				if (intensity(s11.rgb) < surmax + _ScreenDetectionAggressiveness && intensity(s11.rgb) > surmin - _ScreenDetectionAggressiveness /*&& intensity(s11.rgb) < 0.999f*/)
					return 0;
				return 1;
			}

			float4 sampleSurroundingPixels(float2 uv : TEXCOORD) : COLOR
			{
				float xmin = _CameraDepthTexture_TexelSize.x;
				float ymin = _CameraDepthTexture_TexelSize.y;
				float xoff = uv.x / 1.0f;
				float yoff = uv.y / 1.0f;
	
				float4 s01 = tex2D(_MainTex, float2(xoff - xmin, yoff       ));
				float4 s10 = tex2D(_MainTex, float2(xoff       , yoff - ymin));
				float4 s12 = tex2D(_MainTex, float2(xoff       , yoff + ymin));
				float4 s21 = tex2D(_MainTex, float2(xoff + xmin, yoff       ));
	
				return float4(
					(s01.r + s10.r + s12.r + s21.r) / 4.0f,
					(s01.g + s10.g + s12.g + s21.g) / 4.0f,
					(s01.b + s10.b + s12.b + s21.b) / 4.0f,
					1.0f
				);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(0.0, 0.0, 0.0, 1.0);

				// We can check the depth buffer for pixel holes and blur them.
				if (_MSAA == 0)
				{
					if (isShimmeringDepthPixel(i.uv))
					{
						if (_DebugMode == 0)
							col = sampleSurroundingPixels(i.uv);
						else
							col.g = 1.0f;
					} else col = tex2D(_MainTex, i.uv);
				}

				// MSAA causes white outlines that do not exist in the depth buffer.
				// We detect awkward screen pixels that stand out and blur them instead.
				else
				{
					if (isShimmeringScreenPixel(i.uv))
					{
						if (_DebugMode == 0)
							col = sampleSurroundingPixels(i.uv);
						else
							col.g = 1.0f;
					} else col = tex2D(_MainTex, i.uv);
				}

				return col;
			}

			ENDCG
		}
	}
}
