using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GPUSkinningSampler : MonoBehaviour 
{
#if UNITY_EDITOR
    [HideInInspector]
    [SerializeField]
	public string animName = null;

    [HideInInspector]
    [System.NonSerialized]
	public AnimationClip animClip = null;

    [HideInInspector]
    [SerializeField]
    public AnimationClip[] animClips = null;

    [HideInInspector]
    [SerializeField]
    public GPUSkinningWrapMode[] wrapModes = null;

    [HideInInspector]
    [SerializeField]
    public bool[] isSelected = null;

    [HideInInspector]
    [System.NonSerialized]
    public int samplingClipIndex = -1;

    [HideInInspector]
    [SerializeField]
	public GPUSkinningQuality skinQuality = GPUSkinningQuality.Bone2;

    [HideInInspector]
    [SerializeField]
	public Transform rootBoneTransform = null;

    [HideInInspector]
    [SerializeField]
    public GPUSkinningAnimation anim = null;

    [HideInInspector]
    [SerializeField]
	public GPUSkinningShaderType shaderType = GPUSkinningShaderType.Unlit;

	[HideInInspector]
	[System.NonSerialized]
	public bool isSampling = false;

    [HideInInspector]
    [SerializeField]
    public Mesh savedMesh = null;

    [HideInInspector]
    [SerializeField]
    public Material savedMtrl = null;

    [HideInInspector]
    [SerializeField]
    public Shader savedShader = null;

    [HideInInspector]
    [SerializeField]
    public bool updateOrNew = true;

	private Animator animator = null;
    private RuntimeAnimatorController runtimeAnimatorController = null;

	private SkinnedMeshRenderer smr = null;

	private GPUSkinningAnimation gpuSkinningAnimation = null;

    private GPUSkinningClip gpuSkinningClip = null;

	[HideInInspector]
	[System.NonSerialized]
	public int samplingTotalFrams = 0;

	[HideInInspector]
	[System.NonSerialized]
	public int samplingFrameIndex = 0;

	public const string TEMP_SAVED_ANIM_PATH = "GPUSkinning_Temp_Save_Anim_Path";
	public const string TEMP_SAVED_MTRL_PATH = "GPUSkinning_Temp_Save_Mtrl_Path";
	public const string TEMP_SAVED_MESH_PATH = "GPUSkinning_Temp_Save_Mesh_Path";
    public const string TEMP_SAVED_SHADER_PATH = "GPUSkinning_Temp_Save_Shader_Path";

    public void BeginSample()
    {
        samplingClipIndex = 0;
    }

    public void EndSample()
    {
        samplingClipIndex = -1;
    }

    public bool IsSamplingProgress()
    {
        return samplingClipIndex != -1;
    }

    public void StartSample()
	{
        if (isSampling)
        {
            return;
        }

        if(animClips.Length == 0)
        {
            ShowDialog("Missing AnimationClip");
            return;
        }

        animClip = animClips[samplingClipIndex];
        if (animClip == null || !isSelected[samplingClipIndex])
		{
            isSampling = false;
			return;
		}

        int numFrames = (int)(animClip.frameRate * animClip.length);
        if(numFrames == 0)
        {
            isSampling = false;
            return;
        }

        if (rootBoneTransform == null)
		{
			ShowDialog("Missing RootBoneTransform");
			return;
		}

		smr = GetComponentInChildren<SkinnedMeshRenderer>();
		if(smr == null)
		{
			ShowDialog("Missing SkinnedMeshRenderer");
			return;
		}
		if(smr.sharedMesh == null)
		{
			ShowDialog("Missing SkinnedMeshRenderer.Mesh");
			return;
		}

		Mesh mesh = smr.sharedMesh;
		if(mesh == null)
		{
			ShowDialog("Missing Mesh");
			return;
		}

		if(anim == null && string.IsNullOrEmpty(animName.Trim()))
		{
			ShowDialog("Empty AnimName");
			return;
		}

		samplingFrameIndex = 0;

		gpuSkinningAnimation = anim == null ? ScriptableObject.CreateInstance<GPUSkinningAnimation>() : anim;
		gpuSkinningAnimation.name = animName;

        if(samplingClipIndex == 0)
        {
            if(!updateOrNew)
            {
                gpuSkinningAnimation.clips = null;
            }
        }

		List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
		CollectBones(bones_result, smr.bones, mesh.bindposes, null, rootBoneTransform, 0);
        GPUSkinningBone[] newBones = bones_result.ToArray();
        GenerateBonesGUID(newBones);
        if (anim != null) RestoreCustomBoneData(anim.bones, newBones);
        gpuSkinningAnimation.bones = newBones;
        gpuSkinningAnimation.rootBoneIndex = 0;

        int numClips = gpuSkinningAnimation.clips == null ? 0 : gpuSkinningAnimation.clips.Length;
        int clipIndex = -1;
        for (int i = 0; i < numClips; ++i)
        {
            if (gpuSkinningAnimation.clips[i].name == animClip.name)
            {
                clipIndex = i;
                break;
            }
        }

        gpuSkinningClip = new GPUSkinningClip();
        gpuSkinningClip.name = animClip.name;
        gpuSkinningClip.fps = (int)animClip.frameRate;
        gpuSkinningClip.length = animClip.length;
        gpuSkinningClip.wrapMode = wrapModes[samplingClipIndex];
        gpuSkinningClip.frames = new GPUSkinningFrame[numFrames];

        if(gpuSkinningAnimation.clips == null)
        {
            gpuSkinningAnimation.clips = new GPUSkinningClip[] { gpuSkinningClip };
        }
        else
        {
            if (clipIndex == -1)
            {
                List<GPUSkinningClip> clips = new List<GPUSkinningClip>(gpuSkinningAnimation.clips);
                clips.Add(gpuSkinningClip);
                gpuSkinningAnimation.clips = clips.ToArray();
            }
            else
            {
                gpuSkinningAnimation.clips[clipIndex] = gpuSkinningClip;
            }
        }

        SetCurrentAnimationClip();

        isSampling = true;
    }

    private void RestoreCustomBoneData(GPUSkinningBone[] bonesOrig, GPUSkinningBone[] bonesNew)
    {
        for(int i = 0; i < bonesNew.Length; ++i)
        {
            for(int j = 0; j < bonesOrig.Length; ++j)
            {
                if(bonesNew[i].guid == bonesOrig[i].guid)
                {
                    bonesNew[i].isExposed = bonesOrig[i].isExposed;
                    break;
                }
            }
        }
    }

    private void GenerateBonesGUID(GPUSkinningBone[] bones)
    {
        int numBones = bones == null ? 0 : bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            string boneHierarchyPath = GPUSkinningUtil.BoneHierarchyPath(anim, i);
            string guid = GPUSkinningUtil.MD5(boneHierarchyPath);
            bones[i].guid = guid;
        }
    }

    private void SetCurrentAnimationClip()
    {
        AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController();
        AnimationClip[] clips = runtimeAnimatorController.animationClips;
        AnimationClipPair[] pairs = new AnimationClipPair[clips.Length];
        for(int i = 0; i < clips.Length; ++i)
        {
            AnimationClipPair pair = new AnimationClipPair();
            pairs[i] = pair;
            pair.originalClip = clips[i];
            pair.overrideClip = animClip;
        }
        animatorOverrideController.runtimeAnimatorController = runtimeAnimatorController;
        animatorOverrideController.clips = pairs;
        animator.runtimeAnimatorController = animatorOverrideController;
    }

    private Mesh CreateNewMesh()
    {
        Mesh mesh = smr.sharedMesh;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Color[] colors = mesh.colors;
        Vector2[] uv = mesh.uv;

        Mesh newMesh = new Mesh();
        newMesh.name = "GPUSkinning_Mesh";
        newMesh.vertices = mesh.vertices;
        if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
        if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
        if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
        if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

        int numVertices = mesh.vertexCount;
        BoneWeight[] boneWeights = mesh.boneWeights;
        Vector4[] uv2 = new Vector4[numVertices];
		Vector4[] uv3 = new Vector4[numVertices];
        Transform[] smrBones = smr.bones;
        for(int i = 0; i < numVertices; ++i)
        {
            BoneWeight boneWeight = boneWeights[i];

			BoneWeightSortData[] weights = new BoneWeightSortData[4];
			weights[0] = new BoneWeightSortData(){ index=boneWeight.boneIndex0, weight=boneWeight.weight0 };
			weights[1] = new BoneWeightSortData(){ index=boneWeight.boneIndex1, weight=boneWeight.weight1 };
			weights[2] = new BoneWeightSortData(){ index=boneWeight.boneIndex2, weight=boneWeight.weight2 };
			weights[3] = new BoneWeightSortData(){ index=boneWeight.boneIndex3, weight=boneWeight.weight3 };
			System.Array.Sort(weights);

			GPUSkinningBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
			GPUSkinningBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
			GPUSkinningBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
			GPUSkinningBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

            Vector4 skinData_01 = new Vector4();
			skinData_01.x = GetBoneIndex(bone0);
			skinData_01.y = weights[0].weight;
			skinData_01.z = GetBoneIndex(bone1);
			skinData_01.w = weights[1].weight;
			uv2[i] = skinData_01;

			Vector4 skinData_23 = new Vector4();
			skinData_23.x = GetBoneIndex(bone2);
			skinData_23.y = weights[2].weight;
			skinData_23.z = GetBoneIndex(bone3);
			skinData_23.w = weights[3].weight;
			uv3[i] = skinData_23;
        }
        newMesh.SetUVs(1, new List<Vector4>(uv2));
		newMesh.SetUVs(2, new List<Vector4>(uv3));

        newMesh.triangles = mesh.triangles;
        return newMesh;
    }

	private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
	{
		public int index = 0;

		public float weight = 0;

		public int CompareTo(BoneWeightSortData b)
		{
			return weight > b.weight ? -1 : 1;
		}
	}

	private void CollectBones(List<GPUSkinningBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, GPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
	{
		GPUSkinningBone currentBone = new GPUSkinningBone();
		bones_result.Add(currentBone);

		int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
		currentBone.transform = currentBoneTransform;
		currentBone.name = currentBone.transform.gameObject.name;
		currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
		currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);

		if(parentBone != null)
		{
			parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
		}

		int numChildren = currentBone.transform.childCount;
		if(numChildren > 0)
		{
			currentBone.childrenBonesIndices = new int[numChildren];
			for(int i = 0; i < numChildren; ++i)
			{
				CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i) , i);
			}
		}
	}

	private void Awake()
	{
		animator = GetComponent<Animator>();
		if(animator == null)
		{
			DestroyImmediate(this);
			ShowDialog("Cannot find Animator Component");
			return;
		}
        if(animator.runtimeAnimatorController == null)
        {
            DestroyImmediate(this);
            ShowDialog("Missing RuntimeAnimatorController");
            return;
        }
        if(animator.runtimeAnimatorController is AnimatorOverrideController)
        {
            DestroyImmediate(this);
            ShowDialog("RuntimeAnimatorController could not be a AnimatorOverrideController");
            return;
        }
        runtimeAnimatorController = animator.runtimeAnimatorController;
	}

	private void Update()
	{
		if(!isSampling)
		{
			return;
		}

        int totalFrams = (int)(gpuSkinningClip.length * gpuSkinningClip.fps);
		samplingTotalFrams = totalFrams;

        if (samplingFrameIndex >= totalFrams)
        {
            string savePath = null;
            if (anim == null)
            {
                savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), animName);
            }
            else
            {
                string animPath = AssetDatabase.GetAssetPath(anim);
                savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
            }

			if(!string.IsNullOrEmpty(savePath))
			{
				if(!savePath.Contains(Application.dataPath.Replace('\\', '/')))
				{
					ShowDialog("Must be select directory in project");
				}
				else
				{
					SaveUserPreferDir(savePath);

					string dir = "Assets" + savePath.Substring(Application.dataPath.Length);

					string savedAnimPath = dir + "/GPUSKinning_Anim_" + animName + ".asset";
					EditorUtility.SetDirty(gpuSkinningAnimation);
                    if (anim != gpuSkinningAnimation)
                    {
                        AssetDatabase.CreateAsset(gpuSkinningAnimation, savedAnimPath);
                    }
                    WriteTempData(TEMP_SAVED_ANIM_PATH, savedAnimPath);
                    anim = gpuSkinningAnimation;

                    if (samplingClipIndex == 0)
                    {
                        Mesh newMesh = CreateNewMesh();
                        if (savedMesh != null)
                        {
                            newMesh.bounds = savedMesh.bounds;
                        }
                        string savedMeshPath = dir + "/GPUSKinning_Mesh_" + animName + ".asset";
                        AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                        WriteTempData(TEMP_SAVED_MESH_PATH, savedMeshPath);
                        savedMesh = newMesh;

                        CreateShaderAndMaterial(dir);
                    }

					AssetDatabase.Refresh();
					AssetDatabase.SaveAssets();
				}
			}
            isSampling = false;
            return;
        }
        
        float time = gpuSkinningClip.length * ((float)samplingFrameIndex / totalFrams);
        GPUSkinningFrame frame = new GPUSkinningFrame();
        gpuSkinningClip.frames[samplingFrameIndex] = frame;
        frame.matrices = new Matrix4x4[gpuSkinningAnimation.bones.Length];
		animator.speed = 0;
		animator.SetTime(time);
		animator.speed = 1;
        StartCoroutine(SamplingCoroutine(frame));
        ++samplingFrameIndex;
    }

    private IEnumerator SamplingCoroutine(GPUSkinningFrame frame)
    {
		yield return new WaitForEndOfFrame();

        GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
        int numBones = bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            Transform boneTransform = bones[i].transform;
            GPUSkinningBone currentBone = GetBoneByTransform(boneTransform);
            frame.matrices[i] = currentBone.bindpose;
            do
            {
                Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                frame.matrices[i] = mat * frame.matrices[i];
                if (currentBone.parentBoneIndex == -1)
                {
                    break;
                }
                else
                {
                    currentBone = bones[currentBone.parentBoneIndex];
                }
            }
            while (true);
        }
    }

	private void CreateShaderAndMaterial(string dir)
	{
		string shaderTemplate = 
			shaderType == GPUSkinningShaderType.Unlit ? "GPUSkinningUnlit_Template" : 
			shaderType == GPUSkinningShaderType.StandardSpecular ? "GPUSkinningSpecular_Template" :
			shaderType == GPUSkinningShaderType.StandardMetallic ? "GPUSkinningMetallic_Template" : string.Empty;

		string shaderStr = ((TextAsset)Resources.Load(shaderTemplate)).text;
		shaderStr = shaderStr.Replace("_$AnimName$_", animName);
		shaderStr = shaderStr.Replace("_$NumBones$_", gpuSkinningAnimation.bones.Length.ToString());
		shaderStr = SkinQualityShaderStr(shaderStr);
		string shaderPath = dir + "/GPUSKinning_Shader_" + animName + ".shader";
		File.WriteAllText(shaderPath, shaderStr);
        WriteTempData(TEMP_SAVED_SHADER_PATH, shaderPath);
		AssetDatabase.ImportAsset(shaderPath);

		Material mtrl = new Material(AssetDatabase.LoadMainAssetAtPath(shaderPath) as Shader);
		if(smr.sharedMaterial != null)
		{
			mtrl.CopyPropertiesFromMaterial(smr.sharedMaterial);
		}
		string savedMtrlPath = dir + "/GPUSKinning_Material_" + animName + ".mat";
		AssetDatabase.CreateAsset(mtrl, savedMtrlPath);
        WriteTempData(TEMP_SAVED_MTRL_PATH, savedMtrlPath);
	}

	private string SkinQualityShaderStr(string shaderStr)
	{
		GPUSkinningQuality removalQuality1 = 
			skinQuality == GPUSkinningQuality.Bone1 ? GPUSkinningQuality.Bone2 : 
			skinQuality == GPUSkinningQuality.Bone2 ? GPUSkinningQuality.Bone1 : 
			skinQuality == GPUSkinningQuality.Bone4 ? GPUSkinningQuality.Bone1 : GPUSkinningQuality.Bone1;

		GPUSkinningQuality removalQuality2 = 
			skinQuality == GPUSkinningQuality.Bone1 ? GPUSkinningQuality.Bone4 : 
			skinQuality == GPUSkinningQuality.Bone2 ? GPUSkinningQuality.Bone4 : 
			skinQuality == GPUSkinningQuality.Bone4 ? GPUSkinningQuality.Bone2 : GPUSkinningQuality.Bone1;

		shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality1 + @"[\s\S]*" + removalQuality1 + @"\$_", string.Empty);
		shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality2 + @"[\s\S]*" + removalQuality2 + @"\$_", string.Empty);
		shaderStr = shaderStr.Replace("_$" + skinQuality, string.Empty);
		shaderStr = shaderStr.Replace(skinQuality + "$_", string.Empty);

		return shaderStr;
	}

    private GPUSkinningBone GetBoneByTransform(Transform transform)
	{
		GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
		int numBones = bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            if(bones[i].transform == transform)
            {
                return bones[i];
            }
        }
        return null;
	}

    private int GetBoneIndex(GPUSkinningBone bone)
    {
        return System.Array.IndexOf(gpuSkinningAnimation.bones, bone);
    }

	private void ShowDialog(string msg)
	{
		EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
	}

	private void SaveUserPreferDir(string dirPath)
	{
		PlayerPrefs.SetString("GPUSkinning_UserPreferDir", dirPath);
	}

	private string GetUserPreferDir()
	{
		return PlayerPrefs.GetString("GPUSkinning_UserPreferDir", Application.dataPath);
	}

    public static void WriteTempData(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static string ReadTempData(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }

    public static void DeleteTempData(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }
#endif
}
