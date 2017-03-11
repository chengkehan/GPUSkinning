using UnityEngine;
using System.Collections;

/// <summary>
/// This is a test case of joint.
/// In this example, a weapon is attached to a joint.
/// </summary>
[System.Serializable]
public class GPUSkinning_Joint : GPUSkinning_Component
{
    public GameObject weaponPrefab = null;

    public string jointPath = null;

    public Vector3 position;

    public Vector3 eulerAngle;

    public Vector3 scale;

    private GPUSkinning_Bone bone = null;

	private int boneIndex = 0;

    private Matrix4x4 localMatrix;

    private Mesh mesh = null;

    [System.NonSerialized]
    public Material material = null;

    private bool isDrawing = false;

    private Matrix4x4[] instancingMatrices = null;

    private Matrix4x4[] hierarchyToObjectMatrices = null;

    private MaterialPropertyBlock playMode0_mpb = null;

    private MaterialPropertyBlock playMode1_mpb = null;

    private bool isBuiltInDrawMeshInstancedSupported = false;

    private int shaderPropId_HierarchyToObjectMat = 0;

    private int shaderPorpId_HierarchyToObjectMats = 0;

    private int shaderPropId_JointLocalMatrix = 0;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        shaderPropId_HierarchyToObjectMat = Shader.PropertyToID("_HierarchyToObjectMat");
        shaderPorpId_HierarchyToObjectMats = Shader.PropertyToID("_HierarchyToObjectMats");
        shaderPropId_JointLocalMatrix = Shader.PropertyToID("_JointLocalMatrix");

        localMatrix = Matrix4x4.TRS(position, Quaternion.Euler(eulerAngle), scale);
        mesh = weaponPrefab.GetComponent<MeshFilter>().sharedMesh;
        material = new Material(weaponPrefab.GetComponent<MeshRenderer>().sharedMaterial);

#if UNITY_5_5 || UNITY_5_6 || UNITY_5_7 || UNITY_5_8 || UNITY_5_9 || UNITY_6 || UNITY_2017 || UNITY_2018 // etc.
        isBuiltInDrawMeshInstancedSupported = true;
#else
        isBuiltInDrawMeshInstancedSupported = false;
#endif

