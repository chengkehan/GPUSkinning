using UnityEngine;
using System.Collections;

/// <summary>
/// Skinning By Matrix Texture
/// </summary>
public class GPUSkinning_MatrixTexture : GPUSkinning_Component
{
    private int shaderPropID_MatricesTex = 0;

    private int shaderPropID_MatricesTexFrameTexels = 0;

    private int shaderPropID_MatricesTexSize = 0;

    private int shaderPropID_AnimLength = 0;

    private int shaderPropID_AnimFPS = 0;

    private Texture2D matricesTex = null;

    private int matricesTexFrameTexels = 0;

    private int matricesTexWidth = 128;

    private int matricesTexHeight = 128;

    private Mesh[] additionalVertexStreames = null;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        if (IsMatricesTextureSupported())
        {
            shaderPropID_MatricesTex = Shader.PropertyToID("_MatricesTex");
            shaderPropID_MatricesTexSize = Shader.PropertyToID("_MatricesTexSize");
            shaderPropID_AnimLength = Shader.PropertyToID("_AnimLength");
            shaderPropID_AnimFPS = Shader.PropertyToID("_AnimFPS");

            matricesTex = new Texture2D(matricesTexWidth, matricesTexHeight, TextureFormat.RGBAHalf, false);
            matricesTex.name = "_MatricesTex";
            matricesTex.filterMode = FilterMode.Point;

            additionalVertexStreames = new Mesh[50];
            GPUSkinningUtil.InitAdditionalVertexStream(additionalVertexStreames, gpuSkinning.model.newMesh);

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
            foreach (var m in additionalVertexStreames)
            {
                Object.Destroy(m);
            }
            additionalVertexStreames = null;
        }
    }

    public bool IsSupported()
    {
        return matricesTex != null;
    }

    public Mesh RandomAdditionalVertexStream()
    {
        return additionalVertexStreames == null ? null : additionalVertexStreames[Random.Range(0, additionalVertexStreames.Length)];
    }

    public void Update()
    {
        if (matricesTex != null)
        {
            gpuSkinning.model.newMtrl.SetTexture(shaderPropID_MatricesTex, matricesTex);
            gpuSkinning.model.newMtrl.SetFloat(shaderPropID_MatricesTexFrameTexels, matricesTexFrameTexels);
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

            GPUSkinningUtil.ExtractBoneAnimMatrix(
                gpuSkinning, 
                gpuSkinning.model.boneAnimations[0],
                (mat) =>
                {
                    Color c = colorBuffer[colorBufferIndex];
                    c.r = mat.m00; c.g = mat.m01; c.b = mat.m02; c.a = mat.m03;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = mat.m10; c.g = mat.m11; c.b = mat.m12; c.a = mat.m13;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = mat.m20; c.g = mat.m21; c.b = mat.m22; c.a = mat.m23;
                    colorBuffer[colorBufferIndex++] = c;
                },
                (frameIndex) =>
                {
                    if (frameIndex == 0)
                    {
                        shaderPropID_MatricesTexFrameTexels = Shader.PropertyToID("_MatricesTexFrameTexels");
                        matricesTexFrameTexels = colorBufferIndex;
                    }
                }
            );

            matricesTex.SetPixels(colorBuffer);
            matricesTex.Apply(false, true);
        }
    }
}
