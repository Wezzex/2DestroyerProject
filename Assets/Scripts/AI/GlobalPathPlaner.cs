using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

public class GlobalPathPlaner : MonoBehaviour
{
    private Transform startTransform;
    private Transform endTransform;
    private Transform entetyTransform;

    private Transform[] Points;
    private float pathLength;

    public NavMeshAgent agent;
    public NavMeshPath path;

    [SerializeField] private float SmoothingLength = 1;
    [SerializeField] private int SmoothingSections = 10;
    [SerializeField, Range (0, 1)] private float SmoothingFactor = 0.5f;
    [SerializeField] bool bUsesBezierSmoothing = false;

    [SerializeField] private int areaMask = NavMesh.AllAreas;

    [SerializeField] private float replanInterval = 0.5f;
    [SerializeField] private float goalReplanDistance = 2f;

    private Vector3 InfinityVector = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
    }

    private float PathLength(NavMeshPath path)
    {
        if (path.corners.Length < 2)
        {
            return 0;
        }

        float lengthSoFar = 0.0f;
        for (int i = 0; i < path.corners.Length; i++)
        {
            lengthSoFar += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return lengthSoFar;

    }

public void SetDestination(Vector3 position)
    {
        NavMesh.CalculatePath(startTransform.position, endTransform.position, NavMesh.AllAreas, path);
        if (Points.Length > 2)
        {
            BezierCurve[] bezierCurves = new BezierCurve[Points.Length - 1];
        }
    }

    private void SmoothCurve(BezierCurve[] Curves, Vector3[] corners)
    {
        for (int i = 0; i < Curves.Length; i++)
        {
            if (Curves[i] == null)
            {
                Curves[i] = new BezierCurve();
            }

            Vector3 position = corners[i];
            Vector3 lastPosition = i == 0 ? corners[i] : corners[i - 1];
            Vector3 nextPosition = corners[i + 1];

            Vector3 lastDirection = (position - lastPosition).normalized;
            Vector3 nextDirection = (nextPosition - position).normalized;

            Vector3 startTangent = (lastDirection + nextDirection) * SmoothingLength;
            Vector3 endTangent = (nextDirection + lastDirection) * -1 * SmoothingLength;

            Curves[i].Points[0] = position;
            Curves[i].Points[1] = position + startTangent;
            Curves[i].Points[2] = nextPosition + endTangent;
            Curves[i].Points[3] = nextPosition;


        }

        {
            Vector3 nextDirection = (Curves[1].EndPosition - Curves[1].StartPosition.normalized);
            Vector3 lastDirection = (Curves[0].EndPosition - Curves[0].StartPosition.normalized);

            Curves[0].Points[2] = Curves[0].Points[3] + (nextDirection + lastDirection) * -1 * SmoothingLength;
        }
    }

    private Vector3[] GetPathLocations(BezierCurve[] Curves)
    {
        Vector3[] pathLocations = new Vector3[Curves.Length * SmoothingSections];

        int index = 0;
        for (int i = 0; i < Curves.Length; i++)
        {
            Vector3[] segments = Curves[i].GetSegments(SmoothingSections);
            for (int j = 0; j < segments.Length; j++)
            {
                pathLocations[index] = segments[j];
                index++;
            }
        }

        pathLocations = PostProcessPath(Curves, pathLocations);
        return pathLocations;
    }

    private Vector3[] PostProcessPath(BezierCurve[] Curves, Vector3[] Path)
    {
        Vector3[] path = RemoveOverSmoothing(Curves, Path);

        path = RemoveTooClosePoints(path);
        path = SamplePathPositions(path);

        return path;
    }
    private Vector3[] RemoveTooClosePoints(Vector3[] Path)
    {
        if (Path.Length <= 2)
        {
            return Path;
        }

        int lastIndex = 0;
        int index = 1;
        for (int i = 0; i < Path.Length; i++)
        {
            if (Vector3.Distance(Path[index], Path[lastIndex]) < agent.radius)
            {
                Path[index] = InfinityVector;
            }
            else
            {
                lastIndex = index;
            }
            index++;
        }
        return Path.Except(new Vector3[] { InfinityVector }).ToArray();
    }

    private Vector3[] SamplePathPositions(Vector3[] Path)
    {
        for (int i = 0; Path.Length > 0; i++)
        {
            if (NavMesh.SamplePosition(Path[i], out NavMeshHit hit, agent.radius * 1.5f, agent.areaMask))
            {
                Path[i] = hit.position;
            }
            else
            {
                Debug.LogWarning($"No NavMesh point close to {Path[i]}. Check smoothing settings!");
                Path[i] = InfinityVector;
            }
        }

        return Path.Except(new Vector3[] { InfinityVector }).ToArray();
    }


    private Vector3[] RemoveOverSmoothing(BezierCurve[] Curves, Vector3[] Path)
    {
        if (Path.Length <= 2)
        {
            return Path;
        }

        int index = 1;
        int lastIndex = 0;
        for (int i = 0; index < Curves.Length; index++)
        {
            Vector3 targetDirection = (Curves[i].EndPosition - Curves[i].StartPosition).normalized;

            for (int j = 0; j < SmoothingSections; j++)
            {
                Vector3 segmentDirection = (Path[index] - Path[lastIndex]).normalized;
                float dot = Vector3.Dot(targetDirection, segmentDirection);
                Debug.Log($"Target Direction: {targetDirection}. Segment Direction: {segmentDirection} = Dot: {dot} with index:{index} and lastIndex:{lastIndex}");
                if (dot <= SmoothingFactor)
                {
                    Path[index] = InfinityVector;
                }
                else
                {
                    lastIndex = index;
                }
                index++;
            }
            index++;
        }

        Path[Path.Length - 1] = Curves[Curves.Length - 1].EndPosition;
        Vector3[] TrimmedPath = Path.Except(new Vector3[] { InfinityVector }).ToArray();

        Debug.Log($"Original Smoothed Path: {Path.Length}. Trimmed Path: {TrimmedPath.Length}");

        return TrimmedPath;

    }

    private void Update()
    {
        
    }
}
