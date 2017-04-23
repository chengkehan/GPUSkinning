using UnityEngine;
using System.Collections;

public class GPUSkinningAnimation : ScriptableObject
{
    public string guid = null;

    public string name = null;

    public GPUSkinningBone[] bones = null;

    public int rootBoneIndex = 0;

    public GPUSkinningClip[] clips = null;

    public Bounds bounds;

    public int textureWidth = 0;

    public int textureHeight = 0;

    public bool rootMotionEnabled = false;

    public bool rootMotionPositionXBakeIntoPose = false;

    public bool rootMotionPositionYBakeIntoPose = false;

    public bool rootMotionPositionZBakeIntoPose = false;

    public bool rootMotionRotationBakeIntoPose = false;

    public float rootMotionPositionXOffset = 0;

    public float rootMotionPositionYOffset = 0;

    public float rootMotionPositionZOffset = 0;

    public float rootMotionRotationOffset = 0;
}
