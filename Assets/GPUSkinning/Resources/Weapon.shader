Shader "Unlit/Weapon"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
#ifdef GPU_INSTANCING_ON
		UNITY_INSTANCE_ID // UNITY_VERTEX_INPUT_INSTANCE_ID(Unity5.5 and later version)
#endif
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	uniform float4x4 _HierarchyToObjectMat;
	uniform float4x4 _HierarchyToObjectMats[410]; // Be careful with the limitation of the number of uniform.
	uniform float4x4 _JointLocalMatrix;

	sampler2D _MainTex;
	float4 _MainTex_ST;

	v2f vert(appdata v)
	{
#ifdef GPU_INSTANCING_ON
		UNITY_SETUP_INSTANCE_ID(v);
#endif

		v2f o;
#if defined(BUILT_IN_DRAW_MESH_INSTANCED_ON) && defined(GPU_INSTANCING_ON)
	#ifdef GPU_SKINNING_MATRIX_ARRAY
		o.vertex = UnityObjectToClipPos(mul(_HierarchyToObjectMat, v.vertex));
	#endif
	#ifdef GPU_SKINNING_MATRIX_TEXTURE
		o.vertex = UnityObjectToClipPos(mul(_HierarchyToObjectMats[unity_InstanceID], mul(_JointLocalMatrix, v.vertex)));
	#endif
#else
		o.vertex = UnityObjectToClipPos(v.vertex);
#endif
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, i.uv);
		col.rgb *= fixed3(0.5, 1, 0.5);
		return col;
	}

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile GPU_SKINNING_MATRIX_ARRAY GPU_SKINNING_MATRIX_TEXTURE
			#pragma multi_compile GPU_INSTANCING_ON GPU_INSTANCING_OFF
			#pragma multi_compile BUILT_IN_DRAW_MESH_INSTANCED_ON BUILT_IN_DRAW_MESH_INSTANCED_OFF
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
			#pragma multi_compile GPU_INSTANCING_OFF GPU_INSTANCING_ON
			#pragma multi_compile BUILT_IN_DRAW_MESH_INSTANCED_OFF BUILT_IN_DRAW_MESH_INSTANCED_ON
			ENDCG
		}
	}
}
