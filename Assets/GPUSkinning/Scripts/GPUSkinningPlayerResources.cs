using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerResources
{
    public GPUSkinningAnimation anim = null;

    public Mesh mesh = null;

    public GPUSkinningPlayerMaterial mtrl = null;

    public Texture2D texture = null;

    public List<GPUSkinningPlayerMono> players = new List<GPUSkinningPlayerMono>();

    private MaterialPropertyBlock[] mpbs = null;

    private static int shaderPropID_GPUSkinning_MatrixArray = -1;

    private static int shaderPropID_GPUSkinning_TextureMatrix = 0;

    private static int shaderPropID_GPUSkinning_NumPixelsPerFrame = 0;

    private static int shaderPropID_GPUSkinning_TextureSize = 0;

    private static int shaderPropID_GPUSkinning_ClipLength = 0;

    private static int shaderPropID_GPUSkinning_ClipFPS = 0;

    private static int shaderPorpID_GPUSkinning_Time = 0;

    private static int shaderPropID_GPUSkinning_PixelSegmentation = 0;

    private static int shaderPropID_GPUSkinning_IndividualDefference_MaterialPropertyBlock = 0;

    private GPUSkinningIndividualDifferenceMode individualDefferenceMode = GPUSkinningIndividualDifferenceMode.NONE;
    public GPUSkinningIndividualDifferenceMode IndividualDefferenceMode
    {
        get
        {
            return individualDefferenceMode;
        }
        set
        {
            individualDefferenceMode = value;

            if(value == GPUSkinningIndividualDifferenceMode.NONE)
            {
                SetIndividualDifferenceMode_None();
            }
            else if(value == GPUSkinningIndividualDifferenceMode.MATERIAL_PROPERTY_BLOCK)
            {
                SetIndividualDifferenceMode_MaterialPropertyBlock();
            }
            else if(value == GPUSkinningIndividualDifferenceMode.ADDITINOAL_VERTEX_STREAMS)
            {

            }
            else
            {
                throw new System.NotImplementedException();
            }
        }
    }

    private GPUSkinningBoneMode boneMode = GPUSkinningBoneMode.MATRIX_ARRAY;
    public void SetBoneMode(GPUSkinningBoneMode boneMode, bool isEnforced)
    {
        this.boneMode = boneMode;
        UpdateBoneMode(isEnforced);
    }
    public GPUSkinningBoneMode GetBoneMode()
    {
        return this.boneMode;
    }

    public GPUSkinningPlayerResources()
    {
        if(shaderPropID_GPUSkinning_MatrixArray == -1)
        {
            shaderPropID_GPUSkinning_MatrixArray = Shader.PropertyToID("_GPUSkinning_MatrixArray");
            shaderPropID_GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
            shaderPropID_GPUSkinning_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_NumPixelsPerFrame");
            shaderPropID_GPUSkinning_TextureSize = Shader.PropertyToID("_GPUSkinning_TextureSize");
            shaderPropID_GPUSkinning_ClipLength = Shader.PropertyToID("_GPUSkinning_ClipLength");
            shaderPropID_GPUSkinning_ClipFPS = Shader.PropertyToID("_GPUSkinning_ClipFPS");
            shaderPorpID_GPUSkinning_Time = Shader.PropertyToID("_GPUSkinning_Time");
            shaderPropID_GPUSkinning_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_PixelSegmentation");
            shaderPropID_GPUSkinning_IndividualDefference_MaterialPropertyBlock = Shader.PropertyToID("_GPUSkinning_IndividualDefference_MaterialPropertyBlock");
        }
    }

    public void Destroy()
    {
        anim = null;
        mesh = null;

        if (mtrl != null)
        {
            mtrl.Destroy();
            mtrl = null;
        }

        if (texture != null)
        {
            Object.DestroyImmediate(texture);
            texture = null;
        }

        if (players != null)
        {
            players.Clear();
            players = null;
        }

        DestroyMaterialPropertyBlocks();
    }

    public void UpdateBoneMode_MatrixArray(Matrix4x4[] matrices)
    {
        mtrl.Material.SetMatrixArray(shaderPropID_GPUSkinning_MatrixArray, matrices);
    }

    public void UpdateBoneMode_TextureMatrix(GPUSkinningClip playingClip, float time)
    {
        mtrl.Material.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);
        mtrl.Material.SetFloat(shaderPropID_GPUSkinning_NumPixelsPerFrame, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/);
        mtrl.Material.SetVector(shaderPropID_GPUSkinning_TextureSize, new Vector4(anim.textureWidth, anim.textureHeight, 0, 0));
        mtrl.Material.SetFloat(shaderPropID_GPUSkinning_ClipLength, playingClip.length);
        mtrl.Material.SetFloat(shaderPropID_GPUSkinning_ClipFPS, playingClip.fps);
        mtrl.Material.SetFloat(shaderPorpID_GPUSkinning_Time, time);
        mtrl.Material.SetFloat(shaderPropID_GPUSkinning_PixelSegmentation, playingClip.pixelSegmentation);
    }

    private void UpdateBoneMode(bool isEnforced)
    {
        if (mtrl != null && mtrl.Material != null)
        {
            if (boneMode == GPUSkinningBoneMode.MATRIX_ARRAY)
            {
                if (mtrl.Material.IsKeywordEnabled("GPU_SKINNING_TEXTURE_MATRIX") || isEnforced)
                {
                    mtrl.Material.EnableKeyword("GPU_SKINNING_MATRIX_ARRAY");
                    mtrl.Material.DisableKeyword("GPU_SKINNING_TEXTURE_MATRIX");
                }
            }
            else
            {

                if (mtrl.Material.IsKeywordEnabled("GPU_SKINNING_MATRIX_ARRAY") || isEnforced)
                {
                    mtrl.Material.DisableKeyword("GPU_SKINNING_MATRIX_ARRAY");
                    mtrl.Material.EnableKeyword("GPU_SKINNING_TEXTURE_MATRIX");
                }
            }
        }
    }

    private void SetIndividualDifferenceMode_None()
    {
        int numPlayers = players.Count;
        for(int i = 0; i < numPlayers; ++i)
        {
            players[i].Player.MeshRenderer.SetPropertyBlock(null);
            players[i].Player.MeshRenderer.additionalVertexStreams = null;
        }
        DestroyMaterialPropertyBlocks();
    }

    private void SetIndividualDifferenceMode_MaterialPropertyBlock()
    {
        InitMaterialPropertyBlocks();

        int numPlayers = players.Count;
        for (int i = 0; i < numPlayers; ++i)
        {
            players[i].Player.MeshRenderer.additionalVertexStreams = null;
        }
    }

    private void InitMaterialPropertyBlocks()
    {
        if(mpbs == null)
        {
            float clipLength = 0;
            int numClips = anim.clips.Length;
            for(int i = 0; i < numClips; ++i)
            {
                if(anim.clips[i].length > clipLength)
                {
                    clipLength = anim.clips[i].length;
                }
            }

            mpbs = new MaterialPropertyBlock[10];
            for(int i = 0; i < mpbs.Length; ++i)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetFloat(shaderPropID_GPUSkinning_IndividualDefference_MaterialPropertyBlock, Random.Range(0.0f, clipLength));
                mpbs[i] = mpb;
            }
        }
    }

    private void DestroyMaterialPropertyBlocks()
    {
        mpbs = null;
    }

    private class PlayerItem
    {
        public GPUSkinningPlayerMono player = null;
    }
}
