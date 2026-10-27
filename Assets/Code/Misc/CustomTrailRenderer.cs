using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CustomTrailRenderer : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("How long the trail lasts in seconds.")]
    [SerializeField] private float trailTime = 1.0f;

    [Tooltip("Minimum distance required between points to add a new vertex.")]
    [SerializeField] private float minVertexDistance = 0.1f;

    [Tooltip("Optional transform space for the trail's motion. If null, world space is used.")]
    [SerializeField] private Transform spaceTransform;

    public LineRenderer lineRenderer;
    public bool emitting = true;

    private struct TrailPoint
    {
        public Vector3 position;     // Stored in the selected coordinate space
        public float timeCreated;
    }

    private readonly List<TrailPoint> points = new List<TrailPoint>();

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true; // We handle space conversion manually for flexibility
    }

    private void LateUpdate()
    {
        Vector3 currentPosInSpace = GetCurrentPositionInSpace();
        float currentTime = Time.time;

        // 1. Add a new point if moved far enough or if the trail is empty
        if (points.Count == 0 || Vector3.Distance(points[^1].position, currentPosInSpace) >= minVertexDistance)
        {
            if (emitting)
            {
                points.Add(new TrailPoint 
                { 
                    position = currentPosInSpace, 
                    timeCreated = currentTime 
                });
            }
        }
        else if (points.Count > 0)
        {
            // Continuously update the newest point's position for smooth tracking between frames
            var latestPoint = points[^1];
            latestPoint.position = currentPosInSpace;
            points[^1] = latestPoint;
        }

        // 2. Remove expired points older than trailTime
        points.RemoveAll(p => (currentTime - p.timeCreated) > trailTime);

        // 3. Update the LineRenderer
        UpdateLineRenderer();
    }

    private Vector3 GetCurrentPositionInSpace()
    {
        if (spaceTransform == null)
        {
            return transform.position;
        }
        else
        {
            // Convert world position into the local space of the target transform
            return spaceTransform.InverseTransformPoint(transform.position);
        }
    }

    private void UpdateLineRenderer()
    {
        if (points.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = points.Count;
        Vector3[] worldPositions = new Vector3[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            if (spaceTransform == null)
            {
                worldPositions[i] = points[i].position;
            }
            else
            {
                // Convert back to world space for the LineRenderer
                worldPositions[i] = spaceTransform.TransformPoint(points[i].position);
            }
        }

        lineRenderer.SetPositions(worldPositions);
    }

    /// <summary>
    /// Clears the current trail immediately.
    /// </summary>
    public void ClearTrail()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}