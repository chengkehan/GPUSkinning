using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GPUSkinningPlayer
{
    public delegate void OnAnimEvent(GPUSkinningPlayer player, int eventId);

    private GameObject go = null;

    private Transform transform = null;

    private MeshRenderer mr = null;

    private MeshFilter mf = null;

    private float time = 0;

    private float timeDiff = 0;

    private float crossFadeTime = -1;

    private float crossFadeProgress = 0;

    private float lastPlayedTime = 0;

    private GPUSkinningClip lastPlayedClip = null;

    private int lastPlayingFrameIndex = -1;

    private GPUSkinningClip lastPlayingClip = null;

    private GPUSkinningClip playingClip = null;

    private GPUSkinningPlayerResources res = null;

    private MaterialPropertyBlock mpb = null;

    private int rootMotionFrameIndex = -1;

    public event OnAnimEvent onAnimEvent;

    private bool rootMotionEnabled = false;
    public bool RootMotionEnabled
    {
        get
        {
            return rootMotionEnabled;
        }
        set
        {
            rootMotionFrameIndex = -1;
            rootMotionEnabled = value;
        }
    }

    private GPUSKinningCullingMode cullingMode = GPUSKinningCullingMode.CullUpdateTransforms;
    public GPUSKinningCullingMode CullingMode
    {
        get
        {
            return Application.isPlaying ? cullingMode : GPUSKinningCullingMode.AlwaysAnimate;
        }
        set
        {
            cullingMode = value;
        }
    }

    private bool visible = false;
    public bool Visible
    {
        get
        {
            return Application.isPlaying ? visible : true;
        }
        set
        {
            visible = value;
        }
    }

    private bool lodEnabled = true;
    public bool LODEnabled
    {
        get
        {
            return lodEnabled;
        }
        set
        {
            lodEnabled = value;
            res.LODSettingChanged(this);
        }
    }

    private bool isPlaying = false;
    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
    }

    public string PlayingClipName
    {
        get
        {
            return playingClip == null ? null : playingClip.name;
        }
    }
    
    public Vector3 Position
    {
        get
        {
            return transform == null ? Vector3.zero : transform.position;
        }
    }

    public Vector3 LocalPosition
    {
        get
        {
            return transform == null ? Vector3.zero : transform.localPosition;
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

    public GPUSkinningWrapMode WrapMode
    {
        get
        {
            return playingClip == null ? GPUSkinningWrapMode.Once : playingClip.wrapMode;
        }
    }

    public bool IsTimeAtTheEndOfLoop
    {
        get
        {
            if(playingClip == null)
            {
                return false;
            }
            else
            {
                return GetFrameIndex() == ((int)(playingClip.length * playingClip.fps) - 1);
            }
        }
    }

    public float NormalizedTime
    {
        get
        {
            if(playingClip == null)
            {
                return 0;
            }
            else
            {
                return (float)GetFrameIndex() / (float)((int)(playingClip.length * playingClip.fps) - 1);
            }
        }
        set
        {
            if(playingClip != null)
            {
                float v = Mathf.Clamp01(value);
                if(WrapMode == GPUSkinningWrapMode.Once)
                {
                    this.time = v * playingClip.length;
                }
                else if(WrapMode == GPUSkinningWrapMode.Loop)
                {
                    if(playingClip.individualDifferenceEnabled)
                    {
                        res.Time = playingClip.length +  v * playingClip.length - this.timeDiff;
                    }
                    else
                    {
                        res.Time = v * playingClip.length;
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }
            }
        }
    }

    public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningPlayerResources res)
    {
        go = attachToThisGo;
        transform = go.transform;
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

        GPUSkinningMaterial mtrl = GetCurrentMaterial();
        mr.sharedMaterial = mtrl == null ? null : mtrl.material;
        mf.sharedMesh = res.mesh;

        mpb = new MaterialPropertyBlock();

        ConstructJoints();
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
                    (playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) || 
                    (playingClip != null && !isPlaying))
                {
                    SetNewPlayingClip(clips[i]);
                }
                return;
            }
        }
    }

    public void CrossFade(string clipName, float fadeLength)
    {
        if (playingClip == null)
        {
            Play(clipName);
        }
        else
        {
            GPUSkinningClip[] clips = res.anim.clips;
            int numClips = clips == null ? 0 : clips.Length;
            for (int i = 0; i < numClips; ++i)
            {
                if (clips[i].name == clipName)
                {
                    if (playingClip != clips[i])
                    {
                        crossFadeProgress = 0;
                        crossFadeTime = fadeLength;
                        SetNewPlayingClip(clips[i]);
                        return;
                    }
                    if ((playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                        (playingClip != null && !isPlaying))
                    {
                        SetNewPlayingClip(clips[i]);
                        return;
                    }
                }
            }
        }
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Resume()
    {
        if(playingClip != null)
        {
            isPlaying = true;
        }
    }

    public void SetLODMesh(Mesh mesh)
    {
        if(!LODEnabled)
        {
            mesh = res.mesh;
        }

        if(mf != null && mf.sharedMesh != mesh)
        {
            mf.sharedMesh = mesh;
        }
    }

#if UNITY_EDITOR
    public void Update_Editor(float timeDelta)
    {
        Update_Internal(timeDelta);
    }
#endif

    public void Update(float timeDelta)
    {
        Update_Internal(timeDelta);
    }

    private void FillEvents(GPUSkinningClip clip, GPUSkinningBetterList<GPUSkinningAnimEvent> events)
    {
        events.Clear();
        if(clip != null && clip.events != null && clip.events.Length > 0)
        {
            events.AddRange(clip.events);
        }
    }

    private void SetNewPlayingClip(GPUSkinningClip clip)
    {
        lastPlayedClip = playingClip;
        lastPlayedTime = GetCurrentTime();

        isPlaying = true;
        playingClip = clip;
        rootMotionFrameIndex = -1;
        time = 0;
        timeDiff = Random.Range(0, playingClip.length);
    }

    private void Update_Internal(float timeDelta)
    {
        if (!isPlaying || playingClip == null)
        {
            return;
        }

        GPUSkinningMaterial currMtrl = GetCurrentMaterial();
        if(currMtrl == null)
        {
            return;    
        }

        if(mr.sharedMaterial != currMtrl.material)
        {
            mr.sharedMaterial = currMtrl.material;
        }

        if (playingClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            UpdateMaterial(timeDelta, currMtrl);
        }
        else if(playingClip.wrapMode == GPUSkinningWrapMode.Once)
        {
            if (time >= playingClip.length)
            {
                time = playingClip.length;
                UpdateMaterial(timeDelta, currMtrl);
            }
            else
            {
                UpdateMaterial(timeDelta, currMtrl);
                time += timeDelta;
                if(time > playingClip.length)
                {
                    time = playingClip.length;
                }
            }
        }
        else
        {
            throw new System.NotImplementedException();
        }

        crossFadeProgress += timeDelta;
        lastPlayedTime += timeDelta;
    }

    private void UpdateEvents(GPUSkinningClip playingClip, int playingFrameIndex, GPUSkinningClip corssFadeClip, int crossFadeFrameIndex)
    {
        UpdateClipEvent(playingClip, playingFrameIndex);
        UpdateClipEvent(corssFadeClip, crossFadeFrameIndex);
    }

    private void UpdateClipEvent(GPUSkinningClip clip, int frameIndex)
    {
        if(clip == null || clip.events == null || clip.events.Length == 0)
        {
            return;
        }

        GPUSkinningAnimEvent[] events = clip.events;
        int numEvents = events.Length;
        for(int i = 0; i < numEvents; ++i)
        {
            if(events[i].frameIndex == frameIndex && onAnimEvent != null)
            {
                onAnimEvent(this, events[i].eventId);
                break;
            }
        }
    }

    private void UpdateMaterial(float deltaTime, GPUSkinningMaterial currMtrl)
    {
        int frameIndex = GetFrameIndex();
        if(lastPlayingClip == playingClip && lastPlayingFrameIndex == frameIndex)
        {
            res.Update(deltaTime, currMtrl);
            return;
        }
        lastPlayingClip = playingClip;
        lastPlayingFrameIndex = frameIndex;

        float blend_crossFade = 1;
        int frameIndex_crossFade = -1;
        GPUSkinningFrame frame_crossFade = null;
        if (res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            frameIndex_crossFade = GetCrossFadeFrameIndex();
            frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
            blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
        }

        GPUSkinningFrame frame = playingClip.frames[frameIndex];
        if (Visible || 
            CullingMode == GPUSKinningCullingMode.AlwaysAnimate)
        {
            res.Update(deltaTime, currMtrl);
            res.UpdatePlayingData(
                mpb, playingClip, frameIndex, frame, playingClip.rootMotionEnabled && rootMotionEnabled,
                lastPlayedClip, GetCrossFadeFrameIndex(), crossFadeTime, crossFadeProgress
            );
            mr.SetPropertyBlock(mpb);
            UpdateJoints(frame);
        }

        if (playingClip.rootMotionEnabled && rootMotionEnabled && frameIndex != rootMotionFrameIndex)
        {
            if (CullingMode != GPUSKinningCullingMode.CullCompletely)
            {
                rootMotionFrameIndex = frameIndex;
                DoRootMotion(frame_crossFade, 1 - blend_crossFade, false);
                DoRootMotion(frame, blend_crossFade, true);
            }
        }

        UpdateEvents(playingClip, frameIndex, frame_crossFade == null ? null : lastPlayedClip, frameIndex_crossFade);
    }

    private GPUSkinningMaterial GetCurrentMaterial()
    {
        if(res == null)
        {
            return null;
        }

        if(playingClip == null)
        {
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
        }
        if(playingClip.rootMotionEnabled && rootMotionEnabled)
        {
            if(res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                if(lastPlayedClip.rootMotionEnabled)
                {
                    return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOn_CrossFadeRootOn);
                }
                return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOn_CrossFadeRootOff);
            }
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOn_BlendOff);
        }
        if(res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
        {
            if (lastPlayedClip.rootMotionEnabled)
            {
                return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOn_CrossFadeRootOn);
            }
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOn_CrossFadeRootOff);
        }
        else
        {
            return res.GetMaterial(GPUSkinningPlayerResources.MaterialState.RootOff_BlendOff);
        }
    }

    private void DoRootMotion(GPUSkinningFrame frame, float blend, bool doRotate)
    {
        if(frame == null)
        {
            return;
        }

        Quaternion deltaRotation = frame.rootMotionDeltaPositionQ;
        Vector3 newForward = deltaRotation * transform.forward;
        Vector3 deltaPosition = newForward * frame.rootMotionDeltaPositionL * blend;
        transform.Translate(deltaPosition, Space.World);

        if (doRotate)
        {
            transform.rotation *= frame.rootMotionDeltaRotation;
        }
    }

    private float GetCurrentTime()
    {
        float time = 0;
        if (WrapMode == GPUSkinningWrapMode.Once)
        {
            time = this.time;
        }
        else if (WrapMode == GPUSkinningWrapMode.Loop)
        {
            time = res.Time + (playingClip.individualDifferenceEnabled ? this.timeDiff : 0);
        }
        else
        {
            throw new System.NotImplementedException();
        }
        return time;
    }

    private int GetFrameIndex()
    {
        float time = GetCurrentTime();
        if (playingClip.length == time)
        {
            return GetTheLastFrameIndex_WrapMode_Once(playingClip);
        }
        else
        {
            return GetFrameIndex_WrapMode_Loop(playingClip, time);
        }
    }

    private int GetCrossFadeFrameIndex()
    {
        if (lastPlayedClip == null)
        {
            return 0;
        }

        if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Once)
        {
            if (lastPlayedTime >= lastPlayedClip.length)
            {
                return GetTheLastFrameIndex_WrapMode_Once(lastPlayedClip);
            }
            else
            {
                return GetFrameIndex_WrapMode_Loop(lastPlayedClip, lastPlayedTime);
            }
        }
        else if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            return GetFrameIndex_WrapMode_Loop(lastPlayedClip, lastPlayedTime);
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }

    private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip)
    {
        return (int)(clip.length * clip.fps) - 1;
    }

    private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time)
    {
        return (int)(time * clip.fps) % (int)(clip.length * clip.fps);
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
                // TODO: Update Joint when Animation Blend

                Matrix4x4 jointMatrix = frame.matrices[joint.BoneIndex] * bones[joint.BoneIndex].BindposeInv;
                if(playingClip.rootMotionEnabled && rootMotionEnabled)
                {
                    jointMatrix = frame.RootMotionInv(res.anim.rootBoneIndex) * jointMatrix;
                }

                jointTransform.localPosition = jointMatrix.MultiplyPoint(Vector3.zero);

                Vector3 jointDir = jointMatrix.MultiplyVector(Vector3.right);
                Quaternion jointRotation = Quaternion.FromToRotation(Vector3.right, jointDir);
                jointTransform.localRotation = jointRotation;
            }
            else
            {
                joints.RemoveAt(i);
                --i;
                --numJoints;
            }
        }
    }

    private void ConstructJoints()
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
