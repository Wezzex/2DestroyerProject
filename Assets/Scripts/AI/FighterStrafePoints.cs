using UnityEngine;

public class FighterStrafePoints : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform fighterTransform;

    [Tooltip("Only used if the fighter's heading is unknown; normally the break-off point below is used.")]
    [SerializeField] private float minStrafeRadius = 5;
    [SerializeField] private float maxStrafeRadius = 10;

    [Header("Break-off after an attack pass")]
    [Tooltip("How far past the target (along the attack direction) the fighter breaks off before turning back.")]
    [SerializeField] private float breakOffDistanceMin = 25f;
    [SerializeField] private float breakOffDistanceMax = 45f;
    [Tooltip("How far to one side of the attack line, so the next run comes in from a new angle.")]
    [SerializeField] private float breakOffSideMin = 15f;
    [SerializeField] private float breakOffSideMax = 30f;

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
            strafePoints[strafePointIndex] = BreakOffPoint();
        }
        Utility.LogInfo($"OnReached({reached})");
    }

    private const int walkableTries = 8;

    // Just past the target and off to one side of the attack line: the fighter overshoots, swings out,
    // and turns straight back for the next run, instead of flying off to a random point far away.
    private Vector3 BreakOffPoint()
    {
        Vector3 forward = fighterTransform != null ? fighterTransform.forward : Vector3.zero;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return RandomPointAroundTarget();
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 target = targetTransform.position;

        // A pass can end after the fighter has already flown past the target; measure from wherever it is,
        // so the break-off point is always ahead of it.
        Vector3 fromTarget = fighterTransform.position - target;
        fromTarget.y = 0f;
        target += forward * Mathf.Max(0f, Vector3.Dot(fromTarget, forward));

        Vector3 candidate = target;
        for (int i = 0; i < walkableTries; i++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            candidate = target
                      + forward * Random.Range(breakOffDistanceMin, breakOffDistanceMax)
                      + right * side * Random.Range(breakOffSideMin, breakOffSideMax);
            if (NavMeshUtility.IsWalkable(candidate)) break;
        }
        return NavMeshUtility.ClosestWalkable(candidate);
    }

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
