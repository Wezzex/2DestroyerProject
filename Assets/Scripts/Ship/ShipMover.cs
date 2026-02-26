using System;
using Unity.VisualScripting;
using UnityEngine;

public class ShipMover : MonoBehaviour
{

    public Rigidbody rb;

    private Vector2 movementVector;
    [SerializeField] private float maxSpeed = 10;
    [SerializeField] private float rotationSpeed = 100;

    [SerializeField] private float acceleration = 70;
    [SerializeField] private float deacceleration = 50;
    [SerializeField] private float currentSpeed = 0;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Move(Vector2 movementVector)
    {
        this.movementVector = movementVector;

        //CalculateSpeed(movementVector);
        //if (movementVector.y > 0)
        //{
        //    currentForwardDirection = 1;
        //}
        //else if (movementVector.y < 0)
        //{
        //    currentForwardDirection = -1;
        //}
        //else
        //{
        //    currentForwardDirection = 0;
        //}
    }

    //private void CalculateSpeed(Vector2 movementVector)
    //{
    //    if (Math.Abs(movementVector.y) > 0)
    //    {
    //        currentSpeed += acceleration * Time.deltaTime;
    //    }
    //    else 
    //    {
    //        currentSpeed -= deacceleration * Time.deltaTime;
    //    }

    //    currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
    //}

    private void FixedUpdate()
    {
        float targetSpeed = movementVector.y * maxSpeed;

        float rate = (Mathf.Abs(targetSpeed) > 0.001f) ? acceleration : deacceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        rb.linearVelocity = transform.forward * currentSpeed;

        rb.MoveRotation(transform.rotation * Quaternion.Euler(0, movementVector.x * rotationSpeed * Time.fixedDeltaTime, 0));

    }
}
