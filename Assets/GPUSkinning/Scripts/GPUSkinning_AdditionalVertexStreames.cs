using UnityEngine;
using System.Collections;

public class GPUSkinning_AdditionalVertexStreames
{
	private Mesh[] streams = null;

	private float[] values = null;

	public GPUSkinning_AdditionalVertexStreames(Mesh mesh)
	{
		Random.InitState(0);

		streams = new Mesh[30];
		values = new float[streams.Length];

		Vector3[] vertices = mesh.vertices;
		for (int i = 0; i < streams.Length; ++i)
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
			m.vertices = vertices;
			m.uv2 = uv2;
			m.UploadMeshData(true);
			streams[i] = m;

			values[i] = rnd;
		}
	}

	public void SetRandomStream(MeshRenderer mr)
	{
		mr.additionalVertexStreams = streams[Random.Range(0, streams.Length)];
	}

	public void SetRandomStream(GPUSkinning_SpawnObject spawnObject)
	{
		int rndIndex = Random.Range(0, streams.Length);
		spawnObject.mr.additionalVertexStreams = streams[rndIndex];
		spawnObject.timeOffset_instancingOff = values[rndIndex];
	}

	public void ClearStream(GPUSkinning_SpawnObject spawnObject)
	{
		spawnObject.mr.additionalVertexStreams = null;
		spawnObject.timeOffset_instancingOff = 0;
	}

	public void Destroy()
	{
		if(streams != null)
		{
			int numStreams = streams.Length;
			for(int i = 0; i < numStreams; ++i)
			{
				Object.Destroy(streams[i]);
				streams[i] = null;
			}
			streams = null;
		}

		values = null;
	}
}
