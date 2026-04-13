using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GlobalPathPlaner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shipTransform; // set to ship root (or leave null to use this.transform)
    [SerializeField] private NavMeshAgent agent;      // optional (used for radius/areaMask)

    [Header("Replan")]
    [SerializeField] private float replanInterval = 0.5f;
    [SerializeField] private float goalReplanDistance = 2f;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Smoothing")]
    [SerializeField] private float smoothingLength = 1f;
    [SerializeField] private int smoothingSections = 10;
    [SerializeField, Range(0, 1)] private float smoothingFactor = 0.5f;

    [Header("Post Processing")]
    [SerializeField] private bool sampleToNavMesh = false;
    [SerializeField] private bool removeTooClosePoints = true;

    private readonly Vector3 InfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    private NavMeshPath navPath;
    private Vector3 goal;
    private bool hasGoal;

    private float nextReplanTime;
    private Vector3 lastGoal;

    private Vector3[] pathPoints = Array.Empty<Vector3>();
    public IReadOnlyList<Vector3> PathPoints => pathPoints;
    public bool HasPath => pathPoints != null && pathPoints.Length >= 2;

    private void Awake()
    {
        navPath = new NavMeshPath();

        if (shipTransform == null)
            shipTransform = transform;

        // If you don't use NavMeshAgent for movement, you can still keep one for radius/areaMask.
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private float Now() => useUnscaledTime ? Time.unscaledTime : Time.time;

    /// <summary>
    /// Sets the target and triggers replanning on the next Update.
    /// </summary>
    public void SetDestination(Vector3 position)
    {
        hasGoal = true;
        goal = position;
    }

    public void ClearDestination()
    {
        hasGoal = false;
        pathPoints = Array.Empty<Vector3>();
    }

    private void Update()
    {
        if (!hasGoal) return;

        bool timeToReplan = Now() >= nextReplanTime;
        bool goalMoved = Vector3.Distance(goal, lastGoal) >= goalReplanDistance;

        if (timeToReplan || goalMoved || !HasPath)
        {
            Replan();
            nextReplanTime = Now() + replanInterval;
            lastGoal = goal;
        }
    }

    private void Replan()
    {
        Vector3 start = shipTransform.position;
        Vector3 end = goal;

        // 2.5D: keep y consistent for planning (optional, depends on your navmesh height)
        end.y = start.y;

        int mask = (agent != null) ? agent.areaMask : NavMesh.AllAreas;

        bool ok = NavMesh.CalculatePath(start, end, mask, navPath);
        if (!ok || navPath.corners == null || navPath.corners.Length < 2)
        {
            pathPoints = Array.Empty<Vector3>();
            return;
        }

        // Build curves from corners
        Vector3[] corners = navPath.corners;
        BezierCurve[] curves = BuildCurvesFromCorners(corners);

        // Sample curves into points
        Vector3[] sampled = GetPathLocations(curves);

        // Post process (optional)
        sampled = PostProcessPath(curves, sampled);

        pathPoints = sampled;
    }

    private BezierCurve[] BuildCurvesFromCorners(Vector3[] corners)
    {
        int segmentCount = corners.Length - 1;
        var curves = new BezierCurve[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            curves[i] = new BezierCurve();

            Vector3 p0 = corners[i];
            Vector3 p3 = corners[i + 1];

            Vector3 prev = (i == 0) ? p0 : corners[i - 1];
            Vector3 next = (i + 2 < corners.Length) ? corners[i + 2] : p3;

            Vector3 inDir = (p3 - prev).normalized;
            Vector3 outDir = (next - p0).normalized;

            Vector3 p1 = p0 + inDir * smoothingLength;
            Vector3 p2 = p3 - outDir * smoothingLength;

            curves[i].Points[0] = p0;
            curves[i].Points[1] = p1;
            curves[i].Points[2] = p2;
            curves[i].Points[3] = p3;
        }

        return curves;
    }

    private Vector3[] GetPathLocations(BezierCurve[] curves)
    {
        if (curves == null || curves.Length == 0)
            return Array.Empty<Vector3>();

        var points = new List<Vector3>(curves.Length * smoothingSections + 1);

        for (int i = 0; i < curves.Length; i++)
        {
            Vector3[] segments = curves[i].GetSegments(smoothingSections);
            points.AddRange(segments);
        }

        // Ensure last endpoint is included
        points.Add(curves[curves.Length - 1].EndPosition);

        return points.ToArray();
    }

    private Vector3[] PostProcessPath(BezierCurve[] curves, Vector3[] path)
    {
        if (path == null || path.Length < 2)
            return path ?? Array.Empty<Vector3>();

        // Optional oversmoothing removal
        path = RemoveOverSmoothing(curves, path);

        // Optional too-close point removal
        if (removeTooClosePoints)
            path = RemoveTooClosePoints(path);

        // Optional sample to NavMesh
        if (sampleToNavMesh)
            path = SamplePathPositions(path);

        return path;
    }

    private Vector3[] RemoveTooClosePoints(Vector3[] path)
    {
        if (path.Length <= 2) return path;

        float minDist = (agent != null) ? agent.radius : 1f;

        int lastIndex = 0;
        for (int index = 1; index < path.Length; index++)
        {
            if (Vector3.Distance(path[index], path[lastIndex]) < minDist)
            {
                path[index] = InfinityVector;
            }
            else
            {
                lastIndex = index;
            }
        }

        return path.Where(p => p != InfinityVector).ToArray();
    }

    private Vector3[] SamplePathPositions(Vector3[] path)
    {
        float sampleRadius = (agent != null) ? agent.radius * 1.5f : 2f;
        int mask = (agent != null) ? agent.areaMask : NavMesh.AllAreas;

        for (int i = 0; i < path.Length; i++)
        {
            if (NavMesh.SamplePosition(path[i], out NavMeshHit hit, sampleRadius, mask))
            {
                path[i] = hit.position;
            }
            else
            {
                path[i] = InfinityVector;
            }
        }

        return path.Where(p => p != InfinityVector).ToArray();
    }

    private Vector3[] RemoveOverSmoothing(BezierCurve[] curves, Vector3[] path)
    {
        if (curves == null || curves.Length == 0) return path;
        if (path.Length <= 2) return path;

        // This method assumes the path was built from curves in order.
        // We'll compare each curve direction vs each sampled segment direction.
        int index = 1;
        int lastIndex = 0;

        for (int i = 0; i < curves.Length && index < path.Length; i++)
        {
            Vector3 targetDir = (curves[i].EndPosition - curves[i].StartPosition).normalized;

            for (int j = 0; j < smoothingSections && index < path.Length; j++)
            {
                Vector3 segDir = (path[index] - path[lastIndex]);
                if (segDir.sqrMagnitude > 0.000001f)
                    segDir.Normalize();

                float dot = Vector3.Dot(targetDir, segDir);

                if (dot <= smoothingFactor)
                {
                    path[index] = InfinityVector;
                }
                else
                {
                    lastIndex = index;
                }

                index++;
            }
        }

        // Force end point to the last curve end
        path[path.Length - 1] = curves[curves.Length - 1].EndPosition;

        return path.Where(p => p != InfinityVector).ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        if (pathPoints == null || pathPoints.Length < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 1; i < pathPoints.Length; i++)
        {
            Gizmos.DrawLine(pathPoints[i - 1], pathPoints[i]);
        }
    }
}