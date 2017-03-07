using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

public class GPUSkinning : MonoBehaviour
{
    [Header("Spawn")]
    public Transform[] spawnPoints = null;

    [Header("LOD")]
    public Mesh lodMesh = null;

    [Header("Terrain Setting")]
    public Texture2D terrainTexture = null;
    public Terrain terrain = null;

    public GPUSkinning_ProceduralDrawing proceduralDrawing = null;

    public GPUSkinning_LOD lod = null;

    public GPUSkinning_Instancing instancing = null;

    public GPUSkinning_Terrain hill = null;

    public GPUSkinning_PlayingMode playingMode = null;

    public GPUSkinning_MatrixTexture matrixTexture = null;

    public GPUSkinning_MatrixArray matrixArray = null;

    public GPUSkinning_Model model = null;

    private void Start()
    {

        model = new GPUSkinning_Model();
        model.Init(this);

        matrixArray = new GPUSkinning_MatrixArray();
        matrixArray.Init(this);

        matrixTexture = new GPUSkinning_MatrixTexture();
        matrixTexture.Init(this);

        playingMode = new GPUSkinning_PlayingMode();
        playingMode.Init(this);

        hill = new GPUSkinning_Terrain();
        hill.Init(this);

        instancing = new GPUSkinning_Instancing();
        instancing.Init(this);

        proceduralDrawing = new GPUSkinning_ProceduralDrawing();
        proceduralDrawing.Init(this);

        lod = new GPUSkinning_LOD();
        lod.Init(this);

        model.PostInit();
    }

    private float second = 0.0f;
    private void Update()
    {
        lod.Update();

        if (playingMode.IsPlayMode0())
        {
            matrixArray.Update(second);
            second += Time.deltaTime;
        }
        else
        {
            matrixTexture.Update();
        }

        hill.Update();
    }

    private void OnDestroy()
    {
        DestroyGPUSkinningComponent(ref model);
        DestroyGPUSkinningComponent(ref matrixArray);
        DestroyGPUSkinningComponent(ref matrixTexture);
        DestroyGPUSkinningComponent(ref proceduralDrawing);
        DestroyGPUSkinningComponent(ref lod);
        DestroyGPUSkinningComponent(ref instancing);
        DestroyGPUSkinningComponent(ref hill);
        DestroyGPUSkinningComponent(ref playingMode);
    }

    private void DestroyGPUSkinningComponent<T>(ref T component) where T : GPUSkinning_Component
    {
        if(component != null)
        {
            component.Destroy();
            component = null;
        }
    }

    private void OnGUI()
    {
        int btnSize = Screen.height / 6;
        Rect btnRect = new Rect(0, 0, btnSize * 2, btnSize);

        playingMode.OnGUI(ref btnRect, btnSize);

        instancing.OnGUI(ref btnRect, btnSize);

        proceduralDrawing.OnGUI(ref btnRect, btnSize);

        hill.OnGUI(ref btnRect, btnSize);
    }
}
