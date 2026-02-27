using UnityEngine;

[CreateAssetMenu(fileName = "NewTurretData", menuName = "Data/TurretData")]
public class TurretData : ScriptableObject
{
    public GameObject laserProjectilePrefab;
    public float reloadDelay = 0.5f;
    public LaserProjectileData laserProjectileData;
}
