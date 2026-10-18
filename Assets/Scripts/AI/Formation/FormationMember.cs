using UnityEngine;

// A unit's place in a formation, handed out by its formation owner (StationDefense or WaveFleet).
// Read by AIHoldFormationBehaviour and the destroyer behaviour tree. Lives on the unit root, next to AIContext.
public class FormationMember : MonoBehaviour
{
    private bool hasSlot;
    private AIDetector detector;

    public Object Owner { get; private set; }
    public Vector3 SlotPosition { get; private set; }
    public Vector3 Facing { get; private set; } = Vector3.forward;

    // Behaviour tree conditions (EnemyAIRunner finds bool properties by name).
    public bool HasFormationSlot => hasSlot && !IsBreakingFormation;
    public bool IsBreakingFormation { get; private set; }

    private void Awake()
    {
        detector = GetComponentInChildren<AIDetector>(true);
    }

    // One owner at a time: a garrison destroyer can't also be steered by a wave.
    public bool TryClaim(Object owner)
    {
        if (Owner != null && Owner != owner) return false;
        Owner = owner;
        return true;
    }

    public void Release(Object owner)
    {
        if (Owner != owner) return;

        Owner = null;
        hasSlot = false;
        SetBreaking(false, null);
    }

    public void SetSlot(Vector3 position, Vector3 facing)
    {
        hasSlot = true;
        SlotPosition = position;

        facing.y = 0f;
        if (facing.sqrMagnitude > 0.0001f) Facing = facing.normalized;
    }

    // Breaking units pursue the target on their own; they keep it beyond their own sensor range until recalled.
    public void SetBreaking(bool breaking, Transform target)
    {
        if (detector == null) detector = GetComponentInChildren<AIDetector>(true);

        if (breaking && target != null)
        {
            IsBreakingFormation = true;
            if (detector != null) detector.HoldTarget(target);
        }
        else if (IsBreakingFormation)
        {
            IsBreakingFormation = false;
            if (detector != null) detector.ReleaseTarget();
        }
    }
}
