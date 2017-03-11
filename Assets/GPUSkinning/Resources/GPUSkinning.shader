Shader "Unlit/GPUSkinning"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	uniform float4x4 _Matrices[24];
	uniform sampler2D _MatricesTex;
	uniform float _NumPixelsPerFrame;
	uniform float4 _MatricesTexSize;
	uniform float _AnimLength;
	uniform float _AnimFPS;
	uniform sampler2D _TerrainTex;
	uniform float3 _TerrainSize;
	uniform float3 _TerrainPos;
	uniform float _GameTime;

#ifdef GPU_INSTANCING_ON
	UNITY_INSTANCING_CBUFFER_START(MyProperties)
	UNITY_DEFINE_INSTANCED_PROP(float, _mpb_time)
	UNITY_INSTANCING_CBUFFER_END
#endif

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;
		float4 tangent : TANGENT;
#ifdef GPU_INSTANCING_ON
		UNITY_INSTANCE_ID // UNITY_VERTEX_INPUT_INSTANCE_ID(Unity5.5 and later version)
#endif
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	inline float4 indexToUV(int index)
	{
		int row = (int)(index / _MatricesTexSize.x);
		int col = index - row * _MatricesTexSize.x;
		return float4(col / _MatricesTexSize.x, row / _MatricesTexSize.y, 0, 0);
	}

	inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
	{
		int matStartIndex = frameStartIndex + boneIndex * 3;
		float4 row0 = tex2Dlod(_MatricesTex, indexToUV(matStartIndex));
		float4 row1 = tex2Dlod(_MatricesTex, indexToUV(matStartIndex + 1));
		float4 row2 = tex2Dlod(_MatricesTex, indexToUV(matStartIndex + 2));
		float4 row3 = float4(0, 0, 0, 1);
		float4x4 mat = float4x4(row0, row1, row2, row3);
		return mat;
	}

	v2f vert(appdata v)
	{
		v2f o;
#ifdef GPU_INSTANCING_ON
		UNITY_SETUP_INSTANCE_ID(v);
#endif

#ifdef GPU_SKINNING_MATRIX_TEXTURE
	#ifdef GPU_INSTANCING_ON
		float timeOffset = UNITY_ACCESS_INSTANCED_PROP(_mpb_time);
	#endif
	#ifdef GPU_INSTANCING_OFF
		float timeOffset = v.uv2.x;
	#endif
		int frameIndex = (int)(((_GameTime + timeOffset) * _AnimFPS) % (_AnimLength * _AnimFPS));
		int frameStartIndex = frameIndex * _NumPixelsPerFrame;

		float4x4 mat0 = getMatrix(frameStartIndex, v.tangent.x);
		float4x4 mat1 = getMatrix(frameStartIndex, v.tangent.z);

		float4 pos =
			mul(mat0, v.vertex) * v.tangent.y +
			mul(mat1, v.vertex) * v.tangent.w;
#endif

#ifdef GPU_SKINNING_MATRIX_ARRAY
		float4 pos =
			mul(_Matrices[v.tangent.x], v.vertex) * v.tangent.y +
			mul(_Matrices[v.tangent.z], v.vertex) * v.tangent.w;
#endif

#ifdef TERRAIN_HEIGHT_ON
		// Terrain
		float4 wPos = float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], 1);
		float2 wPos_uv = saturate((wPos.xz - _TerrainPos.xz) / _TerrainSize.xz);
		wPos_uv.y = 1 - wPos_uv.y;
		fixed3 terrainC = tex2Dlod(_TerrainTex, float4(wPos_uv, 0, 0));
		wPos = mul(unity_ObjectToWorld, pos);
		wPos.y += _TerrainPos.y + terrainC.r * _TerrainSize.y;

		o.vertex = mul(UNITY_MATRIX_VP, wPos);
#endif
#ifdef TERRAIN_HEIGHT_OFF
		o.vertex = mul(UNITY_MATRIX_MVP, pos);
#endif
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);



		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, i.uv);
		return col;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile GPU_SKINNING_MATRIX_ARRAY GPU_SKINNING_MATRIX_TEXTURE
			#pragma multi_compile TERRAIN_HEIGHT_ON TERRAIN_HEIGHT_OFF
			#pragma multi_compile GPU_INSTANCING_ON GPU_INSTANCING_OFF
			ENDCG
		}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile GPU_SKINNING_MATRIX_ARRAY GPU_SKINNING_MATRIX_TEXTURE
			#pragma multi_compile TERRAIN_HEIGHT_ON TERRAIN_HEIGHT_OFF
			#pragma multi_compile GPU_INSTANCING_OFF GPU_INSTANCING_ON
			ENDCG
		}
	}
}
