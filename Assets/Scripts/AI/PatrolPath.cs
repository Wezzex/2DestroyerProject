using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    [SerializeField] private GameObject patrolPointPrefab;
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();

    public int Length { get => patrolPoints.Count; }

    [Header("Gizmos parameters")]
    [SerializeField] private Color pointColor = Color.blue;
    [SerializeField] private float pointSize = 0.3f;
    [SerializeField] private Color lineColor = Color.magenta;

    public struct PathPoint
    {
        public int Index;
        public Vector2 Position;
    }

    public PathPoint GetClosestPathPoint(Vector3 shipPosition)
    {
        var minDistance = float.MaxValue;
        var index = -1;
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            var tempDistance = Vector2.Distance(shipPosition, patrolPoints[i].position);
            if (tempDistance < minDistance)
            {
                minDistance = tempDistance;
                index = i;
            }
        }

        return new PathPoint { Index = index, Position = patrolPoints[index].position };
    }

    public PathPoint GetNextPathPoint(int index)
    {
        var newIndex = index + 1 >= patrolPoints.Count ? 0 : index + 1;
        return new PathPoint { Index = newIndex, Position = patrolPoints[newIndex].position };
    }

    /*private void OnDrawGizmos()
    {
        if (patrolPoints.Count == 0)
        {
            return;
        }
        for (int i = patrolPoints.Count - 1; i >= 0; i--)
        {
            if (i == -1 || patrolPoints[i] == null)
            {
                return;
            }

            Gizmos.color = pointColor;
            Gizmos.DrawSphere(patrolPoints[i].position, pointSize);

            if (patrolPoints.Count == 1 || i == 0)
            {
                return;
            }
            Gizmos.color = lineColor;
            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i - 1].position);

            if (patrolPoints.Count > 2 && i == patrolPoints.Count - 1)
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
            }
        }
    }*/
}
