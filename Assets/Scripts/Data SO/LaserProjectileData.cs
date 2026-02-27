using UnityEngine;

[CreateAssetMenu(fileName = "NewLaserProjectileData", menuName = "Data/LaserProjectileData")]
public class LaserProjectileData : ScriptableObject
{

    public float speed = 5;
    public int damage = 5;
    public float maxDistance = 100;
}
