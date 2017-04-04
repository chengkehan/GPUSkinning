using UnityEngine;
using UnityEditor;
using System.Collections;

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

    private float time = 0;

    private Vector3 camLookAtOffset = Vector3.zero;

    private Rect previewEditBtnRect;

    private Rect interactionRect;

    private GameObject[] boundsGos = null;

    private Bounds bounds;

    private Material boundsMtrl = null;

    public override void OnInspectorGUI ()
	{
//		base.OnInspectorGUI ();

		GPUSkinningSampler sampler = target as GPUSkinningSampler;
		if(sampler == null)
		{
			return;
		}

		if(sampler.anim != null)
		{
			sampler.animName = sampler.anim.name;
		}

		BeginBox();
		{
			sampler.animName = EditorGUILayout.TextField("Animation Name", sampler.animName);
			sampler.anim = EditorGUILayout.ObjectField("Animation", sampler.anim, typeof(GPUSkinningAnimation)) as GPUSkinningAnimation;
			
			sampler.skinQuality = (GPUSkinningQuality)EditorGUILayout.EnumPopup("Quality", sampler.skinQuality);

			sampler.shaderType = (GPUSkinningShaderType)EditorGUILayout.EnumPopup("Shader Type", sampler.shaderType);

			sampler.animClip = EditorGUILayout.ObjectField("Clip", sampler.animClip, typeof(AnimationClip)) as AnimationClip;
			sampler.rootBoneTransform = EditorGUILayout.ObjectField("Root Bone", sampler.rootBoneTransform, typeof(Transform)) as Transform;

			if(GUILayout.Button("Step1: Play Scene"))
			{
				EditorApplication.isPlaying = true;
			}

			if(Application.isPlaying)
			{
				if(GUILayout.Button("Step2: Start Sample"))
				{
					if(sampler != null)
					{
						sampler.StartSample();
					}
				}

				if(sampler.isSampling)
				{
					Rect rect = GUILayoutUtility.GetLastRect();
					EditorGUI.ProgressBar(new Rect(rect.x, rect.y + rect.height+5, rect.width, 20), (float)(sampler.samplingFrameIndex+1) / sampler.samplingTotalFrams, "Sampling");
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.Space();
				}
			}
		}
		EndBox();

		BeginBox();
		{
			if(GUILayout.Button("Preview/Edit"))
			{
				Object obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_ANIM_PATH));
				if(obj != null && obj is GPUSkinningAnimation)
				{
					anim = obj as GPUSkinningAnimation;
				}
				obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_MESH_PATH));
				if(obj != null && obj is Mesh)
				{
					mesh = obj as Mesh;
                    bounds = mesh.bounds;
				}
				obj = AssetDatabase.LoadMainAssetAtPath(GPUSkinningSampler.ReadTempData(GPUSkinningSampler.TEMP_SAVED_MTRL_PATH));
				if(obj != null && obj is Material)
				{
					mtrl = obj as Material;
				}
				if(anim == null || mesh == null || mtrl == null)
				{
					EditorUtility.DisplayDialog("GPUSkinning", "Missing Sampleing Resources", "OK");
				}
				else
				{
					if(rt == null)
					{
                        linearToGammeMtrl = new Material(Shader.Find("GPUSkinning/GPUSkinningSamplerEditor_LinearToGamma"));
                        linearToGammeMtrl.hideFlags = HideFlags.HideAndDontSave;

                        rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
						rt.hideFlags = HideFlags.HideAndDontSave;

                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            rtGamma = new RenderTexture(1024, 1024, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
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

                        GameObject previewGo = new GameObject("GPUSkinningPreview_Go");
                        previewGo.hideFlags = HideFlags.HideAndDontSave;
                        previewGo.transform.position = new Vector3(100, 100, 103);
                        preview = previewGo.AddComponent<GPUSkinningPreview>();
                        preview.hideFlags = HideFlags.HideAndDontSave;
                        preview.anim = anim;
                        preview.mesh = mesh;
                        preview.mtrl = mtrl;
                        preview.clipName = sampler.animClip.name;
                        preview.Init();
                    }
				}
			}
            GetLastGUIRect(ref previewEditBtnRect);

            if (rt != null)
            {
                int previewRectSize = (int)(previewEditBtnRect.width * 0.9f);
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
                    Interaction(interactionRect);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space();
                    BeginBox();
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.BeginVertical();
                        {
                            GUILayout.Label("Bounds");
                            EditorGUILayout.Space();

                            Vector3 boundsCenter = bounds.center;
                            boundsCenter.x = EditorGUILayout.Slider("center.x", boundsCenter.x, -10, 10);
                            boundsCenter.y = EditorGUILayout.Slider("center.y", boundsCenter.y, -10, 10);
                            boundsCenter.z = EditorGUILayout.Slider("center.z", boundsCenter.z, -10, 10);
                            bounds.center = boundsCenter;

                            EditorGUILayout.Space();

                            Vector3 boundsExts = bounds.extents;
                            boundsExts.x = EditorGUILayout.Slider("extends.x", boundsExts.x, 0.1f, 10);
                            boundsExts.y = EditorGUILayout.Slider("extends.y", boundsExts.y, 0.1f, 10);
                            boundsExts.z = EditorGUILayout.Slider("extends.z", boundsExts.z, 0.1f, 10);
                            bounds.extents = boundsExts;

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
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                    EndBox();
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndHorizontal();
            }
		}
		EndBox();

        Repaint();
	}

    private void Interaction(Rect rect)
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

    private void DrawBounds()
    {
        if(boundsGos == null)
        {
            boundsMtrl = new Material(Shader.Find("GPUSkinning/GPUSkinningSamplerEditor_Bounds"));

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
            //Debug.LogError(bounds);
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
        }
    }

    private void GPUSkinningPreviewUpdateHandler()
    {
        float deltaTime = Time.realtimeSinceStartup - time;
        
        if(preview != null)
        {
            DrawBounds();

            preview.DoUpdate(deltaTime);
            cam.Render();
        }

        time = Time.realtimeSinceStartup;
    }

    private void GetLastGUIRect(ref Rect rect)
    {
        Rect guiRect = GUILayoutUtility.GetLastRect();
        if (guiRect.x != 0)
        {
            rect = guiRect;
        }
    }

    private void Awake()
    {
        EditorApplication.update += GPUSkinningPreviewUpdateHandler;
        time = Time.realtimeSinceStartup;
    }

	private void OnDestroy()
    {
        EditorApplication.update -= GPUSkinningPreviewUpdateHandler;

        //GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_ANIM_PATH);
        //GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_MESH_PATH);
        //GPUSkinningSampler.DeleteTempData(GPUSkinningSampler.TEMP_SAVED_MTRL_PATH);

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

            if(boundsGos != null)
            {
                foreach(GameObject boundsGo in boundsGos)
                {
                    DestroyImmediate(boundsGo);
                }
                boundsGos = null;
            }

            DestroyImmediate(boundsMtrl);
            boundsMtrl = null;
        }
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
