using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private PlayerShipInput playerInput;
     public Vector2 moveValue;

    private ShipMovment shipMovement;

    private void Awake()
    {
       shipMovement = gameObject.GetComponent<ShipMovment>();
    }
    void MovementVector()
    {
        moveValue = playerInput.inputVector;
        Debug.Log(moveValue);
    }

    private void Update()
    {
        MovementVector();
    }

}
