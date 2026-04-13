using UnityEngine;

public class FollowPlannedPath : MonoBehaviour
{
    [SerializeField] private GlobalPathPlaner planner;
    [SerializeField] private ShipMover shipMover;

    [Header("Follow")]
    [SerializeField] private float arriveDistance = 3f;
    [SerializeField] private int lookAheadPoints = 3;

    [Header("Steering")]
    [SerializeField] private float fullTurnAngle = 45f;
    [SerializeField] private float alignThreshold = 15f;
    [SerializeField] private float reducedThrust = 0.5f;

    [SerializeField] int lastPathVersion = -1;

    private int pathIndex;

    private void Awake()
    {
        if (planner == null) planner = GetComponent<GlobalPathPlaner>();
        if (shipMover == null) shipMover = GetComponent<ShipMover>();
    }

    private void Update()
    {
        if (planner == null || shipMover == null) return;
        if (!planner.HasPath)
        {
            shipMover.Move(Vector2.zero);
            return;
        }

        if (planner.PathVersion != lastPathVersion)
        {
            lastPathVersion = planner.PathVersion;
            pathIndex = 0;
        }

        var points = planner.PathPoints;
        if (points.Count < 2)
        {
            shipMover.Move(Vector2.zero);
            return;
        }

        pathIndex = Mathf.Clamp(pathIndex, 0, points.Count - 1);

        Vector3 shipPos = transform.position; shipPos.y = 0f;
        Vector3 current = points[pathIndex]; current.y = 0f;

        if (Vector3.Distance(shipPos, current) <= arriveDistance)
            pathIndex = Mathf.Min(pathIndex + 1, points.Count - 1);

        int targetIndex = Mathf.Min(pathIndex + lookAheadPoints, points.Count - 1);
        Vector3 target = points[targetIndex]; target.y = 0f;

        Vector3 toTarget = target - shipPos;

        bool closeEnough = toTarget.sqrMagnitude <= arriveDistance * arriveDistance;

        Vector3 forwardFlat = transform.forward;
        forwardFlat.y = 0f;

        if (forwardFlat.sqrMagnitude > 0.00001f)
        {
            forwardFlat.Normalize();
        }

        bool passedWaypoint = false;
        if (toTarget.sqrMagnitude > 0.00001f && forwardFlat.sqrMagnitude > 0.00001f)
        {
            toTarget.Normalize();
            passedWaypoint = Vector3.Dot(forwardFlat, toTarget) < 0.0f;
        }

        if (closeEnough || passedWaypoint)
        {
            pathIndex = Mathf.Min(pathIndex + 1, points.Count - 1);
        }

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            shipMover.Move(Vector2.zero);
            return;
        }
        toTarget.Normalize();

        Vector3 forward = transform.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            shipMover.Move(Vector2.zero);
            return;
        }
        forward.Normalize();

        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

        fullTurnAngle = Mathf.Max(fullTurnAngle, 0.0001f);
        float turn = Mathf.Clamp(angle / fullTurnAngle, -1f, 1f);
        float thrust = (Mathf.Abs(angle) < alignThreshold) ? 1f : reducedThrust;

        shipMover.Move(new Vector2(turn, thrust));
    }

    public void ResetFollower() => pathIndex = 0;
}
