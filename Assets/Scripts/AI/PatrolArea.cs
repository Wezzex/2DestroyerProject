using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class PatrolArea : MonoBehaviour
{
    [SerializeField] private GameObject PatrolPointPrefab;
    [SerializeField] private Transform patrolAncor;
    [SerializeField] private Transform shipTransform;

    [SerializeField] private GameObject[] PatrolPoints;
    [SerializeField] private int currentTargetIndex;

    [SerializeField] float radiusMin, radiusMax;
    [SerializeField] float minSpawnDistance = 25.0f;
    [SerializeField] float minSpawnSeperation = 25.0f;

    [SerializeField] private float pointSize = 0.3f;

    public void InitilizeSpawnPoints()
    {
        
        PatrolPoints = new GameObject[2];

        GameObject pointA = Instantiate(PatrolPointPrefab, GenerateRandomPatrolPoint(), Quaternion.identity);
        GameObject pointB = Instantiate(PatrolPointPrefab, GenerateRandomPatrolPoint(), Quaternion.identity);

        pointA.transform.parent = this.transform;
        pointB.transform.parent = this.transform;

        PatrolPoints[0] = pointA;
        PatrolPoints[1] = pointB;


        currentTargetIndex = 0;
    }


    public Vector3 GetCurrentTargetPosition()
    {
        return PatrolPoints[currentTargetIndex].transform.position;
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

        Vector3 spawnPointPosition = RandomPointAroundAncor();

        return spawnPointPosition;

    }

    private Vector3 RandomPointAroundAncor()
    {
        return GetRandomPointInRange(patrolAncor.transform.position, radiusMin, radiusMax);
    }

    private void TeleportPatrolPoint(int index)
    {

        Vector3 newPointPosition = TryGenerateValidSpawn(shipTransform.transform.position, PatrolPoints[1 - index].transform.position);

        PatrolPoints[index].transform.position = newPointPosition;
    }

    private Vector3 TryGenerateValidSpawn(Vector3 shipPosition, Vector3 otherPointPosition)
    {

        Vector3 candidate = RandomPointAroundAncor();
        int attemts = 5;

        for (int i = 0; i < attemts; i++)
        {
            candidate = RandomPointAroundAncor();

            if (Vector3.Distance(candidate, shipPosition) < minSpawnDistance) continue;
            if (Vector3.Distance(candidate, otherPointPosition) < minSpawnSeperation) continue;

            return candidate;
        }

        return candidate;
    }

    public void SetPatrolAncor(Vector3 ancor)
    {
         patrolAncor.transform.position = ancor;
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

        

        if (PatrolPoints == null || PatrolPoints.Length == 0)
        {
            return;
        }

        for (int i = 0; i < PatrolPoints.Length; i++)
        {
            if (i == currentTargetIndex)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            if(PatrolPoints[i] == null) continue;

            Gizmos.DrawSphere(PatrolPoints[i].transform.position, pointSize);
        }
    }

}
