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

    private static int shaderPropID_GPUSkinning_TextureMatrix = -1;

    private static int shaderPropID_GPUSkinning_NumPixelsPerFrame = 0;

    private static int shaderPropID_GPUSkinning_TextureSize = 0;

    private static int shaderPropID_GPUSkinning_ClipLength = 0;

    private static int shaderPropID_GPUSkinning_ClipFPS = 0;

    private static int shaderPorpID_GPUSkinning_Time = 0;

    private static int shaderPropID_GPUSkinning_PixelSegmentation = 0;

    private static int shaderPropID_GPUSkinning_RootMotionInv = 0;

    private static int shaderPropID_GPUSkinning_RootMotionEnabled = 0;

    public GPUSkinningPlayerResources()
    {
        if(shaderPropID_GPUSkinning_TextureMatrix == -1)
        {
            shaderPropID_GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
            shaderPropID_GPUSkinning_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_NumPixelsPerFrame");
            shaderPropID_GPUSkinning_TextureSize = Shader.PropertyToID("_GPUSkinning_TextureSize");
            shaderPropID_GPUSkinning_ClipLength = Shader.PropertyToID("_GPUSkinning_ClipLength");
            shaderPropID_GPUSkinning_ClipFPS = Shader.PropertyToID("_GPUSkinning_ClipFPS");
            shaderPorpID_GPUSkinning_Time = Shader.PropertyToID("_GPUSkinning_Time");
            shaderPropID_GPUSkinning_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_PixelSegmentation");
            shaderPropID_GPUSkinning_RootMotionInv = Shader.PropertyToID("_GPUSkinning_RootMotionInv");
            shaderPropID_GPUSkinning_RootMotionEnabled = Shader.PropertyToID("_GPUSkinning_RootMotionEnabled");
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
    }

    public void UpdateMaterial()
    {
        if(mtrl.MaterialCanBeSetData())
        {
            mtrl.MarkMaterialAsSet();
            mtrl.Material.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);
            mtrl.Material.SetFloat(shaderPropID_GPUSkinning_NumPixelsPerFrame, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/);
            mtrl.Material.SetVector(shaderPropID_GPUSkinning_TextureSize, new Vector4(anim.textureWidth, anim.textureHeight, 0, 0));
        }
    }

    public void UpdatePlayingData(MaterialPropertyBlock mpb, GPUSkinningClip playingClip, float time, GPUSkinningFrame frame, bool rootMotionEnabled)
    {
        mpb.SetFloat(shaderPropID_GPUSkinning_ClipLength, playingClip.length);
        mpb.SetFloat(shaderPropID_GPUSkinning_ClipFPS, playingClip.fps);
        mpb.SetFloat(shaderPorpID_GPUSkinning_Time, time);
        mpb.SetFloat(shaderPropID_GPUSkinning_PixelSegmentation, playingClip.pixelSegmentation);
        mpb.SetFloat(shaderPropID_GPUSkinning_RootMotionEnabled, rootMotionEnabled ? 1 : -1);
        if (rootMotionEnabled)
        {
            Matrix4x4 rootMotionInv = frame.RootMotionInv(anim.rootBoneIndex);

            Matrix4x4 bakeIntoPoseMat = Matrix4x4.identity;
            if(anim.rootMotionPositionXBakeIntoPose)
            {
                bakeIntoPoseMat[0, 3] = frame.rootPosition.x;
            }
            bakeIntoPoseMat[0, 3] += anim.rootMotionPositionXOffset;
            if(anim.rootMotionPositionYBakeIntoPose)
            {
                bakeIntoPoseMat[1, 3] = frame.rootPosition.y;
            }
            bakeIntoPoseMat[1, 3] += anim.rootMotionPositionYOffset;
            if(anim.rootMotionPositionZBakeIntoPose)
            {
                bakeIntoPoseMat[2, 3] = frame.rootPosition.z;
            }
            bakeIntoPoseMat[2, 3] += anim.rootMotionPositionZOffset;

            mpb.SetMatrix(shaderPropID_GPUSkinning_RootMotionInv, bakeIntoPoseMat * rootMotionInv);
        }
    }
}
