using UnityEngine;
using System.Collections;

/// <summary>
/// Skinning by Matrix Array
/// </summary>
public class GPUSkinning_MatrixArray : GPUSkinning_Component
{
    private int shaderPropID_Matrices = 0;

    private Matrix4x4[] matricesUniformBlock = null;

    private bool isCurrFrameIndexChanged = false;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        shaderPropID_Matrices = Shader.PropertyToID("_Matrices");

        matricesUniformBlock = new Matrix4x4[gpuSkinning.GetComponentInChildren<SkinnedMeshRenderer>().bones.Length];
    }

    public void Update(float second)
    {
        UpdateBoneAnimationMatrix(null, second);
        Play();
    }

    private void Play()
    {
        if (isCurrFrameIndexChanged)
        {
            isCurrFrameIndexChanged = false;

            int numBones = gpuSkinning.model.bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                matricesUniformBlock[i] = gpuSkinning.model.bones[i].animationMatrix;
            }
            // TODO: Use 3x4 matrix to transfer more data
            gpuSkinning.model.newMtrl.SetMatrixArray(shaderPropID_Matrices, matricesUniformBlock);
        }
    }

    private int currFrameIndex = -1;
    public void UpdateBoneAnimationMatrix(string animName, float time)
    {
        GPUSkinning_BoneAnimation boneAnimation = gpuSkinning.model.boneAnimations[0];//GetBoneAnimation(animName);
        int frameIndex = (int)((time * boneAnimation.fps) % (boneAnimation.length * boneAnimation.fps));
        if (currFrameIndex != frameIndex)
        {
            currFrameIndex = frameIndex;
            isCurrFrameIndexChanged = true;

            GPUSkinning_BoneAnimationFrame frame = boneAnimation.frames[frameIndex];

            UpdateBoneTransformMatrix(gpuSkinning.model.bones[gpuSkinning.model.rootBoneIndex], Matrix4x4.identity, frame);
        }
    }

    private void UpdateBoneTransformMatrix(GPUSkinning_Bone bone, Matrix4x4 parentMatrix, GPUSkinning_BoneAnimationFrame frame)
    {
        int index = BoneAnimationFrameIndexOf(frame, bone);
        Matrix4x4 mat = parentMatrix * frame.matrices[index];
        bone.animationMatrix = mat * bone.bindpose;

        GPUSkinning_Bone[] children = bone.children;
        int numChildren = children.Length;
        for (int i = 0; i < numChildren; ++i)
        {
            UpdateBoneTransformMatrix(children[i], mat, frame);
        }
    }

    private int BoneAnimationFrameIndexOf(GPUSkinning_BoneAnimationFrame frame, GPUSkinning_Bone bone)
    {
        GPUSkinning_Bone[] bones = frame.bones;
        int numBones = bones.Length;
        for (int i = 0; i < numBones; ++i)
        {
            if (bones[i] == bone)
            {
                return i;
            }
        }
        return -1;
    }
}
