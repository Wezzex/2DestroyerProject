using System;
using UnityEngine;
using UnityEngine.Events;

public class LaserProjectile : MonoBehaviour, IWeapon
{
    [SerializeField] private LaserProjectileData laserProjectileData;

    private Vector3 spawnPosition;
    private float distanceMoved = 0;
    private Rigidbody rb;

    [SerializeField] private GameObject impactPrefab;

    public UnityEvent OnSpawn = new UnityEvent();
    public UnityEvent OnHit   = new UnityEvent();

    public IDamagable Idamageble;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 direction, LaserProjectileData laserProjectileData)
    {
        this.laserProjectileData = laserProjectileData;

        spawnPosition = transform.position; 
        OnSpawn?.Invoke();

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

    void CreateExplotion()
    {
        Vector3 explotionSpawnPosition = transform.position;
        GameObject explotion = Instantiate(impactPrefab, explotionSpawnPosition, Quaternion.identity);

    }

    private void DisableObject()
    {
        rb.linearVelocity = Vector3.zero;


        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider collision)
    {
        // IDamagable iDamagable = collision.TryGetComponent<IDamagable>();
        if (collision.TryGetComponent(out IDamagable iDamagable))
        {
            iDamagable.TakeDamage(laserProjectileData.damage);
            CreateExplotion();
            DisableObject();
        }
    }

    public void Fire(IDamagable target)
    {
        target.TakeDamage(laserProjectileData.damage);
    }
}
