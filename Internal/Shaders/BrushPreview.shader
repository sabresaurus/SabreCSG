// Based on "Legacy Shaders/Transparent/Diffuse"
Shader "SabreCSG/BrushPreview"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Fog { Mode Off }
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert alpha:blend nofog

		fixed4 _Color;

		struct Input
		{
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = _Color;
	
			o.Albedo = c.rgb;
			o.Alpha = clamp((distance(IN.worldPos, _WorldSpaceCameraPos) * 0.2f) - 1.0f, 0.0f, c.a);
		}
		ENDCG
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}
