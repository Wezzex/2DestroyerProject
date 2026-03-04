using UnityEngine;

[CreateAssetMenu(fileName = "NewTurretData", menuName = "Data/TurretData")]
public class TurretData : ScriptableObject
{
    public GameObject laserProjectilePrefab;
    public float reloadDelay = 0.5f;
    public float fireSequence = 0.1f;
    public float fieringVolly = 1;
    public LaserProjectileData laserProjectileData;
}
