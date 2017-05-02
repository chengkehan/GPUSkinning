using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerResources
{
    public GPUSkinningAnimation anim = null;

    public Mesh mesh = null;

    public Material mtrl = null;

    public Texture2D texture = null;

    public List<GPUSkinningPlayerMono> players = new List<GPUSkinningPlayerMono>();

    private GPUSkinningExecuteOncePerFrame executeOncePerFrame = new GPUSkinningExecuteOncePerFrame();

    private float time = 0;
    public float Time
    {
        get
        {
            return time;
        }
    }

    private static int shaderPropID_GPUSkinning_TextureMatrix = -1;

    private static int shaderPropID_GPUSkinning_NumPixelsPerFrame = 0;

    private static int shaderPropID_GPUSkinning_TextureSize = 0;

    private static int shaderPorpID_GPUSkinning_FrameIndex = 0;

    private static int shaderPropID_GPUSkinning_PixelSegmentation = 0;

    private static int shaderPropID_GPUSkinning_RootMotion = 0;

    private static int shaderPropID_GPUSkinning_RootMotionEnabled = 0;

    private static int shaderPropID_GPUSkinning_CrossFadeEnabled = 0;

    private static int shaderPropID_GPUSkinning_CrossFadeBlend = 0;

    private static int shaderPropID_GPUSkinning_PixelSegmentation_CrossFade = 0;

    private static int shaderPorpID_GPUSkinning_FrameIndex_CrossFade = 0;

    private static int shaderPropID_GPUSkinning_RootMotion_CrossFade = 0;

    private static int shaderPropID_GPUSkinning_RootMotionEnabled_CrossFade = 0;

    public GPUSkinningPlayerResources()
    {
        if (shaderPropID_GPUSkinning_TextureMatrix == -1)
        {
            shaderPropID_GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
            shaderPropID_GPUSkinning_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_NumPixelsPerFrame");
            shaderPropID_GPUSkinning_TextureSize = Shader.PropertyToID("_GPUSkinning_TextureSize");
            shaderPorpID_GPUSkinning_FrameIndex = Shader.PropertyToID("_GPUSkinning_FrameIndex");
            shaderPropID_GPUSkinning_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_PixelSegmentation");
            shaderPropID_GPUSkinning_RootMotion = Shader.PropertyToID("_GPUSkinning_RootMotion");
            shaderPropID_GPUSkinning_RootMotionEnabled = Shader.PropertyToID("_GPUSkinning_RootMotionEnabled");
            shaderPropID_GPUSkinning_CrossFadeEnabled = Shader.PropertyToID("_GPUSkinning_CrossFadeEnabled");
            shaderPropID_GPUSkinning_CrossFadeBlend = Shader.PropertyToID("_GPUSkinning_CrossFadeBlend");
            shaderPropID_GPUSkinning_PixelSegmentation_CrossFade = Shader.PropertyToID("_GPUSkinning_PixelSegmentation_CrossFade");
            shaderPorpID_GPUSkinning_FrameIndex_CrossFade = Shader.PropertyToID("_GPUSkinning_FrameIndex_CrossFade");
            shaderPropID_GPUSkinning_RootMotion_CrossFade = Shader.PropertyToID("_GPUSkinning_RootMotion_CrossFade");
            shaderPropID_GPUSkinning_RootMotionEnabled_CrossFade = Shader.PropertyToID("_GPUSkinning_RootMotionEnabled_CrossFade");
        }
    }

    public void Destroy()
    {
        anim = null;
        mesh = null;

        if (mtrl != null)
        {
            Object.Destroy(mtrl);
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

    public void Update(float deltaTime)
    {
        if (executeOncePerFrame.CanBeExecute())
        {
            executeOncePerFrame.MarkAsExecuted();
            mtrl.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);
            mtrl.SetFloat(shaderPropID_GPUSkinning_NumPixelsPerFrame, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/);
            mtrl.SetVector(shaderPropID_GPUSkinning_TextureSize, new Vector4(anim.textureWidth, anim.textureHeight, 0, 0));
            time += deltaTime;
        }
    }

    public void UpdatePlayingData(MaterialPropertyBlock mpb, GPUSkinningClip playingClip, int frameIndex, GPUSkinningFrame frame, bool rootMotionEnabled)
    {
        mpb.SetFloat(shaderPorpID_GPUSkinning_FrameIndex, frameIndex);
        mpb.SetFloat(shaderPropID_GPUSkinning_PixelSegmentation, playingClip.pixelSegmentation);
        mpb.SetFloat(shaderPropID_GPUSkinning_RootMotionEnabled, rootMotionEnabled ? 1 : -1);
        if (rootMotionEnabled)
        {
            Matrix4x4 rootMotionInv = frame.RootMotionInv(anim.rootBoneIndex);
            mpb.SetMatrix(shaderPropID_GPUSkinning_RootMotion, rootMotionInv);
        }
    }

    public void UpdateCrossFade(MaterialPropertyBlock mpb, GPUSkinningClip lastPlayedClip, int frameIndex, ref float crossFadeTime, ref float crossFadeProgress)
    {
        if(IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            mpb.SetFloat(shaderPropID_GPUSkinning_RootMotionEnabled_CrossFade, lastPlayedClip.rootMotionEnabled ? 1 : -1);
            if (lastPlayedClip.rootMotionEnabled)
            {
                mpb.SetMatrix(shaderPropID_GPUSkinning_RootMotion_CrossFade, lastPlayedClip.frames[frameIndex].RootMotionInv(anim.rootBoneIndex));
            }

            mpb.SetFloat(shaderPorpID_GPUSkinning_FrameIndex_CrossFade, frameIndex);
            mpb.SetFloat(shaderPropID_GPUSkinning_PixelSegmentation_CrossFade, lastPlayedClip.pixelSegmentation);
            mpb.SetFloat(shaderPropID_GPUSkinning_CrossFadeBlend, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime));
            mpb.SetFloat(shaderPropID_GPUSkinning_CrossFadeEnabled, 1);
        }
        else
        {
            mpb.SetFloat(shaderPropID_GPUSkinning_CrossFadeEnabled, -1);
        }
    }

    public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
    {
        return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
    }

    public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
    {
        return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
    }
}
