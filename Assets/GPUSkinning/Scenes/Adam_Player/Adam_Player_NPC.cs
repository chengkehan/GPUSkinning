using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Adam_Player_NPC : MonoBehaviour 
{
	private GPUSkinningPlayer player = null;

	private float actionTime = 0;

	private float time = 0;

	private void Start () 
	{
		player = GetComponent<GPUSkinningPlayerMono>().Player;
		player.Play("Idle");

		actionTime = Random.Range(5, 30);
	}

	private void Update () 
	{
		time += Time.deltaTime;
		if(time > actionTime)
		{
			time = 0;
			float rnd = Random.value;
			if(rnd < 0.25f)
			{
				player.CrossFade("TurnOnSpotLeftA", 0.2f);
			}
			else if(rnd < 0.5f)
			{
				player.CrossFade("TurnOnSpotRightA", 0.2f);
			}
			else if(rnd < 0.75f)
			{
				player.CrossFade("TurnOnSpotRightC", 0.2f);
			}
			else
			{
				player.CrossFade("TurnOnSpotLeftC", 0.2f);
			}
		}

		if(player.IsTimeAtTheEndOfLoop)
		{
			player.CrossFade("Idle", 0.8f);
		}
	}
}
