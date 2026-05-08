using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GlobalPathPlaner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shipTransform; 
    [SerializeField] private NavMeshAgent agent;      

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

    private bool hasGoal;
    private Vector3 goal;
    private Vector3 lastGoal;
    private float nextReplanTime;

    private Vector3[] pathPoints = Array.Empty<Vector3>();
    public IReadOnlyList<Vector3> PathPoints => pathPoints;
    public bool HasPath => pathPoints != null && pathPoints.Length >= 2;

    public int PathVersion { get; private set; }

    private void Awake()
    {
        navPath = new NavMeshPath();

        if (shipTransform == null)
        {
            shipTransform = transform;
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    private float Now()
    {
        return useUnscaledTime? Time.unscaledTime: Time.time;
    }
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

        end.y = start.y;

        int mask = agent != null ? agent.areaMask : NavMesh.AllAreas;

        bool ok = NavMesh.CalculatePath(start, end, mask, navPath);
        if (!ok || navPath.corners == null || navPath.corners.Length < 2)
        {
            pathPoints = Array.Empty<Vector3>();
            return;
        }

        Vector3[] corners = navPath.corners;
        BezierCurve[] curves = BuildCurves(corners);

        Vector3[] sampled = SamlpleCurves(curves);

        sampled = PostProcessPath(curves, sampled);

        pathPoints = sampled;

        PathVersion++;
    }

    private BezierCurve[] BuildCurves(Vector3[] corners)
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

    private Vector3[] SamlpleCurves(BezierCurve[] curves)
    {
        if (curves == null || curves.Length == 0)
            return Array.Empty<Vector3>();

        List<Vector3> points = new List<Vector3>(curves.Length * smoothingSections + 1);

        for (int i = 0; i < curves.Length; i++)
        {
            Vector3[] segments = curves[i].GetSegments(smoothingSections);
            for (int j = 0; j < segments.Length; j++)
            {
                points.AddRange(segments);
            }
        }

        points.Add(curves[curves.Length - 1].EndPosition);

        return points.ToArray();
    }

    private Vector3[] PostProcessPath(BezierCurve[] curves, Vector3[] path)
    {
        if (path == null || path.Length < 2)
            return path ?? Array.Empty<Vector3>();

        path = RemoveOverSmoothing(curves, path);

        if (removeTooClosePoints)
            path = RemoveTooClosePoints(path);

        if (sampleToNavMesh)
            path = SampleToNavMesh(path);

        return path;
    }

    private Vector3[] RemoveTooClosePoints(Vector3[] path)
    {
        if (path.Length <= 2) return path;

        float minDist = (agent != null) ? agent.radius : 1f;

        int lastIndex = 0;
        for (int i = 1; i < path.Length; i++)
        {
            if (Vector3.Distance(path[i], path[lastIndex]) < minDist)
            {
                path[i] = InfinityVector;
            }
            else
            {
                lastIndex = i;
            }
        }

        return FilterInfinity(path);
    }

    private Vector3[] FilterInfinity(Vector3[] path)
    {
        List<Vector3> clean = new List<Vector3>(path.Length);
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] != InfinityVector)
            {
                clean.Add(path[i]);
            }
        }
            return clean.ToArray();
    }

    private Vector3[] SampleToNavMesh(Vector3[] path)
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

        return FilterInfinity(path);
    }

    private Vector3[] RemoveOverSmoothing(BezierCurve[] curves, Vector3[] path)
    {
        if (curves == null || curves.Length == 0) return path;
        if (path.Length <= 2) return path;

        int index = 1;
        int lastIndex = 0;

        for (int i = 0; i < curves.Length && index < path.Length; i++)
        {
            Vector3 targetDirection = (curves[i].EndPosition - curves[i].StartPosition).normalized;

            for (int j = 0; j < smoothingSections && index < path.Length; j++)
            {
                Vector3 segmentDirection = path[index] - path[lastIndex];

                float dot = Vector3.Dot(targetDirection, segmentDirection);

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

        return FilterInfinity(path);
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