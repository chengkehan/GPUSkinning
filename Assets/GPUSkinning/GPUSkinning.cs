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

        // 保存骨骼动画权重
        Vector4[] tangents = new Vector4[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            BoneWeight boneWeight = mesh.boneWeights[i];
            tangents[i].x = boneWeight.boneIndex0;
            tangents[i].y = boneWeight.weight0;
            tangents[i].z = boneWeight.boneIndex1;
            tangents[i].w = boneWeight.weight1;
        }

        // New Mesh
        newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.tangents = tangents;
        newMesh.uv = mesh.uv;
        newMesh.triangles = mesh.triangles;
        mf.sharedMesh = newMesh;

        // 为每个角色生成差异化数据
        additionalVertexStreames = new Mesh[100];
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
            m.vertices = newMesh.vertices;
            m.uv2 = uv2;
            m.UploadMeshData(true);
            additionalVertexStreames[i] = m;
        }
        mr.additionalVertexStreams = additionalVertexStreames[0];

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
                    spawnObject.mr.additionalVertexStreams = additionalVertexStreames[Random.Range(0, additionalVertexStreames.Length)];
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

        // 将骨骼动画数据保存到纹理中
        if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
        {
            matricesTex = new Texture2D(matricesTexWidth, matricesTexHeight, TextureFormat.RGBAHalf, false);
            matricesTex.name = "_MatricesTex";
            matricesTex.filterMode = FilterMode.Point;
        }

        BakeAnimationsToTexture();

        SetPlayMode0();

        InitTerrain();

        SetTerrainHeightSwitch();
    }

    private float second = 0.0f;
    private void Update()
    {
        PressKeyToSwitchMode();

        ViewFrustumCulling();

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

    // TODO:
    private void ViewFrustumCulling()
    {
        // 如果在视锥体外则关闭 Renderer
    }

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

    // 将骨骼动画数据保存到纹理中
    private void BakeAnimationsToTexture()
    {
        if (matricesTex != null)
        {
            Color[] colorBuffer = matricesTex.GetPixels();
            int colorBufferIndex = 0;

            GPUSkinning_BoneAnimation boneAnimation = boneAnimations[0];
            for (int frameIndex = 0; frameIndex < boneAnimation.frames.Length; ++frameIndex)
            {
                float second = (float)(frameIndex) / (float)boneAnimation.fps;
                UpdateBoneAnimationMatrix(null, second);
                int numBones = bones.Length;
                for (int i = 0; i < numBones; ++i)
                {
                    Matrix4x4 animMat = bones[i].animationMatrix;

                    Color c = colorBuffer[colorBufferIndex];
                    c.r = animMat.m00; c.g = animMat.m01; c.b = animMat.m02; c.a = animMat.m03;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = animMat.m10; c.g = animMat.m11; c.b = animMat.m12; c.a = animMat.m13;
                    colorBuffer[colorBufferIndex++] = c;

                    c = colorBuffer[colorBufferIndex];
                    c.r = animMat.m20; c.g = animMat.m21; c.b = animMat.m22; c.a = animMat.m23;
                    colorBuffer[colorBufferIndex++] = c;
                }

                if (matricesTexFrameTexels == 0)
                {
                    shaderPropID_MatricesTexFrameTexels = Shader.PropertyToID("_MatricesTexFrameTexels");
                    matricesTexFrameTexels = colorBufferIndex;
                }
            }

            matricesTex.SetPixels(colorBuffer);
            matricesTex.Apply(false, true);
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
    private void PressKeyToSwitchMode()
    {
        if (Input.GetKeyUp(KeyCode.Space))
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
    }

    private void OnGUI()
    {
        if (matricesTex != null)
        {
            GUILayout.Label("Press Space Key to Switch Mode");
        }
        if(terrain != null)
        {
            if(GUILayout.Button("Terrain"))
            {
                terrain.enabled = !terrain.enabled;
            }
        }
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
