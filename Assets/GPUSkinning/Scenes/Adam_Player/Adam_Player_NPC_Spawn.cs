using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Adam_Player_NPC_Spawn : MonoBehaviour 
{
	public GameObject spawnTarget = null;

	public int spawnCount = 100;

	private void Start () 
	{
		for(int i = 0; i < spawnCount; ++i)
		{
			GameObject newGo = GameObject.Instantiate(spawnTarget);
			newGo.SetActive(true);
			newGo.transform.parent = transform;
			newGo.transform.localPosition = new Vector3(Random.Range(-40, 40), 0, Random.Range(-40, 40));
			newGo.transform.localEulerAngles = new Vector3(0, Random.Range(0, 360), 0);
		}
	}
}