        if (isBuiltInDrawMeshInstancedSupported)
        {
            material.EnableKeyword("BUILT_IN_DRAW_MESH_INSTANCED_ON");
            material.DisableKeyword("BUILT_IN_DRAW_MESH_INSTANCED_OFF");
        }
        else
        {
            material.EnableKeyword("BUILT_IN_DRAW_MESH_INSTANCED_OFF");
            material.DisableKeyword("BUILT_IN_DRAW_MESH_INSTANCED_ON");
        }
    }

    public override void Destroy()
    {
        base.Destroy();

        if(material != null)
        {
            Object.Destroy(material);
            material = null;
        }

        instancingMatrices = null;
        hierarchyToObjectMatrices = null;

        playMode0_mpb = null;
        playMode1_mpb = null;
    }

    public void OnGUI(ref Rect rect, int size)
    {
        if (gpuSkinning.terrain.terrain != null)
        {
            return;
        }

		if(!isBuiltInDrawMeshInstancedSupported)
		{
			rect.x += size;
			Color tempColor = GUI.color;
			GUI.color = Color.red;
			GUI.Label(rect, "DrawMeshInstanced is not supported. Bad performance.");
			GUI.color = tempColor;
			rect.x -= size;
		}
        if(GUI.Button(rect, "Weapon"))
        {
            isDrawing = !isDrawing;
            rect.y += size;
        }
    }

    public void Update()
    {
        if(!isDrawing)
        {
            return;
        }

		if(bone == null)
		{
			bone = gpuSkinning.model.GetBoneByHierarchyName(jointPath);
			if(bone != null)
			{
				boneIndex = System.Array.IndexOf(gpuSkinning.model.bones, bone);
			}
		}
		if(bone == null)
		{
			return;
		}

        if (instancingMatrices == null)
        {
            instancingMatrices = new Matrix4x4[gpuSkinning.model.spawnObjects.Length];
            playMode0_mpb = new MaterialPropertyBlock();

            hierarchyToObjectMatrices = new Matrix4x4[gpuSkinning.model.spawnObjects.Length];
            playMode1_mpb = new MaterialPropertyBlock();
        }

        if (gpuSkinning.playingMode.IsPlayMode0())
        {
            Matrix4x4 hierarchyToObject = bone.hierarchyMatrix * localMatrix;
            var spawnObjects = gpuSkinning.model.spawnObjects;
            int numSpawnObjects = spawnObjects == null ? 0 : spawnObjects.Length;
            

            for (int i = 0; i < numSpawnObjects; ++i)
            {
                var spawnObject = spawnObjects[i];

                // (1)
                // I think "transform.hasChanged" is not a reliable api in a complex case, it would be better to write your own game logic instead.
                if (spawnObject.transform.hasChanged)
                {
                    spawnObject.transform.hasChanged = false;
                    // (2)
                    // Huge amount of accessing "transform.localToWorldMatrix" will spend much cpu time, so cache these in a buffer, updating once data is dirty.
                    instancingMatrices[i] = spawnObject.transform.localToWorldMatrix;
                }

                if (!isBuiltInDrawMeshInstancedSupported || !gpuSkinning.instancing.IsGPUInstancingOn())
                {
                    // (3)
                    // Matrix multiplication is a slow operation. Optimize it.
                    Matrix4x4 hierarchyToWorld = instancingMatrices[i] * hierarchyToObject;
                    // (4)
                    // Invoke many times of "Graphics.DrawMesh" api will cause performance impact. 
                    // So "Graphics.DrawMesh" is not better than MeshRenderer in some cases.
                    // Here is just a example for joint. Overhead should be considered in production. 
                    Graphics.DrawMesh(mesh, hierarchyToWorld, material, 0);
                }
            }

            if (isBuiltInDrawMeshInstancedSupported && gpuSkinning.instancing.IsGPUInstancingOn())
            {
                playMode0_mpb.SetMatrix(shaderPropId_HierarchyToObjectMat, hierarchyToObject);
#if UNITY_5_5 || UNITY_5_6 || UNITY_5_7 || UNITY_5_8 || UNITY_5_9 || UNITY_6 || UNITY_2017 || UNITY_2018 // etc.
                // (5)
                // https://forum.unity3d.com/threads/graphics-drawmesh-drawmeshinstanced-fundamentally-bottlenecked-by-the-cpu.429120/
                Graphics.DrawMeshInstanced(mesh, 0, material, instancingMatrices, numSpawnObjects, playMode0_mpb);
#endif
            }
        }
        else
        {
			var spawnObjects = gpuSkinning.model.spawnObjects;
			int numSpawnObjects = spawnObjects == null ? 0 : spawnObjects.Length;
			bool isInstancingOn = gpuSkinning.instancing.IsGPUInstancingOn();
			int fps = gpuSkinning.model.boneAnimations[0].fps;
			float animLength = gpuSkinning.model.boneAnimations[0].length;

			for(int i = 0; i < numSpawnObjects; ++i)
			{
				var spawnObject = spawnObjects[i];

                // (1)
                if(spawnObject.transform.hasChanged)
                {
                    spawnObject.transform.hasChanged = false;
                    // (2)
                    instancingMatrices[i] = spawnObject.transform.localToWorldMatrix;
                }

				float timeOffset = isInstancingOn ? spawnObject.timeOffset_instancingOn : spawnObject.timeOffset_instancingOff;
				int frameIndex = (int)(((gpuSkinning.second + timeOffset) * fps) % (animLength * fps));
				int frameStartIndex = frameIndex * gpuSkinning.matrixTexture.numHierarchyMatricesPerFrame;
				Matrix4x4 hierarchyToObject = gpuSkinning.matrixTexture.hierarchyMatrices[frameStartIndex + boneIndex];
                
                if (!isBuiltInDrawMeshInstancedSupported || !gpuSkinning.instancing.IsGPUInstancingOn())
                {
                    // (3)
                    Matrix4x4 hierarchyToWorld = instancingMatrices[i] * hierarchyToObject * localMatrix;
                    // (4)
                    Graphics.DrawMesh(mesh, hierarchyToWorld, material, 0);
                }
                else
                {
                    hierarchyToObjectMatrices[i] = hierarchyToObject;
                }
			}

            if (isBuiltInDrawMeshInstancedSupported && gpuSkinning.instancing.IsGPUInstancingOn())
            {
                playMode1_mpb.SetMatrixArray(shaderPorpId_HierarchyToObjectMats, hierarchyToObjectMatrices);
                playMode1_mpb.SetMatrix(shaderPropId_JointLocalMatrix, localMatrix);
#if UNITY_5_5 || UNITY_5_6 || UNITY_5_7 || UNITY_5_8 || UNITY_5_9 || UNITY_6 || UNITY_2017 || UNITY_2018 // etc.
                // (5)
                Graphics.DrawMeshInstanced(mesh, 0, material, instancingMatrices, numSpawnObjects, playMode1_mpb);
#endif
            }
        }
    }
}
