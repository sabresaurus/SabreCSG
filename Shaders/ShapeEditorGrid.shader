Shader "SabreCSG/ShapeEditorGrid"
{
	Properties
	{
		_OffsetX ("Offset X", Float) = 0.0
		_OffsetY ("Offset Y", Float) = 0.0
		_ScrollX ("Scroll X", Float) = 0.0
		_ScrollY ("Scroll Y", Float) = 0.0
		_Zoom ("Zoom", Float) = 16.0
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
					o.pos = UnityObjectToClipPos(float4(IN.pos,1));
					return o;
				}

				float _OffsetX;
				float _OffsetY;
				float _ScrollX;
				float _ScrollY;
				float _Zoom;

				// fragment shader
				fixed4 frag(v2f IN) : SV_Target
				{
					// calculate grid offset and scrolling.
					float4 pos = IN.pos;
					pos.x -= _OffsetX + _ScrollX;
					pos.y -= _OffsetY + _ScrollY;

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
						discard;
						
					return col;
				}
			ENDCG
		}
	}
}