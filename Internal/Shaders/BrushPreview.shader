// Based on "Legacy Shaders/Transparent/Diffuse"
Shader "SabreCSG/BrushPreview"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_GridSize("Grid Size", float) = 1.0
		_GridStrength("Grid Strength", Range(0,1)) = 0.25
		_GridThickness("Grid Thickness", Range(0.01,0.1)) = 0.05
		[HideInInspector] _GridToggle("Grid Toggle", float) = 1.0
		[HideInInspector] _FaceToggle("Face Toggle", float) = 1.0
	}

	SubShader
	{
		Fog { Mode Off }
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200

		CGPROGRAM
			#pragma surface surf Lambert alpha:blend nofog

			fixed4 _Color;
			float _GridStrength;
			float _GridSize;
			float _GridThickness;
			half _GridToggle;
			half _FaceToggle;

			struct Input
			{
				float3 worldPos;
				float3 worldNormal;
			};

			float mod(float val, float mod) {

				while (val < 0) {
					val += mod*100;
				}

				return fmod(val, mod);
			}

			void surf (Input IN, inout SurfaceOutput o)
			{
				fixed4 c = _Color;
				c.a *= _FaceToggle;

				float3 worldNormal = abs(IN.worldNormal);
			
				worldNormal.x = (worldNormal.x > worldNormal.y && worldNormal.x > worldNormal.z)?1:0;
				worldNormal.y = (worldNormal.y > worldNormal.x && worldNormal.y > worldNormal.z)?1:0;
				worldNormal.z = (worldNormal.z > worldNormal.y && worldNormal.z > worldNormal.x)?1:0;

				float3 worldspace = IN.worldPos;
				worldspace -= (_GridThickness * _GridSize) * 0.5;

				float2 grid = float2(
					(worldspace.z * worldNormal.x) + (worldspace.x * worldNormal.z) + (worldspace.x * worldNormal.y),
					(worldspace.y * worldNormal.x) + (worldspace.y * worldNormal.z) + (worldspace.z * worldNormal.y)
				);

				_GridSize = max(0.01, _GridSize);

				grid.x = step((1.0 - _GridThickness)*_GridSize, mod(grid.x, _GridSize));
				grid.y = step((1.0 - _GridThickness)*_GridSize, mod(grid.y, _GridSize));

				float g = saturate(grid.x + grid.y);
                
				o.Emission = c.rgb + (g * _GridStrength * _GridToggle);
				o.Alpha = c.a + (g * _GridStrength * _GridToggle);
			}
		ENDCG
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}
