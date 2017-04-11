using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(GPUSkinningSampler))]
public class GPUSkinningSamplerEditor : Editor 
{
	private GPUSkinningAnimation anim = null;

	private Mesh mesh = null;

	private Material mtrl = null;

	private RenderTexture rt = null;

    private RenderTexture rtGamma = null;

    private Material linearToGammeMtrl = null;

	private Camera cam = null;

    private GPUSkinningPreview preview = null;

    private int previewClipIndex = 0;

    private float time = 0;

    private Vector3 camLookAtOffset = Vector3.zero;

    private Rect previewEditBtnRect;

    private Rect interactionRect;

    private GameObject[] boundsGos = null;

    private Bounds bounds;

    private Material boundsMtrl = null;

    private bool isBoundsVisible = true;

    private GameObject[] arrowGos = null;

    private Material[] arrowMtrls = null;

    private float boundsAutoExt = 0.1f;

    private bool isBoundsFoldout = true;

    private bool isJointsFoldout = true;

    public override void OnInspectorGUI ()
	{
		GPUSkinningSampler sampler = target as GPUSkinningSampler;
		if(sampler == null)
		{
			return;
		}

        sampler.MappingAnimationClips();

        OnGUI_Sampler(sampler);

        OnGUI_Preview(sampler);

        if (preview != null)
        {
            Repaint();
        }
	}

