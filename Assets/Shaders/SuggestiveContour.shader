Shader "Unlit/SuggestiveContour"
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
	float _Height; 
	float _Width;

	float _ThresholdScalar;
	float _DetectRadius;

	v2f vert_sc(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	float GetIntensity(float4 color)
	{
		return sqrt(color.x*color.x+color.y*color.y+color.z*color.z);
	}

	float3 DetectContour(float stepWidth, float stepHeight, float2 centerCoord)
	{
		float centerIntensity = GetIntensity(tex2D(_MainTex, centerCoord));

		int darkerAmount = 0;
		float maxIntensity = centerIntensity;

		for (int i = -_DetectRadius; i <= _DetectRadius; i++)
		{
			for (int j = -_DetectRadius; j <= _DetectRadius; j++)
			{
				float surrondingIntensity = GetIntensity(tex2D(_MainTex, centerCoord + float2(i * stepWidth, j * stepHeight)));

				if (surrondingIntensity < centerIntensity)
				{
					darkerAmount++;
				}

				if (surrondingIntensity > maxIntensity)
				{
					maxIntensity = surrondingIntensity;
				}
			}
		}

		if (maxIntensity - centerIntensity > _ThresholdScalar* _DetectRadius)
		{
			if (darkerAmount / (_DetectRadius * _DetectRadius) < 1 - (1 / _DetectRadius))
			{
				return float3(0.0, 0.0, 0.0);
			}
		}

		return float3(1.0, 1.0, 1.0);
	}

	fixed4 frag_sc(v2f i) : SV_Target
	{
		float stepWidth = 1 / _Width;
		float stepHeight = 1 / _Height;

		float3 result = DetectContour(stepWidth, stepHeight, i.uv);
	
		return fixed4(result,1.0);
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