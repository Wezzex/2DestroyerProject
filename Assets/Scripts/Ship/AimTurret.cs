using UnityEngine;

public class AimTurret : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 150.0f;

    public void Aim(Vector2 inputValue)
    {
        if (inputValue.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float desiredAngle = Mathf.Atan2(inputValue.x, inputValue.y) * Mathf.Rad2Deg ;

        float turretRotationSpeed = rotationSpeed * Time.deltaTime;
        Quaternion turretDirection = Quaternion.Euler(0.0f, desiredAngle, 0.0f);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, turretDirection, turretRotationSpeed);

    }
}
