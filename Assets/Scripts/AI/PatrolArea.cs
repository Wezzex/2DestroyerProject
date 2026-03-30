using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class PatrolArea : MonoBehaviour
{
    [SerializeField] private GameObject PatrolPointPrefab;
    [SerializeField] private Transform PatrolAncor;
    [SerializeField] private Transform parentObject;

    [SerializeField] private GameObject[] PatrolPoints;
    [SerializeField] private int currentTargetIndex;

    [SerializeField] float radiusMin, radiusMax;
    [SerializeField] float minSpawnDist = 25.0f;

    [SerializeField] private float pointSize = 0.3f;


    private void Start()
    {
        PatrolPoints = new GameObject[2];

        GameObject pointA = Instantiate(PatrolPointPrefab, GenerateRandomPatrolPoint(), Quaternion.identity);
        GameObject pointB = Instantiate(PatrolPointPrefab, GenerateRandomPatrolPoint(), Quaternion.identity);

        pointA.transform.parent = parentObject;
        pointB.transform.parent = parentObject;

        PatrolPoints[0] = pointA;
        PatrolPoints[1] = pointB;

        currentTargetIndex = 0;

    }

    public void OnReachedPoint()
    {
        int reachedPoint = currentTargetIndex;

        int newTargetIndex = 1 - currentTargetIndex;

        currentTargetIndex = newTargetIndex;

        TeleportPatrolPoint(reachedPoint);

    }

    private Vector3 GenerateRandomPatrolPoint()
    {
        Vector3 spawnPointPosition = GetRandomPointInRange(PatrolAncor.position, radiusMin, radiusMax);

        if (Vector3.Distance(spawnPointPosition, parentObject.position) < minSpawnDist)
        {
            GetRandomPointInRange(PatrolAncor.position, radiusMin, radiusMax);
        }

        return spawnPointPosition;

    }

    private Vector3 TeleportPatrolPoint(int index)
    {
        Vector3 newPointPosition = GetRandomPointInRange(PatrolAncor.position, radiusMin, radiusMax);
        

        PatrolPoints[index].transform.position = newPointPosition;

        return newPointPosition;
    }



    private static Vector3 GetRandomPointInRange(Vector3 center, float minRadius, float maxRadius)
    {
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        return new Vector3(center.x + x, center.y, center.z + z);

    }


    private void OnDrawGizmosSelected()
    {

        if (PatrolAncor != null)
        { 
            Gizmos.color = Color.magenta;
        }

        if (PatrolPoints == null || PatrolPoints.Length == 0)
        {
            return;
        }

        for (int i = 0; i < PatrolPoints.Length; i++)
        {
            if(PatrolPoints[i] == null) return;

            Gizmos.DrawSphere(PatrolPoints[i].transform.position, pointSize);
        }
    }

}
