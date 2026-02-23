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
    [SerializeField] private AimTurret[] aimTurrets;
    [SerializeField] private TurretCannon[] turrets;
    private float turretAimValue;
    private bool bIsShooting;



    private void Awake()
    {
        if(shipMovement == null)
        {
       shipMovement = gameObject.GetComponent<ShipMovment>();
        }

        if (aimTurrets == null || aimTurrets.Length == 0)
        {
            aimTurrets = GetComponentsInChildren<AimTurret>();
        }
        if(turrets == null || turrets.Length == 0)
        {
            turrets = GetComponentsInChildren<TurretCannon>();
        }
    }
    void MovementVector()
    {
        moveValue = playerInput.inputVector;
    }

    void HandleTurretMovement(Vector2 aimValue)
    {
        foreach(var aimTurret in aimTurrets)

        aimTurret.Aim(aimValue);
    }


    public void HandleShoot()
    {
        foreach(var turret in turrets) 
        { 
            turret.Shoot(); 

        }
    }


    private void OnEnable()
    {
        playerInput.OnShootPressed.AddListener(() => bIsShooting = true);
        playerInput.OnShootReleased.AddListener(() => bIsShooting = false);
    }

    private void OnDisable()
    {
       playerInput.OnShootPressed.RemoveAllListeners();
       playerInput.OnShootReleased.RemoveAllListeners();
    }

    private void Update()
    {
        Vector3 cameraForward = cameraTransform.forward;

        cameraForward.y = 0;

        if(cameraForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 aimDirection = new Vector3(cameraForward.x, cameraForward.z);

        MovementVector();
        HandleTurretMovement(aimDirection);

        if(bIsShooting)
        {
            HandleShoot();
        }
    }

}
