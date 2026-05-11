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
        strafePoints[strafePointIndex] = fighterTransform.position;

        currentIndex = strafePointIndex;
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

    private Vector3 RandomPointAroundTarget()
    {
        return GetRandomPointInRange(targetTransform.position, minStrafeRadius, maxStrafeRadius);
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
