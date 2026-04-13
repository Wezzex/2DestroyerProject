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


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        float targetSpeed = movementVector.y * shipMovementData.maxSpeed;

        float rate = (Mathf.Abs(targetSpeed) > 0.001f) ? shipMovementData.acceleration : shipMovementData.deacceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        rb.linearVelocity = transform.forward * currentSpeed;

        rb.MoveRotation(transform.rotation * Quaternion.Euler(0, movementVector.x * shipMovementData.rotationSpeed * Time.fixedDeltaTime, 0));
    }
}
