using UnityEngine;
using System.Collections;

public class Adam_Player : MonoBehaviour
{
    public GPUSkinningAnimation anim = null;

    public Mesh mesh = null;

    public Material mtrl = null;

    private GPUSkinningPlayer player = null;

	void Start ()
    {
        player = new GPUSkinningPlayer(gameObject, anim, mesh, new Material(mtrl));
		player.Play("Adam_Walk");
	}

	void Update ()
    {
        player.Update(Time.deltaTime);	
	}
}
