using UnityEngine;
using System.Collections;

public struct GPUSkinning_ComputeShader_Vertex
{
    public const int size = 36;

    public Vector3 vertex;

    public Vector4 tangents;

    public Vector2 uv;
}

public struct GPUSkinning_ComputeShader_Model
{
    public const int size = 16;

    public Vector3 pos;

    public float time;
}

public struct GPUSkinning_ComputeShader_Matrix
{
    public const int size = 64;

    public Matrix4x4 mat;
}

public struct GPUSkinning_CompueteShader_GlobalData
{
    public const int size = 12;

    public int oneFrameMatricesStride;

    public int fps;

    public float animLength;
}