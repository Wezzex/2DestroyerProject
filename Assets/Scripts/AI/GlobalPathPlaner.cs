using UnityEngine;
using UnityEngine.AI;

public class GlobalPathPlaner : MonoBehaviour
{
    private Transform startTransform;
    private Transform endTransform;

    private Vector3[] corners;
    private float pathLength;

    public NavMeshAgent agent;
    public NavMeshPath path;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        path = GetComponent<NavMeshPath>();
    }

    private void Update()
    {
        NavMesh.CalculatePath(startTransform.position, endTransform.position, NavMesh.AllAreas, path);
    }
}
