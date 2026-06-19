#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraPathAnimator))]
public class CameraPathAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraPathAnimator anim = (CameraPathAnimator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Capture Keyframes", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "1. Frame the Scene view how you want it.\n" +
            "2. Click 'Capture Current Scene View' to add a keyframe.\n" +
            "3. Re-frame the Scene view, then click again.\n" +
            "4. Press Play to fly the camera through all keyframes.",
            MessageType.Info
        );

        // --- Big capture button ---
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("📷  Capture Current Scene View", GUILayout.Height(35)))
        {
            CaptureSceneViewAsKeyframe(anim);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // --- Quick action buttons ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Remove Last") && anim.keyframes.Count > 0)
        {
            Undo.RecordObject(anim, "Remove Last Keyframe");
            anim.keyframes.RemoveAt(anim.keyframes.Count - 1);
            EditorUtility.SetDirty(anim);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Clear All") && anim.keyframes.Count > 0)
        {
            if (EditorUtility.DisplayDialog("Clear all keyframes?",
                "This will remove all captured camera keyframes.", "Yes, clear", "Cancel"))
            {
                Undo.RecordObject(anim, "Clear Keyframes");
                anim.keyframes.Clear();
                EditorUtility.SetDirty(anim);
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Keyframes captured: {anim.keyframes.Count}");

        // --- Preview buttons for each captured keyframe ---
        if (anim.keyframes.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview Keyframes", EditorStyles.boldLabel);

            for (int i = 0; i < anim.keyframes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {anim.keyframes[i].label}", GUILayout.Width(120));
                if (GUILayout.Button("Go to this view", GUILayout.Width(120)))
                {
                    MoveSceneViewToKeyframe(anim.keyframes[i]);
                }
                if (GUILayout.Button("Update from current view", GUILayout.Width(180)))
                {
                    UpdateKeyframeFromSceneView(anim, i);
                }
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    Undo.RecordObject(anim, "Remove Keyframe");
                    anim.keyframes.RemoveAt(i);
                    EditorUtility.SetDirty(anim);
                    SceneView.RepaintAll();
                    break; // list changed, exit loop
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void CaptureSceneViewAsKeyframe(CameraPathAnimator anim)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            EditorUtility.DisplayDialog("No Scene View",
                "Open a Scene view window first, then click Capture.", "OK");
            return;
        }

        Undo.RecordObject(anim, "Capture Camera Keyframe");

        CameraKeyframe kf = new CameraKeyframe
        {
            label = $"View {anim.keyframes.Count + 1}",
            position = sv.camera.transform.position,
            rotation = sv.camera.transform.rotation,
            fieldOfView = sv.camera.fieldOfView
        };
        anim.keyframes.Add(kf);

        EditorUtility.SetDirty(anim);
        SceneView.RepaintAll();
    }

    void UpdateKeyframeFromSceneView(CameraPathAnimator anim, int index)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        Undo.RecordObject(anim, "Update Camera Keyframe");

        CameraKeyframe kf = anim.keyframes[index];
        kf.position = sv.camera.transform.position;
        kf.rotation = sv.camera.transform.rotation;
        kf.fieldOfView = sv.camera.fieldOfView;
        anim.keyframes[index] = kf;

        EditorUtility.SetDirty(anim);
        SceneView.RepaintAll();
    }

    void MoveSceneViewToKeyframe(CameraKeyframe kf)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        // Move the Scene view camera to match the keyframe
        sv.pivot = kf.position + (kf.rotation * Vector3.forward) * 5f;
        sv.rotation = kf.rotation;
        sv.size = 5f;
        sv.Repaint();
    }
}
#endif