using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShipInput : MonoBehaviour
{
    //Acceses the Input Actions
    private ShipInputActions shipInput;

    public Vector2 inputVector;

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
        shipInput.Player.Movement.performed += OnMovementPreformed;
        shipInput.Player.Movement.canceled += OnMovementCanceled;

    }
        //Disables shipInput
    private void OnDisable()
    {
        shipInput.Disable();
        shipInput.Player.Movement.performed -= OnMovementPreformed;
        shipInput.Player.Movement.canceled -= OnMovementCanceled;
    }
    public void OnMovementCanceled(InputAction.CallbackContext context)
    {
        inputVector = Vector2.zero;
    }

    public void OnMovementPreformed(InputAction.CallbackContext context)
    {
        inputVector = shipInput.Player.Movement.ReadValue<Vector2>();
    }
}
