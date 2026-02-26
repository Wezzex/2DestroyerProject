using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{

    private InputActionsSystem shipInput;

    public UnityEvent OnShoot = new UnityEvent();
    public UnityEvent<Vector2> OnMoveShip = new UnityEvent<Vector2>();
    public UnityEvent<Vector2> OnMoveTurret = new UnityEvent<Vector2>();

    public UnityEvent OnShootPressed;
    public UnityEvent OnShootReleased;
    public bool bIsShooting { get; private set; }

    [SerializeField] private ShipController shipController;

    [SerializeField] private Transform cameraTransform;
    public Vector2 CameraLookDirection;

    Vector2 movementVector;

    private void Awake()
    {
        if (shipInput == null)
        { 
            shipInput = new InputActionsSystem();
        }
    }

    private void Update()
    {
        GetTurretMovement();
        CameraRotationInput();
    }

    //Enables shipInput
    private void OnEnable()
    {
        shipInput.Enable();
        shipInput.Player.Movement.performed += OnMovementPerformed;
        shipInput.Player.Movement.canceled += OnMovementCanceled;

        shipInput.Player.Shooting.performed += OnShootingPerformed;
        shipInput.Player.Shooting.canceled += OnShootingCanceled;

    }

    //Disables shipInput
    private void OnDisable()
    {
        shipInput.Player.Movement.performed -= OnMovementPerformed;
        shipInput.Player.Movement.canceled -= OnMovementCanceled;

        shipInput.Player.Shooting.performed -= OnShootingPerformed;
        shipInput.Player.Shooting.canceled -= OnShootingCanceled;
        shipInput.Disable();

    }


    public void OnMovementCanceled(InputAction.CallbackContext context)
    {
        movementVector = Vector2.zero;
        OnMoveShip?.Invoke(movementVector);
    }


    public void OnMovementPerformed(InputAction.CallbackContext context)
    {
        movementVector = shipInput.Player.Movement.ReadValue<Vector2>();
        OnMoveShip?.Invoke(movementVector);
    }

    private void OnShootingPerformed(InputAction.CallbackContext context)
    {
        shipController.SetShootingState(true);
        OnShootPressed?.Invoke();
    }

    private void OnShootingCanceled(InputAction.CallbackContext context)
    {
        shipController.SetShootingState(false);
        OnShootReleased?.Invoke();
    }


    private void GetTurretMovement()
    {
        OnMoveTurret?.Invoke(CameraLookDirection);
    }


    public void CameraRotationInput()
    {
        Vector3 cameraForward = cameraTransform.forward;

        cameraForward.y = 0;

        if (cameraForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        CameraLookDirection = new Vector2(cameraForward.x, cameraForward.z);
    }
}
