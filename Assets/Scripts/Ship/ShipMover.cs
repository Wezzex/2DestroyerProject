using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class ShipMover : MonoBehaviour
{

    public Rigidbody rb;

    public ShipMovementData shipMovementData;

    private Vector2 movementVector;
    [SerializeField] private float currentSpeed = 0;


    private SubLightDrive subLightDrive;

    public float CurrentSpeed => currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        subLightDrive = GetComponent<SubLightDrive>();
    }

    public void Move(Vector2 movementVector)
    {

        if (float.IsNaN(movementVector.x) || float.IsNaN(movementVector.y) ||
        float.IsInfinity(movementVector.x) || float.IsInfinity(movementVector.y))
        {
            movementVector = Vector2.zero;
        }

        this.movementVector = movementVector;
    }

    private void FixedUpdate()
    {
        // The drive owns the speed while charging, cruising or dropping out, and the ship can't turn.
        // Read the rigidbody's pose, not the transform's: the transform is only synced once per frame, so with
        // several physics steps in one frame it is stale.
        Vector3 forward = rb.rotation * Vector3.forward;

        if (subLightDrive != null && subLightDrive.IsActive)
        {
            currentSpeed = subLightDrive.UpdateSpeed(currentSpeed, shipMovementData.maxSpeed, shipMovementData.deacceleration, Time.fixedDeltaTime);
            rb.linearVelocity = forward * currentSpeed;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        float targetSpeed = movementVector.y * shipMovementData.maxSpeed;

        float rate = (Mathf.Abs(targetSpeed) > 0.001f) ? shipMovementData.acceleration : shipMovementData.deacceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        rb.linearVelocity = forward * currentSpeed;

        // Turn with angular velocity, which physics applies every step. This used to be
        // MoveRotation(transform.rotation * step): with the stale transform, every physics step in a frame
        // restarted from the same rotation, so all but one step per frame was lost and ships turned at a
        // fraction of rotationSpeed (measured ~10-25% at low frame rates). Also keeps ships level.
        rb.angularVelocity = Vector3.up * (movementVector.x * shipMovementData.rotationSpeed * Mathf.Deg2Rad);
    }
}
