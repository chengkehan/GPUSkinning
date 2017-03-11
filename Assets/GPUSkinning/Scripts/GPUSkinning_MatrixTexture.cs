using UnityEngine;
using System.Collections;

/// <summary>
/// Skinning By Matrix Texture
/// </summary>
public class GPUSkinning_MatrixTexture : GPUSkinning_Component
{
    private int shaderPropID_MatricesTex = 0;

	private int shaderPropID_NumPixelsPerFrame = 0;

    private int shaderPropID_MatricesTexSize = 0;

    private int shaderPropID_AnimLength = 0;

    private int shaderPropID_AnimFPS = 0;

	public Matrix4x4[] hierarchyMatrices = null;

    private Texture2D matricesTex = null;

	private int numPixelsPerFrame = 0;

	public int numHierarchyMatricesPerFrame = 0;

    private int matricesTexWidth = 128;

    private int matricesTexHeight = 128;

	public GPUSkinning_AdditionalVertexStreames additionalVertexStreames = null;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        if (IsMatricesTextureSupported())
        {
            shaderPropID_MatricesTex = Shader.PropertyToID("_MatricesTex");
            shaderPropID_MatricesTexSize = Shader.PropertyToID("_MatricesTexSize");
            shaderPropID_AnimLength = Shader.PropertyToID("_AnimLength");
            shaderPropID_AnimFPS = Shader.PropertyToID("_AnimFPS");
			shaderPropID_NumPixelsPerFrame = Shader.PropertyToID("_NumPixelsPerFrame");

            matricesTex = new Texture2D(matricesTexWidth, matricesTexHeight, TextureFormat.RGBAHalf, false);
            matricesTex.name = "_MatricesTex";
            matricesTex.filterMode = FilterMode.Point;

			additionalVertexStreames = new GPUSkinning_AdditionalVertexStreames(gpuSkinning.model.newMesh);

            BakeAnimationsToTexture();
        }
    }

    public override void Destroy()
    {
        base.Destroy();

        if (matricesTex != null)
        {
            Object.Destroy(matricesTex);
            matricesTex = null;
        }
        if (additionalVertexStreames != null)
        {
			additionalVertexStreames.Destroy();
            additionalVertexStreames = null;
        }

		hierarchyMatrices = null;
    }

    public bool IsSupported()
    {
        return matricesTex != null;
    }

    public void Update()
    {
        if (matricesTex != null)
        {
            gpuSkinning.model.newMtrl.SetTexture(shaderPropID_MatricesTex, matricesTex);
            gpuSkinning.model.newMtrl.SetFloat(shaderPropID_NumPixelsPerFrame, numPixelsPerFrame);
            gpuSkinning.model.newMtrl.SetVector(shaderPropID_MatricesTexSize, new Vector4(matricesTex.width, matricesTex.height, 0, 0));
            gpuSkinning.model.newMtrl.SetFloat(shaderPropID_AnimLength, gpuSkinning.model.boneAnimations[0].length);
            gpuSkinning.model.newMtrl.SetFloat(shaderPropID_AnimFPS, gpuSkinning.model.boneAnimations[0].fps);
        }
    }

    private bool IsMatricesTextureSupported()
    {
        return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf);
    }

    private void BakeAnimationsToTexture()
    {
        if (matricesTex != null)
        {
            Color[] colorBuffer = matricesTex.GetPixels();
            int colorBufferIndex = 0;
			hierarchyMatrices = new Matrix4x4[colorBuffer.Length / 3];
			int hierarchyMatrixIndex = 0;

            GPUSkinningUtil.ExtractBoneAnimMatrix(
                gpuSkinning, 
                gpuSkinning.model.boneAnimations[0],
				(animMat, hierarchyMat) =>
                {
					hierarchyMatrices[hierarchyMatrixIndex++] = hierarchyMat;

                    Color c = colorBuffer[colorBufferIndex];
                    c.r = animMat.m00; c.g = animMat.m01; c.b = animMat.m02; c.a = animMat.m03;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = animMat.m10; c.g = animMat.m11; c.b = animMat.m12; c.a = animMat.m13;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = animMat.m20; c.g = animMat.m21; c.b = animMat.m22; c.a = animMat.m23;
                    colorBuffer[colorBufferIndex++] = c;
                },
                (frameIndex) =>
                {
                    if (frameIndex == 0)
                    {
						numHierarchyMatricesPerFrame = hierarchyMatrixIndex;
                        numPixelsPerFrame = colorBufferIndex;
                    }
                }
            );

            matricesTex.SetPixels(colorBuffer);
            matricesTex.Apply(false, true);
        }
    }
}
