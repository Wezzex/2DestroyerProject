using System;
using System.Linq;
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

    [SerializeField] private float SmoothingLength = 1;
    [SerializeField] private int SmoothingSections = 10;
    [SerializeField, Range (0, 1)] private float SmoothingFactor = 0.5f;

    private Vector3 InfinityVector = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

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

    

    private void Update()
    {
        
    }
}
