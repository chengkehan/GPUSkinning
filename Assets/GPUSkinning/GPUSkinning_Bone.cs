using UnityEngine;
using System.Collections;

public class GPUSkinning_Bone
{
    public Transform transform = null;

    public Matrix4x4 bindpose;

    public GPUSkinning_Bone parent = null;

    public GPUSkinning_Bone[] children = null;

    public Matrix4x4 animationMatrix;

    public string name
    {
        get
        {
            return transform.gameObject.name;
        }
    }
}
