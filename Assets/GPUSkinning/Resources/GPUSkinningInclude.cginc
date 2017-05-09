#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

uniform sampler2D _GPUSkinning_TextureMatrix;
uniform float3 _GPUSkinning_TextureSize_NumPixelsPerFrame;

UNITY_INSTANCING_CBUFFER_START(GPUSkinningProperties0)
	UNITY_DEFINE_INSTANCED_PROP(float2, _GPUSkinning_FrameIndex_PixelSegmentation)
#if !defined(ROOTON_BLENDOFF) && !defined(ROOTOFF_BLENDOFF)
	UNITY_DEFINE_INSTANCED_PROP(float3, _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade)
#endif
UNITY_INSTANCING_CBUFFER_END

#if defined(ROOTON_BLENDOFF) || defined(ROOTON_BLENDON_CROSSFADEROOTON) || defined(ROOTON_BLENDON_CROSSFADEROOTOFF)
UNITY_INSTANCING_CBUFFER_START(GPUSkinningProperties1)
	UNITY_DEFINE_INSTANCED_PROP(float4x4, _GPUSkinning_RootMotion)
UNITY_INSTANCING_CBUFFER_END
#endif

#if defined(ROOTON_BLENDON_CROSSFADEROOTON) || defined(ROOTOFF_BLENDON_CROSSFADEROOTON)
UNITY_INSTANCING_CBUFFER_START(GPUSkinningProperties2)
	UNITY_DEFINE_INSTANCED_PROP(float4x4, _GPUSkinning_RootMotion_CrossFade)
UNITY_INSTANCING_CBUFFER_END
#endif

inline float4 indexToUV(float index)
{
	int row = (int)(index / _GPUSkinning_TextureSize_NumPixelsPerFrame.x);
	float col = index - row * _GPUSkinning_TextureSize_NumPixelsPerFrame.x;
	return float4(col / _GPUSkinning_TextureSize_NumPixelsPerFrame.x, row / _GPUSkinning_TextureSize_NumPixelsPerFrame.y, 0, 0);
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
	float2 frameIndex_segment = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_FrameIndex_PixelSegmentation);
	float segment = frameIndex_segment.y;
	float frameIndex = frameIndex_segment.x;
	float frameStartIndex = segment + frameIndex * _GPUSkinning_TextureSize_NumPixelsPerFrame.z;
	return frameStartIndex;
}

#if !defined(ROOTON_BLENDOFF) && !defined(ROOTOFF_BLENDOFF)
inline float getFrameStartIndex_crossFade()
{
	float3 frameIndex_segment = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade);
	float segment = frameIndex_segment.y;
	float frameIndex = frameIndex_segment.x;
	float frameStartIndex = segment + frameIndex * _GPUSkinning_TextureSize_NumPixelsPerFrame.z;
	return frameStartIndex;
}
#endif

#define crossFadeBlend UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade).z

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

#define skin1_noroot(mat0, mat1, mat2, mat3) mul(mat0, vertex) * uv2.y;

#define skin1_root(mat0, mat1, mat2, mat3, root) mul(root, mul(mat0, vertex)) * uv2.y;

#define skin2_noroot(mat0, mat1, mat2, mat3) mul(mat0, vertex) * uv2.y + \
									mul(mat1, vertex) * uv2.w;

#define skin2_root(mat0, mat1, mat2, mat3, root) mul(root, mul(mat0, vertex)) * uv2.y + \
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

#define rootOff_BlendOff(quality) textureMatrix(uv2, uv3); \
									return skin##quality##_noroot(mat0, mat1, mat2, mat3);

#define rootOn_BlendOff(quality) textureMatrix(uv2, uv3); \
									float4x4 root = rootMotion; \
									return skin##quality##_root(mat0, mat1, mat2, mat3, root);

#define rootOn_BlendOn_CrossFadeRootOn(quality) textureMatrix(uv2, uv3); \
												textureMatrix_crossFade(uv2, uv3); \
												float4x4 root = rootMotion; \
												float4x4 root_crossFade = rootMotion_crossFade; \
												float4 pos0 = skin##quality##_root(mat0, mat1, mat2, mat3, root); \
												float4 pos1 = skin##quality##_root(mat0_crossFade, mat1_crossFade, mat2_crossFade, mat3_crossFade, root_crossFade); \
												return float4(skin_blend(pos0, pos1), 1);

