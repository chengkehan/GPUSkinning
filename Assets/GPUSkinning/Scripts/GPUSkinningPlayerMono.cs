using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GPUSkinningPlayerMono : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    public GPUSkinningAnimation anim = null;

    [HideInInspector]
    [SerializeField]
    public Mesh mesh = null;

    [HideInInspector]
    [SerializeField]
    public Material mtrl = null;

    [HideInInspector]
    [SerializeField]
    public TextAsset texture = null;

    private GPUSkinningPlayer player = null;
    public GPUSkinningPlayer Player
    {
        get
        {
            return player;
        }
    }

    public void Init()
    {
        if(player != null)
        {
            return;
        }

        if (anim != null && mesh != null && mtrl != null)
        {
            player = new GPUSkinningPlayer(gameObject, anim, mesh, mtrl, texture);

            if (anim != null && anim.clips != null && anim.clips.Length > 0)
            {
                player.Play(anim.clips[0].name);
            }
        }
    }

#if UNITY_EDITOR
    public void Update_Editor(float deltaTime)
    {
        if(player != null && !Application.isPlaying)
        {
            player.Update_Editor(deltaTime);
        }
    }

    private void OnValidate()
    {
        Init();
        Update_Editor(0);
    }
#endif

    private void Start()
    {
        Init();
#if UNITY_EDITOR
        Update_Editor(0); 
#endif
    }

    private void Update()
    {
        if (player != null)
        {
#if UNITY_EDITOR
            if(Application.isPlaying)
            {
                player.Update(Time.deltaTime);
            }
            else
            {
                player.Update_Editor(0);
            }
#else
            player.Update(Time.deltaTime);
#endif
        }
    }

    private void OnDestroy()
    {
        if(player != null)
        {
            player.Destroy();
            player = null;
        }
    }
}
