using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Extract bone animation data from Animation Component.
/// Store data and hierarchy as customize Data Structure.
/// </summary>
[System.Serializable]
public class GPUSkinning_Model : GPUSkinning_Component
{
    public Transform[] spawnPoints = null;

    private SkinnedMeshRenderer smr = null;

    private Mesh mesh = null;

    [System.NonSerialized]
    public GPUSkinning_Bone[] bones = null;

    [System.NonSerialized]
    public int rootBoneIndex = 0;

    private MeshFilter mf = null;

    private MeshRenderer mr = null;

    [System.NonSerialized]
    public Material newMtrl = null;

    [System.NonSerialized]
    public Mesh newMesh = null;

    [System.NonSerialized]
    public GPUSkinning_BoneAnimation[] boneAnimations = null;

    [System.NonSerialized]
    public GPUSkinning_SpawnObject[] spawnObjects = null;

    private int shaderPropID_Time = 0;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        shaderPropID_Time = Shader.PropertyToID("_GameTime");

        smr = gpuSkinning.GetComponentInChildren<SkinnedMeshRenderer>();
        mesh = smr.sharedMesh;

        // Init Bones
        int numBones = smr.bones.Length;
        bones = new GPUSkinning_Bone[numBones];
        for (int i = 0; i < numBones; ++i)
        {
            GPUSkinning_Bone bone = new GPUSkinning_Bone();
            bones[i] = bone;
            bone.transform = smr.bones[i];
            bone.name = bone.transform.gameObject.name;
            bone.bindpose = mesh.bindposes[i]/*smr to bone*/;
        }

        // Construct Hierarchy
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
        mf = gpuSkinning.gameObject.AddComponent<MeshFilter>();
        mr = gpuSkinning.gameObject.AddComponent<MeshRenderer>();

        newMtrl = new Material(Shader.Find("Unlit/GPUSkinning"));
        newMtrl.CopyPropertiesFromMaterial(smr.sharedMaterial);
        mr.sharedMaterial = newMtrl;

        // New Mesh
        newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.tangents = GPUSkinningUtil.ExtractBoneWeights(mesh);
        newMesh.uv = mesh.uv;
        newMesh.triangles = mesh.triangles;
        mf.sharedMesh = newMesh;

#if UNITY_EDITOR
        // Extract bone animation data
        int boneAnimationsCount = 0;
        boneAnimations = new GPUSkinning_BoneAnimation[gpuSkinning.GetComponent<Animator>().runtimeAnimatorController.animationClips.Length];
        foreach (AnimationClip animClip in gpuSkinning.GetComponent<Animator>().runtimeAnimatorController.animationClips)
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
        // Save as ScriptableObject
        AssetDatabase.CreateAsset(boneAnimations[0], "Assets/GPUSkinning/Resources/anim0.asset");
        AssetDatabase.Refresh();
#else
		// Read from ScriptableObject directly
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

        // Spawn many models
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
                }
            }
            spawnObjects = list.ToArray();
        }
    }

    public override void Destroy()
    {
        base.Destroy();

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
    }

    public void PostInit()
    {
		gpuSkinning.matrixTexture.additionalVertexStreames.SetRandomStream(mr);

        if(spawnObjects != null)
        {
            foreach(var obj in spawnObjects)
            {
				gpuSkinning.matrixTexture.additionalVertexStreames.SetRandomStream(obj);
            }
        }

        // Disable Unity's built-in Animation
        GameObject.Destroy(gpuSkinning.transform.FindChild("pelvis").gameObject);
        GameObject.Destroy(gpuSkinning.transform.FindChild("mutant_mesh").gameObject);
        Object.Destroy(gpuSkinning.GetComponent<Animator>());
        smr.enabled = false;

        newMesh.UploadMeshData(true);

        //PrintBones();
    }

    public void Update()
    {
        newMtrl.SetFloat(shaderPropID_Time, gpuSkinning.second);
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

    public GPUSkinning_Bone GetBoneByHierarchyName(string hierarchyName)
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
}
