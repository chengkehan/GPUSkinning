using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUSkinningPlayerMono))]
public class GPUSkinningPlayerMonoEditor : Editor
{
    private float time = 0;

    public override void OnInspectorGUI()
    {
        GPUSkinningPlayerMono player = target as GPUSkinningPlayerMono;
        if (player == null)
        {
            return;
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("anim"));
        if (EditorGUI.EndChangeCheck())
        {
            player.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"));
        if (EditorGUI.EndChangeCheck())
        {
            player.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mtrl"));
        if (EditorGUI.EndChangeCheck())
        {
            player.Init();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textureRawData"));
        if (EditorGUI.EndChangeCheck())
        {
            player.Init();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void Awake()
    {
        time = Time.realtimeSinceStartup;
        EditorApplication.update += UpdateHandler;

        GPUSkinningPlayerMono player = target as GPUSkinningPlayerMono;
        if (player != null)
        {
            player.Init();
        }
    }

    private void OnDestroy()
    {
        EditorApplication.update -= UpdateHandler;
    }

    private void UpdateHandler()
    {
        float deltaTime = Time.realtimeSinceStartup - time;
        time = Time.realtimeSinceStartup;

        GPUSkinningPlayerMono player = target as GPUSkinningPlayerMono;
        if (player != null)
        {
            player.Update_Editor(deltaTime);
        }

        foreach(var sceneView in SceneView.sceneViews)
        {
            if (sceneView is SceneView)
            {
                (sceneView as SceneView).Repaint();
            }
        }
    }
}
