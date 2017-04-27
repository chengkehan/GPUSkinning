using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerMaterial
{
    private int frameCount = -1;

    private Material mtrl = null;
    public Material Material
    {
        get
        {
            return mtrl;
        }
    }

    public GPUSkinningPlayerMaterial(Material mtrl)
    {
        this.mtrl = mtrl;
    }

    public bool MaterialCanBeSetData()
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

    public void MarkMaterialAsSet()
    {
        if (Application.isPlaying)
        {
            frameCount = Time.frameCount;
        }
    }

    public void Destroy()
    {
        if(mtrl != null)
        {
            Object.DestroyImmediate(mtrl);
            mtrl = null;
        }
    }
}
