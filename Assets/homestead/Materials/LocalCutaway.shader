// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TransparentLocalSectionPlane"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_maxVertexY("Max vertex height", float) = 99
		_showPercentY("percentage of height to show", float) = 1
		_cutawayColor("Cutaway Color", Color) = (1,1,1,0)
		_tint("Tint", Color) = (1,1,1,0)
	}

		SubShader
	{
		Tags{ "Queue"="Transparent" "RenderType" = "Fade" }
		Cull Off

		CGPROGRAM
#pragma surface surf Lambert alpha:fade

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
	float _showPercentY;
	fixed4 _cutawayColor;
	fixed4 _tint;

	void vert(float4 vertex: POSITION, out Input o) {
		o.objY = UnityObjectToClipPos(vertex).y;
		UNITY_INITIALIZE_OUTPUT(Input, o);
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		//clip((IN.objY / _maxVertexY) - (_showPercentY / 100));
		clip(IN.objY - _showPercentY / 100);
		float fd = dot(IN.viewDir, IN.worldNormal);

		o.Alpha = _tint.a;

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