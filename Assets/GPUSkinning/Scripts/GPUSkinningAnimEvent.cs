using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GPUSkinningAnimEvent : System.IComparable<GPUSkinningAnimEvent>
{
    public float normalizedTime = 0;

    public int eventId = 0;

    public int CompareTo(GPUSkinningAnimEvent other)
    {
        return normalizedTime > other.normalizedTime ? -1 : 1;
    }
}
