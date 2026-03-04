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

    private IEnumerator FiringSequence()
    {
        if (bCanShoot)
        {
            bCanShoot = false;
            currentReloadDelay = turretData.reloadDelay;
            
                StartCoroutine(Firing());

        }
        StopCoroutine(Firing());
        yield return null;
    }

    private IEnumerator Firing()
    {

        for (int f = 0; f < turretData.fieringVolly; f++)
        {
            for (int i = 0; i < laserSpawnPoint.Count; i++)
            {
                Transform spawnPoints = laserSpawnPoint[i];

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
        yield return new WaitForSeconds(turretData.reloadDelay);
        yield return null;
    }

    public void Shoot()
    {
        if (isActiveAndEnabled || shipController.bIsShooting)
        {
            StartCoroutine(FiringSequence());
        }
        else
        {
            StopCoroutine(FiringSequence());
        }
    }


}              
