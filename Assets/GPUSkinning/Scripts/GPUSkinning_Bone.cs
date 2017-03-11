using UnityEngine;
using System.Collections;

public class GPUSkinning_Bone
{
    public Transform transform = null;

    public Matrix4x4 bindpose;

    public GPUSkinning_Bone parent = null;

    public GPUSkinning_Bone[] children = null;

    public Matrix4x4 animationMatrix;

    public Matrix4x4 hierarchyMatrix;

    public string name = null;
}
