using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private List<Joint> joints = null;

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

        CreateJoints();
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
            UpdateJoints(frame);
        }

        time += timeDelta;
    }

    public void Destroy()
    {
        if(joints != null)
        {
            for(int i = 0; i < joints.Count; ++i)
            {
                Joint joint = joints[i];
                joints[i] = null;
                Object.DestroyImmediate(joint.transform.gameObject);
            }
            joints = null;
        }
    }

    private int GetFrameIndex()
    {
        return (int)((time * playingClip.fps) % (playingClip.length * playingClip.fps));
    }

    private void UpdateJoints(GPUSkinningFrame frame)
    {
        if(joints == null)
        {
            return;
        }

        Matrix4x4[] matrices = frame.matrices;
        GPUSkinningBone[] bones = anim.bones;
        int numJoints = joints.Count;
        for(int i = 0; i < numJoints; ++i)
        {
            Joint joint = joints[i];
            joint.transform.localPosition = (frame.matrices[joint.boneIndex] * bones[joint.boneIndex].BindposeInv).MultiplyPoint(Vector3.zero);
        }
    }

    private void CreateJoints()
    {
        GPUSkinningBone[] bones = anim.bones;
        int numBones = bones == null ? 0 : bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            GPUSkinningBone bone = bones[i];
            if(bone.isExposed)
            {
                if(joints == null)
                {
                    joints = new List<Joint>();
                }

                Joint joint = new Joint();
                joints.Add(joint);

                joint.boneIndex = i;

                GameObject jointGo = new GameObject(bone.name);
                joint.transform = jointGo.transform;
                joint.transform.parent = go.transform;
                joint.transform.localPosition = Vector3.zero;
                joint.transform.localScale = Vector3.one;
                if(!Application.isPlaying)
                {
                    jointGo.hideFlags = HideFlags.DontSave;
                }
            }
        }
    }

    private class Joint
    {
        public int boneIndex = 0;

        public Transform transform = null;
    }
}
