Shader "Unlit/ProceduralModel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct ins_data
			{
				uint vertexID : SV_VertexID;
				uint instanceID : SV_InstanceID;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			struct Vertex
			{
				float3 vertex;
				float4 tangents;
				float2 uv;
			};

			struct Matrix
			{
				float4x4 mat;
			};

			struct GlobalData
			{
				int oneFrameMatricesStride;
				int fps;
				float animLength;
			};

			struct Model
			{
				float3 pos;
				float time;
			};

			StructuredBuffer<Vertex> _VertCB;
			StructuredBuffer<Matrix> _MatCB;
			StructuredBuffer<GlobalData> _GlobalCB;
			StructuredBuffer<Model> _ModelCB;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (ins_data v)
			{
				v2f o;
				
				GlobalData global = _GlobalCB[0];
				Vertex vertex = _VertCB[v.vertexID];
				Model model = _ModelCB[v.instanceID];

				float4 vert = float4(vertex.vertex, 1);
				float4 tan = vertex.tangents;

				int frameIndex = (int)(((_Time.y + model.time) * global.fps) % (global.animLength * global.fps));
				int frameStartIndex = frameIndex * global.oneFrameMatricesStride;

				float4x4 mat0 = _MatCB[frameStartIndex + tan.x].mat;
				float4x4 mat1 = _MatCB[frameStartIndex + tan.z].mat;

				float4 pos =
					mul(mat0, vert) * tan.y +
					mul(mat1, vert) * tan.w;

				pos.xyz += model.pos;

				o.uv = vertex.uv;
				o.vertex = mul(UNITY_MATRIX_VP, pos);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb *= fixed3(1, 0.5, 0.5);
				return col;
			}
			ENDCG
		}
	}
}
