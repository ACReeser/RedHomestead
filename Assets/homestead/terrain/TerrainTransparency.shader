﻿Shader "Nature/Terrain/Transparent"
{
	Properties
	{
		_Control("Control (RGBA)", 2D) = "red" {}
		_Splat3("Layer 3 (A)", 2D) = "white" {}
		_Splat2("Layer 2 (B)", 2D) = "white" {}
		_Splat1("Layer 1 (G)", 2D) = "white" {}
		_Splat0("Layer 0 (R)", 2D) = "white" {}
		//Used in fallback on old cards & base map
		_MainTex("BaseMap (RGB)", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,1)
	}

		SubShader
	{
		Tags
	{
		"SplatCount" = "4"
		"Queue" = "Geometry-100"
		"RenderType" = "Opaque"
	}
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
#pragma surface surf Lambert
#pragma target 4.0
	struct Input
	{
		float2 uv_Control : TEXCOORD0;
		float2 uv_Splat0 : TEXCOORD1;
		float2 uv_Splat1 : TEXCOORD2;
		float2 uv_Splat2 : TEXCOORD3;
		float2 uv_Splat3 : TEXCOORD4;
	};

	sampler2D _Control;
	sampler2D _Splat0,_Splat1,_Splat2,_Splat3;

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 splat_control = tex2D(_Control, IN.uv_Control);
		fixed4 firstSplat = tex2D(_Splat0, IN.uv_Splat0);
		fixed3 col;
		col = splat_control.r * tex2D(_Splat0, IN.uv_Splat0).rgb;
		col += splat_control.g * tex2D(_Splat1, IN.uv_Splat1).rgb;
		col += splat_control.b * tex2D(_Splat2, IN.uv_Splat2).rgb;
		col += splat_control.a * tex2D(_Splat3, IN.uv_Splat3).rgb;
		o.Albedo = col;
		o.Alpha = 1;
		if (tex2D(_Splat0, IN.uv_Splat0).a == 0)
			o.Alpha = 1 - splat_control.r;
		else if (tex2D(_Splat1, IN.uv_Splat1).a == 0)
			o.Alpha = 1 - splat_control.g;
		else if (tex2D(_Splat2, IN.uv_Splat2).a == 0)
			o.Alpha = 1 - splat_control.b;
		else if (tex2D(_Splat3, IN.uv_Splat3).a == 0)
			o.Alpha = 1 - splat_control.a;
	}
	ENDCG
	}

		Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Lightmap-AddPass"
		Dependency "BaseMapShader" = "Diffuse"

		//Fallback to Diffuse
		Fallback "Diffuse"
}