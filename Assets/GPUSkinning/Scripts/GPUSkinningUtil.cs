using UnityEngine;
using System.Collections;
using System.Security.Cryptography;

public class GPUSkinningUtil
{
    public static string BonesHierarchyTree(GPUSkinningAnimation gpuSkinningAnimation)
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

    public static string BoneHierarchyPath(GPUSkinningAnimation gpuSkinningAnimation, int boneIndex)
    {
        if(gpuSkinningAnimation == null || gpuSkinningAnimation.bones == null)
        {
            return null;
        }

        GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
        if(boneIndex < 0 || boneIndex >= bones.Length)
        {
            return null;
        }

        GPUSkinningBone bone = bones[boneIndex];
        string path = bone.name;
        while(bone.parentBoneIndex != -1)
        {
            bone = bones[bone.parentBoneIndex];
            path = bone.name + "/" + path;
        }
        return path;
    }

    public static string MD5(string input)
    {
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] bytValue, bytHash;
        bytValue = System.Text.Encoding.UTF8.GetBytes(input);
        bytHash = md5.ComputeHash(bytValue);
        md5.Clear();
        string sTemp = string.Empty;
        for (int i = 0; i < bytHash.Length; i++)
        {
            sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
        }
        return sTemp.ToLower();
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
