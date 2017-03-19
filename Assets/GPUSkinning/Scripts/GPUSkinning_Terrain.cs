using UnityEngine;
using System.Collections;

/// <summary>
/// Calculating the altitude of a model then adding the altitude's value to vertices' position in vertex shader.
/// As you know, doing altitude calculating in vertex shader is unreasonable, because of redundant computation, especially there are many vertices in a mesh.
/// So sometimes moving altitude calculating to App-Side is better.
/// </summary>
[System.Serializable]
public class GPUSkinning_Terrain : GPUSkinning_Component
{
    public Texture2D terrainTexture = null;

    public Terrain terrain = null;

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

        if (terrain != null)
        {
            terrainData = terrain.terrainData;
            terrainSize = terrainData.size;
        }

        if (terrain == null)
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
        if (terrain != null)
        {
            gpuSkinning.model.newMtrl.SetTexture(shaderPropID_TerrainTex, terrainTexture);
            gpuSkinning.model.newMtrl.SetVector(shaderPropID_TerrainSize, terrainSize);
            gpuSkinning.model.newMtrl.SetVector(shaderPropID_TerrainPos, terrain.transform.position);
        }
    }

    public void OnGUI(ref Rect rect, int size)
    {
        if (terrain != null)
        {
            if (GUI.Button(rect, "Terrain"))
            {
                terrain.enabled = !terrain.enabled;
            }
            rect.y += size;
        }
    }
}
