Shader "SabreCSG/Handle" {
Properties{
		_MainTex("Base (RGB)", 2D) = "white" { }
	}
	SubShader{
		Pass{
		ZTest Always
		ZWrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityShaderVariables.cginc"

		// uniforms
		float4 _MainTex_ST;

		// vertex shader input data
	struct appdata {
		float3 pos : POSITION;
		half4 color : COLOR;
		float3 uv0 : TEXCOORD0;
	};

	// vertex-to-fragment interpolators
	struct v2f {
		fixed4 color : COLOR0;
		float2 uv0 : TEXCOORD0;
		float4 pos : SV_POSITION;
	};

	// vertex shader
	v2f vert(appdata IN) {
		v2f o;
		half4 color = IN.color;
		half3 viewDir = 0.0;
		o.color = saturate(color);
		// compute texture coordinates
		o.uv0 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
		// transform position
		o.pos = mul(UNITY_MATRIX_MVP, float4(IN.pos,1));
		return o;
	}

	// textures
	sampler2D _MainTex;


	fixed4 frag(v2f IN) : SV_Target
	{
		fixed4 col;
		fixed4 tex, tmp0, tmp1, tmp2;
		// SetTexture #0
		tex = tex2D(_MainTex, IN.uv0.xy);
		col = IN.color * tex;
//		col.a = 1;
		return col;
	}
		ENDCG
	}
	}
}