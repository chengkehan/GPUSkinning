using UnityEngine;
using System.Collections;

/// <summary>
/// GPU Instancing
/// </summary>
public class GPUSkinning_Instancing : GPUSkinning_Component
{
    private int shaderPropID_mpb_time = 0;

    private MaterialPropertyBlock[] gpuInstancing_mpbs = null;

	private float[] mpb_values = null;

    private int isGPUInstancingSupported = -1;

    private bool isGPUInstancingOn = false;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        shaderPropID_mpb_time = Shader.PropertyToID("_mpb_time");

        if (CheckGPUInstancingIsSupported())
        {
            gpuSkinning.model.newMtrl.shader.maximumLOD = 200;
            gpuSkinning.joint.material.shader.maximumLOD = 200;
            SetGPUInstancingMaterialPropertyBlock();
        }
        else
        {
            gpuSkinning.model.newMtrl.shader.maximumLOD = 100;
            gpuSkinning.joint.material.shader.maximumLOD = 100;
            ClearGPUInstancingMaterialPropertyBlock();
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        gpuInstancing_mpbs = null;
		mpb_values = null;
    }

    public bool IsGPUInstancingOn()
    {
        return isGPUInstancingOn;
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
			Color tempColor = GUI.color;
            GUI.color = Color.red;
            GUI.Label(rect, "GPU Instancing is not supported!");
            GUI.color = tempColor;
            rect.y += size;
        }
    }

    private void SwitchGPUInstancing()
    {
        if (CheckGPUInstancingIsSupported())
        {
            if (gpuSkinning.model.newMtrl.shader.maximumLOD == 200)
            {
                gpuSkinning.joint.material.shader.maximumLOD = 100;
                gpuSkinning.model.newMtrl.shader.maximumLOD = 100;
                ClearGPUInstancingMaterialPropertyBlock();
            }
            else
            {
                gpuSkinning.joint.material.shader.maximumLOD = 200;
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
			mpb_values = new float[gpuInstancing_mpbs.Length];
            for (int i = 0; i < gpuInstancing_mpbs.Length; ++i)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
				float rndValue = Random.value * 10;
				mpb.SetFloat(shaderPropID_mpb_time, rndValue);
                gpuInstancing_mpbs[i] = mpb;
				mpb_values[i] = rndValue;
            }
        }

        if (gpuSkinning.model.spawnObjects != null)
        {
            foreach (var obj in gpuSkinning.model.spawnObjects)
            {
				int rndIndex = Random.Range(0, gpuInstancing_mpbs.Length);
				obj.mr.SetPropertyBlock(gpuInstancing_mpbs[rndIndex]);
				obj.timeOffset_instancingOn = mpb_values[rndIndex];
            }
        }

        isGPUInstancingOn = true;
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

        isGPUInstancingOn = false;
    }
}
