using UnityEngine;
using System.Collections;

public class GPUSkinning_LOD : GPUSkinning_Component
{
    private Mesh newLodMesh = null;

    private CullingGroup lodCullingGroup = null;

    private BoundingSphere[] lodBoundingSpheres = null;

    private Mesh[] additionalVertexStreames = null;

    public override void Init(GPUSkinning gpuSkinning)
    {
        base.Init(gpuSkinning);

        newLodMesh = new Mesh();
        newLodMesh.vertices = gpuSkinning.lodMesh.vertices;
        newLodMesh.uv = gpuSkinning.lodMesh.uv;
        newLodMesh.triangles = gpuSkinning.lodMesh.triangles;
        newLodMesh.tangents = GPUSkinningUtil.ExtractBoneWeights(gpuSkinning.lodMesh);

        additionalVertexStreames = new Mesh[50];
        GPUSkinningUtil.InitAdditionalVertexStream(additionalVertexStreames, newLodMesh);

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
            foreach (var m in additionalVertexStreames)
            {
                Object.Destroy(m);
            }
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
                    mr.additionalVertexStreams = RandomAdditionalVertexStream();
                }
            }
            else
            {
                if (mf.sharedMesh != gpuSkinning.model.newMesh)
                {
                    mf.sharedMesh = gpuSkinning.model.newMesh;
                    mr.additionalVertexStreams = gpuSkinning.matrixTexture.RandomAdditionalVertexStream();
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

    private Mesh RandomAdditionalVertexStream()
    {
        return additionalVertexStreames == null ? null : additionalVertexStreames[Random.Range(0, additionalVertexStreames.Length)];
    }
}
