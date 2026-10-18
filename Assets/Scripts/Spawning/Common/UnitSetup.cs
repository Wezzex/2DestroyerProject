using UnityEngine;
using UnityEngine.AI;

// Shared helpers for anything that puts an AI ship into the world (DeploySpawner, WaveDirector).
public static class UnitSetup
{
    // Ties a unit to the thing it defends: its station-reach check and its patrol area.
    public static void AssignHome(GameObject unit, Transform home)
    {
        AIContext context = unit.GetComponent<AIContext>();
        if (context != null)
        {
            context.SetParentStation(home);
        }

        PatrolArea patrolArea = unit.GetComponentInChildren<PatrolArea>(true);
        if (patrolArea != null)
        {
            patrolArea.SetPatrolAncor(home);
            patrolArea.InitilizeSpawnPoints();
        }
    }

    // Points the unit's own sensor at a target so its behaviour tree engages immediately.
    public static void AssignTarget(GameObject unit, Transform target)
    {
        if (target == null) return;

        AIDetector detector = unit.GetComponentInChildren<AIDetector>();
        if (detector != null)
        {
            detector.Target = target;
        }
    }

    // The part of a ship that actually moves. Ship prefabs keep the root still and move the "Motor"
    // child (which holds the UnitManager, ShipMover and Rigidbody).
    public static Transform GetBody(GameObject unit)
    {
        UnitManager unitManager = unit.GetComponentInChildren<UnitManager>(true);
        return unitManager != null ? unitManager.transform : unit.transform;
    }

    // Moves a unit to a position: root and body are both reset, velocity cleared, NavMeshAgent warped.
    public static void Teleport(GameObject unit, Vector3 position, Quaternion rotation)
    {
        unit.transform.SetPositionAndRotation(position, rotation);

        Transform body = GetBody(unit);
        if (body != unit.transform)
        {
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
        }

        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        NavMeshAgent agent = unit.GetComponentInChildren<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(body.position);
        }
    }

    // Finds the closest point on the NavMesh, so units never spawn inside an obstacle or off the map.
    public static bool TryGetNavMeshPoint(Vector3 desired, float searchRadius, out Vector3 result)
    {
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            result.y = desired.y;
            return true;
        }

        result = desired;
        return false;
    }

    public static Transform GetOrCreateContainer(string name)
    {
        GameObject container = GameObject.Find(name);
        if (container == null)
        {
            container = new GameObject(name);
        }
        return container.transform;
    }
}
