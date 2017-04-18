using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GPUSkinningPlayer
{
    private GameObject go = null;

    private MeshRenderer mr = null;
    public MeshRenderer MeshRenderer
    {
        get
        {
            return mr;
        }
    }

    private MeshFilter mf = null;

    private float time = 0;

    private GPUSkinningClip playingClip = null;

    private GPUSkinningPlayerResources res = null;

    private bool isPlaying = false;

    private int playingFrameIndex = -1;

    public GPUSkinningIndividualDifferenceMode IndividualDifferenceMode
    {
        set
        {
            res.IndividualDefferenceMode = value;
        }
        get
        {
            return res.IndividualDefferenceMode;
        }
    }

    public GPUSkinningBoneMode BoneMode
    {
        set
        {
            res.SetBoneMode(value, false);
        }
        get
        {
            return res.GetBoneMode();
        }
    }

    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
    }

    private List<GPUSkinningPlayerJoint> joints = null;
    public List<GPUSkinningPlayerJoint> Joints
    {
        get
        {
            return joints;
        }
    }

    public float NormalizedTime
    {
        get
        {
            return playingClip == null || playingFrameIndex == -1 ? 0 : (float)playingFrameIndex / (playingClip.frames.Length - 1);
        }
    }

    public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningPlayerResources res)
    {
        go = attachToThisGo;
        this.res = res;

        mr = go.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = go.AddComponent<MeshRenderer>();
        }
        mf = go.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = go.AddComponent<MeshFilter>();
        }

        mr.sharedMaterial = res.mtrl.Material;
        mf.sharedMesh = res.mesh;

        CreateJoints();

        res.SetBoneMode(GPUSkinningBoneMode.MATRIX_ARRAY, true);
    }

    public void Play(string clipName)
    {
        GPUSkinningClip[] clips = res.anim.clips;
        int numClips = clips == null ? 0 : clips.Length;
        for(int i = 0; i < numClips; ++i)
        {
            if(clips[i].name == clipName)
            {
                if (playingClip != clips[i] || 
                    (playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && Mathf.Approximately(NormalizedTime, 1.0f)))
                {
                    isPlaying = true;
                    playingClip = clips[i];
                    time = 0;
                    playingFrameIndex = -1;
                }
                return;
            }
        }
    }

    public void Stop()
    {
        isPlaying = false;
    }

#if UNITY_EDITOR
    public void Update_Editor(float timeDelta)
    {
        Update_Internal(timeDelta, true);
    }
#endif

    public void Update(float timeDelta)
    {
        Update_Internal(timeDelta, false);
    }

    private void Update_Internal(float timeDelta, bool isEnforced)
    {
        if (!isPlaying || playingClip == null)
        {
            return;
        }

        if(res == null || res.mtrl == null || res.mtrl.Material == null)
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
            if (time >= playingClip.length)
            {
                frameIndex = playingClip.frames.Length - 1;
            }
            else
            {
                frameIndex = GetFrameIndex();
            }
        }
        if (playingFrameIndex != frameIndex || isEnforced)
        {
            playingFrameIndex = frameIndex;
            if (res.mtrl.MaterialCanBeSetData() || isEnforced)
            {
                res.mtrl.MarkMaterialAsSet();
                GPUSkinningFrame frame = playingClip.frames[frameIndex];
                if (BoneMode == GPUSkinningBoneMode.MATRIX_ARRAY)
                {
                    res.UpdateBoneMode_MatrixArray(frame.matrices);
                }
                else
                {
                    res.UpdateBoneMode_TextureMatrix(playingClip, time);
                }
                UpdateJoints(frame);
            }
        }

        time += timeDelta;
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
        GPUSkinningBone[] bones = res.anim.bones;
        int numJoints = joints.Count;
        for(int i = 0; i < numJoints; ++i)
        {
            GPUSkinningPlayerJoint joint = joints[i];
            Transform jointTransform = Application.isPlaying ? joint.Transform : joint.transform;
            if (jointTransform != null)
            {
                jointTransform.localPosition = (frame.matrices[joint.BoneIndex] * bones[joint.BoneIndex].BindposeInv).MultiplyPoint(Vector3.zero);
            }
            else
            {
                joints.RemoveAt(i);
                --i;
                --numJoints;
            }
        }
    }

    private void CreateJoints()
    {
        if (joints == null)
        {
            GPUSkinningPlayerJoint[] existingJoints = go.GetComponentsInChildren<GPUSkinningPlayerJoint>();

            GPUSkinningBone[] bones = res.anim.bones;
            int numBones = bones == null ? 0 : bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                GPUSkinningBone bone = bones[i];
                if (bone.isExposed)
                {
                    if (joints == null)
                    {
                        joints = new List<GPUSkinningPlayerJoint>();
                    }

                    bool inTheExistingJoints = false;
                    if (existingJoints != null)
                    {
                        for (int j = 0; j < existingJoints.Length; ++j)
                        {
                            if(existingJoints[j] != null && existingJoints[j].BoneGUID == bone.guid)
                            {
                                if (existingJoints[j].BoneIndex != i)
                                {
                                    existingJoints[j].Init(i, bone.guid);
                                    GPUSkinningUtil.MarkAllScenesDirty();
                                }
                                joints.Add(existingJoints[j]);
                                existingJoints[j] = null;
                                inTheExistingJoints = true;
                                break;
                            }
                        }
                    }

                    if(!inTheExistingJoints)
                    {
                        GameObject jointGo = new GameObject(bone.name);
                        jointGo.transform.parent = go.transform;
                        jointGo.transform.localPosition = Vector3.zero;
                        jointGo.transform.localScale = Vector3.one;

                        GPUSkinningPlayerJoint joint = jointGo.AddComponent<GPUSkinningPlayerJoint>();
                        joints.Add(joint);
                        joint.Init(i, bone.guid);
                        GPUSkinningUtil.MarkAllScenesDirty();
                    }
                }
            }

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
                DelayCall = () => 
                {
                    UnityEditor.EditorApplication.delayCall -= DelayCall;
                    DeleteInvalidJoints(existingJoints);
                };
                UnityEditor.EditorApplication.delayCall += DelayCall;
#endif
            }
            else
            {
                DeleteInvalidJoints(existingJoints);
            }
        }
    }

    private void DeleteInvalidJoints(GPUSkinningPlayerJoint[] joints)
    {
        if (joints != null)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                if (joints[i] != null)
                {
                    for (int j = 0; j < joints[i].transform.childCount; ++j)
                    {
                        Transform child = joints[i].transform.GetChild(j);
                        child.parent = go.transform;
                        child.localPosition = Vector3.zero;
                    }
                    Object.DestroyImmediate(joints[i].transform.gameObject);
                    GPUSkinningUtil.MarkAllScenesDirty();
                }
            }
        }
    }
}
