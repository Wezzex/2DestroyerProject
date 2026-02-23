using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerShipInput : MonoBehaviour
{
    //Acceses the Input Actions
    private ShipInputActions shipInput;

    public Vector2 inputVector;

    public UnityEvent OnShootPressed;
    public UnityEvent OnShootReleased;

    private void Awake()
    {
        //If shipInput is null, Create a new ShipInputActions
        if (shipInput == null)
        {
            shipInput = new ShipInputActions();
        }
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
        shipInput.Disable();
        shipInput.Player.Movement.performed -= OnMovementPerformed;
        shipInput.Player.Movement.canceled -= OnMovementCanceled;

        shipInput.Player.Shooting.performed -= OnShootingPerformed;
        shipInput.Player.Shooting.canceled -= OnShootingCanceled;

        shipInput.Disable();

    }


    private void OnShootingPerformed(InputAction.CallbackContext context)
    {
        OnShootPressed?.Invoke();
    }

    private void OnShootingCanceled(InputAction.CallbackContext context)
    {
        OnShootReleased?.Invoke();
    }

    public void OnMovementCanceled(InputAction.CallbackContext context)
    {
        inputVector = Vector2.zero;
    }

    public void OnMovementPerformed(InputAction.CallbackContext context)
    {
        inputVector = shipInput.Player.Movement.ReadValue<Vector2>();
    }
}
