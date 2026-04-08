using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

public class GlobalPathPlaner : MonoBehaviour
{
    private Transform startTransform;
    private Transform endTransform;

    private Transform[] Points;
    private float pathLength;

    public NavMeshAgent agent;
    public NavMeshPath path;

    [SerializeField] private float SmoothingLength;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        path = GetComponent<NavMeshPath>();
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

    private void SmoothCurve(BezierCurve[] curves, Vector3[] corners)
    {
        for (int i = 0; i < curves.Length; i++)
        {
            if (curves[i] == null)
            {
                curves[i] = new BezierCurve();
            }

            Vector3 position = corners[i];
            Vector3 lastPosition = i == 0 ? corners[i] : corners[i - 1];
            Vector3 nextPosition = corners[i + 1];

            Vector3 lastDirection = (position - lastPosition).normalized;
            Vector3 nextDirection = (nextPosition - position).normalized;

            Vector3 startTangent = (lastDirection + nextDirection) * SmoothingLength;
            Vector3 endTangent = (nextDirection + lastDirection) * -1 * SmoothingLength;
        }
    }

    private void Update()
    {
        
    }
}
