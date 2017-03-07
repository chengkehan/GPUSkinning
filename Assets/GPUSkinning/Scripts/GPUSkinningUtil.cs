using UnityEngine;
using System.Collections;

public class GPUSkinningUtil
{
    public static void InitAdditionalVertexStream(Mesh[] additionalVertexStreames, Mesh mesh)
    {
        Vector3[] newMeshVertices = mesh.vertices;
        for (int i = 0; i < additionalVertexStreames.Length; ++i)
        {
            Mesh m = new Mesh();
            float rnd = Random.Range(0.0f, 10.0f);
            Vector2[] uv2 = new Vector2[mesh.vertexCount];
            for (int j = 0; j < mesh.vertexCount; ++j)
            {
                Vector2 uv = Vector2.zero;
                uv.x = rnd;
                uv2[j] = uv;
            }
            m.vertices = newMeshVertices;
            m.uv2 = uv2;
            m.UploadMeshData(true);
            additionalVertexStreames[i] = m;
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

    public static void ExtractBoneAnimMatrix(GPUSkinning gpuSkinning, GPUSkinning_BoneAnimation boneAnimation, System.Action<Matrix4x4> boneAnimMatrixCB, System.Action<int> frameCB)
    {
        for (int frameIndex = 0; frameIndex < boneAnimation.frames.Length; ++frameIndex)
        {
            float second = (float)(frameIndex) / (float)boneAnimation.fps;
            gpuSkinning.matrixArray.UpdateBoneAnimationMatrix(null, second);
            int numBones = gpuSkinning.model.bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                Matrix4x4 animMat = gpuSkinning.model.bones[i].animationMatrix;
                boneAnimMatrixCB(animMat);
            }
            frameCB(frameIndex);
        }
    }
}
