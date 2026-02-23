using UnityEngine;

public class AimTurret : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 150.0f;
    [SerializeField] private float offset = 180;

    public void Aim(Vector2 inputValue)
    {
        if (inputValue.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float desiredAngle = Mathf.Atan2(inputValue.x, inputValue.y) * Mathf.Rad2Deg ;

        float turretRotationSpeed = rotationSpeed * Time.deltaTime;
        Quaternion turretDirection = Quaternion.Euler(0.0f, desiredAngle, 0.0f);

        //Get camera look rotation
        //var turretDirection = (Vector3)inputValue - transform.position;

        //var desiredAngle = Mathf.Atan2(turretDirection.x, -turretDirection.y) * Mathf.Rad2Deg;

        //var turretRotationSpeed = -rotationSpeed * Time.deltaTime;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, turretDirection, turretRotationSpeed);

    }
}
