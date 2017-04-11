using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GPUSkinningPlayerJoint : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private int boneIndex = 0;

    [HideInInspector]
    [SerializeField]
    private string boneGUID = null;

    private Transform bone = null;

    public int BoneIndex
    {
        get
        {
            return boneIndex;
        }
    }

    public string BoneGUID
    {
        get
        {
            return boneGUID;
        }
    }

    public Transform Transform
    {
        get
        {
            return bone;
        }
    }

    private void Awake()
    {
        hideFlags = HideFlags.HideInInspector;
        this.bone = transform;
    }

    public void Init(int boneIndex, string boneGUID)
    {
        this.boneIndex = boneIndex;
        this.boneGUID = boneGUID;
    }
}
