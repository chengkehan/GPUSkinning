using UnityEngine;
using System.Collections;

public class GPUSkinning_Terrain : GPUSkinning_Component
{
    private TerrainData terrainData = null;

    private Vector4 terrainSize;

    private int shaderPropID_TerrainTex = 0;

    private int shaderPropID_TerrainSize = 0;

    private int shaderPropID_TerrainPos = 0;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        shaderPropID_TerrainTex = Shader.PropertyToID("_TerrainTex");
        shaderPropID_TerrainSize = Shader.PropertyToID("_TerrainSize");
        shaderPropID_TerrainPos = Shader.PropertyToID("_TerrainPos");

        if (gpuSkinning.terrain != null)
        {
            terrainData = gpuSkinning.terrain.terrainData;
            terrainSize = terrainData.size;
        }

        if (gpuSkinning.terrain == null)
        {
            gpuSkinning.model.newMtrl.EnableKeyword("TERRAIN_HEIGHT_OFF");
            gpuSkinning.model.newMtrl.DisableKeyword("TERRAIN_HEIGHT_ON");
        }
        else
        {
            gpuSkinning.model.newMtrl.EnableKeyword("TERRAIN_HEIGHT_ON");
            gpuSkinning.model.newMtrl.DisableKeyword("TERRAIN_HEIGHT_OFF");
        }
    }

    public void Update()
    {
        if (gpuSkinning.terrain != null)
        {
            gpuSkinning.model.newMtrl.SetTexture(shaderPropID_TerrainTex, gpuSkinning.terrainTexture);
            gpuSkinning.model.newMtrl.SetVector(shaderPropID_TerrainSize, terrainSize);
            gpuSkinning.model.newMtrl.SetVector(shaderPropID_TerrainPos, gpuSkinning.terrain.transform.position);
        }
    }

    public void OnGUI(ref Rect rect, int size)
    {
        if (gpuSkinning.terrain != null)
        {
            if (GUI.Button(rect, "Terrain"))
            {
                gpuSkinning.terrain.enabled = !gpuSkinning.terrain.enabled;
            }
            rect.y += size;
        }
    }
}