#define rootOn_BlendOn_CrossFadeRootOff(quality) textureMatrix(uv2, uv3); \
												textureMatrix_crossFade(uv2, uv3); \
												float4x4 root = rootMotion; \
												float4 pos0 = skin##quality##_root(mat0, mat1, mat2, mat3, root); \
												float4 pos1 = skin##quality##_noroot(mat0_crossFade, mat1_crossFade, mat2_crossFade, mat3_crossFade); \
												return float4(skin_blend(pos0, pos1), 1);

#define rootOff_BlendOn_CrossFadeRootOn(quality) textureMatrix(uv2, uv3); \
												textureMatrix_crossFade(uv2, uv3); \
												float4x4 root_crossFade = rootMotion_crossFade; \
												float4 pos0 = skin##quality##_noroot(mat0, mat1, mat2, mat3); \
												float4 pos1 = skin##quality##_root(mat0_crossFade, mat1_crossFade, mat2_crossFade, mat3_crossFade, root_crossFade); \
												return float4(skin_blend(pos0, pos1), 1);

#define rootOff_BlendOn_CrossFadeRootOff(quality) textureMatrix(uv2, uv3); \
												textureMatrix_crossFade(uv2, uv3); \
												float4 pos0 = skin##quality##_noroot(mat0, mat1, mat2, mat3); \
												float4 pos1 = skin##quality##_noroot(mat0_crossFade, mat1_crossFade, mat2_crossFade, mat3_crossFade); \
												return float4(skin_blend(pos0, pos1), 1);

inline float4 skin1(float4 vertex, float4 uv2, float4 uv3)
{
#if ROOTOFF_BLENDOFF
	rootOff_BlendOff(1);
#endif
#if ROOTON_BLENDOFF
	rootOn_BlendOff(1);
#endif
#if ROOTON_BLENDON_CROSSFADEROOTON
	rootOn_BlendOn_CrossFadeRootOn(1);
#endif
#if ROOTON_BLENDON_CROSSFADEROOTOFF
	rootOn_BlendOn_CrossFadeRootOff(1);
#endif
#if ROOTOFF_BLENDON_CROSSFADEROOTON
	rootOff_BlendOn_CrossFadeRootOn(1);
#endif
#if ROOTOFF_BLENDON_CROSSFADEROOTOFF
	rootOff_BlendOn_CrossFadeRootOff(1);
#endif
	return 0;
}

inline float4 skin2(float4 vertex, float4 uv2, float4 uv3)
{
#if ROOTOFF_BLENDOFF
	rootOff_BlendOff(2);
#endif
#if ROOTON_BLENDOFF
	rootOn_BlendOff(2);
#endif
#if ROOTON_BLENDON_CROSSFADEROOTON
	rootOn_BlendOn_CrossFadeRootOn(2);
#endif
#if ROOTON_BLENDON_CROSSFADEROOTOFF
	rootOn_BlendOn_CrossFadeRootOff(2);
#endif
#if ROOTOFF_BLENDON_CROSSFADEROOTON
	rootOff_BlendOn_CrossFadeRootOn(2);
#endif
#if ROOTOFF_BLENDON_CROSSFADEROOTOFF
	rootOff_BlendOn_CrossFadeRootOff(2);
#endif
	return 0;
}

inline float4 skin4(float4 vertex, float4 uv2, float4 uv3)
{
#if ROOTOFF_BLENDOFF
	rootOff_BlendOff(4);
#endif
#if ROOTON_BLENDOFF
	rootOn_BlendOff(4);
#endif
#if ROOTON_BLENDON_CROSSFADEROOTON
	rootOn_BlendOn_CrossFadeRootOn(4);
#endif
#if ROOTON_BLENDON_CROSSFADEROOTOFF
	rootOn_BlendOn_CrossFadeRootOff(4);
#endif
#if ROOTOFF_BLENDON_CROSSFADEROOTON
	rootOff_BlendOn_CrossFadeRootOn(4);
#endif
#if ROOTOFF_BLENDON_CROSSFADEROOTOFF
	rootOff_BlendOn_CrossFadeRootOff(4);
#endif
	return 0;
}

#endif