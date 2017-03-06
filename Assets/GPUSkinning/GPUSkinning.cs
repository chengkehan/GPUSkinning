using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

public class GPUSkinning : MonoBehaviour
{
    public Transform[] spawnPoints = null;

    private SkinnedMeshRenderer smr = null;

    private Mesh mesh = null;

    private GPUSkinning_Bone[] bones = null;

    private int rootBoneIndex = 0;

    private MeshFilter mf = null;

    private MeshRenderer mr = null;

    private Material newMtrl = null;

    private Mesh newMesh = null;

    private GPUSkinning_BoneAnimation[] boneAnimations = null;

    private GPUSkinning_SpawnObject[] spawnObjects = null;

    private void Start()
    {
        shaderPropID_Matrices = Shader.PropertyToID("_Matrices");
        shaderPropID_MatricesTex = Shader.PropertyToID("_MatricesTex");
        shaderPropID_MatricesTexSize = Shader.PropertyToID("_MatricesTexSize");
        shaderPropID_AnimLength = Shader.PropertyToID("_AnimLength");
        shaderPropID_AnimFPS = Shader.PropertyToID("_AnimFPS");
        shaderPropID_TerrainTex = Shader.PropertyToID("_TerrainTex");
        shaderPropID_TerrainSize = Shader.PropertyToID("_TerrainSize");
        shaderPropID_TerrainPos = Shader.PropertyToID("_TerrainPos");
        shaderPropID_mpb_time = Shader.PropertyToID("_mpb_time");

        smr = GetComponentInChildren<SkinnedMeshRenderer>();
        mesh = smr.sharedMesh;

        // 初始化骨骼对象
        int numBones = smr.bones.Length;
        bones = new GPUSkinning_Bone[numBones];
        for (int i = 0; i < numBones; ++i)
        {
            GPUSkinning_Bone bone = new GPUSkinning_Bone();
            bones[i] = bone;
            bone.transform = smr.bones[i];
            bone.bindpose = mesh.bindposes[i]/*smr to bone*/;
        }

        matricesUniformBlock = new Matrix4x4[numBones];

        // 构建骨骼的层级结构
        for (int i = 0; i < numBones; ++i)
        {
            if (bones[i].transform == smr.rootBone)
            {
                rootBoneIndex = i;
                break;
            }
        }
        System.Action<GPUSkinning_Bone> CollectChildren = null;
        CollectChildren = (currentBone) =>
        {
            List<GPUSkinning_Bone> children = new List<GPUSkinning_Bone>();
            for (int j = 0; j < currentBone.transform.childCount; ++j)
            {
                Transform childTransform = currentBone.transform.GetChild(j);
                GPUSkinning_Bone childBone = GetBoneByTransform(childTransform);
                if (childBone != null)
                {
                    childBone.parent = currentBone;
                    children.Add(childBone);
                    CollectChildren(childBone);
                }
            }
            currentBone.children = children.ToArray();
        };
        CollectChildren(bones[rootBoneIndex]);
        
        // New MeshFilter MeshRenderer
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        newMtrl = new Material(Shader.Find("Unlit/GPUSkinning"));
        newMtrl.CopyPropertiesFromMaterial(smr.sharedMaterial);
        mr.sharedMaterial = newMtrl;

        // New Mesh
        newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.tangents = ExtractBoneWeights(mesh);
        newMesh.uv = mesh.uv;
        newMesh.triangles = mesh.triangles;
        mf.sharedMesh = newMesh;

        // 为每个角色生成差异化数据
        if (IsMatricesTextureSupported())
        {
            InitAdditionalVertexStream(newMesh);
            mr.additionalVertexStreams = RandomAdditionalVertexStream();
        }

#if UNITY_EDITOR
        // 从 Unity 的 Animation 中提取骨骼动画所需要的数据
        int boneAnimationsCount = 0;
        boneAnimations = new GPUSkinning_BoneAnimation[GetComponent<Animator>().runtimeAnimatorController.animationClips.Length];
        foreach (AnimationClip animClip in GetComponent<Animator>().runtimeAnimatorController.animationClips)
        {
            GPUSkinning_BoneAnimation boneAnimation = ScriptableObject.CreateInstance<GPUSkinning_BoneAnimation>();
            boneAnimation.fps = 60;
            boneAnimation.animName = animClip.name;
            boneAnimation.frames = new GPUSkinning_BoneAnimationFrame[(int)(animClip.length * boneAnimation.fps)];
            boneAnimation.length = animClip.length;
            boneAnimations[boneAnimationsCount++] = boneAnimation;

            for (int frameIndex = 0; frameIndex < boneAnimation.frames.Length; ++frameIndex)
            {
                GPUSkinning_BoneAnimationFrame frame = new GPUSkinning_BoneAnimationFrame();
                boneAnimation.frames[frameIndex] = frame;
                float second = (float)(frameIndex) / (float)boneAnimation.fps;

                List<GPUSkinning_Bone> bones2 = new List<GPUSkinning_Bone>();
                List<Matrix4x4> matrices = new List<Matrix4x4>();
                List<string> bonesHierarchyNames = null; if (boneAnimation.bonesHierarchyNames == null) { bonesHierarchyNames = new List<string>(); }
                EditorCurveBinding[] curvesBinding = AnimationUtility.GetCurveBindings(animClip);
                foreach (var curveBinding in curvesBinding)
                {
                    GPUSkinning_Bone bone = GetBoneByHierarchyName(curveBinding.path);

                    if (bones2.Contains(bone))
                    {
                        continue;
                    }
                    bones2.Add(bone);

                    if (bonesHierarchyNames != null)
                    {
                        bonesHierarchyNames.Add(GetBoneHierarchyName(bone));
                    }

                    AnimationCurve curveRX = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalRotation.x");
                    AnimationCurve curveRY = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalRotation.y");
                    AnimationCurve curveRZ = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalRotation.z");
                    AnimationCurve curveRW = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalRotation.w");

                    AnimationCurve curvePX = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalPosition.x");
                    AnimationCurve curvePY = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalPosition.y");
                    AnimationCurve curvePZ = AnimationUtility.GetEditorCurve(animClip, curveBinding.path, curveBinding.type, "m_LocalPosition.z");

                    float curveRX_v = curveRX.Evaluate(second);
                    float curveRY_v = curveRY.Evaluate(second);
                    float curveRZ_v = curveRZ.Evaluate(second);
                    float curveRW_v = curveRW.Evaluate(second);

                    float curvePX_v = curvePX.Evaluate(second);
                    float curvePY_v = curvePY.Evaluate(second);
                    float curvePZ_v = curvePZ.Evaluate(second);

                    Vector3 translation = new Vector3(curvePX_v, curvePY_v, curvePZ_v);
                    Quaternion rotation = new Quaternion(curveRX_v, curveRY_v, curveRZ_v, curveRW_v);
                    NormalizeQuaternion(ref rotation);
                    matrices.Add(
                        Matrix4x4.TRS(translation, rotation, Vector3.one)
                    );
                }

                frame.bones = bones2.ToArray();
                frame.matrices = matrices.ToArray();
                if (boneAnimation.bonesHierarchyNames == null)
                {
                    boneAnimation.bonesHierarchyNames = bonesHierarchyNames.ToArray();
                }
            }
        }
        AssetDatabase.CreateAsset(boneAnimations[0], "Assets/GPUSkinning/Resources/anim0.asset");
        AssetDatabase.Refresh();
#else
		// 直接读取序列化的骨骼动画数据
        boneAnimations = new GPUSkinning_BoneAnimation[] { Resources.Load("anim0") as GPUSkinning_BoneAnimation };
        foreach(var boneAnimation in boneAnimations)
        {
            foreach(var frame in boneAnimation.frames)
            {
				int numBones2 = boneAnimation.bonesHierarchyNames.Length;
                frame.bones = new GPUSkinning_Bone[numBones2];
                for(int i = 0; i < numBones2; ++i)
                {
					frame.bones[i] = GetBoneByHierarchyName(boneAnimation.bonesHierarchyNames[i]);
                }
            }
        }
#endif

        // 创建出更多的角色模型
        if (spawnPoints != null)
        {
            List<GPUSkinning_SpawnObject> list = new List<GPUSkinning_SpawnObject>();
            for (int i = 0; i < spawnPoints.Length; ++i)
            {
                for (int j = 0; j < spawnPoints[i].childCount; ++j)
                {
                    GPUSkinning_SpawnObject spawnObject = new GPUSkinning_SpawnObject();
                    list.Add(spawnObject);
                    spawnObject.transform = spawnPoints[i].GetChild(j);
                    spawnObject.mf = spawnObject.transform.gameObject.AddComponent<MeshFilter>();
                    spawnObject.mr = spawnObject.transform.gameObject.AddComponent<MeshRenderer>();
                    spawnObject.mr.sharedMaterial = newMtrl;
                    spawnObject.mf.sharedMesh = newMesh;
                    if (additionalVertexStreames != null)
                    {
                        // 当开启 GPUInstancing 时，这个是不需要的，偷懒就直接设置了
                        spawnObject.mr.additionalVertexStreams = RandomAdditionalVertexStream();
                    }
                }
            }
            spawnObjects = list.ToArray();
        }

        // 销毁并暂停 Unity 的 Animator
        GameObject.Destroy(transform.FindChild("pelvis").gameObject);
        GameObject.Destroy(transform.FindChild("mutant_mesh").gameObject);
        Object.Destroy(gameObject.GetComponent<Animator>());

        smr.enabled = false;

        //PrintBones();

        CreateMatricesTexture();

        BakeAnimationsToTexture();

        SetPlayMode0();

        InitTerrain();

        SetTerrainHeightSwitch();

        InitGPUInstancing();

        InitComputeBuffer(newMesh);

        SetupLodMesh();

        newMesh.UploadMeshData(true);
    }

