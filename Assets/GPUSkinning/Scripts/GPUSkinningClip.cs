using UnityEngine;
using System.Collections;

[System.Serializable]
public class GPUSkinningClip
{
    public string name = null;

    public float length = 0.0f;

    public int fps = 0;

    public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;

    public GPUSkinningFrame[] frames = null;

    public int pixelSegmentation = 0;

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
