using UnityEngine;
using System.Collections;

public class GPUSkinning_Component
{
    protected GPUSkinning gpuSkinning = null;

    public virtual void Init(GPUSkinning gpuSkinning)
    {
        this.gpuSkinning = gpuSkinning;
    }

    public virtual void Destroy()
    {
        gpuSkinning = null;
    }
}
