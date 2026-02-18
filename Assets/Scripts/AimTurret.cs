using UnityEngine;

public class AimTurret : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 150.0f;

    public void Aim(Vector2 inputValue)
    {
        //Get camera look rotation
        var turretDirection = (Vector3)inputValue - transform.position;

        var desiredAngle = Mathf.Atan2(turretDirection.x, -turretDirection.y) * Mathf.Rad2Deg;

        var turretRotationSpeed = rotationSpeed * Time.deltaTime;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, desiredAngle - 180, 0), turretRotationSpeed);


        Debug.Log(turretDirection);
    }
}
