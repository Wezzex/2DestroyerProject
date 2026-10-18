using System;
using System.Collections;
using UnityEngine;

public class AIDetector : MonoBehaviour
{
    [SerializeField, Range(1, 500)] private float viewRadius = 100;
    [SerializeField] private float detectionCheckDelay = 0.1f;
    [SerializeField] private Transform target = null;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask visibilityLayer;

    [field: SerializeField]
    public bool TargetVisible { get; private set; }

    // Fired when the sensor picks up a target / loses it. Used by DistressBeacon and DeploySpawner.
    public event Action<Transform> TargetAcquired;
    public event Action<Transform> TargetLost;

    public float ViewRadius => viewRadius;

    // Set while a squad is ordered to engage: the target is kept beyond viewRadius, because the owner's sensor
    // decides when contact is lost. Without it a fighter lost the player on every wide strafing run.
    private bool bHoldTarget;

    public void HoldTarget(Transform holdTarget)
    {
        bHoldTarget = true;
        if (Target != holdTarget) Target = holdTarget;
    }

    public void ReleaseTarget()
    {
        bHoldTarget = false;
    }

    public Transform Target
    {
        get => target;

        set
        {
            Transform previous = target;
            target = value;
            TargetVisible = false;

            if (previous == null && value != null) TargetAcquired?.Invoke(value);
            else if (previous != null && value == null) TargetLost?.Invoke(previous);
        }
    }

    // OnEnable instead of Start so the sensor restarts when a docked unit is re-activated.
    private void OnEnable()
    {
        StartCoroutine(DetectionCoroutine());
    }

    private void Update()
    {
        if (Target != null)
        {
            TargetVisible = CheckTargetVisible();
        }
    }

    private bool CheckTargetVisible()
    {
        Vector3 origin = transform.position;
        Vector3 direction = (Target.position - origin);
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Min(viewRadius, distance), visibilityLayer))
        {
            Debug.DrawLine(transform.position, hit.point, Color.red);
            return (playerLayerMask & (1 << hit.collider.gameObject.layer)) != 0;
        }
        return false;
    }

    private void DetectTarget()
    {
        if (Target == null)
        {
            CheckIfPlayerInRange();
        }
        else if (Target != null)
        {
            DetectIfOutOfRange();
        }
    
    }

    private void CheckIfPlayerInRange()
    {
        Collider[] collision = Physics.OverlapSphere(transform.position, viewRadius, playerLayerMask);
        if (collision.Length > 0)
        {
            Target = collision[0].transform;
        }
    }

    private void DetectIfOutOfRange()
    {
        if (Target == null || Target.gameObject.activeSelf == false)
        {
            Target = null;
        }
        else if (!bHoldTarget && FlatDistance(transform.position, Target.position) > viewRadius)
        {
            Target = null;
        }
    }

    // The game plays on the XZ plane, so ignore height (Vector2.Distance would drop Z instead).
    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    IEnumerator DetectionCoroutine()
    {
        while (true)
        {
            DetectTarget();
            yield return new WaitForSeconds(detectionCheckDelay);
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
}
