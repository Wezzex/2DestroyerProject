using UnityEngine;

public class FighterStrafePoints : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform fighterTransform;

    [SerializeField] private float minStrafeRadius = 5;
    [SerializeField] private float maxStrafeRadius = 10;

    private readonly Vector3[] strafePoints = new Vector3[2];
    private int currentIndex;
    public bool bIsInitilized { get; private set; }

    public Transform Target => targetTransform;
    public int CurrentIndex => currentIndex;
    public bool IsAttackingTarget => currentIndex == playerPointIndex;

    private const int playerPointIndex = 0;
    private const int strafePointIndex = 1;

    public void InitilizeStrafePoints(Transform target, Transform fighter)
    {
        targetTransform = target;
        fighterTransform = fighter;

        if (targetTransform == null || fighterTransform == null)
        {
            bIsInitilized = false;
            return;
        }

        strafePoints[playerPointIndex] = targetTransform.position;
        strafePoints[strafePointIndex] = RandomPointAroundTarget();

        // Start with an attack run straight at the target.
        currentIndex = playerPointIndex;
        bIsInitilized = true;
    }

    public Vector3 GetCurrentTarget()
    {
        if (!bIsInitilized || targetTransform == null)
        {
            return transform.position;
        }

        strafePoints[playerPointIndex] = targetTransform.position;
        return strafePoints[currentIndex];
    }

    public void OnReached()
    {
        if (!bIsInitilized) return;

        int reached = currentIndex;
        currentIndex = 1 - currentIndex;

        if (reached == playerPointIndex)
        {
            strafePoints[strafePointIndex] = RandomPointAroundTarget();
        }
        Utility.LogInfo($"OnReached({reached})");
    }

    private const int walkableTries = 8;

    // Skip points inside a station's non-walkable area; fall back to the closest walkable point.
    private Vector3 RandomPointAroundTarget()
    {
        Vector3 candidate = GetRandomPointInRange(targetTransform.position, minStrafeRadius, maxStrafeRadius);
        for (int i = 0; i < walkableTries && !NavMeshUtility.IsWalkable(candidate); i++)
        {
            candidate = GetRandomPointInRange(targetTransform.position, minStrafeRadius, maxStrafeRadius);
        }
        return NavMeshUtility.ClosestWalkable(candidate);
    }

    private static Vector3 GetRandomPointInRange(Vector3 center, float minRadius, float maxRadius)
    {
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        return new Vector3(center.x + x, center.y, center.z + z);

    }
}
