using System.Collections;
using UnityEngine;

public class AIFighterPursuitBehaviour : AIBehavior
{

    [SerializeField] private FighterStrafePoints strafe;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;

    [SerializeField, Range(0.1f, 10f)] private float arriveDistance = 5;
    [Tooltip("How close the fighter gets to the target before breaking off its attack run.")]
    [SerializeField, Range(1f, 50f)] private float attackPassDistance = 15f;

    [Header("Flying past")]
    [Tooltip("An attack run also ends once the fighter has flown past the target within this distance " +
             "(e.g. pushed wide by its squadmates), instead of turning back to circle it.")]
    [SerializeField, Range(1f, 100f)] private float attackBreakOffDistance = 40f;
    [Tooltip("Same for a strafe point.")]
    [SerializeField, Range(1f, 100f)] private float strafePassDistance = 20f;
    [Tooltip("A leg (attack run or strafe) taking longer than this is abandoned and the next one starts.")]
    [SerializeField, Range(1f, 60f)] private float maxLegTime = 10f;

    [SerializeField] private Transform shipCPosition;

    private float legStartTime;
    private bool goalWasAhead;

    public override string Name => "FighterPursuit";

    private void Start()
    {
        if (strafe == null) strafe = GetComponentInChildren<FighterStrafePoints>();
        if (planer == null) planer = GetComponentInChildren<GlobalPathPlaner>();
        if (followPlannedPath == null) followPlannedPath = GetComponentInChildren<FollowPlannedPath>();

        if (shipCPosition == null)
        {
            shipCPosition = transform;
        }
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("FighterPursuit is called", shipController);
        if (shipController == null || aIDetector == null) return;
        if (aIDetector.Target == null) return;
        if (strafe == null || planer == null || followPlannedPath == null) return;

        followPlannedPath.StopMoving = false;


        if (!strafe.bIsInitilized || strafe.Target != aIDetector.Target)
        {
            strafe.InitilizeStrafePoints(aIDetector.Target, shipCPosition);
            followPlannedPath.ResetFollower();
            StartLeg();
        }

        Vector3 shipPosition = shipCPosition.position;
        shipPosition.y = 0f;

        // If the target is hugging a station, run at the station's edge instead of into its hull.
        Vector3 goal = planer.SnapToNavMesh(strafe.GetCurrentTarget());
        goal.y = shipPosition.y;

        planer.SetDestination(goal);

        // Check arrival on both legs so the fighter keeps cycling:
        // attack run at the target -> break off to a strafe point -> attack run again.
        if (HasFinishedLeg(shipPosition, goal))
        {
            strafe.OnReached();
            followPlannedPath.ResetFollower();
            StartLeg();

            Vector3 next = planer.SnapToNavMesh(strafe.GetCurrentTarget());
            next.y = shipPosition.y;

            planer.SetDestination(next);
        }
    }

    private void StartLeg()
    {
        legStartTime = Time.time;
        goalWasAhead = false;
    }

    // In a squad a fighter often can't reach the exact point: squadmates push it aside, or it arrives at an angle
    // and its turning circle carries it past. Count the leg as done once it has flown past the goal too.
    private bool HasFinishedLeg(Vector3 shipPosition, Vector3 goal)
    {
        bool attacking = strafe.IsAttackingTarget;
        Vector3 toGoal = goal - shipPosition;
        float distance = toGoal.magnitude;

        if (distance <= (attacking ? attackPassDistance : arriveDistance)) return true;

        Vector3 forward = shipCPosition.forward;
        forward.y = 0f;
        bool ahead = Vector3.Dot(forward, toGoal) > 0f;

        // Only "passed" if the goal was in front at some point this leg, otherwise a new goal that
        // happens to start behind the fighter would end the leg straight away.
        if (ahead)
        {
            goalWasAhead = true;
        }
        else if (goalWasAhead && distance <= (attacking ? attackBreakOffDistance : strafePassDistance))
        {
            return true;
        }

        return Time.time - legStartTime >= maxLegTime;
    }
}
