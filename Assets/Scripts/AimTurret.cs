using UnityEngine;

public class AimTurret : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [SerializeField] private float rotationSpeed = 15.0f;

    private void Aim()
    {
        //Get camera look rotation
        Quaternion turretDirection = mainCamera.transform.rotation;

        var desiredAngle = Mathf.Atan2(turretDirection.y, turretDirection.x) * Mathf.Rad2Deg;

        var turretRotationSpeed = rotationSpeed * Time.deltaTime;

        transform.rotation = Quaternion.RotateTowards(turretDirection, turretDirection, turretRotationSpeed);


        Debug.Log(turretDirection);
    }

    private void OnDrawGizmos()
    {
    }
}
