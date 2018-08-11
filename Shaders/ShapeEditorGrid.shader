Shader "SabreCSG/ShapeEditorGrid"
{
	Properties
	{
		_OffsetX ("Offset X", Float) = 0.0
		_OffsetY ("Offset Y", Float) = 0.0
		_ScrollX ("Scroll X", Float) = 0.0
		_ScrollY ("Scroll Y", Float) = 0.0
        _Zoom ("Zoom", Float) = 16.0
		_PixelsPerPoint ("Pixels Per Point", Float) = 1.0
		_Background ("Background", 2D) = "white" { }
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
				};

				// vertex-to-fragment interpolators
				struct v2f
				{
					float4 pos : SV_POSITION;
				};

				// vertex shader
				v2f vert(appdata IN)
				{
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, float4(IN.pos,1));
					return o;
				}

				float _OffsetX;
				float _OffsetY;
				float _ScrollX;
				float _ScrollY;
				float _Zoom;
                float _PixelsPerPoint;
                float4 _Background_TexelSize;
                sampler2D _Background;
                float _Height;

				// fragment shader
				fixed4 frag(v2f IN) : SV_Target
				{
					// calculate grid offset and scrolling.
                    float4 pos = IN.pos;
#if UNITY_UV_STARTS_AT_TOP
                    pos.y -= _PixelsPerPoint * _OffsetY;
#else
                    pos.y = _Height - pos.y;
                    pos.y += _PixelsPerPoint * _OffsetY;
#endif
                    pos /= _PixelsPerPoint;

                    pos.x -= _OffsetX + _ScrollX;
                    pos.y -=  _ScrollY;

					// the 1x1 grid line light gray color.
					fixed4 col;
                    col = fixed4(0.922f, 0.922f, 0.922f, 1.0f);

					// calculate zoom.
					float s8 = (_Zoom * 8);
					float x1 = pos.x % s8;
					float y1 = pos.y % s8;
					float x2 = pos.x % _Zoom;
					float y2 = pos.y % _Zoom;

					// fix an issue with negative modulo causing y to be off by one pixel.
					if (y2 < 0.0f)
					{
						pos.y -= 1.0f;
						y1 = pos.y % s8;
						y2 = pos.y % _Zoom;
					}
					
					// draw the 8x8 darker grid lines.
					if (abs(x1) < 1.0f || abs(y1) < 1.0f)
						col = fixed4(0.843f, 0.843f, 0.843f, 1.0f);

					// discard 15x15 pixels causing the white boxes for the grid.
					if (abs(x2) >= 1.0f && abs(y2) >= 1.0f)
						col = fixed4(1.0f, 1.0f, 1.0f, 1.0f);

					// draw the center grid lines.
					if ((pos.x >= -2 && pos.x <= 1) || (pos.y >= -2 && pos.y <= 2))
						col = fixed4(0.882f, 0.882f, 0.882f, 1.0f);

					// draw the user background.
					fixed2 size = fixed2(_Background_TexelSize.z * (_Zoom / 16), _Background_TexelSize.w * (_Zoom / 16));
					fixed2 offset = fixed2(pos.x + size.x / 2.0f, pos.y + size.y / 2.0f);
					if (offset.x > 0 && offset.y > 0 && offset.x < size.x && offset.y < size.y)
						col = tex2D(_Background, fixed2(offset.x / size.x, offset.y / -size.y)).rgba;

					return col;
				}
			ENDCG
		}
	}
}