using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurretCannon : MonoBehaviour
{

    [SerializeField] private List<Transform> laserSpawnPoint = new List<Transform>();
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float reloadDelay = 1;

    [SerializeField] private bool bCanShoot = true;
    private Collider[] shipColliders;
    private float currentReloadDelay;

    private void Awake()
    {
        shipColliders = GetComponentsInParent<Collider>();
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
            currentReloadDelay = reloadDelay;

            foreach (var spawnPoint in laserSpawnPoint)
            {
                GameObject laserProjectile = Instantiate(laserPrefab , spawnPoint.position, spawnPoint.rotation);
                //laserProjectile.transform.position = spawnPoint.position;
                //laserProjectile.transform.rotation = spawnPoint.rotation;
                laserProjectile.GetComponent<LaserProjectile>().Initialize(spawnPoint.up);

                foreach (var shipCollider in shipColliders)
                {
                    Physics.IgnoreCollision(laserProjectile.GetComponent<Collider>(), shipCollider);
                }
            }
        }
    }

}              
