using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GPUSkinningAnimEvent : System.IComparable<GPUSkinningAnimEvent>
{
    public int frameIndex = 0;

    public int eventId = 0;

    public int CompareTo(GPUSkinningAnimEvent other)
    {
        return frameIndex > other.frameIndex ? -1 : 1;
    }
}
