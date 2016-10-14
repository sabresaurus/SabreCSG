Shader "SabreCSG/Line Dashed" {
	SubShader{
		Pass{
		ZWrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityShaderVariables.cginc"

		// uniforms

		// vertex shader input data
	struct appdata {
		float3 pos : POSITION;
		float2 uv : TEXCOORD0;
		half4 color : COLOR;
	};

	// vertex-to-fragment interpolators
	struct v2f {
		fixed4 color : COLOR0;
		float2 uv : TEXCOORD0;
		float4 pos : SV_POSITION;
	};

	// vertex shader
	v2f vert(appdata IN) {
		v2f o;
		half4 color = IN.color;
		half3 viewDir = 0.0;
		o.color = saturate(color);
		// compute texture coordinates
		// transform position
		o.pos = mul(UNITY_MATRIX_MVP, float4(IN.pos,1));
		o.uv = IN.uv;
		return o;
	}

	// fragment shader
	fixed4 frag(v2f IN) : SV_Target
	{
		fixed4 col;
		col = IN.color;
		// This should flip between 0 and 1 values 4 times a UV unit
		float interpolant = floor(2 * frac((IN.uv.x) * 2));

		// Set the target color as the supplied color or black based on the interpolant
		col = lerp(col, fixed4(0,0,0,1), interpolant);

		return col;
	}
		ENDCG
	}
	}
}