using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(GPUSkinningSampler))]
public class GPUSkinningSamplerEditor : Editor 
{
	private GPUSkinningAnimation anim = null;

	private Mesh mesh = null;

	private Material mtrl = null;

    private TextAsset texture = null;

	private RenderTexture rt = null;

    private RenderTexture rtGamma = null;

    private Material linearToGammeMtrl = null;

	private Camera cam = null;

    private GPUSkinningPlayerMono preview = null;

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

    private bool isLODFoldout = true;

    private bool isRootMotionFoldout = true;

    private bool isAnimEventsFoldout = true;

    private bool rootMotionEnabled = false;

    private GameObject[] gridGos = null;

    private Material gridMtrl = null;

    private bool guiEnabled = false;

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
        guiEnabled = !Application.isPlaying;

        BeginBox();
        {
            GUI.enabled = guiEnabled;
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
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("texture"), new GUIContent());
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUI.enabled = true && guiEnabled;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("skinQuality"), new GUIContent("Quality"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderType"), new GUIContent("Shader Type"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("rootBoneTransform"), new GUIContent("Root Bone"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("createNewShader"), new GUIContent("New Shader"));

                OnGUI_AnimClips(sampler);

                OnGUI_LOD(sampler);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Step1: Play Scene"))
            {
                DestroyPreview();
                EditorApplication.isPlaying = true;
            }

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Step2: Start Sample"))
                {
                    if (!LodDistancesIsLegal(sampler))
                    {
                        GPUSkinningSampler.ShowDialog("Errors must be fixed before sampling.");
                    }
                    else
                    {
                        DestroyPreview();
                        LockInspector(true);
                        sampler.BeginSample();
                        sampler.StartSample();
                    }
                }
            }
        }
        EndBox();
    }

    private SerializedProperty animClips_array_size_sp = null;
    private SerializedProperty wrapModes_array_size_sp = null;
    private SerializedProperty fpsList_array_size_sp = null;
    private SerializedProperty rootMotionEnabled_array_size_sp = null;
    private SerializedProperty individualDifferenceEnabled_array_size_sp = null;
    private List<SerializedProperty> animClips_item_sp = null;
    private List<SerializedProperty> wrapModes_item_sp = null;
    private List<SerializedProperty> fpsList_item_sp = null;
    private List<SerializedProperty> rootMotionEnabled_item_sp = null;
    private List<SerializedProperty> individualDifferenceEnabled_item_sp = null;
    private int animClips_count = 0;
    private void OnGUI_AnimClips(GPUSkinningSampler sampler)
    {
        System.Action ResetItemSp = () =>
        {
            animClips_item_sp.Clear();
            wrapModes_item_sp.Clear();
            fpsList_item_sp.Clear();
            rootMotionEnabled_item_sp.Clear();
            individualDifferenceEnabled_item_sp.Clear();

            wrapModes_array_size_sp.intValue = animClips_array_size_sp.intValue;
            fpsList_array_size_sp.intValue = animClips_array_size_sp.intValue;
            rootMotionEnabled_array_size_sp.intValue = animClips_array_size_sp.intValue;
            individualDifferenceEnabled_array_size_sp.intValue = animClips_array_size_sp.intValue;

            for (int i = 0; i < animClips_array_size_sp.intValue; i++)
            {
                animClips_item_sp.Add(serializedObject.FindProperty(string.Format("animClips.Array.data[{0}]", i)));
                wrapModes_item_sp.Add(serializedObject.FindProperty(string.Format("wrapModes.Array.data[{0}]", i)));
                fpsList_item_sp.Add(serializedObject.FindProperty(string.Format("fpsList.Array.data[{0}]", i)));
                rootMotionEnabled_item_sp.Add(serializedObject.FindProperty(string.Format("rootMotionEnabled.Array.data[{0}]", i)));
                individualDifferenceEnabled_item_sp.Add(serializedObject.FindProperty(string.Format("individualDifferenceEnabled.Array.data[{0}]", i)));
            }

            animClips_count = animClips_item_sp.Count;
        };

        if (animClips_array_size_sp == null) animClips_array_size_sp = serializedObject.FindProperty("animClips.Array.size");
        if (wrapModes_array_size_sp == null) wrapModes_array_size_sp = serializedObject.FindProperty("wrapModes.Array.size");
        if (fpsList_array_size_sp == null) fpsList_array_size_sp = serializedObject.FindProperty("fpsList.Array.size");
        if (rootMotionEnabled_array_size_sp == null) rootMotionEnabled_array_size_sp = serializedObject.FindProperty("rootMotionEnabled.Array.size");
        if (individualDifferenceEnabled_array_size_sp == null) individualDifferenceEnabled_array_size_sp = serializedObject.FindProperty("individualDifferenceEnabled.Array.size");
        if(animClips_item_sp == null)
        {
            animClips_item_sp = new List<SerializedProperty>();
            wrapModes_item_sp = new List<SerializedProperty>();
            fpsList_item_sp = new List<SerializedProperty>();
            rootMotionEnabled_item_sp = new List<SerializedProperty>();
            individualDifferenceEnabled_item_sp = new List<SerializedProperty>();
            ResetItemSp();
        }

        BeginBox();
        {
            if(!sampler.IsAnimatorOrAnimation())
            {
                EditorGUILayout.HelpBox("Set AnimClips with Animation Component", MessageType.Info);
            }

            EditorGUILayout.PrefixLabel("Sample Clips");

            GUI.enabled = sampler.IsAnimatorOrAnimation() && guiEnabled;
            int no = animClips_array_size_sp.intValue;
            int no2 = wrapModes_array_size_sp.intValue;
            int no3 = fpsList_array_size_sp.intValue;
            int no4 = rootMotionEnabled_array_size_sp.intValue;
            int no5 = individualDifferenceEnabled_array_size_sp.intValue;

            EditorGUILayout.BeginHorizontal();
            {
                animClips_count = EditorGUILayout.IntField("Size", animClips_count);
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    if (animClips_count != no)
                    {
                        animClips_array_size_sp.intValue = animClips_count;
                    }
                    if (animClips_count != no2)
                    {
                        wrapModes_array_size_sp.intValue = animClips_count;
                    }
                    if (animClips_count != no3)
                    {
                        fpsList_array_size_sp.intValue = animClips_count;
                    }
                    if (animClips_count != no4)
                    {
                        rootMotionEnabled_array_size_sp.intValue = animClips_count;
                    }
                    if (animClips_count != no5)
                    {
                        individualDifferenceEnabled_array_size_sp.intValue = animClips_count;
                        ResetItemSp();
                    }
                    return;
                }
                if(GUILayout.Button("Reset", GUILayout.Width(60)))
                {
                    ResetItemSp();
                    GUI.FocusControl(string.Empty);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true && guiEnabled;

            EditorGUILayout.BeginHorizontal();
            {
                for (int j = -1; j < 5; ++j)
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            if(j == -1)
                            {
                                GUILayout.Label("   ");
                            }
                            if(j == 0)
                            {
                                GUILayout.Label("FPS");
                            }
                            if(j == 1)
                            {
                                GUILayout.Label("Wrap Mode");
                            }
                            if(j == 2)
                            {
                                GUILayout.Label("Anim Clip");
                            }
                            if(j == 3)
                            {
                                GUILayout.Label("Root Motion");
                            }
                            if(j == 4)
                            {
                                GUILayout.Label("Individual Difference");
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        for (int i = 0; i < no; i++)
                        {
                            var prop = animClips_item_sp[i];
                            var prop2 = wrapModes_item_sp[i];
                            var prop3 = fpsList_item_sp[i];
                            var prop4 = rootMotionEnabled_item_sp[i];
                            var prop5 = individualDifferenceEnabled_item_sp[i];
                            if (prop != null)
                            {
                                if(j == -1)
                                {
                                    GUILayout.Label((i + 1) + ":    ");
                                }
                                if(j == 0)
                                {
                                    EditorGUILayout.PropertyField(prop3, new GUIContent());
                                    prop3.intValue = Mathf.Clamp(prop3.intValue, 0, 60);
                                }
                                if(j == 1)
                                {
                                    EditorGUILayout.PropertyField(prop2, new GUIContent());
                                }
                                if(j == 2)
                                {
                                    GUI.enabled = sampler.IsAnimatorOrAnimation() && guiEnabled;
                                    EditorGUILayout.PropertyField(prop, new GUIContent());
                                    GUI.enabled = true && guiEnabled;
                                }
                                if(j == 3)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.FlexibleSpace();
                                    prop4.boolValue = GUILayout.Toggle(prop4.boolValue, string.Empty);
                                    GUILayout.FlexibleSpace();
                                    EditorGUILayout.EndHorizontal();
                                }
                                if (j == 4)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.FlexibleSpace();
                                    GUI.enabled = prop2.enumValueIndex == 1 && guiEnabled;
                                    prop5.boolValue = GUILayout.Toggle(prop5.boolValue, string.Empty);
                                    if(!GUI.enabled)
                                    {
                                        prop5.boolValue = false;
                                    }
                                    GUI.enabled = true && guiEnabled;
                                    GUILayout.FlexibleSpace();
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
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
                texture = sampler.texture;
                if (mesh != null)
                {
                    bounds = mesh.bounds;
                }
                if (anim == null || mesh == null || mtrl == null || texture == null)
                {
                    EditorUtility.DisplayDialog("GPUSkinning", "Missing Sampling Resources", "OK");
                }
                else
                {
                    if (rt == null && !EditorApplication.isPlaying)
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
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
                        camGo.transform.position = new Vector3(999, 1002, 999);

                        previewClipIndex = 0;

                        GameObject previewGo = new GameObject("GPUSkinningPreview_Go");
                        previewGo.hideFlags = HideFlags.HideAndDontSave;
                        previewGo.transform.position = new Vector3(999, 999, 1002);
                        preview = previewGo.AddComponent<GPUSkinningPlayerMono>();
                        preview.hideFlags = HideFlags.HideAndDontSave;
                        preview.Init(anim, mesh, mtrl, texture);
                        preview.Player.RootMotionEnabled = rootMotionEnabled;
                        preview.Player.LODEnabled = false;
                        preview.Player.CullingMode = GPUSKinningCullingMode.AlwaysAnimate;
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
                    EditorGUILayout.BeginVertical();
                    {
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

                        EditorGUILayout.HelpBox("Drag to Orbit\nCtrl + Drag to Pitch\nAlt+ Drag to Zoom\nPress P Key to Pause", MessageType.None);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUI.ProgressBar(new Rect(interactionRect.x, interactionRect.y + interactionRect.height, interactionRect.width, 5), preview.Player.NormalizedTime, string.Empty);

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                OnGUI_PreviewClipsOptions();

                OnGUI_AnimTimeline();

                EditorGUILayout.Space();

                OnGUI_RootMotion();

                EditorGUILayout.Space();

                OnGUI_EditBounds();

                EditorGUILayout.Space();

                OnGUI_Joints();
            }
        }
        EndBox();

        serializedObject.ApplyModifiedProperties();
    }

    private bool animTimeline_dragging = false;
    private void OnGUI_AnimTimeline()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            BeginBox();
            {
                EditorGUILayout.Space();
                if(isAnimEventsFoldout) EditorGUILayout.BeginVertical(GUILayout.Height(120));
                else EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        isAnimEventsFoldout = EditorGUILayout.Foldout(isAnimEventsFoldout, isAnimEventsFoldout ? string.Empty : "Events");
                        SetEditorPrefsBool("isAnimEventsFoldout", isAnimEventsFoldout);
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (isAnimEventsFoldout)
                    {
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x += 10;
                        rect.y += 20;
                        rect.width -= 20;
                        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));

                        OnGUI_AnimEvents_DrawThumb(rect, preview.Player.NormalizedTime, animTimeline_dragging);

                        Event e = Event.current;
                        Vector2 mousePos = e.mousePosition;
                        if (rect.Contains(mousePos))
                        {
                            if(e.type == EventType.MouseDown)
                            {
                                animTimeline_dragging = true;
                                OnGUI_AnimTimeline_DraggingUpdate(mousePos, rect);
                            }
                        }
                        if(e.type == EventType.MouseUp)
                        {
                            animTimeline_dragging = false;
                        }
                        if(animTimeline_dragging && e.type == EventType.MouseDrag)
                        {
                            OnGUI_AnimTimeline_DraggingUpdate(mousePos, rect);
                        }

                        OnGUI_AnimEvents(rect);
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

    private float OnGUI_AnimTimeline_MouseDown_NormalizedTime(Vector2 mousePos, Rect rect)
    {
        return Mathf.Clamp01((mousePos.x - rect.x) / rect.width);
    }

    private void OnGUI_AnimTimeline_DraggingUpdate(Vector2 mousePos, Rect rect)
    {
        float normalizedTime = OnGUI_AnimTimeline_MouseDown_NormalizedTime(mousePos, rect);
        preview.Player.NormalizedTime = normalizedTime;
        OnGUI_AnimTimeline_PlayerUpdate();
    }

    private void OnGUI_AnimTimeline_PlayerUpdate()
    {
        preview.Player.Resume();
        preview.Player.Update_Editor(0);
        preview.Player.Stop();
    }

    private bool animEvent_dragging = false;
    private int animEvent_dragging_index = -1;
    private int animEvent_edit_index = -1;
    private void OnGUI_AnimEvents(Rect rect)
    {
        rect.y += 30;
        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));

        if (anim == null || anim.clips.Length == 0 || previewClipIndex >= anim.clips.Length)
        {
            return;
        }

        GPUSkinningClip clip = anim.clips[previewClipIndex];

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;

        if (e.type == EventType.MouseUp)
        {
            if(animEvent_dragging)
            {
                ApplyAnimModification();
                OnGUI_AnimTimeline_PlayerUpdate();
            }
            animEvent_dragging = false;
            animEvent_dragging_index = -1;
        }

        if (clip.events != null)
        {
            if (e.type == EventType.MouseDrag && animEvent_dragging && animEvent_dragging_index != -1)
            {
                float normalizedTime = OnGUI_AnimTimeline_MouseDown_NormalizedTime(mousePos, rect);
                clip.events[animEvent_dragging_index].frameIndex = GPUSkinningUtil.NormalizeTimeToFrameIndex(clip, normalizedTime);
            }

            for (int i = 0; i < clip.events.Length; ++i)
            {
                GPUSkinningAnimEvent evt = clip.events[i];
                Rect thumbRect = OnGUI_AnimEvents_DrawThumb(
                    rect, 
                    GPUSkinningUtil.FrameIndexToNormalizedTime(clip, evt.frameIndex), 
                    (animEvent_dragging && animEvent_dragging_index == i) || animEvent_edit_index == i
                );

                Rect thumbLabelRect = thumbRect; thumbLabelRect.y += 20;
                thumbLabelRect.width = 400;
				EditorGUI.LabelField(thumbLabelRect, evt.eventId.ToString());

                if(thumbRect.Contains(mousePos) && e.type == EventType.MouseDown)
                {
                    if (e.control)
                    {
                        List<GPUSkinningAnimEvent> newEvents = new List<GPUSkinningAnimEvent>(clip.events);
                        newEvents.RemoveAt(i);
                        clip.events = newEvents.ToArray();
                        ApplyAnimModification();
                        --i;
                        OnGUI_AnimTimeline_PlayerUpdate();
                        animEvent_edit_index = -1;
                    }
                    else
                    {
                        animEvent_dragging = true;
                        animEvent_dragging_index = i;
                        animEvent_edit_index = i;
						GUI.FocusControl(string.Empty);
                    }
                }
            }
        }
        
        if (rect.Contains(mousePos))
        {
            if(e.type == EventType.MouseDown && !e.control && !animEvent_dragging)
            {
                List<GPUSkinningAnimEvent> newEvents = new List<GPUSkinningAnimEvent>();
                if(clip.events != null)
                {
                    newEvents.AddRange(clip.events);
                }
                GPUSkinningAnimEvent newEvent = new GPUSkinningAnimEvent();
                newEvents.Add(newEvent);
                float normalizedTime = OnGUI_AnimTimeline_MouseDown_NormalizedTime(mousePos, rect);
                newEvent.frameIndex = GPUSkinningUtil.NormalizeTimeToFrameIndex(clip, normalizedTime);
                clip.events = newEvents.ToArray();
                ApplyAnimModification();
                OnGUI_AnimTimeline_PlayerUpdate();
            }
        }

        if(animEvent_edit_index != -1 && clip.events != null)
        {
            OnGUI_AnimEvents_Edit(rect, clip.events[animEvent_edit_index]);

			Rect tipsRect = rect;
			tipsRect.y += 50;
			OnGUI_AnimEvents_Tips(tipsRect);
        }
		else
		{
			Rect tipsRect = rect;
			tipsRect.y += 30;
			OnGUI_AnimEvents_Tips(tipsRect);
		}
    }

    private void OnGUI_AnimEvents_Edit(Rect bgRect, GPUSkinningAnimEvent evt)
    {
        Rect rect = bgRect;
        rect.y += 30;
        EditorGUI.PrefixLabel(rect, new GUIContent("EventId:"));
        rect.x += 80;
        rect.width -= 80;
        EditorGUI.BeginChangeCheck();
        evt.eventId = EditorGUI.IntField(rect, evt.eventId);
        if(EditorGUI.EndChangeCheck())
        {
            ApplyAnimModification();
			OnGUI_AnimTimeline_PlayerUpdate();
        }
    }

	private void OnGUI_AnimEvents_Tips(Rect bgRect)
	{
        bgRect.height *= 1.8f;
		EditorGUI.HelpBox(bgRect, "Click to Add Event \nCtrl + Click to Delete", MessageType.None);
	}

    private Rect OnGUI_AnimEvents_DrawThumb(Rect bgRect, float value01, bool isDragging)
    {
        Color c = isDragging ? new Color(0, 0.6f, 0.6f, 0.6f) : new Color(0, 0, 0, 0.5f);
        Color bc = new Color(0, 0, 0, 0.3f);

        Rect rectThumb = bgRect;
        rectThumb.y -= 4;
        rectThumb.height += 8;
        rectThumb.width = 10;
        rectThumb.x = bgRect.x + bgRect.width * value01 - rectThumb.width * 0.5f;
        EditorGUI.DrawRect(rectThumb, c);

        float borderSize = 1f;
        Rect rectBorder = rectThumb;
        rectBorder.width = borderSize;
        rectBorder.x -= borderSize;
        EditorGUI.DrawRect(rectBorder, bc);
        rectBorder.x += rectThumb.width + borderSize;
        EditorGUI.DrawRect(rectBorder, bc);
        rectBorder = rectThumb;
        rectBorder.height = borderSize;
        rectBorder.y -= borderSize;
        EditorGUI.DrawRect(rectBorder, bc);
        rectBorder.y += rectThumb.height + borderSize;
        EditorGUI.DrawRect(rectBorder, bc);

        return rectThumb;
    }

    private void OnGUI_RootMotion()
    {
        List<GPUSkinningClip> rootMotionClips = new List<GPUSkinningClip>();
        for(int i = 0; i < anim.clips.Length; ++i)
        {
            if(anim.clips[i].rootMotionEnabled)
            {
                rootMotionClips.Add(anim.clips[i]);
            }
        }

        if (rootMotionClips.Count > 0)
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
                            isRootMotionFoldout = EditorGUILayout.Foldout(isRootMotionFoldout, isRootMotionFoldout ? string.Empty : "Root Motion");
                            SetEditorPrefsBool("isRootMotionFoldout", isRootMotionFoldout);
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        if (isRootMotionFoldout)
                        {
                            EditorGUI.BeginChangeCheck();
                            GUI.enabled = anim.clips[previewClipIndex].rootMotionEnabled && guiEnabled;
                            rootMotionEnabled = EditorGUILayout.Toggle("Apply Root Motion", rootMotionEnabled);
                            GUI.enabled = true && guiEnabled;
                            if (EditorGUI.EndChangeCheck())
                            {
                                preview.Player.RootMotionEnabled = rootMotionEnabled;
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
    }

    private void OnGUI_RootMotion_BakeIntoPose_Label(string label)
    {
        BeginIndentLevel(1);
        {
            EditorGUILayout.LabelField(label);
        }
        EndIndentLevel();
    }

    private void OnGUI_RootMotion_BakeIntoPose_Bool(string label, ref bool f)
    {
        BeginIndentLevel(2);
        {
            EditorGUI.BeginChangeCheck();
            bool b = EditorGUILayout.Toggle(label, f);
            if (EditorGUI.EndChangeCheck())
            {
                f = b;
                ApplyAnimModification();
            }
        }
        EndIndentLevel();
    }

    private void OnGUI_RootMotion_BakeIntoPose_Float(string label, ref float f)
    {
        BeginIndentLevel(2);
        {
            EditorGUI.BeginChangeCheck();
            float v = EditorGUILayout.FloatField(label, f);
            if (EditorGUI.EndChangeCheck())
            {
                f = v;
                ApplyAnimModification();
            }
        }
        EndIndentLevel();
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

                            if(anim.lodMeshes != null)
                            {
                                for(int i = 0; i < anim.lodMeshes.Length; ++i)
                                {
                                    Mesh lodMesh = anim.lodMeshes[i];
                                    if(lodMesh != null)
                                    {
                                        lodMesh.bounds = bounds;
                                        EditorUtility.SetDirty(lodMesh);
                                    }
                                }
                            }

                            EditorUtility.SetDirty(mesh);
                            ApplyAnimModification();
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

    private void OnGUI_LOD(GPUSkinningSampler sampler)
    {
        BeginBox();
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    isLODFoldout = EditorGUILayout.Foldout(isLODFoldout, isLODFoldout ? string.Empty : "LOD");
                    SetEditorPrefsBool("isLODFoldout", isLODFoldout);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                if (isLODFoldout)
                {
                    OnGUI_LODMeshes(sampler);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        EndBox();
    }
     
    private void OnGUI_LODMeshes(GPUSkinningSampler sampler)
    {
        SerializedProperty sizeSP = serializedObject.FindProperty("lodMeshes.Array.size");
        SerializedProperty dist_sizeSP = serializedObject.FindProperty("lodDistances.Array.size");

        sizeSP.intValue = EditorGUILayout.IntField("Size", sizeSP.intValue);
        dist_sizeSP.intValue = sizeSP.intValue;

        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("LOD Distance");
            }
            EditorGUILayout.EndHorizontal();

            for(int i = 0; i < sizeSP.intValue; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.ObjectField(serializedObject.FindProperty("lodMeshes.Array.data[" + i + "]"), new GUIContent("LOD" + (i + 1)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplySamplerModification(sampler);
                    }

                    EditorGUI.BeginChangeCheck();
                    SerializedProperty distSP = serializedObject.FindProperty("lodDistances.Array.data[" + i + "]");
                    distSP.floatValue = EditorGUILayout.FloatField(distSP.floatValue, GUILayout.Width(80));
                    if(EditorGUI.EndChangeCheck())
                    {
                        ApplySamplerModification(sampler);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            OnGUI_LODDistancesChecking(sampler);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sphereRadius"));
    }

    private void OnGUI_LODDistancesChecking(GPUSkinningSampler sampler)
    {
        if(!LodDistancesIsLegal(sampler))
        {
            EditorGUILayout.HelpBox("Error: LOD distances must be sorted in ascending order.", MessageType.Error);
        }
    }

    private bool LodDistancesIsLegal(GPUSkinningSampler sampler)
    {
        if(sampler.lodDistances == null)
        {
            return true;
        }

        float value = float.MinValue;
        bool isLegal = true;
        for (int i = 0; i < sampler.lodDistances.Length; ++i)
        {
            if (sampler.lodDistances[i] <= value)
            {
                isLegal = false;
                break;
            }
            value = sampler.lodDistances[i];
        }

        return isLegal;
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
                ApplyAnimModification();
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
                preview.Player.Play(options[previewClipIndex]);
            }
            if (preview.Player.IsPlaying && !preview.Player.IsTimeAtTheEndOfLoop)
            {
                if (GUILayout.Button("||", GUILayout.Width(50)))
                {
                    preview.Player.Stop();
                }
            }
            else
            {
                Color guiColor = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button(">", GUILayout.Width(50)))
                {
                    if(preview.Player.IsTimeAtTheEndOfLoop)
                    {
                        preview.Player.Play(options[previewClipIndex]);
                    }
                    else
                    {
                        preview.Player.Resume();
                    }
                }
                GUI.color = guiColor;
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void PreviewInteraction_CameraRestriction()
    {
        if(preview == null)
        {
            return;
        }
        Transform camTrans = cam.transform;
        Transform modelTrans = preview.transform;
        Vector3 lookAtPoint = modelTrans.position + camLookAtOffset;

        Vector3 distV = camTrans.position - modelTrans.position;
        if(distV.magnitude > 10)
        {
            distV.Normalize();
            distV *= 10;
            camTrans.position = modelTrans.position + distV;
        }

        camTrans.LookAt(lookAtPoint);
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

            if(e.type == EventType.MouseDrag)
            {
                if (e.control)
                {
                    camLookAtOffset.y += e.delta.y * 0.02f;
                }
                else if(e.alt)
                {
                    camTrans.Translate(0, 0, -e.delta.y * 0.1f, Space.Self);
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
        }
    }

    private void PreviewDrawGrid()
    {
        if(gridGos == null)
        {
            gridMtrl = new Material(Shader.Find("GPUSkinning/GPUSkinningSamplerEditor_Grid"));
            gridMtrl.hideFlags = HideFlags.HideAndDontSave;
            gridMtrl.color = Color.gray;

            gridGos = new GameObject[2];

            gridGos[0] = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gridGos[0].hideFlags = HideFlags.HideAndDontSave;
            gridGos[0].transform.localScale = new Vector3(0.001f, 1, 1000);
            gridGos[0].transform.localPosition = preview.transform.localPosition;
            gridGos[0].GetComponent<MeshRenderer>().sharedMaterial = gridMtrl;

            gridGos[1] = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gridGos[1].hideFlags = HideFlags.HideAndDontSave;
            gridGos[1].transform.localScale = new Vector3(1000, 1, 0.001f);
            gridGos[1].transform.localPosition = preview.transform.localPosition;
            gridGos[1].GetComponent<MeshRenderer>().sharedMaterial = gridMtrl;
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
        Matrix4x4 rootMotionInv = anim.clips[0].rootMotionEnabled ? matrices[anim.rootBoneIndex].inverse : Matrix4x4.identity;
        GPUSkinningBone[] bones = anim.bones;
        Vector3 min = Vector3.one * 9999;
        Vector3 max = min * -1;
        for (int i = 0; i < bones.Length; ++i)
        {
            Vector4 pos = (rootMotionInv * matrices[i] * bones[i].bindpose.inverse) * new Vector4(0, 0, 0, 1);
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

    private void SortAnimEvents(GPUSkinningAnimation anim)
    {
        foreach(GPUSkinningClip clip in anim.clips)
        {
            if(clip.events == null || clip.events.Length == 0)
            {
                continue;
            }

            System.Array.Sort(clip.events);
        }
    }

    private void ApplyAnimModification()
    {
        if(preview != null && anim != null)
        {
            SortAnimEvents(anim);
            EditorUtility.SetDirty(anim);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }

    private void ApplySamplerModification(GPUSkinningSampler sampler)
    {
        if(sampler != null)
        {
            EditorUtility.SetDirty(sampler);
            EditorUtility.SetDirty(sampler.gameObject);
            EditorSceneManager.SaveOpenScenes();
        }
    }

    private void UpdateHandler()
    {
        if(preview != null && EditorApplication.isPlaying)
        {
            DestroyPreview();
            return;
        }

        GPUSkinningSampler sampler = target as GPUSkinningSampler;

        if (EditorApplication.isCompiling)
        {
            if (Selection.activeGameObject == sampler.gameObject)
            {
                Selection.activeGameObject = null;
                return; 
            }
        }

        float deltaTime = Time.realtimeSinceStartup - time;
        
        if(preview != null)
        {
            PreviewDrawBounds();
            PreviewDrawArrows();
            PreviewDrawGrid();

            preview.Update_Editor(deltaTime);
            PreviewInteraction_CameraRestriction();
            cam.Render();
        }

        time = Time.realtimeSinceStartup;

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

            if(gridGos != null)
            {
                foreach(GameObject gridGo in gridGos)
                {
                    DestroyImmediate(gridGo);
                }
                gridGos = null;

                DestroyImmediate(gridMtrl);
                gridMtrl = null;
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
            obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_TEXTURE_PATH));
            if(obj != null && obj is TextAsset)
            {
                serializedObject.FindProperty("texture").objectReferenceValue = obj;
            }

            serializedObject.ApplyModifiedProperties();

            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_ANIM_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_MESH_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_MTRL_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_SHADER_PATH);
            GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_TEXTURE_PATH);
        }

        isBoundsFoldout = GetEditorPrefsBool("isBoundsFoldout", true);
        isJointsFoldout = GetEditorPrefsBool("isJointsFoldout", true);
        isRootMotionFoldout = GetEditorPrefsBool("isRootMotionFoldout", true);
        isLODFoldout = GetEditorPrefsBool("isLODFoldout", true);
        isAnimEventsFoldout = GetEditorPrefsBool("isAnimEventsFoldout", true);

        rootMotionEnabled = true;
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

    private int lastIndentLevel = 0;
    private void BeginIndentLevel(int indentLevel)
    {
        lastIndentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = indentLevel;
    }

    private void EndIndentLevel()
    {
        EditorGUI.indentLevel = lastIndentLevel;
    }
}
