using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerMono : MonoBehaviour
{
    public GPUSkinningAnimation anim = null;

    public Mesh mesh = null;

    public Material mtrl = null;

    private GPUSkinningPlayer player = null;
    public GPUSkinningPlayer Player
    {
        get
        {
            return player;
        }
    }

    private void Start()
    {
        player = new GPUSkinningPlayer(gameObject, anim, mesh, new Material(mtrl));

        if (anim != null && anim.clips != null && anim.clips.Length > 0)
        {
            player.Play(anim.clips[0].name);
        }
    }

    private void Update()
    {
        player.Update(Time.deltaTime);
    }
}
