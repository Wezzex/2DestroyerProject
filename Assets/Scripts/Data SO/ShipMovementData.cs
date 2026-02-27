using UnityEngine;


[CreateAssetMenu(fileName = "NewShipMovementData", menuName = "Data/ShipMovementData")]
public class ShipMovementData : ScriptableObject
{
    public float maxSpeed = 10;
    public float rotationSpeed = 10;
    public float acceleration = 1;
    public float deacceleration = 1;
}
