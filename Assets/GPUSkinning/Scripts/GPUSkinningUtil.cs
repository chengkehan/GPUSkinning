using UnityEngine;
using System.Collections;

public class GPUSkinningUtil
{
    public static string BonesHierarchy(GPUSkinningAnimation gpuSkinningAnimation)
    {
        if(gpuSkinningAnimation == null || gpuSkinningAnimation.bones == null)
        {
            return null;
        }

        string str = string.Empty;
        BonesHierarchy_Internal(gpuSkinningAnimation, gpuSkinningAnimation.bones[gpuSkinningAnimation.rootBoneIndex], string.Empty, ref str);
        return str;
    }

    public static void BonesHierarchy_Internal(GPUSkinningAnimation gpuSkinningAnimation, GPUSkinningBone bone, string tabs, ref string str)
    {
        str += tabs + bone.name + "\n";

        int numChildren = bone.childrenBonesIndices == null ? 0 : bone.childrenBonesIndices.Length;
        for(int i = 0; i < numChildren; ++i)
        {
            BonesHierarchy_Internal(gpuSkinningAnimation, gpuSkinningAnimation.bones[bone.childrenBonesIndices[i]], tabs + "    ", ref str);
        }
    }

    public static Vector4[] ExtractBoneWeights(Mesh mesh)
    {
        Vector4[] tangents = new Vector4[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            BoneWeight boneWeight = mesh.boneWeights[i];
            tangents[i].x = boneWeight.boneIndex0;
            tangents[i].y = boneWeight.weight0;
            tangents[i].z = boneWeight.boneIndex1;
            tangents[i].w = boneWeight.weight1;
        }
        return tangents;
    }

	public static void ExtractBoneAnimMatrix(GPUSkinning gpuSkinning, GPUSkinning_BoneAnimation boneAnimation, System.Action<Matrix4x4, Matrix4x4> boneAnimMatrixCB, System.Action<int> frameCB)
    {
        for (int frameIndex = 0; frameIndex < boneAnimation.frames.Length; ++frameIndex)
        {
            float second = (float)(frameIndex) / (float)boneAnimation.fps;
            gpuSkinning.matrixArray.UpdateBoneAnimationMatrix(null, second);
            int numBones = gpuSkinning.model.bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
				boneAnimMatrixCB(gpuSkinning.model.bones[i].animationMatrix, gpuSkinning.model.bones[i].hierarchyMatrix);
            }
            frameCB(frameIndex);
        }
    }
}
