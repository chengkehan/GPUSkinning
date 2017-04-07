using UnityEngine;
using System.Collections;

public class GPUSkinningPreview : MonoBehaviour
{
    public GPUSkinningAnimation anim = null;

    public Mesh mesh = null;

    public Material mtrl = null;

    public string clipName = null;

    public GPUSkinningPlayer player = null;

    public void Init()
    {
        if(player == null)
        {
            player = new GPUSkinningPlayer(gameObject, anim, mesh, new Material(mtrl));
            player.Play(clipName);
        }
    }

    public void Play(string clipName)
    {
        if(player != null)
        {
            player.Play(clipName);
        }
    }

    public void DoUpdate(float deltaTime) 
    {
        if (player != null)
        {
            player.Update(deltaTime);
        }
    }
}
