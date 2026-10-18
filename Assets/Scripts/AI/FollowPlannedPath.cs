using UnityEditor;
using UnityEngine;

public class FollowPlannedPath : MonoBehaviour
{
    [SerializeField] private GlobalPathPlaner planner;
    [SerializeField] private ShipMover shipMover;
    [Tooltip("Optional. Blends squad (boid) forces into the path direction.")]
    [SerializeField] private SquadFlocking flocking;

    [Header("Follow")]
    [SerializeField] private float arriveDistance = 3f;
    [SerializeField] private int lookAheadPoints = 3;

    [Header("Steering")]
    [SerializeField] private float fullTurnAngle = 45f;
    [SerializeField] private float alignThreshold = 15f;
    [SerializeField] private float reducedThrust = 0.5f;

    [SerializeField] int lastPathVersion = -1;
    private int pathIndex;

    public bool StopMoving { get; set; }
    // While stopped, turn on the spot to face this direction (e.g. a formation's threat axis).
    public Vector3? HoldFacing { get; set; }

    private void Awake()
    {
        if (planner == null) planner = GetComponent<GlobalPathPlaner>();
        if (shipMover == null) shipMover = GetComponent<ShipMover>();
        if (flocking == null) flocking = GetComponent<SquadFlocking>();
    }

    private void Update()
    {
        if (planner == null || shipMover == null) return;

        if (StopMoving || !planner.HasPath)
        {
            Stop();
            return;
        }

        if (planner.PathVersion != lastPathVersion)
        {
            lastPathVersion = planner.PathVersion;
            pathIndex = 0;
        }

        var points = planner.PathPoints;
        if (points == null || points.Count < 2)
        {
            Stop();
            return;
        }

        pathIndex = Mathf.Clamp(pathIndex, 0, points.Count - 1);

        Vector3 shipPosition = FlatY(transform.position);
        Vector3 currentPoint = FlatY(points[pathIndex]);

        if (Vector3.Distance(shipPosition, currentPoint) <= arriveDistance)
        {
            pathIndex = Mathf.Min(pathIndex + 1, points.Count - 1);
        }

        int targetIndex = Mathf.Min(pathIndex + lookAheadPoints, points.Count - 1);
        Vector3 target = FlatY(points[targetIndex]);

        Vector3 toTarget = target - shipPosition;
        bool closeEnough = toTarget.sqrMagnitude <= arriveDistance * arriveDistance;

        Vector3 forwardFalt = FlatY(transform.forward);
        if(forwardFalt.sqrMagnitude > 0.0001) forwardFalt.Normalize();

        bool passedWaypoints = false;
        if (toTarget.sqrMagnitude > 0.0001 && forwardFalt.sqrMagnitude > 0.0001)
        {
            Vector3 toTargetNormalized = toTarget.normalized;
            passedWaypoints = Vector3.Dot(forwardFalt, toTargetNormalized) < 0.0f;
        }

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            if (pathIndex < points.Count - 1)
            {
                pathIndex++;
                return;
            }

            shipMover.Move(new Vector2(0f, reducedThrust));

            return;
        }

        Vector3 desiredDirection = toTarget.normalized;
        float throttle = 1f;
        if (flocking != null)
        {
            desiredDirection = flocking.Steer(desiredDirection, out throttle);
        }

        Vector3 forward = FlatY(transform.forward);

        forward.Normalize();

        float angle = Vector3.SignedAngle(forward, desiredDirection, Vector3.up);

        float safeTurnAngle = Mathf.Max(fullTurnAngle, 0.0001f);
        float turn = Mathf.Clamp(angle / safeTurnAngle, -1, 1f);

        float thrust = ((Mathf.Abs(angle) < alignThreshold) ? 1f : reducedThrust) * throttle;

        shipMover.Move(new Vector2(turn, thrust));
        
    }

    public void ResetFollower()
    {
        pathIndex = 0;
    }

    private void Stop()
    {
        float turn = 0f;
        if (HoldFacing.HasValue)
        {
            Vector3 facing = FlatY(HoldFacing.Value);
            Vector3 forward = FlatY(transform.forward);
            if (facing.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.SignedAngle(forward, facing, Vector3.up);
                if (Mathf.Abs(angle) > 1f) turn = Mathf.Clamp(angle / Mathf.Max(fullTurnAngle, 0.0001f), -1f, 1f);
            }
        }

        shipMover.Move(new Vector2(turn, 0f));
    }

    private static Vector3 FlatY(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
