using UnityEngine;
using System.Collections;
using System.Security.Cryptography;

public class GPUSkinningUtil
{
    public static void MarkAllScenesDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
            DelayCall = () =>
            {
                UnityEditor.EditorApplication.delayCall -= DelayCall;
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            };
            UnityEditor.EditorApplication.delayCall += DelayCall;
        }
#endif
    }

    public static Texture2D CreateTexture2D(TextAsset textureRawData, GPUSkinningAnimation anim)
    {
        if(textureRawData == null || anim == null)
        {
            return null;
        }

        Texture2D texture = new Texture2D(anim.textureWidth, anim.textureHeight, TextureFormat.RGBAHalf, false, true);
        texture.name = "GPUSkinningTextureMatrix";
        texture.filterMode = FilterMode.Point;
        texture.LoadRawTextureData(textureRawData.bytes);
        texture.Apply(false, true);

        return texture;
    }

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

    public static string BoneHierarchyPath(GPUSkinningBone[] bones, int boneIndex)
    {
        if (bones == null || boneIndex < 0 || boneIndex >= bones.Length)
        {
            return null;
        }

        GPUSkinningBone bone = bones[boneIndex];
        string path = bone.name;
        while (bone.parentBoneIndex != -1)
        {
            bone = bones[bone.parentBoneIndex];
            path = bone.name + "/" + path;
        }
        return path;
    }

    public static string BoneHierarchyPath(GPUSkinningAnimation gpuSkinningAnimation, int boneIndex)
    {
        if(gpuSkinningAnimation == null)
        {
            return null;
        }

        return BoneHierarchyPath(gpuSkinningAnimation.bones, boneIndex);
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

    public static int NormalizeTimeToFrameIndex(GPUSkinningClip clip, float normalizedTime)
    {
        if(clip == null)
        {
            return 0;
        }

        normalizedTime = Mathf.Clamp01(normalizedTime);
        return (int)(normalizedTime * (clip.length * clip.fps - 1));
    }

    public static float FrameIndexToNormalizedTime(GPUSkinningClip clip, int frameIndex)
    {
        if(clip == null)
        {
            return 0;
        }

        int totalFrams = (int)(clip.fps * clip.length);
        frameIndex = Mathf.Clamp(frameIndex, 0, totalFrams - 1);
        return (float)frameIndex / (float)(totalFrams - 1);
    }
}
