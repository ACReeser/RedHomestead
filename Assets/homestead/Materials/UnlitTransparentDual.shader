Shader "Unlit/UnlitTransparentColorDual"
{
	Properties{
		_Color("Color Tint", Color) = (1,1,1,1)
		_OffColor("Off Color Tint", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_maxVertexY("Max vertex height", float) = 99
		_showPercentY("percentage of height to show", float) = 1
	}

	SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Fade" }
		LOD 100
		Cull Back
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{
		CGPROGRAM

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		half2 texcoord : TEXCOORD0;
		float objY : TEXCOORD1;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	fixed4 _Color;
	fixed4 _OffColor;
	float _maxVertexY;
	float _showPercentY;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.objY = v.vertex.z;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		if ((_showPercentY / 100) > (i.objY / _maxVertexY)) {
			fixed4 col = tex2D(_MainTex, i.texcoord) * _Color;
			return col;
		}
		else 
		{
			fixed4 col = tex2D(_MainTex, i.texcoord) * _OffColor;
			return col;
		}

	}
		ENDCG
	}
	}

}
