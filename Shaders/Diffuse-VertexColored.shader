Shader "SabreCSG/Diffuse (vertex colored) " {
Properties {
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 200

Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityShaderVariables.cginc"

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

	// vertex shader
	v2f vert(appdata IN) 
	{
		v2f o;
		half4 color = IN.color;
		o.color = color;
		// transform position
		o.pos = mul(UNITY_MATRIX_MVP, float4(IN.pos,1));
		return o;
	}

	// fragment shader
	fixed4 frag(v2f IN) : SV_Target
	{
		fixed4 col;
		col = IN.color;
		return col;
	}
ENDCG
}
}
Fallback "VertexLit"
}
