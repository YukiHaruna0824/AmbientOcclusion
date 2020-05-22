Shader "Unlit/SSAO"
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
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float3 viewRay : TEXCOORD1;
	};


	#define MAX_SAMPLE_KERNEL_COUNT 100

	//ssao use
	sampler2D _MainTex;
	sampler2D _CameraDepthNormalsTexture;
	float4x4 _InverseProjectionMatrix;
	float4 _SampleVectorArray[MAX_SAMPLE_KERNEL_COUNT];
	float _SampleCount;
	float _SampleRadius;

	//filter use
	float4 _MainTex_TexelSize;
	float4 _BlurRadius;
	float _BilaterFilterFactor;

	sampler2D _AOTex;
	sampler2D _NoiseTex;

	//Screen Height Width
	float Height; 
	float Width;

	float3 GetNormal(float2 uv)
	{
		float4 cdn = tex2D(_CameraDepthNormalsTexture, uv);
		return DecodeViewNormalStereo(cdn);
	}

	half CompareNormal(float3 normal1, float3 normal2)
	{
		return smoothstep(_BilaterFilterFactor, 1.0, dot(normal1, normal2));
	}

	v2f vert_ao(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;

		//get screen pos dir 
		float4 clipPos = float4(v.uv.x * 2 - 1.0, v.uv.y * 2 - 1.0, 1.0, 1.0);
		float4 viewRay = mul(_InverseProjectionMatrix, clipPos);
		o.viewRay = viewRay.xyz / viewRay.w;
		return o;
	}


	fixed4 frag_ao(v2f i) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, i.uv);

		float linear01Depth;
		float3 viewNormal;

		float4 cdn = tex2D(_CameraDepthNormalsTexture, i.uv);
		//get depth value and normal value
		DecodeDepthNormal(cdn, linear01Depth, viewNormal);

		
		float3 viewPos = linear01Depth * i.viewRay;
		viewNormal = normalize(viewNormal) * float3(1, 1, -1);

		// noise map (4 * 4)
		float2 noiseScale = float2(Height / 4.0, Width / 4.0);
		float2 noiseUV = float2(i.uv.x * noiseScale.x, i.uv.y * noiseScale.y);

		//sample from noise map
		float3 randvec = tex2D(_NoiseTex, noiseUV).xyz;

		//Gramm-Schimidt method
		float3 tangent = normalize(randvec - viewNormal * dot(randvec, viewNormal));
		float3 bitangent = cross(viewNormal, tangent);
		float3x3 TBN = float3x3(tangent, bitangent, viewNormal);

		int sampleCount = _SampleCount;

		float ao = 0.0;
		for (int i = 0; i < sampleCount; i++)
		{
			//convert to normal tangent base
			float3 randomVec = mul(_SampleVectorArray[i].xyz, TBN);
			float3 randomPos = viewPos + randomVec * _SampleRadius;
			
			//project to screen pos
			float3 rclipPos = mul((float3x3)unity_CameraProjection, randomPos);
			float2 rscreenPos = (rclipPos.xy / rclipPos.z) * 0.5 + 0.5;

			float randomDepth;
			float3 randomNormal;
			float4 rcdn = tex2D(_CameraDepthNormalsTexture, rscreenPos);
			DecodeDepthNormal(rcdn, randomDepth, randomNormal);

			//use step function to prevent to use if statement
			float rangeCheck = smoothstep(0.0, 1.0, _SampleRadius / abs(randomDepth - linear01Depth));
			ao += (randomDepth >= linear01Depth ? 1.0 : 0.0) * rangeCheck;
		}
		
		ao = ao / sampleCount;
		col.rgb = ao;
		return col;
	}

	//Bilateral Filter
	fixed4 frag_blur(v2f i) : SV_Target
	{
		float2 delta = _MainTex_TexelSize.xy * _BlurRadius.xy;

		float2 uv = i.uv;
		float2 uv0a = i.uv - delta;
		float2 uv0b = i.uv + delta;
		float2 uv1a = i.uv - 2.0 * delta;
		float2 uv1b = i.uv + 2.0 * delta;
		float2 uv2a = i.uv - 3.0 * delta;
		float2 uv2b = i.uv + 3.0 * delta;

		float3 normal = GetNormal(uv);
		float3 normal0a = GetNormal(uv0a);
		float3 normal0b = GetNormal(uv0b);
		float3 normal1a = GetNormal(uv1a);
		float3 normal1b = GetNormal(uv1b);
		float3 normal2a = GetNormal(uv2a);
		float3 normal2b = GetNormal(uv2b);

		fixed4 col = tex2D(_MainTex, uv);
		fixed4 col0a = tex2D(_MainTex, uv0a);
		fixed4 col0b = tex2D(_MainTex, uv0b);
		fixed4 col1a = tex2D(_MainTex, uv1a);
		fixed4 col1b = tex2D(_MainTex, uv1b);
		fixed4 col2a = tex2D(_MainTex, uv2a);
		fixed4 col2b = tex2D(_MainTex, uv2b);

		half w = 0.37004405286;
		half w0a = CompareNormal(normal, normal0a) * 0.31718061674;
		half w0b = CompareNormal(normal, normal0b) * 0.31718061674;
		half w1a = CompareNormal(normal, normal1a) * 0.19823788546;
		half w1b = CompareNormal(normal, normal1b) * 0.19823788546;
		half w2a = CompareNormal(normal, normal2a) * 0.11453744493;
		half w2b = CompareNormal(normal, normal2b) * 0.11453744493;

		half3 result;
		result = w * col.rgb;
		result += w0a * col0a.rgb;
		result += w0b * col0b.rgb;
		result += w1a * col1a.rgb;
		result += w1b * col1b.rgb;
		result += w2a * col2a.rgb;
		result += w2b * col2b.rgb;

		result /= w + w0a + w0b + w1a + w1b + w2a + w2b;
		return fixed4(result, 1.0);
	}

	//Combine Color Texture and AO Texture 
	fixed4 frag_composite(v2f i) : SV_Target
	{
		fixed4 ori = tex2D(_MainTex, i.uv);
		fixed4 ao = tex2D(_AOTex, i.uv);
		ori.rgb *= ao.r;
		return ori;
	}
	ENDCG

	SubShader
	{

		Cull Off ZWrite Off ZTest Always

		//Pass 0 : Generate AO 
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_ao
			#pragma fragment frag_ao
			ENDCG
		}

		//Pass 1 : Bilateral Filter Blur
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_ao
			#pragma fragment frag_blur
			ENDCG
		}

		//Pass 2 : Composite AO
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_ao
			#pragma fragment frag_composite
			ENDCG
		}
	}
}