using UnityEngine;

public class ShipMovment : MonoBehaviour
{
    [SerializeField] private ShipController shipController;

    [Header("Thrusting Parameters")]
    [SerializeField] private float velocity = 0.0f;

    private float maxVelocity = 2.0f;
    private float minVelocity = -2.0f;

    private float acceleration = 0.0f;
    private float drag = 1.0f;

    [SerializeField] private bool bIsThrusting = false;

    [Header("Yaw Parameters")]

    [SerializeField] private float yawVelocity = 0.0f;

    private float curretnYaw;
    private float yawAcceleration = 0.0f;
    private float yawAccelerationModifier = 0.25f;


    [SerializeField] private bool bIsRotating = false;


    private Vector3 movementVector;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    void Thrusting()
    {


       
        if (bIsThrusting)
        {
                acceleration = shipController.moveValue.y;

                velocity += acceleration * Time.deltaTime;
        }


        else
        {
            if (velocity > 0)
            {
                velocity -= drag * Time.deltaTime;
            }
            else
            {
                velocity += drag * Time.deltaTime;
            }

            if (Mathf.Abs(velocity) < 0.1)
            {
                velocity = 0;
            }

        }

        //Ships Max and Min Velocity
        if (velocity >= maxVelocity)
        {
            velocity = maxVelocity;
        }

        if (velocity <= minVelocity)
        {
            velocity = minVelocity;
        }

    }
    private void Update()
    {
        if (shipController.moveValue.y < 0 || shipController.moveValue.y > 0)
        {
            bIsThrusting = true;
        }
        else
        {
            bIsThrusting = false;
        }

        if (shipController.moveValue.x < 0 || shipController.moveValue.x > 0)
        {
            bIsRotating = true;
        }
        else
        {
            bIsRotating = false;
        }
    }


    void SetYawRotation()
    {
        yawAcceleration = shipController.moveValue.x * yawAccelerationModifier;
        yawVelocity = bIsRotating ? yawAcceleration : 0.0f;
    }



    private void FixedUpdate()
    {
        //Updates the ships Thrusting

        Thrusting();


        SetYawRotation();
        
        rb.AddForce(transform.forward * velocity, ForceMode.Force);
        transform.Rotate(new Vector3(0, yawVelocity, 0));


    }

}
