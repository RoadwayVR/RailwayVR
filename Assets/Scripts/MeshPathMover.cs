using System.Collections.Generic;
using UnityEngine;

public class MeshPathMover : MonoBehaviour
{
    [Header("Target Meshes")]
    [Tooltip("The mesh GameObjects you can draw the path on. Each must have a Collider (MeshCollider recommended).")]
    public List<GameObject> targetMeshes = new List<GameObject>();

    [Header("Object to Move")]
    [Tooltip("The GameObject that will travel along the drawn path.")]
    public Transform movingObject;

    [Header("Movement Settings")]
    [Tooltip("How fast the object moves along the path (units/sec).")]
    public float moveSpeed = 30f;

    [Tooltip("Distance above the mesh surface (along the surface normal).")]
    public float surfaceOffset = 0.1f;

    [Tooltip("Should the object rotate to face the direction of movement?")]
    public bool faceMovementDirection = false;

    [Tooltip("Should the object align its 'up' to the surface normal?")]
    public bool alignToSurfaceNormal = false;

    [Tooltip("Should the path loop back to the start when finished?")]
    public bool loopPath = true;

    [Tooltip("Auto-start movement when entering Play mode.")]
    public bool autoStartOnPlay = true;

    [Header("Path Data (drawn in Scene view)")]
    [Tooltip("Waypoints drawn on the mesh. Edit by drawing in the Scene view.")]
    public List<Vector3> waypoints = new List<Vector3>();
    public List<Vector3> waypointNormals = new List<Vector3>();

    [Header("Visualization")]
    public Color pathColor = Color.yellow;
    public float pathWidth = 0.1f;
    public float waypointGizmoSize = 0.15f;

    // --- Runtime state ---
    private LineRenderer pathLine;
    private bool isMoving = false;
    private int currentWaypointIndex = 0;

    void Start()
    {
        GameObject lineGO = new GameObject("PathLine");
        lineGO.transform.SetParent(this.transform);
        pathLine = lineGO.AddComponent<LineRenderer>();
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.widthMultiplier = pathWidth;
        pathLine.startColor = pathColor;
        pathLine.endColor = pathColor;
        pathLine.positionCount = waypoints.Count;
        for (int i = 0; i < waypoints.Count; i++)
            pathLine.SetPosition(i, waypoints[i]);

        if (autoStartOnPlay && waypoints.Count >= 2 && movingObject != null)
            StartMoving();
    }

    void Update()
    {
        if (isMoving && waypoints.Count >= 2 && movingObject != null)
            MoveAlongPath();
    }

    public void StartMoving()
    {
        if (waypoints.Count < 2 || movingObject == null) return;
        movingObject.position = waypoints[0];
        currentWaypointIndex = 1;
        isMoving = true;
    }

    public void StopMoving() => isMoving = false;

    /// <summary>
    /// Check if a hit collider belongs to any of the target meshes (including nested children).
    /// </summary>
    public bool IsHitOnAnyTargetMesh(Collider hitCollider)
    {
        if (hitCollider == null) return false;

        foreach (GameObject target in targetMeshes)
        {
            if (target == null) continue;
            if (hitCollider.gameObject == target ||
                hitCollider.transform.IsChildOf(target.transform))
                return true;
        }
        return false;
    }

    void MoveAlongPath()
    {
        Vector3 target = waypoints[currentWaypointIndex];

        movingObject.position = Vector3.MoveTowards(
            movingObject.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (faceMovementDirection || alignToSurfaceNormal)
        {
            Vector3 dir = target - movingObject.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Vector3 up = alignToSurfaceNormal && currentWaypointIndex < waypointNormals.Count
                    ? waypointNormals[currentWaypointIndex]
                    : Vector3.up;

                Quaternion targetRot = faceMovementDirection
                    ? Quaternion.LookRotation(dir.normalized, up)
                    : Quaternion.FromToRotation(Vector3.up, up);

                movingObject.rotation = Quaternion.Slerp(
                    movingObject.rotation,
                    targetRot,
                    Time.deltaTime * 5f
                );
            }
        }

        if (Vector3.Distance(movingObject.position, target) < 0.05f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                if (loopPath)
                {
                    // RESPAWN: teleport back to the first waypoint and start over
                    movingObject.position = waypoints[0];
                    currentWaypointIndex = 1;
                }
                else
                {
                    isMoving = false;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < waypoints.Count; i++)
            Gizmos.DrawSphere(waypoints[i], waypointGizmoSize);

        for (int i = 0; i < waypoints.Count - 1; i++)
            Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count && i < waypointNormals.Count; i++)
            Gizmos.DrawLine(waypoints[i], waypoints[i] + waypointNormals[i] * 0.5f);
    }
}