Shader "SabreCSG/TJunctionEliminator"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			sampler2D _CameraDepthTexture;
			float4 _CameraDepthTexture_TexelSize;

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
			
			sampler2D _MainTex;

			int isShimmeringPixel(float2 uv : TEXCOORD) : COLOR
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
				fixed4 col = tex2D(_MainTex, i.uv);

				if (isShimmeringPixel(i.uv))
				{
					col = sampleSurroundingPixels(i.uv);
					//col.r = 0.0f;
					//col.g = 1.0f;
					//col.b = 0.0f;
				}
				return col;
			}

			//float4 sampleArea(float2 uv : TEXCOORD) : COLOR
			//{
			//	float xmin = _CameraDepthTexture_TexelSize.x;
			//	float ymin = _CameraDepthTexture_TexelSize.y;
			//	float xoff = uv.x / 1.0f;
			//	float yoff = uv.y / 1.0f;
			//
			//	float4 s00 = tex2D(_CameraDepthTexture, float2(xoff - xmin, yoff - ymin));
			//	float4 s01 = tex2D(_CameraDepthTexture, float2(xoff - xmin, yoff       ));
			//	float4 s02 = tex2D(_CameraDepthTexture, float2(xoff - xmin, yoff + ymin));
			//	float4 s10 = tex2D(_CameraDepthTexture, float2(xoff       , yoff - ymin));
			//	float4 s11 = tex2D(_CameraDepthTexture, float2(xoff       , yoff       ));
			//	float4 s12 = tex2D(_CameraDepthTexture, float2(xoff       , yoff + ymin));
			//	float4 s20 = tex2D(_CameraDepthTexture, float2(xoff + xmin, yoff - ymin));
			//	float4 s21 = tex2D(_CameraDepthTexture, float2(xoff + xmin, yoff       ));
			//	float4 s22 = tex2D(_CameraDepthTexture, float2(xoff + xmin, yoff + ymin));
			//
			//	return float4(
			//		(s00.r + s01.r + s02.r + s10.r + s11.r + s12.r + s20.r + s21.r + s22.r) / 9.0f,
			//		(s00.g + s01.g + s02.g + s10.g + s11.g + s12.g + s20.g + s21.g + s22.g) / 9.0f,
			//		(s00.b + s01.b + s02.b + s10.b + s11.b + s12.b + s20.b + s21.b + s22.b) / 9.0f,
			//		1.0f
			//	);
			//}
			ENDCG
		}
	}
}
