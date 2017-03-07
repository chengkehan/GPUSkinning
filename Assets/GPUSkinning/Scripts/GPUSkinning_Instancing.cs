using UnityEngine;
using System.Collections;

/// <summary>
/// GPU Instancing
/// </summary>
public class GPUSkinning_Instancing : GPUSkinning_Component
{
    private int shaderPropID_mpb_time = 0;

    private MaterialPropertyBlock[] gpuInstancing_mpbs = null;

    private int isGPUInstancingSupported = -1;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        shaderPropID_mpb_time = Shader.PropertyToID("_mpb_time");

        if (CheckGPUInstancingIsSupported())
        {
            gpuSkinning.model.newMtrl.shader.maximumLOD = 200;
            SetGPUInstancingMaterialPropertyBlock();
        }
        else
        {
            gpuSkinning.model.newMtrl.shader.maximumLOD = 100;
            ClearGPUInstancingMaterialPropertyBlock();
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        gpuInstancing_mpbs = null;
    }

    public void OnGUI(ref Rect rect, int size)
    {
        if (IsGPUInstancingSupported())
        {
            if (GUI.Button(rect, "GPU Instancing"))
            {
                SwitchGPUInstancing();
            }
            rect.y += size;
        }
        else
        {
            Color oldColor = GUI.color;
            GUI.color = Color.red;
            GUI.Label(rect, "GPU Instancing is not supported!");
            GUI.color = oldColor;
            rect.y += size;
        }
    }

    private void SwitchGPUInstancing()
    {
        if (CheckGPUInstancingIsSupported())
        {
            if (gpuSkinning.model.newMtrl.shader.maximumLOD == 200)
            {
                gpuSkinning.model.newMtrl.shader.maximumLOD = 100;
                ClearGPUInstancingMaterialPropertyBlock();
            }
            else
            {
                gpuSkinning.model.newMtrl.shader.maximumLOD = 200;
                SetGPUInstancingMaterialPropertyBlock();
            }
        }
    }

    private bool CheckGPUInstancingIsSupported()
    {
        if (IsGPUInstancingSupported())
        {
            return true;
        }
        else
        {
            Debug.LogError("GPU Instancing is not supported!");
            return false;
        }
    }

    private bool IsGPUInstancingSupported()
    {
        if (isGPUInstancingSupported == -1)
        {
            isGPUInstancingSupported = SystemInfo.supportsInstancing ? 1 : 0;
        }
        return isGPUInstancingSupported == 1;
    }

    private void SetGPUInstancingMaterialPropertyBlock()
    {
        if (gpuInstancing_mpbs == null)
        {
            gpuInstancing_mpbs = new MaterialPropertyBlock[50];
            for (int i = 0; i < gpuInstancing_mpbs.Length; ++i)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetFloat(shaderPropID_mpb_time, Random.value * 10);
                gpuInstancing_mpbs[i] = mpb;
            }
        }

        if (gpuSkinning.model.spawnObjects != null)
        {
            foreach (var obj in gpuSkinning.model.spawnObjects)
            {
                obj.mr.SetPropertyBlock(gpuInstancing_mpbs[Random.Range(0, gpuInstancing_mpbs.Length)]);
            }
        }
    }

    private void ClearGPUInstancingMaterialPropertyBlock()
    {
        if (gpuSkinning.model.spawnObjects != null)
        {
            foreach (var obj in gpuSkinning.model.spawnObjects)
            {
                obj.mr.SetPropertyBlock(null);
            }
        }
    }
}
