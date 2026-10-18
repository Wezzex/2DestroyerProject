using System;
using UnityEngine;

public enum SubLightState
{
    Idle,
    Charging, // stationary: no thrust, turning or weapons. Damage drains the charge.
    Cruising, // dead straight at cruise speed, no turning or weapons
    Exiting   // braking back to normal speed
}

// High-speed travel. Align the ship, charge the thrusters, then fly straight until told to drop out,
// an obstacle comes up ahead, or the planned distance is covered. ShipMover hands its speed over to the
// drive while it's active. Used by the player (PlayerInput) and by AI wave ships (WaveFleet).
// Lives on the moving body, next to ShipMover.
public class SubLightDrive : MonoBehaviour
{
    [Header("Charge")]
    [SerializeField] private float chargeTime = 3f;
    [Tooltip("Seconds of charge lost per point of damage taken while charging.")]
    [SerializeField] private float chargeLostPerDamage = 0.05f;
    [Tooltip("Minimum seconds of charge lost per hit, however small the hit.")]
    [SerializeField] private float minChargeLostPerHit = 0.25f;

    [Header("Cruise")]
    [SerializeField] private float cruiseSpeed = 400f;
    [Tooltip("Seconds from standstill to cruise speed.")]
    [SerializeField] private float accelerationTime = 0.75f;
    [Tooltip("Seconds from cruise speed back down to the ship's normal max speed.")]
    [SerializeField] private float exitTime = 0.75f;
    [Tooltip("Can't drop out sooner than this after reaching sub-light.")]
    [SerializeField] private float minCruiseTime = 0.5f;

    [Header("Obstacles")]
    [Tooltip("Physics layers that count as obstacles (asteroids etc.). The drive drops out in time to stop short of them. " +
             "Never include the NavMesh ground plane: ships fly at its height, so it would always count as 'ahead'.")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float probeRadius = 5f;
    [Tooltip("Looked ahead on top of the braking distance.")]
    [SerializeField] private float probeMargin = 20f;

    [Header("Keep clear")]
    [Tooltip("Drop out before reaching hostile units that ask to be kept clear of (stations, destroyers: see their SubLightClearance).")]
    [SerializeField] private bool keepClearOfHostiles = true;

    [Header("Debug")]
    [SerializeField] private bool logDropOuts = false;

    [Header("References")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private UnitManager unit;

    private readonly RaycastHit[] probeHits = new RaycastHit[8];
    private Rigidbody rb;
    private CollisionDetectionMode normalCollisionMode;
    private float charge;
    private float cruiseStartTime;
    private float plannedDistance = float.PositiveInfinity;
    private float travelledDistance;
    private bool exitRequested;

    public SubLightState State { get; private set; } = SubLightState.Idle;
    public bool IsActive => State != SubLightState.Idle;
    // Thrust, turning and weapons are locked while charging or cruising.
    public bool LocksControls => State == SubLightState.Charging || State == SubLightState.Cruising;
    public bool IsCruising => State == SubLightState.Cruising;
    public float ChargeProgress => chargeTime > 0f ? Mathf.Clamp01(charge / chargeTime) : 1f;
    public float CruiseSpeed => cruiseSpeed;
    // Why the last cruise ended, e.g. "Station ahead". Shown by PlayerHUD.
    public string LastDropOutReason { get; private set; } = "";
    public float LastDropOutTime { get; private set; } = float.NegativeInfinity;

    private void Reset()
    {
        obstacleMask = LayerMask.GetMask("Obsticles");
    }

    public event Action<SubLightDrive, SubLightState> StateChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) normalCollisionMode = rb.collisionDetectionMode;
        if (unit == null) unit = GetComponent<UnitManager>();
        if (shipController == null) shipController = GetComponentInParent<ShipController>();
    }

