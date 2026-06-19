#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshPathMover))]
public class MeshPathMoverEditor : Editor
{
    private bool drawMode = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshPathMover mover = (MeshPathMover)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Path Editing", EditorStyles.boldLabel);

        // Toggle draw mode
        GUI.backgroundColor = drawMode ? Color.green : Color.white;
        if (GUILayout.Button(drawMode ? "✓ DRAW MODE ON (click on mesh to add waypoints)" : "Enable Draw Mode"))
        {
            drawMode = !drawMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox(
            drawMode
                ? "Left-click on any target mesh in the Scene view to add waypoints.\nShift+click near a waypoint to delete it.\nDisable Draw Mode when finished."
                : "Click 'Enable Draw Mode' to start adding waypoints by clicking on a target mesh.",
            MessageType.Info
        );

        EditorGUILayout.Space(5);

        // Quick action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Path"))
        {
            Undo.RecordObject(mover, "Clear Path");
            mover.waypoints.Clear();
            mover.waypointNormals.Clear();
            EditorUtility.SetDirty(mover);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Remove Last Waypoint") && mover.waypoints.Count > 0)
        {
            Undo.RecordObject(mover, "Remove Last Waypoint");
            mover.waypoints.RemoveAt(mover.waypoints.Count - 1);
            if (mover.waypointNormals.Count > 0)
                mover.waypointNormals.RemoveAt(mover.waypointNormals.Count - 1);
            EditorUtility.SetDirty(mover);
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Waypoint count: {mover.waypoints.Count}");

        // Warn if no target meshes are assigned
        if (mover.targetMeshes == null || mover.targetMeshes.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No Target Meshes assigned. Add at least one mesh to the Target Meshes list to enable path drawing.",
                MessageType.Warning
            );
        }
    }

    void OnSceneGUI()
    {
        MeshPathMover mover = (MeshPathMover)target;

        // Need draw mode ON and at least one target mesh assigned
        if (!drawMode || mover.targetMeshes == null || mover.targetMeshes.Count == 0)
            return;

        // Block normal Scene view selection so clicks go to our tool
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Event e = Event.current;

        // Show movable handles on existing waypoints so users can fine-tune positions
        for (int i = 0; i < mover.waypoints.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(mover.waypoints[i], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mover, "Move Waypoint");
                mover.waypoints[i] = newPos;
                EditorUtility.SetDirty(mover);
            }

            // Label each waypoint with its index
            Handles.Label(mover.waypoints[i] + Vector3.up * 0.3f, $"#{i}");
        }

        // Handle left-click to add a waypoint
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check the hit against ALL assigned target meshes (and their children)
                bool hitTarget = mover.IsHitOnAnyTargetMesh(hit.collider);

                if (hitTarget)
                {
                    Undo.RecordObject(mover, "Add Waypoint");

                    if (e.shift)
                    {
                        // Shift+click: try to delete the closest waypoint within range
                        int closest = -1;
                        float closestDist = 1f; // world-space distance threshold
                        for (int i = 0; i < mover.waypoints.Count; i++)
                        {
                            float d = Vector3.Distance(mover.waypoints[i], hit.point);
                            if (d < closestDist)
                            {
                                closestDist = d;
                                closest = i;
                            }
                        }
                        if (closest >= 0)
                        {
                            mover.waypoints.RemoveAt(closest);
                            if (closest < mover.waypointNormals.Count)
                                mover.waypointNormals.RemoveAt(closest);
                        }
                    }
                    else
                    {
                        // Normal click: add new waypoint at hit position + normal offset
                        Vector3 point = hit.point + hit.normal * mover.surfaceOffset;
                        mover.waypoints.Add(point);
                        mover.waypointNormals.Add(hit.normal);
                    }

                    EditorUtility.SetDirty(mover);
                    e.Use(); // consume the event so it doesn't deselect the GameObject
                }
            }
        }

        // Keep the scene view repainting so handles and gizmos stay responsive
        SceneView.RepaintAll();
    }
}
#endif