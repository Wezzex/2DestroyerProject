using UnityEngine;
using UnityEngine.AI;

// Helpers for keeping AI goals on walkable NavMesh. Stations carve a hole in the NavMesh, so any point
// inside one (player hugging the hull, a random patrol/strafe point, a hangar) can never be reached.
public static class NavMeshUtility
{
    // Must reach out of the largest carved hole (a station is ~37 units from centre to corner).
    public const float DefaultSearchRadius = 60f;
    private const float OnMeshTolerance = 1f;

    // Closest walkable point, at NavMesh height.
    public static bool TrySample(Vector3 position, float searchRadius, int areaMask, out Vector3 result)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, searchRadius, areaMask))
        {
            result = hit.position;
            return true;
        }

        result = position;
        return false;
    }

    // Moves a point inside a non-walkable area to the closest walkable point. Keeps the original height.
    public static Vector3 ClosestWalkable(Vector3 position, float searchRadius = DefaultSearchRadius, int areaMask = NavMesh.AllAreas)
    {
        if (!TrySample(position, searchRadius, areaMask, out Vector3 result)) return position;

        result.y = position.y;
        return result;
    }

    // True if the point lies on walkable NavMesh (height is ignored, ships fly above the mesh).
    public static bool IsWalkable(Vector3 position, int areaMask = NavMesh.AllAreas)
    {
        return TrySample(position, DefaultSearchRadius, areaMask, out Vector3 result)
            && FlatDistance(position, result) <= OnMeshTolerance;
    }

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
