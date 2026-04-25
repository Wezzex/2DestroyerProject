using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(ObjectPool))]
public class TurretCannon : MonoBehaviour
{

    [SerializeField] private List<Transform> laserSpawnPoint = new List<Transform>();
    [SerializeField] private TurretData turretData;
    [SerializeField] private ShipController shipController;

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

        shipController = GetComponentInParent<ShipController>();
    }

    private void Start()
    {
        projectilePool.Initialize(turretData.laserProjectilePrefab, projectilePoolCount);
    }

    private void Update()
    {
    }

    private IEnumerator FiringSequence()
    {
        if (CanShoot() == true)
        {
            StartCoroutine(Firing());
        }
        StopCoroutine(Firing());
        yield return null;
    }

    private IEnumerator Firing()
    {
        bCanShoot = false;

        for (int f = 0; f < turretData.fieringVolly; f++)
        {
            foreach (var laserSpawn in laserSpawnPoint)
            {
                Transform spawnPoints = laserSpawn;

                GameObject laserProjectile = projectilePool.CreateObject();
                laserProjectile.transform.position = spawnPoints.position;
                laserProjectile.transform.rotation = spawnPoints.rotation;
                laserProjectile.GetComponent<LaserProjectile>().Initialize(spawnPoints.up, turretData.laserProjectileData);

                foreach (var shipCollider in shipColliders)
                {
                    Physics.IgnoreCollision(laserProjectile.GetComponent<Collider>(), shipCollider);
                }

                yield return new WaitForSeconds(turretData.fireSequence);

            }
        }

        yield return new WaitForSeconds(currentReloadDelay);
        bCanShoot = true;
    }


    private bool CanShoot()
    {
        if (bCanShoot)
        {
            return true;
        }


        return bCanShoot;
    }

    public void Shoot()
    {
        if (!CanShoot()) return;
        

            if (isActiveAndEnabled)
            {
                StartCoroutine(FiringSequence());
            }
            else
            {
                StopCoroutine(FiringSequence());
            }

    }
    
}              
