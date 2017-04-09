using UnityEngine;
using System.Collections;

public class GPUSkinningPlayer
{
    private GameObject go = null;

    private GPUSkinningAnimation anim = null;

    private Mesh mesh = null;

    private Material mtrl = null;

    private MeshRenderer mr = null;

    private MeshFilter mf = null;

    private float time = 0;

    private GPUSkinningClip playingClip = null;

    private int playingFrameIndex = -1;

    private int shaderPropID_GPUSkinning_MatrixArray = 0;

    public float NormalizedTime
    {
        get
        {
            return playingClip == null || playingFrameIndex == -1 ? 0 : (float)playingFrameIndex / (playingClip.frames.Length - 1);
        }
    }

    public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningAnimation anim, Mesh mesh, Material mtrl)
    {
        go = attachToThisGo;
        this.anim = anim;
        this.mesh = mesh;
        this.mtrl = mtrl;

        mr = go.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = go.AddComponent<MeshRenderer>();
            if(!Application.isPlaying)
            {
                mr.hideFlags = HideFlags.DontSave;
            }
        }
        mf = go.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = go.AddComponent<MeshFilter>();
            if(!Application.isPlaying)
            {
                mf.hideFlags = HideFlags.DontSave;
            }
        }

        mr.sharedMaterial = mtrl;
        mf.sharedMesh = mesh;

        shaderPropID_GPUSkinning_MatrixArray = Shader.PropertyToID("_GPUSkinning_MatrixArray");
    }

    public void Play(string clipName)
    {
        GPUSkinningClip[] clips = anim.clips;
        int numClips = clips == null ? 0 : clips.Length;
        for(int i = 0; i < numClips; ++i)
        {
            if(clips[i].name == clipName)
            {
                if (playingClip != clips[i] || 
                    (playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && Mathf.Approximately(NormalizedTime, 1.0f)))
                {
                    playingClip = clips[i];
                    time = 0;
                    playingFrameIndex = -1;
                }
                return;
            }
        }
    }

    public void Update(float timeDelta)
    {
        if (playingClip == null)
        {
            return;
        }

        int frameIndex = 0;
        if (playingClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            frameIndex = GetFrameIndex();
        }
        else
        {
            if(time >= playingClip.length)
            {
                frameIndex = playingClip.frames.Length - 1;
            }
            else
            {
                frameIndex = GetFrameIndex();
            }
        }
        if (playingFrameIndex != frameIndex)
        {
            playingFrameIndex = frameIndex;
            GPUSkinningFrame frame = playingClip.frames[frameIndex];
            mtrl.SetMatrixArray(shaderPropID_GPUSkinning_MatrixArray, frame.matrices);
        }

        time += timeDelta;
    }

    private int GetFrameIndex()
    {
        return (int)((time * playingClip.fps) % (playingClip.length * playingClip.fps));
    }
}
