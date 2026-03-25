using UnityEngine;
using UnityEngine.AI;

public class NavAgentSync : MonoBehaviour
{
    public NavMeshAgent agent;


    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();


        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void FixedUpdate()
    {
        agent.nextPosition = this.transform.position;
    }
}
