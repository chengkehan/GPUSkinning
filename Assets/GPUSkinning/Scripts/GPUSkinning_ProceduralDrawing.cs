using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Procedural Drawing
/// </summary>
public class GPUSkinning_ProceduralDrawing : GPUSkinning_Component
{
    private ComputeBuffer verticesComputeBuffer = null;

    private ComputeBuffer matricesComputeBuffer = null;

    private ComputeBuffer globalDataComputeBuffer = null;

    private ComputeBuffer modelDataComputeBuffer = null;

    private Material proceduralModelMaterial = null;

    private bool drawProceduralModel = false;

    private int numVertices_computeBuffer = 0;

    private int numProceduralInstances = 800;

    private int proceduralModelGap = 1;

    private int shaderPropID_Time = 0;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        if (!SystemInfo.supportsComputeShaders)
        {
            return;
        }

        shaderPropID_Time = Shader.PropertyToID("_GameTime");

        // Material
        Shader shader = Shader.Find("Unlit/ProceduralModel");
        if (!shader.isSupported)
        {
            return;
        }
        proceduralModelMaterial = new Material(shader);
        proceduralModelMaterial.mainTexture = gpuSkinning.model.newMtrl.mainTexture;

        // Vertices
        int[] indices = gpuSkinning.model.newMesh.triangles;
        Vector3[] vertices = gpuSkinning.model.newMesh.vertices;
        Vector4[] tangents = gpuSkinning.model.newMesh.tangents;
        Vector2[] uv = gpuSkinning.model.newMesh.uv;
        verticesComputeBuffer = new ComputeBuffer(indices.Length, GPUSkinning_ComputeShader_Vertex.size);
        var data = new GPUSkinning_ComputeShader_Vertex[indices.Length];
        for (int i = 0; i < indices.Length; ++i)
        {
            var item = new GPUSkinning_ComputeShader_Vertex();
            item.vertex = vertices[indices[i]];
            item.tangents = tangents[indices[i]];
            item.uv = uv[indices[i]];
            data[i] = item;
        }
        verticesComputeBuffer.SetData(data);
        numVertices_computeBuffer = data.Length;

        // Global Data
        var globalData = new GPUSkinning_CompueteShader_GlobalData();
        globalData.fps = gpuSkinning.model.boneAnimations[0].fps;
        globalData.animLength = gpuSkinning.model.boneAnimations[0].length;

        // Bone Animation Matrices
        List<GPUSkinning_ComputeShader_Matrix> cbMatricesList = new List<GPUSkinning_ComputeShader_Matrix>();
        int matIndex = 0;
        GPUSkinningUtil.ExtractBoneAnimMatrix(
            gpuSkinning, 
            gpuSkinning.model.boneAnimations[0], 
			(animMat, hierarchyMat) =>
            {
                var matData = new GPUSkinning_ComputeShader_Matrix();
                matData.mat = animMat;
                cbMatricesList.Add(matData);
                ++matIndex;
            },
            (frameIndex) =>
            {
                if (frameIndex == 0)
                {
                    globalData.oneFrameMatricesStride = matIndex;
                }
            }
        );

        matricesComputeBuffer = new ComputeBuffer(cbMatricesList.Count, GPUSkinning_ComputeShader_Matrix.size);
        matricesComputeBuffer.SetData(cbMatricesList.ToArray());

        // Global Data
        globalDataComputeBuffer = new ComputeBuffer(1, GPUSkinning_CompueteShader_GlobalData.size);
        globalDataComputeBuffer.SetData(new GPUSkinning_CompueteShader_GlobalData[] { globalData });

        // Procedural Model Data
        var modelData = new GPUSkinning_ComputeShader_Model[numProceduralInstances];
        int numProceduralModelsPerRow = (int)Mathf.Sqrt(numProceduralInstances);
        for (int i = 0; i < numProceduralInstances; ++i)
        {
            var aModelData = new GPUSkinning_ComputeShader_Model();
            int row = i / numProceduralModelsPerRow;
            int col = i - row * numProceduralModelsPerRow;
            aModelData.pos = new Vector3(-row * proceduralModelGap, -1, -col * proceduralModelGap);
            aModelData.time = Random.Range(0.0f, 10.0f);
            modelData[i] = aModelData;
        }
        modelDataComputeBuffer = new ComputeBuffer(numProceduralInstances, GPUSkinning_ComputeShader_Model.size);
        modelDataComputeBuffer.SetData(modelData);

        // Draw Procedural Model
        GPUSkinning_Camera.instance.onPostRender += Draw;
    }

    public override void Destroy()
    {
        base.Destroy();

        if (verticesComputeBuffer != null)
        {
            verticesComputeBuffer.Release();
            verticesComputeBuffer = null;
        }
        if (matricesComputeBuffer != null)
        {
            matricesComputeBuffer.Release();
            matricesComputeBuffer = null;
        }
        if (globalDataComputeBuffer != null)
        {
            globalDataComputeBuffer.Release();
            globalDataComputeBuffer = null;
        }
        if (modelDataComputeBuffer != null)
        {
            modelDataComputeBuffer.Release();
            modelDataComputeBuffer = null;
        }
        if (proceduralModelMaterial != null)
        {
            Object.Destroy(proceduralModelMaterial);
            proceduralModelMaterial = null;
        }
    }

    public void OnGUI(ref Rect rect, int size)
    {
        if (proceduralModelMaterial == null)
        {
			Color tempColor = GUI.color;
            GUI.color = Color.red;
            GUI.Label(rect, "Compute Shader is not supported!");
            GUI.color = tempColor;
            rect.y += size;
        }
        else
        {
            if (GUI.Button(rect, "Compute Shader"))
            {
                drawProceduralModel = !drawProceduralModel;
            }
            rect.y += size;
        }
    }

    private void Draw()
    {
        if (proceduralModelMaterial == null)
        {
            return;
        }

        if (!drawProceduralModel)
        {
            return;
        }

        proceduralModelMaterial.SetFloat(shaderPropID_Time, gpuSkinning.second);
        proceduralModelMaterial.SetBuffer("_VertCB", verticesComputeBuffer);
        proceduralModelMaterial.SetBuffer("_MatCB", matricesComputeBuffer);
        proceduralModelMaterial.SetBuffer("_GlobalCB", globalDataComputeBuffer);
        proceduralModelMaterial.SetBuffer("_ModelCB", modelDataComputeBuffer);
        proceduralModelMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, numVertices_computeBuffer, numProceduralInstances);
    }
}
