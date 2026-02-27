using System;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private LaserProjectileData laserProjectileData;

    private Vector3 spawnPosition;
    private float distanceMoved = 0;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 direction, LaserProjectileData laserProjectileData)
    {
        this.laserProjectileData = laserProjectileData;

        spawnPosition = transform.position; 

        direction.y = 0;
        if (direction.sqrMagnitude < 0.00001f)
        {
            direction = transform.forward;
        }
        direction.Normalize();
        rb.linearVelocity = direction * this.laserProjectileData.speed;
    }

    private void Update()
    {
        distanceMoved = Vector3.Distance(transform.position, spawnPosition);
        if (distanceMoved >= laserProjectileData.maxDistance)
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
            damagable.Hit(laserProjectileData.damage);
        }
        DisableObject();
    }
}
