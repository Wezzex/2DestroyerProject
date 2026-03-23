using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class PatrolStrategy : IStrategy
{

    readonly Transform entety;
    readonly NavMeshAgent agent;
    readonly List<Transform> patrolPoints;

    readonly float patrolSpeed;
    int currentIndex;

    bool bIsPathCalculated;

    public PatrolStrategy(Transform entety, NavMeshAgent agent, List<Transform> patrolPoints, float patrolSpeed)
    {
        this.entety = entety;
        this.agent = agent;
        this.patrolPoints = patrolPoints;
        this.patrolSpeed = patrolSpeed;
    }

    public Node.Status Process()
    {
        if(currentIndex == patrolPoints.Count) return Node.Status.Success;

        var target = patrolPoints[currentIndex];
        agent.SetDestination(target.position);
        entety.LookAt(target);

        if (bIsPathCalculated && agent.remainingDistance < 0.1)
        {
            currentIndex++;
            bIsPathCalculated = false;
        }

        if (agent.pathPending)
        {
            bIsPathCalculated = true;
        }

        return Node.Status.Running;
    }

    public void Reset() => currentIndex = 0;
}
