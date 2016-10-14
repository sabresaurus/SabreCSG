// Based on "Legacy Shaders/Transparent/Diffuse"
Shader "SabreCSG/BrushPreview"
{
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
}

SubShader {
	Fog { Mode Off }
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 200

CGPROGRAM
#pragma surface surf Lambert alpha:blend nofog

fixed4 _Color;

struct Input {
	fixed4 color; // Can't declare empty structs in CG, so just put a filler variable to get it compiling
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = _Color;
	
	o.Albedo = c.rgb;
	o.Alpha = c.a;
}
ENDCG
}

Fallback "Legacy Shaders/Transparent/VertexLit"
}
