// Derived from an original shader by Robert Yang, used with permission
Shader "SabreCSG/SeeExcluded" {
  Properties {
		_MainTex("Side", 2D) = "white" {}
	}
	SubShader{
		Pass{
		ZWrite Off
//		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityShaderVariables.cginc"

		// uniforms
			sampler2D _MainTex;

		// vertex shader input data
	struct appdata {
		float3 pos : POSITION;
		float3 normal : NORMAL;
	};

	// vertex-to-fragment interpolators
	struct v2f {
		float4 pos : SV_POSITION;
		float3 worldPos : TEXCOORD0;
		float3 worldNormal : TEXCOORD1;
	};

	// vertex shader
	v2f vert(appdata IN) {
		v2f o;
		o.worldPos = IN.pos;
		o.worldNormal = IN.normal;
		// transform position
		o.pos = mul(UNITY_MATRIX_MVP, float4(IN.pos,1));
		return o;
	}

	// fragment shader
	fixed4 frag(v2f IN) : SV_Target{

	float3 result;


	float3 projNormal = saturate(pow(IN.worldNormal * 1.4, 4));

	float scale = 1;//0.5f;
	// SIDE X
	float3 x = tex2D(_MainTex, IN.worldPos.zy * scale) * abs(IN.worldNormal.x);

	// TOP / BOTTOM
	float3 y = tex2D(_MainTex, IN.worldPos.zx * scale) * abs(IN.worldNormal.y);

	// SIDE Z	
	float3 z = tex2D(_MainTex, IN.worldPos.xy * scale) * abs(IN.worldNormal.z);

	result = z;
	result = lerp(result, x, projNormal.x);
	result = lerp(result, y, projNormal.y);



	return fixed4(result, 0.5);








	}
		ENDCG
	}
	}
}

//
//
//
//Shader "SabreCSG/SeeExcluded" {
//
//	
//	SubShader {
//		Tags {
//			"Queue"="Geometry"
//			"IgnoreProjector"="False"
//			"RenderType"="Opaque"
//		}
//
//		Cull Back
//		ZWrite On
//		
//		CGPROGRAM
//		#pragma surface surf Lambert
//		#pragma exclude_renderers flash
//
//		sampler2D _MainTex;
//		float _Scale;
//		
//		struct Input {
//			float3 worldPos;
//			float3 worldNormal;
//		};
//			
//		void surf (Input IN, inout SurfaceOutput o) {
//			
//		} 
//		ENDCG
//	}
//	Fallback "Diffuse"
//}