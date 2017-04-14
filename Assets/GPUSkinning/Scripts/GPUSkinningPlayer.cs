using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GPUSkinningPlayer
{
    private GameObject go = null;

    private GPUSkinningAnimation anim = null;

    private Mesh mesh = null;

    private Material mtrl = null;

    private Texture2D texture = null;

    private MeshRenderer mr = null;

    private MeshFilter mf = null;

    private float time = 0;

    private GPUSkinningClip playingClip = null;

    private bool isPlaying = false;

    private int playingFrameIndex = -1;

    private int shaderPropID_GPUSkinning_MatrixArray = 0;

    private int shaderPropID_GPUSkinning_TextureMatrix = 0;

    private int shaderPropID_GPUSkinning_NumPixelsPerFrame = 0;

    private int shaderPropID_GPUSkinning_TextureSize = 0;

    private int shaderPropID_GPUSkinning_ClipLength = 0;

    private int shaderPropID_GPUSkinning_ClipFPS = 0;

    private int shaderPorpID_GPUSkinning_Time = 0;

    private int shaderPropID_GPUSkinning_PixelSegmentation = 0;

    private GPUSkinningPlayerMode mode = GPUSkinningPlayerMode.MATRIX_ARRAY;
    public GPUSkinningPlayerMode Mode
    {
        set
        {
            mode = value;
            UpdateMode(false);
        }
        get
        {
            return mode;
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

    public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningAnimation anim, Mesh mesh, Material mtrl, Texture2D texture)
    {
        go = attachToThisGo;
        this.anim = anim;
        this.mesh = mesh;
        this.mtrl = mtrl;

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

        mr.sharedMaterial = this.mtrl;
        mf.sharedMesh = mesh;

        this.texture = texture;

        shaderPropID_GPUSkinning_MatrixArray = Shader.PropertyToID("_GPUSkinning_MatrixArray");
        shaderPropID_GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
        shaderPropID_GPUSkinning_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_NumPixelsPerFrame");
        shaderPropID_GPUSkinning_TextureSize = Shader.PropertyToID("_GPUSkinning_TextureSize");
        shaderPropID_GPUSkinning_ClipLength = Shader.PropertyToID("_GPUSkinning_ClipLength");
        shaderPropID_GPUSkinning_ClipFPS = Shader.PropertyToID("_GPUSkinning_ClipFPS");
        shaderPorpID_GPUSkinning_Time = Shader.PropertyToID("_GPUSkinning_Time");
        shaderPropID_GPUSkinning_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_PixelSegmentation");

        CreateJoints();

        UpdateMode(true);
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

    private void UpdateMode(bool isEnforced)
    {
        if(mtrl != null)
        {
            if(mode == GPUSkinningPlayerMode.MATRIX_ARRAY)
            {
                if (mtrl.IsKeywordEnabled("GPU_SKINNING_TEXTURE_MATRIX") || isEnforced)
                {
                    mtrl.EnableKeyword("GPU_SKINNING_MATRIX_ARRAY");
                    mtrl.DisableKeyword("GPU_SKINNING_TEXTURE_MATRIX");
                }
            }
            else
            {
                
                if (mtrl.IsKeywordEnabled("GPU_SKINNING_MATRIX_ARRAY") || isEnforced)
                {
                    mtrl.DisableKeyword("GPU_SKINNING_MATRIX_ARRAY");
                    mtrl.EnableKeyword("GPU_SKINNING_TEXTURE_MATRIX");
                }
            }
        }
    }

    private void Update_Internal(float timeDelta, bool isEnforced)
    {
#if UNITY_EDITOR
        bool isMeshRenderEnabled = mtrl != null;
        if (mr != null && mr.enabled != isMeshRenderEnabled)
        {
            mr.enabled = isMeshRenderEnabled;
        }
#endif

        if (!isPlaying || playingClip == null)
        {
            return;
        }

        if(mtrl == null)
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
            GPUSkinningFrame frame = playingClip.frames[frameIndex];
            if (mode == GPUSkinningPlayerMode.MATRIX_ARRAY)
            {
                mtrl.SetMatrixArray(shaderPropID_GPUSkinning_MatrixArray, frame.matrices);
            }
            else
            {
                mtrl.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture);
                mtrl.SetFloat(shaderPropID_GPUSkinning_NumPixelsPerFrame, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/);
                mtrl.SetVector(shaderPropID_GPUSkinning_TextureSize, new Vector4(anim.textureWidth, anim.textureHeight, 0, 0));
                mtrl.SetFloat(shaderPropID_GPUSkinning_ClipLength, playingClip.length);
                mtrl.SetFloat(shaderPropID_GPUSkinning_ClipFPS, playingClip.fps);
                mtrl.SetFloat(shaderPorpID_GPUSkinning_Time, time);
                mtrl.SetFloat(shaderPropID_GPUSkinning_PixelSegmentation, playingClip.pixelSegmentation);
            }
            UpdateJoints(frame);
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
        GPUSkinningBone[] bones = anim.bones;
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

            GPUSkinningBone[] bones = anim.bones;
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
