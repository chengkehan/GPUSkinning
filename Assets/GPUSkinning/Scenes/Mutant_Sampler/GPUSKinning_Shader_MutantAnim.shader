Shader "GPUSkinning/GPUSkinning_Unlit_MutantAnim"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Assets/GPUSkinning/Resources/GPUSkinningInclude.cginc"

	uniform float4x4 _GPUSkinning_MatrixArray[26];

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float4 uv2 : TEXCOORD1;
		float4 uv3 : TEXCOORD2;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	v2f vert(appdata v)
	{
		v2f o;

#ifdef GPU_SKINNING_MATRIX_ARRAY
		matrixArray(v.uv2, v.uv3);
#endif

#ifdef GPU_SKINNING_TEXTURE_MATRIX
		textureMatrix(v.uv2, v.uv3);
#endif

		

		
		float4 pos = skin2(v.vertex, v.uv2, v.uv3);
		

		

		o.vertex = UnityObjectToClipPos(pos);
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
			#pragma multi_compile GPU_SKINNING_MATRIX_ARRAY GPU_SKINNING_TEXTURE_MATRIX
			ENDCG
		}
	}
}
