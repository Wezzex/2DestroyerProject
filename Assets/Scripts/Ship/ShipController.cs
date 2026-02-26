using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private ShipMover shipMover;

    [Header("Turret Settings")]
    [SerializeField] private AimTurret[] aimTurrets;
    [SerializeField] private TurretCannon[] turrets;
    private float turretAimValue;
    [SerializeField]private bool bIsShooting;

    private void Awake()
    {
        if (shipMover == null)
        {
            shipMover = GetComponent<ShipMover>();
        }

        if (aimTurrets == null || aimTurrets.Length == 0)
        {
            aimTurrets = GetComponentsInChildren<AimTurret>();
        }

        if (turrets == null || turrets.Length == 0)
        {
            turrets = GetComponentsInChildren<TurretCannon>();
        }
    }

    public void HandleMoveShip(Vector2 movementVector)
    {
        shipMover.Move(movementVector);
    }

    public void HandleTurretMovement(Vector2 aimVector)
    {
        foreach (var aimTurret in aimTurrets)
        {
            aimTurret.Aim(aimVector);
        }
    }

    public void SetShootingState(bool bShooting)
    {
        bIsShooting = bShooting;
    }

    public void HandleShoot()
    {
        if (bIsShooting)
        {
            foreach (var turret in turrets)
            {
                turret.Shoot();
            }
        }
    }

    private void Update()
    {
        HandleShoot();
    }
}
