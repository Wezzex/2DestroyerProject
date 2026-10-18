using UnityEngine;

// Put on a station. When its sensor picks up the player it calls for aid; WaveDirector starts the wave timer.
public class DistressBeacon : MonoBehaviour
{
    // Any station raising the alarm (beacon, intruder). Other stations' defenses use it to face the fight.
    public static event System.Action<DistressBeacon, Transform> AnyAlert;

    [SerializeField] private UnitManager owner;
    [SerializeField] private AIDetector sensor;

    public UnitManager Owner => owner;
    public Transform Station => owner != null ? owner.transform : transform;
    public bool IsAlive => owner == null || !owner.IsDead;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<UnitManager>();
        if (owner == null) owner = GetComponentInChildren<UnitManager>(true);
        if (sensor == null) sensor = (owner != null ? owner.gameObject : gameObject).GetComponentInChildren<AIDetector>();
    }

    private void OnEnable()
    {
        if (sensor != null) sensor.TargetAcquired += OnTargetAcquired;
        if (owner != null) owner.Died += OnOwnerDied;
    }

    private void OnDisable()
    {
        if (sensor != null) sensor.TargetAcquired -= OnTargetAcquired;
        if (owner != null) owner.Died -= OnOwnerDied;
    }

    private void OnTargetAcquired(Transform target)
    {
        if (!IsAlive) return;

        AnyAlert?.Invoke(this, target);
        if (WaveDirector.Instance != null) WaveDirector.Instance.RequestAid(this, target);
    }

    private void OnOwnerDied(UnitManager deadOwner)
    {
        if (WaveDirector.Instance != null) WaveDirector.Instance.CancelAid(this);
    }
}
