using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

public class GPUSkinning : MonoBehaviour
{
    public GPUSkinning_ProceduralDrawing proceduralDrawing = null;

    public GPUSkinning_LOD lod = null;

    public GPUSkinning_Instancing instancing = null;

    public GPUSkinning_Terrain terrain = null;

    public GPUSkinning_PlayingMode playingMode = null;

    public GPUSkinning_MatrixTexture matrixTexture = null;

    public GPUSkinning_MatrixArray matrixArray = null;

    public GPUSkinning_Model model = null;

    public GPUSkinning_Joint joint = null;

    [System.NonSerialized]
    public float second = 0.0f;

    private void Start()
    {
        model.Init(this);

        matrixArray = new GPUSkinning_MatrixArray();
        matrixArray.Init(this);

        matrixTexture = new GPUSkinning_MatrixTexture();
        matrixTexture.Init(this);

        joint.Init(this);

        playingMode = new GPUSkinning_PlayingMode();
        playingMode.Init(this);

        terrain.Init(this);

        proceduralDrawing = new GPUSkinning_ProceduralDrawing();
        proceduralDrawing.Init(this);

        lod.Init(this);

        instancing = new GPUSkinning_Instancing();
        instancing.Init(this);

        model.PostInit();
    }
    
    private void Update()
    {
        model.Update();

        lod.Update();

        if (playingMode.IsPlayMode0())
        {
            matrixArray.Update();
        }
        else
        {
            matrixTexture.Update();
        }

        joint.Update();

        terrain.Update();

        second += Time.deltaTime;
    }

    private void OnDestroy()
    {
        DestroyGPUSkinningComponent(ref model);
        DestroyGPUSkinningComponent(ref matrixArray);
        DestroyGPUSkinningComponent(ref matrixTexture);
        DestroyGPUSkinningComponent(ref proceduralDrawing);
        DestroyGPUSkinningComponent(ref lod);
        DestroyGPUSkinningComponent(ref instancing);
        DestroyGPUSkinningComponent(ref terrain);
        DestroyGPUSkinningComponent(ref playingMode);
        DestroyGPUSkinningComponent(ref joint);
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
        Rect btnRect = new Rect(0, 0, btnSize, btnSize);

        playingMode.OnGUI(ref btnRect, btnSize);

        instancing.OnGUI(ref btnRect, btnSize);

        proceduralDrawing.OnGUI(ref btnRect, btnSize);

        terrain.OnGUI(ref btnRect, btnSize);

        joint.OnGUI(ref btnRect, btnSize);
    }
}
