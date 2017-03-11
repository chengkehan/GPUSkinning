using UnityEngine;
using System.Collections;

/// <summary>
/// Switcher of Matrix Array and Matrix Texture
/// </summary>
public class GPUSkinning_PlayingMode : GPUSkinning_Component
{
    private int playMode = 0;

    private string playModeKey0 = "GPU_SKINNING_MATRIX_ARRAY";

    private string playModeKey1 = "GPU_SKINNING_MATRIX_TEXTURE";

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        SetPlayMode0();
    }

    public void OnGUI(ref Rect rect, int size)
    {
        if (gpuSkinning.matrixTexture.IsSupported())
        {
            if (GUI.Button(rect, "Switch Mode"))
            {
                if (IsPlayMode0())
                {
                    SetPlayMode1();
                }
                else
                {
                    SetPlayMode0();
                }
            }
            rect.y += size;
        }
    }

    private void SetPlayMode0()
    {
        gpuSkinning.model.newMtrl.EnableKeyword(playModeKey0);
        gpuSkinning.model.newMtrl.DisableKeyword(playModeKey1);
        gpuSkinning.joint.material.EnableKeyword(playModeKey0);
        gpuSkinning.joint.material.DisableKeyword(playModeKey1);
        playMode = 0;
    }

    private void SetPlayMode1()
    {
        if (gpuSkinning.matrixTexture.IsSupported())
        {
            gpuSkinning.model.newMtrl.EnableKeyword(playModeKey1);
            gpuSkinning.model.newMtrl.DisableKeyword(playModeKey0);
            gpuSkinning.joint.material.EnableKeyword(playModeKey1);
            gpuSkinning.joint.material.DisableKeyword(playModeKey0);
            playMode = 1;
        }
    }

    public bool IsPlayMode0()
    {
        return playMode == 0;
    }
}
