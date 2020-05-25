Shader "Unlit/SuggestiveContour2"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	sampler2D _MainTex;
	sampler2D _CameraDepthTexture;
	float _Height; 
	float _Width;

	float _Threshold;

	v2f vert_sc(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	fixed4 frag_sc(v2f i) : SV_Target
	{ 
		float stepW = 1.0 / _Width;
		float stepH = 1.0 / _Height;
		float tleft = tex2D(_CameraDepthTexture,i.uv + float2(-stepW, stepH));
		float left = tex2D(_CameraDepthTexture, i.uv + float2(-stepW, 0));
		float bleft = tex2D(_CameraDepthTexture, i.uv + float2(-stepW, -stepH));
		float top = tex2D(_CameraDepthTexture, i.uv + float2(0, stepH));
		float bottom = tex2D(_CameraDepthTexture, i.uv + float2(0, -stepH));
		float tright = tex2D(_CameraDepthTexture, i.uv + float2(stepW, stepH));
		float right = tex2D(_CameraDepthTexture, i.uv + float2(stepW, 0));
		float bright = tex2D(_CameraDepthTexture, i.uv + float2(stepW, -stepH));
	

		float x = tleft + 2.0*left + bleft - tright - 2.0*right - bright;
		float y = -tleft - 2.0*top - tright + bleft + 2.0 * bottom + bright;
		float color = sqrt((x*x) + (y*y));
		if (color > _Threshold) { return fixed4(0.0, 0.0, 0.0,1.0); }
		return fixed4(1.0, 1.0, 1.0,1.0);
	}

	ENDCG

	SubShader
	{

		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_sc
			#pragma fragment frag_sc
			ENDCG
		}

	}
}