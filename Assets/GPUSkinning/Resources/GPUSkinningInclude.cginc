#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

uniform sampler2D _GPUSkinning_TextureMatrix;
uniform float _GPUSkinning_NumPixelsPerFrame;
uniform float2 _GPUSkinning_TextureSize;

UNITY_INSTANCING_CBUFFER_START(GPUSkinningProperties0)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_FrameIndex)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_PixelSegmentation)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_RootMotionEnabled)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_CrossFadeEnabled)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_CrossFadeBlend)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_PixelSegmentation_CrossFade)
	UNITY_DEFINE_INSTANCED_PROP(float, _GPUSkinning_FrameIndex_CrossFade)
UNITY_INSTANCING_CBUFFER_END

UNITY_INSTANCING_CBUFFER_START(GPUSkinningProperties1)
	UNITY_DEFINE_INSTANCED_PROP(float4x4, _GPUSkinning_RootMotion)
UNITY_INSTANCING_CBUFFER_END

UNITY_INSTANCING_CBUFFER_START(GPUSkinningProperties2)
	UNITY_DEFINE_INSTANCED_PROP(float4x4, _GPUSkinning_RootMotion_CrossFade)
UNITY_INSTANCING_CBUFFER_END

inline float4 indexToUV(float index)
{
	int row = (int)(index / _GPUSkinning_TextureSize.x);
	float col = index - row * _GPUSkinning_TextureSize.x;
	return float4(col / _GPUSkinning_TextureSize.x, row / _GPUSkinning_TextureSize.y, 0, 0);
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	float4 row0 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex));
	float4 row1 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex + 1));
	float4 row2 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex + 2));
	float4 row3 = float4(0, 0, 0, 1);
	float4x4 mat = float4x4(row0, row1, row2, row3);
	return mat;
}

inline float getFrameStartIndex()
{
	float segment = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_PixelSegmentation);
	float frameIndex = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_FrameIndex);
	float frameStartIndex = segment + frameIndex * _GPUSkinning_NumPixelsPerFrame;
	return frameStartIndex;
}

inline float getFrameStartIndex_crossFade()
{
	float segment = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_PixelSegmentation_CrossFade);
	float frameIndex = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_FrameIndex_CrossFade);
	float frameStartIndex = segment + frameIndex * _GPUSkinning_NumPixelsPerFrame;
	return frameStartIndex;
}

#define rootMotionEnabled UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_RootMotionEnabled) > 0

#define crossFadeEnabled UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_CrossFadeEnabled) > 0

#define crossFadeBlend UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_CrossFadeBlend)

#define rootMotion UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_RootMotion)

#define rootMotion_crossFade UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_RootMotion_CrossFade)

#define textureMatrix(uv2, uv3) float frameStartIndex = getFrameStartIndex(); \
								float4x4 mat0 = getMatrix(frameStartIndex, uv2.x); \
								float4x4 mat1 = getMatrix(frameStartIndex, uv2.z); \
								float4x4 mat2 = getMatrix(frameStartIndex, uv3.x); \
								float4x4 mat3 = getMatrix(frameStartIndex, uv3.z);

#define textureMatrix_crossFade(uv2, uv3) float frameStartIndex_crossFade = getFrameStartIndex_crossFade(); \
											float4x4 mat0_crossFade = getMatrix(frameStartIndex_crossFade, uv2.x); \
											float4x4 mat1_crossFade = getMatrix(frameStartIndex_crossFade, uv2.z); \
											float4x4 mat2_crossFade = getMatrix(frameStartIndex_crossFade, uv3.x); \
											float4x4 mat3_crossFade = getMatrix(frameStartIndex_crossFade, uv3.z);

#define skin1_noroot(mat0) mul(mat0, vertex) * uv2.y;

#define skin1_root(mat0, root) mul(root, mul(mat0, vertex)) * uv2.y;

#define skin2_noroot(mat0, mat1) mul(mat0, vertex) * uv2.y + \
									mul(mat1, vertex) * uv2.w;

