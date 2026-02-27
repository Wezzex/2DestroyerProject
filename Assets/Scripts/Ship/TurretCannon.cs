using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(ObjectPool))]
public class TurretCannon : MonoBehaviour
{

    [SerializeField] private List<Transform> laserSpawnPoint = new List<Transform>();
    [SerializeField] private TurretData turretData;

    [SerializeField] private bool bCanShoot = true;
    private Collider[] shipColliders;
    private float currentReloadDelay;

    private ObjectPool projectilePool;
    [SerializeField] private int projectilePoolCount = 10;

    private void Awake()
    {
        shipColliders = GetComponentsInChildren<Collider>();
        shipColliders = GetComponentsInParent<Collider>();

        projectilePool = GetComponent<ObjectPool>();
    }

    private void Start()
    {
        projectilePool.Initialize(turretData.laserProjectilePrefab, projectilePoolCount);
    }

    private void Update()
    {
        if (bCanShoot == false)
        {
            currentReloadDelay -= Time.deltaTime;
            if (currentReloadDelay <= 0)
            {
                bCanShoot = true;
            }
        }
    }

   

    public void Shoot()
    {
        if (bCanShoot)
        {
            bCanShoot = false;
            currentReloadDelay = turretData.reloadDelay;

            foreach (var spawnPoint in laserSpawnPoint)
            {
                //GameObject laserProjectile = Instantiate(laserProjectilePrefab , spawnPoint.position, spawnPoint.rotation);
                GameObject laserProjectile = projectilePool.CreateObject();
                laserProjectile.transform.position = spawnPoint.position;
                laserProjectile.transform.rotation = spawnPoint.rotation;
                laserProjectile.GetComponent<LaserProjectile>().Initialize(spawnPoint.up, turretData.laserProjectileData);

                foreach (var shipCollider in shipColliders)
                {
                    Physics.IgnoreCollision(laserProjectile.GetComponent<Collider>(), shipCollider);
                }
            }
        }
    }

}              
