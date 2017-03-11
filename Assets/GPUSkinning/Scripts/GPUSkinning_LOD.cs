using UnityEngine;
using System.Collections;

[System.Serializable]
public class GPUSkinning_LOD : GPUSkinning_Component
{
    public Mesh lodMesh = null;

    private Mesh newLodMesh = null;

    private CullingGroup lodCullingGroup = null;

    private BoundingSphere[] lodBoundingSpheres = null;

	private GPUSkinning_AdditionalVertexStreames additionalVertexStreames = null;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        newLodMesh = new Mesh();
        newLodMesh.vertices = lodMesh.vertices;
        newLodMesh.uv = lodMesh.uv;
        newLodMesh.triangles = lodMesh.triangles;
        newLodMesh.tangents = GPUSkinningUtil.ExtractBoneWeights(lodMesh);

		additionalVertexStreames = new GPUSkinning_AdditionalVertexStreames(newLodMesh);

        // Bounding Sphere
        lodBoundingSpheres = new BoundingSphere[gpuSkinning.model.spawnObjects.Length];
        for (int i = 0; i < lodBoundingSpheres.Length; ++i)
        {
            lodBoundingSpheres[i] = new BoundingSphere(gpuSkinning.model.spawnObjects[i].transform.position, 1f);
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

    public override void Destroy()
    {
        base.Destroy();

        if (newLodMesh != null)
        {
            Object.Destroy(newLodMesh);
            newLodMesh = null;
        }
        if (additionalVertexStreames != null)
        {
			additionalVertexStreames.Destroy();
            additionalVertexStreames = null;
        }
        if (lodCullingGroup != null)
        {
            lodCullingGroup.Dispose();
            lodCullingGroup = null;
        }
        lodBoundingSpheres = null;
    }

    public void Update()
    {
        if (lodBoundingSpheres != null && gpuSkinning.model.spawnObjects != null)
        {
            int length = lodBoundingSpheres.Length;
            for (int i = 0; i < length; ++i)
            {
                BoundingSphere bound = lodBoundingSpheres[i];
                bound.position = gpuSkinning.model.spawnObjects[i].transform.position;
                lodBoundingSpheres[i] = bound;
            }
        }
    }

    private void OnLodCullingGroupOnStateChangedHandler(CullingGroupEvent evt)
    {
        GPUSkinning_SpawnObject obj = gpuSkinning.model.spawnObjects[evt.index];
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
                if (mf.sharedMesh != newLodMesh)
                {
                    mf.sharedMesh = newLodMesh;
					additionalVertexStreames.SetRandomStream(obj);
                }
            }
            else
            {
                if (mf.sharedMesh != gpuSkinning.model.newMesh)
                {
                    mf.sharedMesh = gpuSkinning.model.newMesh;
					gpuSkinning.matrixTexture.additionalVertexStreames.SetRandomStream(obj);
                }
            }
        }
        else
        {
            if (mr.enabled)
            {
                mr.enabled = false;
            }
        }
    }
}
