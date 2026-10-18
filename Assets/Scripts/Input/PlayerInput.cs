using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{

    private InputActionsSystem shipInput;

    public static PlayerInput Instance { get; private set; }

    public UnityEvent OnShoot = new UnityEvent();
    public UnityEvent<Vector2> OnMoveShip = new UnityEvent<Vector2>();
    public UnityEvent<Vector2> OnMoveTurret = new UnityEvent<Vector2>();

    public UnityEvent OnShootPressed;
    public UnityEvent OnShootReleased;
    public bool bIsShooting { get; private set; }

    public event EventHandler OnPauseAction;
    private bool ControlsBlocked => GameManager.Instance != null && GameManager.Instance.IsPaused;

    [SerializeField] private ShipController shipController;
    [Tooltip("Defaults to the SubLightDrive on the ship.")]
    [SerializeField] private SubLightDrive subLightDrive;

    // Looked up by name so this works before Unity regenerates InputActionsSystem.cs from the asset.
    private InputAction subLightAction;

    [SerializeField] private Transform cameraTransform;
    public Vector2 CameraLookDirection;

    Vector2 movementVector;

    private void Awake()
    {
        Instance = this;

        if (shipInput == null)
        { 
            shipInput = new InputActionsSystem();
        }

        subLightAction = shipInput.asset.FindAction("Player/SubLight");
        if (subLightDrive == null && shipController != null) subLightDrive = shipController.GetComponentInChildren<SubLightDrive>();
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

        shipInput.Player.Pause.performed += OnPausePerformed;

        if (subLightAction != null) subLightAction.performed += OnSubLightPerformed;
    }

    // One button: start charging (align the ship first), press again to abort the charge or drop out of sub-light.
    private void OnSubLightPerformed(InputAction.CallbackContext context)
    {
        if (ControlsBlocked || subLightDrive == null) return;

        if (subLightDrive.State == SubLightState.Idle) subLightDrive.TryBeginCharge();
        else subLightDrive.Exit();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    //Disables shipInput
    private void OnDisable()
    {
        shipInput.Player.Movement.performed -= OnMovementPerformed;
        shipInput.Player.Movement.canceled -= OnMovementCanceled;

        shipInput.Player.Shooting.performed -= OnShootingPerformed;
        shipInput.Player.Shooting.canceled -= OnShootingCanceled;

        shipInput.Player.Pause.performed -= OnPausePerformed;

        if (subLightAction != null) subLightAction.performed -= OnSubLightPerformed;

        shipInput.Disable();

    }


    public void OnMovementCanceled(InputAction.CallbackContext context)
    {
        movementVector = Vector2.zero;
        OnMoveShip?.Invoke(movementVector);
    }


    public void OnMovementPerformed(InputAction.CallbackContext context)
    {
        if(ControlsBlocked) return;

        movementVector = shipInput.Player.Movement.ReadValue<Vector2>();
        OnMoveShip?.Invoke(movementVector);
    }

    private void OnShootingPerformed(InputAction.CallbackContext context)
    {
        if (ControlsBlocked) return;

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
