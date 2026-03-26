using System;
using System.Collections;
using UnityEngine;

public class AIDetector : MonoBehaviour
{
    [SerializeField, Range(1, 100)] private float viewRadius = 100;
    [SerializeField] private float detectionCheckDelay = 0.1f;
    [SerializeField] private Transform target = null;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask visibilityLayer;

    [field: SerializeField]
    public bool TargetVisible { get; private set; }

    public Transform Target
    {
        get => target;

        set 
        { 
            target = value; 
            TargetVisible = false;
        }
    }

    private void Start()
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
        if (Target == null || Target.gameObject.activeSelf == false || Vector2.Distance(transform.position, Target.position) > viewRadius)
        {
            Target = null;
        }
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