#define skin2_root(mat0, mat1, root) mul(root, mul(mat0, vertex)) * uv2.y + \
										mul(root, mul(mat1, vertex)) * uv2.w;

#define skin4_noroot(mat0, mat1, mat2, mat3) mul(mat0, vertex) * uv2.y + \
												mul(mat1, vertex) * uv2.w + \
												mul(mat2, vertex) * uv3.y + \
												mul(mat3, vertex) * uv3.w;

#define skin4_root(mat0, mat1, mat2, mat3, root) mul(root, mul(mat0, vertex)) * uv2.y + \
													mul(root, mul(mat1, vertex)) * uv2.w + \
													mul(root, mul(mat2, vertex)) * uv3.y + \
													mul(root, mul(mat3, vertex)) * uv3.w;

#define skin_blend(pos0, pos1) pos1.xyz + (pos0.xyz - pos1.xyz) * crossFadeBlend

inline float4 skin1(float4 vertex, float4 uv2, float4 uv3)
{
	textureMatrix(uv2, uv3);
	if (rootMotionEnabled)
	{
		float4x4 root = rootMotion;
		float4 pos0 = skin1_root(mat0, root);
		if (crossFadeEnabled)
		{
			float4x4 root_crossFade = rootMotion_crossFade;
			textureMatrix_crossFade(uv2, uv3);
			float4 pos1 = skin1_root(mat0_crossFade, root_crossFade);
			return float4(skin_blend(pos0, pos1), 1);
		}
		else
		{
			return pos0;
		}
	}
	else
	{
		float4 pos0 = skin1_noroot(mat0);
		if (crossFadeEnabled)
		{
			textureMatrix_crossFade(uv2, uv3);
			float4 pos1 = skin1_noroot(mat0_crossFade);
			return float4(skin_blend(pos0, pos1), 1);
		}
		else
		{
			return pos0;
		}
	}
}

inline float4 skin2(float4 vertex, float4 uv2, float4 uv3)
{
	textureMatrix(uv2, uv3);
	if (rootMotionEnabled)
	{
		float4x4 root = rootMotion;
		float4 pos0 = skin2_root(mat0, mat1, root);
		if (crossFadeEnabled)
		{
			float4x4 root_crossFade = rootMotion_crossFade;
			textureMatrix_crossFade(uv2, uv3);
			float4 pos1 = skin2_root(mat0_crossFade, mat1_crossFade, root_crossFade);
			return float4(skin_blend(pos0, pos1), 1);
		}
		else
		{
			return pos0;
		}
	}
	else
	{
		float4 pos0 = skin2_noroot(mat0, mat1);
		if (crossFadeEnabled)
		{
			textureMatrix_crossFade(uv2, uv3);
			float4 pos1 = skin2_noroot(mat0_crossFade, mat1_crossFade);
			return float4(skin_blend(pos0, pos1), 1);
		}
		else
		{
			return pos0;
		}
	}
}

inline float4 skin4(float4 vertex, float4 uv2, float4 uv3)
{
	textureMatrix(uv2, uv3);
	if (rootMotionEnabled)
	{
		float4x4 root = rootMotion;
		float4 pos0 = skin4_root(mat0, mat1, mat2, mat3, root);
		if (crossFadeEnabled)
		{ 
			float4x4 root_crossFade = rootMotion_crossFade;
			textureMatrix_crossFade(uv2, uv3);
			float4 pos1 = skin4_root(mat0_crossFade, mat1_crossFade, mat2_crossFade, mat3_crossFade, root_crossFade);
			return float4(skin_blend(pos0, pos1), 1);
		}
		else
		{
			return pos0;
		}
	}
	else
	{
		float4 pos0 = skin4_noroot(mat0, mat1, mat2, mat3);
		if (crossFadeEnabled)
		{
			textureMatrix_crossFade(uv2, uv3);
			float4 pos1 = skin4_noroot(mat0_crossFade, mat1_crossFade, mat2_crossFade, mat3_crossFade);
			return float4(skin_blend(pos0, pos1), 1);
		}
		else
		{
			return pos0;
		}
	}
}

#endif