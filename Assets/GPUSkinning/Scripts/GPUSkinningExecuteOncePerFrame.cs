using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningExecuteOncePerFrame
{
    private int frameCount = -1;

    public bool CanBeExecute()
    {
        if (Application.isPlaying)
        {
            return frameCount != Time.frameCount;
        }
        else
        {
            return true;
        }
    }

    public void MarkAsExecuted()
    {
        if (Application.isPlaying)
        {
            frameCount = Time.frameCount;
        }
    }
}