    private void OnEnable()
    {
        if (unit != null)
        {
            unit.Damaged += OnDamaged;
            unit.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (unit != null)
        {
            unit.Damaged -= OnDamaged;
            unit.Died -= OnDied;
        }
        SetState(SubLightState.Idle);
    }

    // travelDistance: drop out automatically so the ship stops after about this far. Infinite = until told.
    public bool TryBeginCharge(float travelDistance = float.PositiveInfinity)
    {
        if (State != SubLightState.Idle || !isActiveAndEnabled) return false;
        if (unit != null && unit.IsDead) return false;

        // Sub-light runs dead flat: level out any pitch/roll picked up from collisions, keep the heading.
        Quaternion level = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        if (rb != null) rb.MoveRotation(level);
        else transform.rotation = level;

        charge = 0f;
        plannedDistance = travelDistance;
        travelledDistance = 0f;
        exitRequested = false;
        SetState(SubLightState.Charging);
        return true;
    }

    // Charging: abort. Cruising: drop out (after minCruiseTime).
    public void Exit()
    {
        if (State == SubLightState.Charging) SetState(SubLightState.Idle);
        else if (State == SubLightState.Cruising) exitRequested = true;
    }

    // Called by ShipMover every physics step while the drive is active. Returns the new speed.
    public float UpdateSpeed(float currentSpeed, float normalMaxSpeed, float normalDeceleration, float deltaTime)
    {
        switch (State)
        {
            case SubLightState.Charging:
                charge += deltaTime;
                if (charge >= chargeTime)
                {
                    cruiseStartTime = Time.time;
                    SetState(SubLightState.Cruising);
                }
                return Mathf.MoveTowards(currentSpeed, 0f, normalDeceleration * deltaTime);

            case SubLightState.Cruising:
                {
                    float speed = Mathf.MoveTowards(currentSpeed, cruiseSpeed, cruiseSpeed / Mathf.Max(0.01f, accelerationTime) * deltaTime);
                    travelledDistance += speed * deltaTime;

                    if (Time.time - cruiseStartTime >= minCruiseTime && ShouldDropOut(speed, normalMaxSpeed))
                    {
                        SetState(SubLightState.Exiting);
                    }
                    return speed;
                }

            case SubLightState.Exiting:
                {
                    float rate = Mathf.Max(0f, cruiseSpeed - normalMaxSpeed) / Mathf.Max(0.01f, exitTime);
                    float speed = Mathf.MoveTowards(currentSpeed, normalMaxSpeed, rate * deltaTime);
                    travelledDistance += speed * deltaTime;
                    if (speed <= normalMaxSpeed + 0.01f) SetState(SubLightState.Idle);
                    return speed;
                }

            default:
                return currentSpeed;
        }
    }

    // Distance covered while dropping out from the current speed.
    public float BrakingDistance(float speed, float normalMaxSpeed)
    {
        return (speed + normalMaxSpeed) * 0.5f * exitTime;
    }

    private bool ShouldDropOut(float speed, float normalMaxSpeed)
    {
        if (exitRequested) return DropOut("Manual");

        float braking = BrakingDistance(speed, normalMaxSpeed);
        if (travelledDistance + braking >= plannedDistance) return DropOut("Destination reached");

        if (keepClearOfHostiles && HostileAhead(braking, out string hostile)) return DropOut(hostile + " ahead");
        if (ObstacleAhead(braking + probeMargin, out string obstacle)) return DropOut("Obstacle ahead: " + obstacle);

        return false;
    }

    private bool DropOut(string reason)
    {
        LastDropOutReason = reason;
        LastDropOutTime = Time.time;
        if (logDropOuts) Debug.Log($"[SubLightDrive] {name} dropped out: {reason}", this);
        return true;
    }

    // Drop out so the ship stops about SubLightClearance short of a hostile station/destroyer in its path.
    // Units behind, or that the ship will pass wide of, are ignored, so jumping away from a fight still works.
    private bool HostileAhead(float braking, out string hostileName)
    {
        hostileName = "";
        Vector3 position = Flat(transform.position);
        Vector3 forward = Flat(transform.forward);
        if (forward.sqrMagnitude < 0.0001f) return false;
        forward.Normalize();

        bool selfIsPlayer = unit is PlayerManager;

        foreach (UnitManager other in UnitManager.Active)
        {
            if (other == null || other == unit || other.IsDead) continue;

            float clearance = other.SubLightClearance;
            if (clearance <= 0f || (other is PlayerManager) == selfIsPlayer) continue;

            Vector3 toOther = Flat(other.transform.position) - position;
            float along = Vector3.Dot(toOther, forward);
            if (along <= 0f) continue;

            float sideways = (toOther - forward * along).magnitude;
            if (sideways > clearance) continue;

            if (along - clearance <= braking)
            {
                hostileName = other is StationManager ? "Station" : "Enemy ship";
                return true;
            }
        }
        return false;
    }

    private bool ObstacleAhead(float distance, out string obstacleName)
    {
        obstacleName = "";
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return false;

        int count = Physics.SphereCastNonAlloc(transform.position, probeRadius, forward.normalized, probeHits, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hit = probeHits[i].collider;
            if (hit == null || hit.transform.IsChildOf(transform)) continue;

            // Already overlapping at the start of the sweep (distance 0): something the ship is flying through
            // or along, not something it is about to hit.
            if (probeHits[i].distance <= 0f) continue;

            // Ships cruising alongside (e.g. the rest of a jumping wave) aren't in the way.
            SubLightDrive other = hit.attachedRigidbody != null ? hit.attachedRigidbody.GetComponent<SubLightDrive>() : null;
            if (other != null && other.IsCruising) continue;

            obstacleName = hit.name;
            return true;
        }
        return false;
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private void OnDamaged(UnitManager damaged, int amount)
    {
        if (State != SubLightState.Charging) return;

        charge = Mathf.Max(0f, charge - Mathf.Max(minChargeLostPerHit, amount * chargeLostPerDamage));
    }

    private void OnDied(UnitManager dead)
    {
        if (State == SubLightState.Charging) SetState(SubLightState.Idle);
        else if (State == SubLightState.Cruising) exitRequested = true;
    }

    private void SetState(SubLightState state)
    {
        if (State == state) return;
        State = state;

        if (shipController != null) shipController.WeaponsLocked = LocksControls;

        // Cruise speed would tunnel straight through colliders with discrete collision detection.
        if (rb != null)
        {
            rb.collisionDetectionMode = IsActive && state != SubLightState.Charging
                ? CollisionDetectionMode.ContinuousDynamic
                : normalCollisionMode;
        }

        StateChanged?.Invoke(this, state);
    }
}
