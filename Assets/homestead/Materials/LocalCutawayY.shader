Shader "Custom/TransparentLocalSectionPlaneY"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_maxVertexY("Max vertex height", float) = 99
		_minVertexY("Min vertex height", float) = 0
		_showPercentY("percentage of height to show", float) = 1
		_cutawayColor("Cutaway Color", Color) = (1,1,1,0)
		_tint("Tint", Color) = (1,1,1,0)
	}

		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Cull Off

		CGPROGRAM
#pragma surface surf Lambert vertex:vert

	struct Input
	{
		float2 uv_MainTex;
		float objY;
		float3 viewDir;
		float3 worldNormal;
	};

	sampler2D _MainTex;
	sampler2D _BumpMap;
	float _maxVertexY;
	float _minVertexY;
	float _showPercentY;
	fixed4 _cutawayColor;
	fixed4 _tint;

	void vert(inout appdata_full v: POSITION, out Input o) {
		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.objY = v.vertex.y;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		clip((_showPercentY / 100) - (IN.objY / _maxVertexY) + (IN.objY / _minVertexY));

		//o.Alpha = _tint.a;

		float fd = dot(IN.viewDir, IN.worldNormal);
		if (fd.x > 0)
		{
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _tint.rgb;
			return;
		}

		o.Emission = _cutawayColor;
	}
	ENDCG
	}
		Fallback "Diffuse"
}