    private float second = 0.0f;
    private void Update()
    {
        UpdateLodBoundingSpheres();

        if (IsPlayMode0())
        {
            UpdateBoneAnimationMatrix(null, second);
            Play();
            second += Time.deltaTime;
        }
        else
        {
            UpdateMatricesTextureUniforms();
        }

        UpdateTerrainUniforms();
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // LOD Mesh

    [Header("LOD Mesh")]

    public Mesh lodMesh = null;

    private Mesh newLodMesh = null;

    private CullingGroup lodCullingGroup = null;

    private BoundingSphere[] lodBoundingSpheres = null;

    private Mesh[] lodAdditionalVertexStreames = null;

    private Mesh RandomLodAdditionalVertexStream()
    {
        return lodAdditionalVertexStreames == null ? null : lodAdditionalVertexStreames[Random.Range(0, lodAdditionalVertexStreames.Length)];
    }

    private void UpdateLodBoundingSpheres()
    {
        if(lodBoundingSpheres != null && spawnObjects != null)
        {
            int length = lodBoundingSpheres.Length;
            for (int i = 0; i < length; ++i)
            {
                BoundingSphere bound = lodBoundingSpheres[i];
                bound.position = spawnObjects[i].transform.position;
                lodBoundingSpheres[i] = bound;
            }
        }
    }

    private void SetupLodMesh()
    {
        if (lodMesh != null && spawnObjects != null)
        {
            newLodMesh = new Mesh();
            newLodMesh.vertices = lodMesh.vertices;
            newLodMesh.uv = lodMesh.uv;
            newLodMesh.triangles = lodMesh.triangles;
            newLodMesh.tangents = ExtractBoneWeights(lodMesh);

            lodAdditionalVertexStreames = new Mesh[50];
            InitAdditionalVertexStream_Internal(lodAdditionalVertexStreames, newLodMesh);

            // Bounding Sphere
            lodBoundingSpheres = new BoundingSphere[spawnObjects.Length];
            for(int i = 0; i < lodBoundingSpheres.Length; ++i)
            {
                lodBoundingSpheres[i] = new BoundingSphere(spawnObjects[i].transform.position, 1f);
            }

            // Culling Group
            lodCullingGroup = new CullingGroup();
            lodCullingGroup.targetCamera = Camera.main;
            lodCullingGroup.SetBoundingSpheres(lodBoundingSpheres);
            lodCullingGroup.SetBoundingSphereCount(lodBoundingSpheres.Length);
            lodCullingGroup.SetBoundingDistances(new float[] { 10, 15, 25, 40 });
            lodCullingGroup.SetDistanceReferencePoint(Camera.main.transform);
            lodCullingGroup.onStateChanged = OnLodCullingGroupOnStateChangedHandler;

            newLodMesh.UploadMeshData(true);
        }
    }

    private void OnLodCullingGroupOnStateChangedHandler(CullingGroupEvent evt)
    {
        GPUSkinning_SpawnObject obj = spawnObjects[evt.index];
        MeshRenderer mr = obj.mr;
        if (evt.isVisible)
        {
            if (!mr.enabled)
            {
                mr.enabled = true;
            }

            MeshFilter mf = obj.mf;
            if (evt.currentDistance > 1)
            {
                if(mf.sharedMesh != newLodMesh)
                {
                    mf.sharedMesh = newLodMesh;
                    mr.additionalVertexStreams = RandomLodAdditionalVertexStream();
                }
            }
            else
            {
                if(mf.sharedMesh != newMesh)
                {
                    mf.sharedMesh = newMesh;
                    mr.additionalVertexStreams = RandomAdditionalVertexStream();
                }
            }
        }
        else
        {
            if(mr.enabled)
            {
                mr.enabled = false;
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // Compute Shader 

    private ComputeBuffer verticesComputeBuffer = null;

    private ComputeBuffer matricesComputeBuffer = null;

    private ComputeBuffer globalDataComputeBuffer = null;

    private ComputeBuffer modelDataComputeBuffer = null;

    private List<GPUSkinning_ComputeShader_Matrix> cbMatricesList = null;

    private Material proceduralModelMaterial = null;

    private bool drawProceduralModel = false;

    private int numVertices_computeBuffer = 0;

    private int numProceduralInstances = 800;

    private int proceduralModelGap = 1;

    private void DrawProceduralModel()
    {
        if(proceduralModelMaterial == null)
        {
            return;
        }

        if(!drawProceduralModel)
        {
            return;
        }

        proceduralModelMaterial.SetBuffer("_VertCB", verticesComputeBuffer);
        proceduralModelMaterial.SetBuffer("_MatCB", matricesComputeBuffer);
        proceduralModelMaterial.SetBuffer("_GlobalCB", globalDataComputeBuffer);
        proceduralModelMaterial.SetBuffer("_ModelCB", modelDataComputeBuffer);
        proceduralModelMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, numVertices_computeBuffer, numProceduralInstances);
    }

    private void InitComputeBuffer(Mesh mesh)
    {
        if(!SystemInfo.supportsComputeShaders)
        {
            return;
        }

        // Material
        Shader shader = Shader.Find("Unlit/ProceduralModel");
        if(!shader.isSupported)
        {
            return;
        }
        proceduralModelMaterial = new Material(shader);
        proceduralModelMaterial.mainTexture = newMtrl.mainTexture;

        // Vertices
        int[] indices = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector4[] tangents = mesh.tangents;
        Vector2[] uv = mesh.uv;
        verticesComputeBuffer = new ComputeBuffer(indices.Length, GPUSkinning_ComputeShader_Vertex.size);
        var data = new GPUSkinning_ComputeShader_Vertex[indices.Length];
        for(int i = 0; i < indices.Length; ++i)
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
        globalData.fps = boneAnimations[0].fps;
        globalData.animLength = boneAnimations[0].length;

        // Bone Animation Matrices
        cbMatricesList = new List<GPUSkinning_ComputeShader_Matrix>();
        int matIndex = 0;
        ExtractBoneAnimMatrix(
            (mat) => 
            {
                var matData = new GPUSkinning_ComputeShader_Matrix();
                matData.mat = mat;
                cbMatricesList.Add(matData);
                ++matIndex;
            }, 
            (frameIndex) => 
            {
                if(frameIndex == 0)
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
        for(int i = 0; i < numProceduralInstances; ++i)
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
        GPUSkinning_Camera.instance.onPostRender += DrawProceduralModel;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // GPU Instancing

    private int shaderPropID_mpb_time = 0;

    private MaterialPropertyBlock[] gpuInstancing_mpbs = null;

    private int isGPUInstancingSupported = -1;

    private void InitGPUInstancing()
    {
        if(CheckGPUInstancingIsSupported())
        {
            newMtrl.shader.maximumLOD = 200;
            SetGPUInstancingMaterialPropertyBlock();
        }
        else
        {
            newMtrl.shader.maximumLOD = 100;
            ClearGPUInstancingMaterialPropertyBlock();
        }
    }

    private void SwitchGPUInstancing()
    {
        if(CheckGPUInstancingIsSupported())
        {
            if (newMtrl.shader.maximumLOD == 200)
            {
                newMtrl.shader.maximumLOD = 100;
                ClearGPUInstancingMaterialPropertyBlock();
            }
            else
            {
                newMtrl.shader.maximumLOD = 200;
                SetGPUInstancingMaterialPropertyBlock();
            }
        }
    }

    private bool CheckGPUInstancingIsSupported()
    {
        if(IsGPUInstancingSupported())
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
        if(isGPUInstancingSupported == -1)
        {
            isGPUInstancingSupported = SystemInfo.supportsInstancing ? 1 : 0;
        }
        return isGPUInstancingSupported == 1;
    }

    private void SetGPUInstancingMaterialPropertyBlock()
    {
        if(gpuInstancing_mpbs == null)
        {
            gpuInstancing_mpbs = new MaterialPropertyBlock[50];
            for(int i = 0; i < gpuInstancing_mpbs.Length; ++i)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetFloat(shaderPropID_mpb_time, Random.value * 10);
                gpuInstancing_mpbs[i] = mpb;
            }
        }

        if (spawnObjects != null)
        {
            foreach (var obj in spawnObjects)
            {
                obj.mr.SetPropertyBlock(gpuInstancing_mpbs[Random.Range(0, gpuInstancing_mpbs.Length)]);
            }
        }
    }

    private void ClearGPUInstancingMaterialPropertyBlock()
    {
        if (spawnObjects != null)
        {
            foreach (var obj in spawnObjects)
            {
                obj.mr.SetPropertyBlock(null);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // 通过矩阵数组的方式传递骨骼动画数据

    private int shaderPropID_Matrices = 0;

    private Matrix4x4[] matricesUniformBlock = null;

    private bool isCurrFrameIndexChanged = false;
    private void Play()
    {
        if (isCurrFrameIndexChanged)
        {
            isCurrFrameIndexChanged = false;

            int numBones = bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                matricesUniformBlock[i] = bones[i].animationMatrix;
            }
            // TODO: Use 3x4 matrix to transfer more data
            newMtrl.SetMatrixArray(shaderPropID_Matrices, matricesUniformBlock);
        }
    }

    private int currFrameIndex = -1;
    private void UpdateBoneAnimationMatrix(string animName, float time)
    {
        GPUSkinning_BoneAnimation boneAnimation = boneAnimations[0];//GetBoneAnimation(animName);
        int frameIndex = (int)((time * boneAnimation.fps) % (boneAnimation.length * boneAnimation.fps));
        if (currFrameIndex != frameIndex)
        {
            currFrameIndex = frameIndex;
            isCurrFrameIndexChanged = true;

            GPUSkinning_BoneAnimationFrame frame = boneAnimation.frames[frameIndex];

            UpdateBoneTransformMatrix(bones[rootBoneIndex], Matrix4x4.identity, frame);
        }
    }

    private void UpdateBoneTransformMatrix(GPUSkinning_Bone bone, Matrix4x4 parentMatrix, GPUSkinning_BoneAnimationFrame frame)
    {
        int index = BoneAnimationFrameIndexOf(frame, bone);
        Matrix4x4 mat = parentMatrix * frame.matrices[index];
        bone.animationMatrix = mat * bone.bindpose;

        GPUSkinning_Bone[] children = bone.children;
        int numChildren = children.Length;
        for (int i = 0; i < numChildren; ++i)
        {
            UpdateBoneTransformMatrix(children[i], mat, frame);
        }
    }
    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // 通过纹理中存储的矩阵数据传递骨骼动画数据

    private int shaderPropID_MatricesTex = 0;

    private int shaderPropID_MatricesTexFrameTexels = 0;

    private int shaderPropID_MatricesTexSize = 0;

    private int shaderPropID_AnimLength = 0;

    private int shaderPropID_AnimFPS = 0;

    private Texture2D matricesTex = null;

    private int matricesTexFrameTexels = 0;

    private int matricesTexWidth = 128;

    private int matricesTexHeight = 128;

    private Mesh[] additionalVertexStreames = null;

    private void InitAdditionalVertexStream(Mesh mesh)
    {
        additionalVertexStreames = new Mesh[50];
        InitAdditionalVertexStream_Internal(additionalVertexStreames, mesh);
    }

    private void InitAdditionalVertexStream_Internal(Mesh[] additionalVertexStreames, Mesh mesh)
    {
        Vector3[] newMeshVertices = mesh.vertices;
        for (int i = 0; i < additionalVertexStreames.Length; ++i)
        {
            Mesh m = new Mesh();
            float rnd = Random.Range(0.0f, 10.0f);
            Vector2[] uv2 = new Vector2[mesh.vertexCount];
            for (int j = 0; j < mesh.vertexCount; ++j)
            {
                Vector2 uv = Vector2.zero;
                uv.x = rnd;
                uv2[j] = uv;
            }
            m.vertices = newMeshVertices;
            m.uv2 = uv2;
            m.UploadMeshData(true);
            additionalVertexStreames[i] = m;
        }
    }

    private Mesh RandomAdditionalVertexStream()
    {
        return additionalVertexStreames == null ? null : additionalVertexStreames[Random.Range(0, additionalVertexStreames.Length)];
    }

    private void UpdateMatricesTextureUniforms()
    {
        if (matricesTex != null)
        {
            newMtrl.SetTexture(shaderPropID_MatricesTex, matricesTex);
            newMtrl.SetFloat(shaderPropID_MatricesTexFrameTexels, matricesTexFrameTexels);
            newMtrl.SetVector(shaderPropID_MatricesTexSize, new Vector4(matricesTex.width, matricesTex.height, 0, 0));
            newMtrl.SetFloat(shaderPropID_AnimLength, boneAnimations[0].length);
            newMtrl.SetFloat(shaderPropID_AnimFPS, boneAnimations[0].fps);
        }
    }

    private bool IsMatricesTextureSupported()
    {
        return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf);
    }

    private void CreateMatricesTexture()
    {
        if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
        {
            matricesTex = new Texture2D(matricesTexWidth, matricesTexHeight, TextureFormat.RGBAHalf, false);
            matricesTex.name = "_MatricesTex";
            matricesTex.filterMode = FilterMode.Point;
        }
    }

    // 将骨骼动画数据保存到纹理中
    private void BakeAnimationsToTexture()
    {
        if (matricesTex != null)
        {
            Color[] colorBuffer = matricesTex.GetPixels();
            int colorBufferIndex = 0;

            ExtractBoneAnimMatrix(
                (mat) => 
                {
                    Color c = colorBuffer[colorBufferIndex];
                    c.r = mat.m00; c.g = mat.m01; c.b = mat.m02; c.a = mat.m03;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = mat.m10; c.g = mat.m11; c.b = mat.m12; c.a = mat.m13;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = mat.m20; c.g = mat.m21; c.b = mat.m22; c.a = mat.m23;
                    colorBuffer[colorBufferIndex++] = c;
                }, 
                (frameIndex) => 
                {
                    if(frameIndex == 0)
                    {
                        shaderPropID_MatricesTexFrameTexels = Shader.PropertyToID("_MatricesTexFrameTexels");
                        matricesTexFrameTexels = colorBufferIndex;
                    }
                }
            );

            matricesTex.SetPixels(colorBuffer);
            matricesTex.Apply(false, true);
        }
    }

    private void ExtractBoneAnimMatrix(System.Action<Matrix4x4> boneAnimMatrixCB, System.Action<int> frameCB)
    {
        GPUSkinning_BoneAnimation boneAnimation = boneAnimations[0];
        for (int frameIndex = 0; frameIndex < boneAnimation.frames.Length; ++frameIndex)
        {
            float second = (float)(frameIndex) / (float)boneAnimation.fps;
            UpdateBoneAnimationMatrix(null, second);
            int numBones = bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                Matrix4x4 animMat = bones[i].animationMatrix;
                boneAnimMatrixCB(animMat);
            }
            frameCB(frameIndex);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // 切换两种播放模式（数据传输方式）
    // playMode0: uniform array 
    // playMode1: texture

    private int playMode = 0;
    private string playModeKey0 = "GPU_SKINNING_MATRIX_ARRAY";
    private string playModeKey1 = "GPU_SKINNING_MATRIX_TEXTURE";
    private void SetPlayMode0()
    {
        newMtrl.EnableKeyword(playModeKey0);
        newMtrl.DisableKeyword(playModeKey1);
        playMode = 0;
    }
    private void SetPlayMode1()
    {
        if (matricesTex != null)
        {
            newMtrl.EnableKeyword(playModeKey1);
            newMtrl.DisableKeyword(playModeKey0);
            playMode = 1;
        }
    }
    private bool IsPlayMode0()
    {
        return playMode == 0;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------------------------------------------------------------------
    // Terrain

    [Header("Terrain Setting")]

    public Texture2D terrainTexture = null;

    public Terrain terrain = null;

    private TerrainData terrainData = null;

    private Vector4 terrainSize;

    private int shaderPropID_TerrainTex = 0;

    private int shaderPropID_TerrainSize = 0;

    private int shaderPropID_TerrainPos = 0;

    private void InitTerrain()
    {
        if (terrain != null)
        {
            terrainData = terrain.terrainData;
            terrainSize = terrainData.size;
        }
    }

    private void UpdateTerrainUniforms()
    {
        if (terrain != null)
        {
            newMtrl.SetTexture(shaderPropID_TerrainTex, terrainTexture);
            newMtrl.SetVector(shaderPropID_TerrainSize, terrainSize);
            newMtrl.SetVector(shaderPropID_TerrainPos, terrain.transform.position);
        }
    }

    private void SetTerrainHeightSwitch()
    {
        if(terrain == null)
        {
            newMtrl.EnableKeyword("TERRAIN_HEIGHT_OFF");
            newMtrl.DisableKeyword("TERRAIN_HEIGHT_ON");
        }
        else
        {
            newMtrl.EnableKeyword("TERRAIN_HEIGHT_ON");
            newMtrl.DisableKeyword("TERRAIN_HEIGHT_OFF");
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------

    private void OnDestroy()
    {
        if (newMtrl != null)
        {
            Object.Destroy(newMtrl);
            newMtrl = null;
        }
        if (newMesh != null)
        {
            Object.Destroy(newMesh);
            newMesh = null;
        }
        if(newLodMesh != null)
        {
            Object.Destroy(newLodMesh);
            newLodMesh = null;
        }
        if (matricesTex != null)
        {
            Object.Destroy(matricesTex);
            matricesTex = null;
        }
        if (additionalVertexStreames != null)
        {
            foreach (var m in additionalVertexStreames)
            {
                Object.Destroy(m);
            }
            additionalVertexStreames = null;
        }
        if(lodAdditionalVertexStreames != null)
        {
            foreach (var m in lodAdditionalVertexStreames)
            {
                Object.Destroy(m);
            }
            lodAdditionalVertexStreames = null;
        }
        if(verticesComputeBuffer != null)
        {
            verticesComputeBuffer.Release();
            verticesComputeBuffer = null;
        }
        if(matricesComputeBuffer != null)
        {
            matricesComputeBuffer.Release();
            matricesComputeBuffer = null;
        }
        if(globalDataComputeBuffer != null)
        {
            globalDataComputeBuffer.Release();
            globalDataComputeBuffer = null;
        }
        if(modelDataComputeBuffer != null)
        {
            modelDataComputeBuffer.Release();
            modelDataComputeBuffer = null;
        }
        if(proceduralModelMaterial != null)
        {
            Object.Destroy(proceduralModelMaterial);
            proceduralModelMaterial = null;
        }
        if(lodCullingGroup != null)
        {
            lodCullingGroup.Dispose();
            lodCullingGroup = null;
        }
        lodBoundingSpheres = null;
    }

    private void OnGUI()
    {
        int btnSize = Screen.height / 6;
        Rect btnRect = new Rect(0, 0, btnSize * 2, btnSize);
        // Switch Mode
        if (matricesTex != null)
        {
            if(GUI.Button(btnRect, "Switch Mode"))
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
            btnRect.y += btnSize;
        }
        // Terrain
        if (terrain != null)
        {
            if(GUI.Button(btnRect, "Terrain"))
            {
                terrain.enabled = !terrain.enabled;
            }
            btnRect.y += btnSize;
        }
        // GPU Instancing
        if (IsGPUInstancingSupported())
        {
            if (GUI.Button(btnRect, "GPU Instancing"))
            {
                SwitchGPUInstancing();
            }
            btnRect.y += btnSize;
        }
        else
        {
            Color oldColor = GUI.color;
            GUI.color = Color.red;
            GUI.Label(btnRect, "GPU Instancing is not supported!");
            GUI.color = oldColor;
            btnRect.y += btnSize;
        }
        // Procedural Model (Compute Shader)
        if(proceduralModelMaterial == null)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.red;
            GUI.Label(btnRect, "Compute Shader is not supported!");
            GUI.color = oldColor;
            btnRect.y += btnSize;
        }
        else
        {
            if(GUI.Button(btnRect, "Compute Shader"))
            {
                drawProceduralModel = !drawProceduralModel;
            }
            btnRect.y += btnSize;
        }
    }

    private Vector4[] ExtractBoneWeights(Mesh mesh)
    {
        Vector4[] tangents = new Vector4[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            BoneWeight boneWeight = mesh.boneWeights[i];
            tangents[i].x = boneWeight.boneIndex0;
            tangents[i].y = boneWeight.weight0;
            tangents[i].z = boneWeight.boneIndex1;
            tangents[i].w = boneWeight.weight1;
        }
        return tangents;
    }

    private GPUSkinning_Bone GetBoneByTransform(Transform transform)
    {
        foreach (GPUSkinning_Bone bone in bones)
        {
            if (bone.transform == transform)
            {
                return bone;
            }
        }
        return null;
    }

    private GPUSkinning_Bone GetBoneByHierarchyName(string hierarchyName)
    {
        System.Func<GPUSkinning_Bone, string, GPUSkinning_Bone> Search = null;
        Search = (bone, name) =>
        {
            if (name == hierarchyName)
            {
                return bone;
            }
            foreach (GPUSkinning_Bone child in bone.children)
            {
                GPUSkinning_Bone result = Search(child, name + "/" + child.name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        };

        return Search(bones[rootBoneIndex], bones[rootBoneIndex].name);
    }

    private string GetBoneHierarchyName(GPUSkinning_Bone bone)
    {
        string str = string.Empty;

        GPUSkinning_Bone currentBone = bone;
        while (currentBone != null)
        {
            if (str == string.Empty)
            {
                str = currentBone.name;
            }
            else
            {
                str = currentBone.name + "/" + str;
            }

            currentBone = currentBone.parent;
        }

        return str;
    }

    private void PrintBones()
    {
        string text = string.Empty;

        System.Action<GPUSkinning_Bone, string> PrintBone = null;
        PrintBone = (bone, prefix) =>
        {
            text += prefix + bone.transform.gameObject.name + "\n";
            prefix += "    ";
            foreach (var childBone in bone.children)
            {
                PrintBone(childBone, prefix);
            }
        };

        PrintBone(bones[rootBoneIndex], string.Empty);

        Debug.LogError(text);
    }

    private void NormalizeQuaternion(ref Quaternion q)
    {
        float sum = 0;
        for (int i = 0; i < 4; ++i)
            sum += q[i] * q[i];
        float magnitudeInverse = 1 / Mathf.Sqrt(sum);
        for (int i = 0; i < 4; ++i)
            q[i] *= magnitudeInverse;
    }

    private GPUSkinning_BoneAnimation GetBoneAnimation(string animName)
    {
        foreach (var item in boneAnimations)
        {
            if (item.animName == animName)
            {
                return item;
            }
        }
        return null;
    }

    private int BoneAnimationFrameIndexOf(GPUSkinning_BoneAnimationFrame frame, GPUSkinning_Bone bone)
    {
        GPUSkinning_Bone[] bones = frame.bones;
        int numBones = bones.Length;
        for (int i = 0; i < numBones; ++i)
        {
            if (bones[i] == bone)
            {
                return i;
            }
        }
        return -1;
    }
}