    private void OnGUI_Sampler(GPUSkinningSampler sampler)
    {
        BeginBox();
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animName"), new GUIContent("Animation Name"));

            GUI.enabled = false;
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anim"), new GUIContent());
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("savedMesh"), new GUIContent());
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("savedMtrl"), new GUIContent());
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("savedShader"), new GUIContent());
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUI.enabled = true;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("skinQuality"), new GUIContent("Quality"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderType"), new GUIContent("Shader Type"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("rootBoneTransform"), new GUIContent("Root Bone"));

            OnGUI_AnimClips(sampler);

            if (GUILayout.Button("Step1: Play Scene"))
            {
                DestroyPreview();
                EditorApplication.isPlaying = true;
            }

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Step2: Start Sample"))
                {
                    DestroyPreview();
                    LockInspector(true);
                    sampler.BeginSample();
                    sampler.StartSample();
                }
            }
        }
        EndBox();
    }

    private void OnGUI_AnimClips(GPUSkinningSampler sampler)
    {
        BeginBox();
        {
            EditorGUILayout.PrefixLabel("Sample Clips");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateOrNew"), new GUIContent("Update Or New"));

            GUI.enabled = sampler.animation == null;
            int no = serializedObject.FindProperty("animClips.Array.size").intValue;
            int no2 = serializedObject.FindProperty("wrapModes.Array.size").intValue;
            int no3 = serializedObject.FindProperty("isSelected.Array.size").intValue;
            int c = EditorGUILayout.IntField("Size", no);
            if (c != no)
            {
                serializedObject.FindProperty("animClips.Array.size").intValue = c;
            }
            if (c != no2)
            {
                serializedObject.FindProperty("wrapModes.Array.size").intValue = c;
            }
            if (c != no3)
            {
                serializedObject.FindProperty("isSelected.Array.size").intValue = c;
            }
            GUI.enabled = true;

            for (int i = 0; i < no; i++)
            {
                var prop = serializedObject.FindProperty(string.Format("animClips.Array.data[{0}]", i));
                var prop2 = serializedObject.FindProperty(string.Format("wrapModes.Array.data[{0}]", i));
                var prop3 = serializedObject.FindProperty(string.Format("isSelected.Array.data[{0}]", i));
                if (prop != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(prop3);
                        EditorGUILayout.PropertyField(prop2, new GUIContent());
                        GUI.enabled = sampler.animation == null;
                        EditorGUILayout.PropertyField(prop, new GUIContent());
                        GUI.enabled = true;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EndBox();
    }

    private void OnGUI_Preview(GPUSkinningSampler sampler)
    {
        BeginBox();
        {
            if (GUILayout.Button("Preview/Edit"))
            {
                anim = sampler.anim;
                mesh = sampler.savedMesh;
                mtrl = sampler.savedMtrl;
                if (mesh != null)
                {
                    bounds = mesh.bounds;
                }
                if (anim == null || mesh == null || mtrl == null)
                {
                    EditorUtility.DisplayDialog("GPUSkinning", "Missing Sampling Resources", "OK");
                }
                else
                {
                    if (rt == null)
                    {
                        linearToGammeMtrl = new Material(Shader.Find("GPUSkinning/GPUSkinningSamplerEditor_LinearToGamma"));
                        linearToGammeMtrl.hideFlags = HideFlags.HideAndDontSave;

                        rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
                        rt.hideFlags = HideFlags.HideAndDontSave;

                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            rtGamma = new RenderTexture(512, 512, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                            rtGamma.hideFlags = HideFlags.HideAndDontSave;
                        }

                        GameObject camGo = new GameObject("GPUSkinningSamplerEditor_CameraGo");
                        camGo.hideFlags = HideFlags.HideAndDontSave;
                        cam = camGo.AddComponent<Camera>();
                        cam.hideFlags = HideFlags.HideAndDontSave;
                        cam.farClipPlane = 100;
                        cam.targetTexture = rt;
                        cam.enabled = false;
                        camGo.transform.position = new Vector3(100, 100, 100);

                        previewClipIndex = 0;

                        GameObject previewGo = new GameObject("GPUSkinningPreview_Go");
                        previewGo.hideFlags = HideFlags.HideAndDontSave;
                        previewGo.transform.position = new Vector3(100, 100, 103);
                        preview = previewGo.AddComponent<GPUSkinningPreview>();
                        preview.hideFlags = HideFlags.HideAndDontSave;
                        preview.anim = anim;
                        preview.mesh = mesh;
                        preview.mtrl = mtrl;
                        preview.clipName = anim.clips == null || anim.clips.Length == 0 ? null : anim.clips[previewClipIndex].name;
                        preview.Init();
                    }
                }
            }
            GetLastGUIRect(ref previewEditBtnRect);

            if (rt != null)
            {
                int previewRectSize = Mathf.Min((int)(previewEditBtnRect.width * 0.9f), 512);
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    {
                        RenderTexture tempRT = RenderTexture.active;
                        Graphics.Blit(rt, rtGamma, linearToGammeMtrl);
                        RenderTexture.active = tempRT;
                        GUILayout.Box(rtGamma, GUILayout.Width(previewRectSize), GUILayout.Height(previewRectSize));
                    }
                    else
                    {
                        GUILayout.Box(rt, GUILayout.Width(previewRectSize), GUILayout.Height(previewRectSize));
                    }
                    GetLastGUIRect(ref interactionRect);
                    PreviewInteraction(interactionRect);

                    EditorGUI.ProgressBar(new Rect(interactionRect.x, interactionRect.y + interactionRect.height, interactionRect.width, 5), preview.player.NormalizedTime, string.Empty);

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                OnGUI_PreviewClipsOptions();

                OnGUI_EditBounds();

                EditorGUILayout.Space();

                OnGUI_Joints();
            }
        }
        EndBox();

        serializedObject.ApplyModifiedProperties();
    }

    private void OnGUI_EditBounds()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            BeginBox();
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        isBoundsFoldout = EditorGUILayout.Foldout(isBoundsFoldout, isBoundsFoldout ? string.Empty : "Bounds");
                        SetEditorPrefsBool("isBoundsFoldout", isBoundsFoldout);
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (isBoundsFoldout)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Bounds");
                            boundsAutoExt = GUILayout.HorizontalSlider(boundsAutoExt, 0.0f, 1.0f);
                            if (GUILayout.Button("Calculate Auto", GUILayout.Width(100)))
                            {
                                CalculateBoundsAuto();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();

                        isBoundsVisible = EditorGUILayout.Toggle("Visible", isBoundsVisible);

                        EditorGUILayout.Space();

                        Color tempGUIColor = GUI.color;
                        Vector3 boundsCenter = bounds.center;
                        Vector3 boundsExts = bounds.extents;
                        {
                            GUI.color = Color.red;
                            boundsCenter.x = EditorGUILayout.Slider("center.x", boundsCenter.x, -5, 5);
                            boundsExts.x = EditorGUILayout.Slider("extends.x", boundsExts.x, 0.1f, 5);

                            GUI.color = Color.green;
                            boundsCenter.y = EditorGUILayout.Slider("center.y", boundsCenter.y, -5, 5);
                            boundsExts.y = EditorGUILayout.Slider("extends.y", boundsExts.y, 0.1f, 5);
                            GUI.color = Color.blue;
                            boundsCenter.z = EditorGUILayout.Slider("center.z", boundsCenter.z, -5, 5);
                            boundsExts.z = EditorGUILayout.Slider("extends.z", boundsExts.z, 0.1f, 5);
                        }
                        bounds.center = boundsCenter;
                        bounds.extents = boundsExts;
                        GUI.color = tempGUIColor;

                        EditorGUILayout.Space();

                        if (GUILayout.Button("Apply"))
                        {
                            mesh.bounds = bounds;
                            anim.bounds = bounds;
                            EditorUtility.SetDirty(mesh);
                            EditorUtility.SetDirty(anim);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EndBox();
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnGUI_Joints()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            BeginBox();
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        isJointsFoldout = EditorGUILayout.Foldout(isJointsFoldout, isJointsFoldout ? string.Empty : "Joints");
                        SetEditorPrefsBool("isJointsFoldout", isJointsFoldout);
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (isJointsFoldout)
                    {
                        OnGUI_Bone(anim.bones[anim.rootBoneIndex], 0);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EndBox();
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnGUI_Bone(GPUSkinningBone bone, int indentLevel)
    {
        GUILayout.BeginHorizontal();
        {
            for (int i = 0; i < indentLevel; ++i)
            {
                GUILayout.Space(20);
            }

            EditorGUI.BeginChangeCheck();
            bool isExposed = GUILayout.Toggle(bone.isExposed, bone.name);
            if(EditorGUI.EndChangeCheck())
            {
                bone.isExposed = isExposed;
                EditorUtility.SetDirty(anim);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        GUILayout.EndHorizontal();

        int numChildren = bone.childrenBonesIndices == null ? 0 : bone.childrenBonesIndices.Length;
        for (int i = 0; i < numChildren; ++i)
        {
            OnGUI_Bone(anim.bones[bone.childrenBonesIndices[i]], indentLevel + 1);
        }
    }

    private void OnGUI_PreviewClipsOptions()
    {
        if(anim.clips == null || anim.clips.Length == 0 || preview == null)
        {
            return;
        }

        previewClipIndex = Mathf.Clamp(previewClipIndex, 0, anim.clips.Length - 1);
        string[] options = new string[anim.clips.Length];
        for(int i = 0; i < anim.clips.Length; ++i)
        {
            options[i] = anim.clips[i].name;
        }
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            previewClipIndex = EditorGUILayout.Popup(string.Empty, previewClipIndex, options);
            if(EditorGUI.EndChangeCheck())
            {
                preview.Play(options[previewClipIndex]);
            }
            if (anim.clips[previewClipIndex].wrapMode == GPUSkinningWrapMode.Once)
            {
                if (GUILayout.Button("Play", GUILayout.Width(50)))
                {
                    preview.Play(options[previewClipIndex]);
                }
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void PreviewInteraction(Rect rect)
    {
        if (cam != null)
        {
            Transform camTrans = cam.transform;
            Transform modelTrans = preview.transform;

            Vector3 lookAtPoint = modelTrans.position + camLookAtOffset;

            Event e = Event.current;

            Vector2 mousePos = e.mousePosition;
            if(mousePos.x < rect.x || mousePos.x > rect.x + rect.width || mousePos.y < rect.y || mousePos.y > rect.y + rect.height)
            {
                return;
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Orbit);

            if (e.type == EventType.ScrollWheel)
            {
                camTrans.Translate(0, 0, -e.delta.y * 0.1f, Space.Self);
                Vector3 v = camTrans.position - lookAtPoint;
                if(v.magnitude < 1)
                {
                    camTrans.position = lookAtPoint - v.normalized;
                }
            }
            else if(e.type == EventType.MouseDrag)
            {
                if ((e.alt && e.control) || e.button == 2)
                {
                    camLookAtOffset.y += e.delta.y * 0.02f;
                }
                else
                {
                    Vector3 v = camTrans.position - lookAtPoint;
                    camTrans.Translate(-e.delta.x * 0.1f, e.delta.y * 0.1f, 0, Space.Self);
                    Vector3 v2 = camTrans.position - lookAtPoint;
                    v2 = v2.normalized * v.magnitude;
                    camTrans.position = lookAtPoint + v2;
                }
            }

            camTrans.LookAt(lookAtPoint);
        }
    }

    private void PreviewDrawArrows()
    {
        if(arrowGos == null)
        {
            arrowGos = new GameObject[3];
            arrowMtrls = new Material[arrowGos.Length];
            for(int i = 0; i < arrowGos.Length; ++i)
            {
                GameObject go = Instantiate<GameObject>(Resources.Load<GameObject>("Model/GPUSkinningSamplerEditor_Arrow"));
                go.hideFlags = HideFlags.HideAndDontSave;
                arrowGos[i] = go;

                arrowMtrls[i] = new Material(Shader.Find("GPUSkinning/GPUSkinningSamplerEditor_UnlitColor"));
                arrowMtrls[i].hideFlags = HideFlags.HideAndDontSave;

                go.GetComponentInChildren<MeshRenderer>().sharedMaterial = arrowMtrls[i];
            }

            arrowMtrls[0].color = Color.red;
            arrowMtrls[1].color = Color.green;
            arrowMtrls[2].color = Color.blue;
        }

        if(arrowGos != null && preview != null)
        {
            arrowGos[0].transform.parent = preview.transform;
            arrowGos[0].transform.localPosition = Vector3.zero;
            arrowGos[0].transform.localEulerAngles = new Vector3(0, 180, 0);

            arrowGos[1].transform.parent = preview.transform;
            arrowGos[1].transform.localPosition = Vector3.zero;
            arrowGos[1].transform.localEulerAngles = new Vector3(0, 0, -90);

            arrowGos[2].transform.parent = preview.transform;
            arrowGos[2].transform.localPosition = Vector3.zero;
            arrowGos[2].transform.localEulerAngles = new Vector3(0, 90, 0);

            for (int i = 0; i < arrowGos.Length; ++i)
            {
                GameObject arrowGo = arrowGos[i];
                arrowGo.SetActive(isBoundsVisible);
            }
        }
    }

    private void PreviewDrawBounds()
    {
        if(boundsGos == null)
        {
            boundsMtrl = new Material(Shader.Find("GPUSkinning/GPUSkinningSamplerEditor_UnlitColor"));
            boundsMtrl.color = Color.white;

            boundsGos = new GameObject[12];
            for(int i = 0; i < boundsGos.Length; ++i)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.GetComponent<MeshRenderer>().sharedMaterial = boundsMtrl;
                boundsGos[i] = go;
            }
        }

        if(boundsGos != null && preview != null)
        {
            float thinness = 0.01f;
            boundsGos[0].transform.parent = preview.transform;
            boundsGos[0].transform.localScale = new Vector3(thinness, thinness, bounds.extents.z * 2);
            boundsGos[0].transform.localPosition = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, 0);
            boundsGos[1].transform.parent = preview.transform;
            boundsGos[1].transform.localScale = new Vector3(thinness, thinness, bounds.extents.z * 2);
            boundsGos[1].transform.localPosition = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, 0);
            boundsGos[2].transform.parent = preview.transform;
            boundsGos[2].transform.localScale = new Vector3(bounds.extents.x * 2, thinness, thinness);
            boundsGos[2].transform.localPosition = bounds.center + new Vector3(0, -bounds.extents.y, bounds.extents.z);
            boundsGos[3].transform.parent = preview.transform;
            boundsGos[3].transform.localScale = new Vector3(bounds.extents.x * 2, thinness, thinness);
            boundsGos[3].transform.localPosition = bounds.center + new Vector3(0, -bounds.extents.y, -bounds.extents.z);

            boundsGos[4].transform.parent = preview.transform;
            boundsGos[4].transform.localScale = new Vector3(thinness, thinness, bounds.extents.z * 2);
            boundsGos[4].transform.localPosition = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, 0);
            boundsGos[5].transform.parent = preview.transform;
            boundsGos[5].transform.localScale = new Vector3(thinness, thinness, bounds.extents.z * 2);
            boundsGos[5].transform.localPosition = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, 0);
            boundsGos[6].transform.parent = preview.transform;
            boundsGos[6].transform.localScale = new Vector3(bounds.extents.x * 2, thinness, thinness);
            boundsGos[6].transform.localPosition = bounds.center + new Vector3(0, bounds.extents.y, bounds.extents.z);
            boundsGos[7].transform.parent = preview.transform;
            boundsGos[7].transform.localScale = new Vector3(bounds.extents.x * 2, thinness, thinness);
            boundsGos[7].transform.localPosition = bounds.center + new Vector3(0, bounds.extents.y, -bounds.extents.z);

            boundsGos[8].transform.parent = preview.transform;
            boundsGos[8].transform.localScale = new Vector3(thinness, bounds.extents.y * 2, thinness);
            boundsGos[8].transform.localPosition = bounds.center + new Vector3(bounds.extents.x, 0, bounds.extents.z);
            boundsGos[9].transform.parent = preview.transform;
            boundsGos[9].transform.localScale = new Vector3(thinness, bounds.extents.y * 2, thinness);
            boundsGos[9].transform.localPosition = bounds.center + new Vector3(-bounds.extents.x, 0, bounds.extents.z);
            boundsGos[10].transform.parent = preview.transform;
            boundsGos[10].transform.localScale = new Vector3(thinness, bounds.extents.y * 2, thinness);
            boundsGos[10].transform.localPosition = bounds.center + new Vector3(bounds.extents.x, 0, -bounds.extents.z);
            boundsGos[11].transform.parent = preview.transform;
            boundsGos[11].transform.localScale = new Vector3(thinness, bounds.extents.y * 2, thinness);
            boundsGos[11].transform.localPosition = bounds.center + new Vector3(-bounds.extents.x, 0, -bounds.extents.z);

            for(int i = 0; i < boundsGos.Length; ++i)
            {
                GameObject boundsGo = boundsGos[i];
                boundsGo.SetActive(isBoundsVisible);
            }
        }
    }

    private void CalculateBoundsAuto()
    {
        Matrix4x4[] matrices = anim.clips[0].frames[0].matrices;
        GPUSkinningBone[] bones = anim.bones;
        Vector3 min = Vector3.one * 9999;
        Vector3 max = min * -1;
        for(int i = 0; i < bones.Length; ++i)
        {
            Vector4 pos = (matrices[i] * bones[i].bindpose.inverse) * new Vector4(0, 0, 0, 1);
            min.x = Mathf.Min(min.x, pos.x);
            min.y = Mathf.Min(min.y, pos.y);
            min.z = Mathf.Min(min.z, pos.z);
            max.x = Mathf.Max(max.x, pos.x);
            max.y = Mathf.Max(max.y, pos.y);
            max.z = Mathf.Max(max.z, pos.z);
        }
        min -= Vector3.one * boundsAutoExt;
        max += Vector3.one * boundsAutoExt;
        bounds.min = min;
        bounds.max = max;
    }

    private void UpdateHandler()
    {
        float deltaTime = Time.realtimeSinceStartup - time;
        
        if(preview != null)
        {
            PreviewDrawBounds();
            PreviewDrawArrows();

            preview.DoUpdate(deltaTime);
            cam.Render();
        }

        time = Time.realtimeSinceStartup;

        GPUSkinningSampler sampler = target as GPUSkinningSampler;

        if(!sampler.isSampling && sampler.IsSamplingProgress())
        {
            if (++sampler.samplingClipIndex < sampler.animClips.Length)
            {
                sampler.StartSample();
            }
            else
            {
                sampler.EndSample();
                EditorApplication.isPlaying = false;
                EditorUtility.ClearProgressBar();
                LockInspector(false);
            }
        }
        
        if (sampler.isSampling)
        {
            string msg = sampler.animClip.name + "(" + (sampler.samplingClipIndex + 1) + "/" + sampler.animClips.Length +")";
            EditorUtility.DisplayProgressBar("Sampling, DONOT stop playing", msg, (float)(sampler.samplingFrameIndex + 1) / sampler.samplingTotalFrams);
        }
    }

    private void LockInspector(bool isLocked)
    {
        System.Type type = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.InspectorWindow");
        FieldInfo field = type.GetField("m_AllInspectors", BindingFlags.Static | BindingFlags.NonPublic);
        System.Collections.ArrayList windows = new System.Collections.ArrayList(field.GetValue(null) as System.Collections.ICollection);
        foreach (var window in windows)
        {
            PropertyInfo property = type.GetProperty("isLocked");
            property.SetValue(window, isLocked, null);
        }
    }

    private void GetLastGUIRect(ref Rect rect)
    {
        Rect guiRect = GUILayoutUtility.GetLastRect();
        if (guiRect.x != 0)
        {
            rect = guiRect;
        }
    }

    private void DestroyPreview()
    {
        if (rt != null)
        {
            cam.targetTexture = null;

            DestroyImmediate(linearToGammeMtrl);
            linearToGammeMtrl = null;

            DestroyImmediate(rt);
            rt = null;

            if (rtGamma != null)
            {
                DestroyImmediate(rtGamma);
                rtGamma = null;
            }

            DestroyImmediate(cam.gameObject);
            cam = null;

            DestroyImmediate(preview.gameObject);
            preview = null;

            if (boundsGos != null)
            {
                foreach (GameObject boundsGo in boundsGos)
                {
                    DestroyImmediate(boundsGo);
                }
                boundsGos = null;
            }

            if (arrowGos != null)
            {
                foreach (GameObject arrowGo in arrowGos)
                {
                    DestroyImmediate(arrowGo);
                }
                arrowGos = null;

                foreach (Material mtrl in arrowMtrls)
                {
                    DestroyImmediate(mtrl);
                }
                arrowMtrls = null;
            }

            DestroyImmediate(boundsMtrl);
            boundsMtrl = null;
        }
    }

    private void Awake()
    {
        EditorApplication.update += UpdateHandler;
        time = Time.realtimeSinceStartup;

        if (!Application.isPlaying)
        {
            Object obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_ANIM_PATH));
            if (obj != null && obj is GPUSkinningAnimation)
            {
                serializedObject.FindProperty("anim").objectReferenceValue = obj;
            }

            obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_MESH_PATH));
            if (obj != null && obj is Mesh)
            {
                serializedObject.FindProperty("savedMesh").objectReferenceValue = obj;
            }

            obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_MTRL_PATH));
            if (obj != null && obj is Material)
            {
                serializedObject.FindProperty("savedMtrl").objectReferenceValue = obj;
            }

            obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_SHADER_PATH));
            if (obj != null && obj is Shader)
            {
                serializedObject.FindProperty("savedShader").objectReferenceValue = obj;
            }

            serializedObject.ApplyModifiedProperties();

            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_ANIM_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_MESH_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_MTRL_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_SHADER_PATH);
        }

        isBoundsFoldout = GetEditorPrefsBool("isBoundsFoldout", true);
        isJointsFoldout = GetEditorPrefsBool("isJointsFoldout", true);
    }

    private bool GetEditorPrefsBool(string key, bool defaultValue)
    {
        return EditorPrefs.GetBool("GPUSkinningSamplerEditorPrefs_" + key, defaultValue);
    }

    private void SetEditorPrefsBool(string key, bool value)
    {
        EditorPrefs.SetBool("GPUSkinningSamplerEditorPrefs_" + key, value);
    }

	private void OnDestroy()
    {
        EditorApplication.update -= UpdateHandler;
        EditorUtility.ClearProgressBar();
        DestroyPreview();
	}

	private void BeginBox()
	{
		EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"));
		EditorGUILayout.Space();
	}

	private void EndBox()
	{
		EditorGUILayout.Space();
		EditorGUILayout.EndVertical();
	}
}
