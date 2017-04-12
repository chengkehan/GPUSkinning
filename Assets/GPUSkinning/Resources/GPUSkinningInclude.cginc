#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

uniform sampler2D _GPUSkinning_TextureMatrix;
uniform float _GPUSkinning_NumPixelsPerFrame;
uniform float2 _GPUSkinning_TextureSize;
uniform float _GPUSkinning_ClipLength;
uniform float _GPUSkinning_ClipFPS;
uniform float _GPUSkinning_Time;
uniform float _GPUSkinning_PixelSegmentation;

inline float4 indexToUV(int index)
{
	int row = (int)(index / _GPUSkinning_TextureSize.x);
	int col = index - row * _GPUSkinning_TextureSize.x;
	return float4(col / _GPUSkinning_TextureSize.x, row / _GPUSkinning_TextureSize.y, 0, 0);
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
	int matStartIndex = frameStartIndex + boneIndex * 3;
	float4 row0 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex));
	float4 row1 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex + 1));
	float4 row2 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex + 2));
	float4 row3 = float4(0, 0, 0, 1);
	float4x4 mat = float4x4(row0, row1, row2, row3);
	return mat;
}

inline int getFrameStartIndex()
{
	int frameIndex = (int)fmod((_GPUSkinning_Time + 0) * _GPUSkinning_ClipFPS, _GPUSkinning_ClipLength * _GPUSkinning_ClipFPS);
	int frameStartIndex = _GPUSkinning_PixelSegmentation + frameIndex * _GPUSkinning_NumPixelsPerFrame;
	return frameStartIndex;
}

#endif