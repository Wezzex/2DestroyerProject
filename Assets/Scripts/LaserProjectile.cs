using System;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private int damage;
    [SerializeField] private float maxDistance;

    private Vector3 spawnPosition;
    private float distanceMoved = 0;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 direction)
    {
        spawnPosition = transform.position; 

        direction.y = 0;
        if (direction.sqrMagnitude < 0.00001f)
        {
            direction = transform.forward;
        }
        direction.Normalize();
        rb.linearVelocity = direction * speed;
    }

    private void Update()
    {
        distanceMoved = Vector3.Distance(transform.position, spawnPosition);
        if (distanceMoved >= maxDistance)
        {
            DisableObject();
        }
    }

    private void DisableObject()
    {
        rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("Collided " + collision.name);

        var damagable = collision.GetComponent<Damagable>();
        if (damagable != null)
        {
            damagable.Hit(damage);
        }
        DisableObject();
    }
}
