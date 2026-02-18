using UnityEngine;
using UnityEngine.UIElements;

public class ShipController : MonoBehaviour
{
    [SerializeField] private PlayerShipInput playerInput;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Settings")]
    public Vector2 moveValue;
    private ShipMovment shipMovement;

    [Header("Turret Settings")]
    [SerializeField] private AimTurret aimTurret;
    private float turretAimValue;


    private void Awake()
    {
        if(shipMovement == null)
        {
       shipMovement = gameObject.GetComponent<ShipMovment>();
        }

        if (aimTurret == null)
        {
            aimTurret = gameObject.GetComponent<AimTurret>();
        }
    }
    void MovementVector()
    {
        moveValue = playerInput.inputVector;
        Debug.Log(moveValue);
    }

    void HandleTurretMovement(Vector2 aimValue)
    {
        aimTurret.Aim(aimValue);
    }

    private void Update()
    {
        Vector2 cameraDir = cameraTransform.forward;

        MovementVector();
        HandleTurretMovement(cameraDir);
    }

}
