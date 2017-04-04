using UnityEngine;
using System.Collections;

public class GPUSkinningPlayer
{
    private GameObject go = null;

    private GPUSkinningAnimation anim = null;

    private Mesh mesh = null;

    private Material mtrl = null;

    private MeshRenderer mr = null;

    private MeshFilter mf = null;

    private float time = 0;

    private GPUSkinningClip playingClip = null;

    private int playingFrameIndex = -1;

    private Matrix4x4[] matricesUniformBlock = null;

    private int shaderPropID_GPUSkinning_MatrixArray = 0;

    public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningAnimation anim, Mesh mesh, Material mtrl)
    {
        go = attachToThisGo;
        this.anim = anim;
        this.mesh = mesh;
        this.mtrl = mtrl;

        mr = go.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = go.AddComponent<MeshRenderer>();
        }
        mf = go.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = go.AddComponent<MeshFilter>();
        }

        mr.sharedMaterial = mtrl;
        mf.sharedMesh = mesh;

        shaderPropID_GPUSkinning_MatrixArray = Shader.PropertyToID("_GPUSkinning_MatrixArray");
    }

    public void Play(string clipName)
    {
        GPUSkinningClip[] clips = anim.clips;
        int numClips = clips == null ? 0 : clips.Length;
        for(int i = 0; i < numClips; ++i)
        {
            if(clips[i].name == clipName)
            {
                playingClip = clips[i];
                return;
            }
        }

        Debug.LogError("Missing Clip:" + clipName);
    }

    public void Update(float timeDelta)
    {
        if (playingClip == null)
        {
            return;
        }

        int frameIndex = (int)((time * playingClip.fps) % (playingClip.length * playingClip.fps));
        if (playingFrameIndex != frameIndex)
        {
            playingFrameIndex = frameIndex;

            //
            GPUSkinningFrame frame = playingClip.frames[frameIndex];
            Matrix4x4[] frameMatrices = frame.matrices;
            GPUSkinningBone[] bones = anim.bones;
            int numBones = bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                GPUSkinningBone bone = bones[i];
                GPUSkinningBone currentBone = bone;
                bone.animationMatrix = frameMatrices[i] * bone.bindpose;
                while (currentBone.parentBoneIndex != -1)
                {
                    bone.animationMatrix = frameMatrices[currentBone.parentBoneIndex] * bone.animationMatrix;
                    currentBone = bones[currentBone.parentBoneIndex];
                }
            }

            //
            if (matricesUniformBlock == null)
            {
                matricesUniformBlock = new Matrix4x4[numBones];
            }
            for (int i = 0; i < numBones; ++i)
            {
                matricesUniformBlock[i] = bones[i].animationMatrix;
            }
            mtrl.SetMatrixArray(shaderPropID_GPUSkinning_MatrixArray, matricesUniformBlock);
        }

        time += timeDelta;
    }
}
