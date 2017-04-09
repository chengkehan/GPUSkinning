using UnityEngine;
using System.Collections;

[System.Serializable]
public class GPUSkinningBone
{
	[System.NonSerialized]
	public Transform transform = null;

	public Matrix4x4 bindpose;

	public int parentBoneIndex = -1;

	public int[] childrenBonesIndices = null;

	[System.NonSerialized]
	public Matrix4x4 animationMatrix;

	public string name = null;

    public bool isExposed = false;
}
