using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CameraKeyframe
{
    public string label = "View";
    public Vector3 position;
    public Quaternion rotation;
    public float fieldOfView = 60f;
}

public class CameraPathAnimator : MonoBehaviour
{
    [Header("Camera to Animate")]
    [Tooltip("The camera that will fly through the keyframes. Usually the Main Camera.")]
    public Camera targetCamera;

    [Header("Keyframes (captured from Scene view)")]
    public List<CameraKeyframe> keyframes = new List<CameraKeyframe>();

    [Header("Animation Settings")]
    [Tooltip("How many seconds to spend traveling between each pair of keyframes.")]
    public float secondsPerSegment = 3f;

    [Tooltip("Use smooth easing (ease-in-out) between keyframes.")]
    public bool smoothEasing = true;

    [Tooltip("Loop back to the first keyframe after reaching the last one.")]
    public bool loop = false;

    [Tooltip("Start animating automatically when Play begins.")]
    public bool autoStartOnPlay = true;

    [Header("Visualization (Scene view)")]
    public Color pathColor = new Color(0.2f, 0.8f, 1f);
    public float keyframeGizmoSize = 0.5f;

    // --- Runtime state ---
    private float elapsedTime = 0f;
    private bool isPlaying = false;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (autoStartOnPlay && keyframes.Count >= 2)
            StartAnimation();
    }

    public void StartAnimation()
    {
        if (keyframes.Count < 2 || targetCamera == null) return;
        elapsedTime = 0f;
        isPlaying = true;
        ApplyKeyframe(keyframes[0]);
    }

    public void StopAnimation() => isPlaying = false;

    void Update()
    {
        if (!isPlaying || keyframes.Count < 2 || targetCamera == null) return;

        elapsedTime += Time.deltaTime;

        // --- Calculate cumulative distances along the path ---
        float[] cumulativeDistances = new float[keyframes.Count];
        cumulativeDistances[0] = 0f;
        for (int i = 1; i < keyframes.Count; i++)
        {
            cumulativeDistances[i] = cumulativeDistances[i - 1] +
                Vector3.Distance(keyframes[i - 1].position, keyframes[i].position);
        }
        float totalDistance = cumulativeDistances[keyframes.Count - 1];

        // Total duration scales with total path length, using secondsPerSegment as "seconds per unit batch"
        // We treat secondsPerSegment as the total time divided by (count - 1) for backwards compatibility
        float totalDuration = secondsPerSegment * (keyframes.Count - 1);

        if (elapsedTime >= totalDuration)
        {
            if (loop)
                elapsedTime %= totalDuration;
            else
            {
                ApplyKeyframe(keyframes[keyframes.Count - 1]);
                isPlaying = false;
                return;
            }
        }

        // --- Global progress 0..1 along the entire path ---
        float globalT = elapsedTime / totalDuration;

        // Apply easing to the ENTIRE journey (not per-segment)
        float easedGlobalT = smoothEasing ? SmoothStep(globalT) : globalT;

        // --- Find which segment we're in based on distance, not time ---
        float targetDistance = easedGlobalT * totalDistance;

        int segmentIndex = 0;
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            if (targetDistance <= cumulativeDistances[i + 1])
            {
                segmentIndex = i;
                break;
            }
        }

        // Local t within the current segment (linear, no extra easing — easing is already global)
        float segmentStart = cumulativeDistances[segmentIndex];
        float segmentEnd = cumulativeDistances[segmentIndex + 1];
        float segmentLength = segmentEnd - segmentStart;
        float localT = segmentLength > 0.0001f
            ? (targetDistance - segmentStart) / segmentLength
            : 0f;

        CameraKeyframe from = keyframes[segmentIndex];
        CameraKeyframe to = keyframes[segmentIndex + 1];

        targetCamera.transform.position = Vector3.Lerp(from.position, to.position, localT);
        targetCamera.transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, localT);
        targetCamera.fieldOfView = Mathf.Lerp(from.fieldOfView, to.fieldOfView, localT);
    }

    void ApplyKeyframe(CameraKeyframe kf)
    {
        targetCamera.transform.position = kf.position;
        targetCamera.transform.rotation = kf.rotation;
        targetCamera.fieldOfView = kf.fieldOfView;
    }

    // Ease-in-out curve: starts slow, accelerates, ends slow
    float SmoothStep(float t) => t * t * (3f - 2f * t);

    // Visualize keyframes and the path in the Scene view
    void OnDrawGizmos()
    {
        if (keyframes == null || keyframes.Count == 0) return;

        // Draw camera-shaped gizmos at each keyframe
        for (int i = 0; i < keyframes.Count; i++)
        {
            Gizmos.color = pathColor;
            Gizmos.matrix = Matrix4x4.TRS(keyframes[i].position, keyframes[i].rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * keyframeGizmoSize);
            // Draw a small line showing camera forward direction
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * keyframeGizmoSize * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }

        // Draw lines connecting the keyframes
        Gizmos.color = pathColor;
        for (int i = 0; i < keyframes.Count - 1; i++)
            Gizmos.DrawLine(keyframes[i].position, keyframes[i + 1].position);

        if (loop && keyframes.Count > 1)
            Gizmos.DrawLine(keyframes[keyframes.Count - 1].position, keyframes[0].position);
    }
}